/*
The MIT License (MIT)

Copyright (c) 2018 Giovanni Paolo Vigano' && 2025 SOSXR

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SOSXR.Talkies;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


/// <summary>
/// Adaptation for Unity of the M2MQTT library (https://github.com/eclipse/paho.mqtt.m2mqtt),
/// modified to run on UWP (also tested on Microsoft HoloLens).
/// </summary>
namespace MQTTUnity
{
    /// <summary>
    ///     Generic MonoBehavior wrapping a MQTT client, using a double buffer to postpone message processing in the main thread.
    /// </summary>
    public class MQTTClient : MonoBehaviour
    {
        [UnityEngine.Header("MQTT broker configuration")] [Tooltip("IP address or URL of the host running the broker")]
        [SerializeField] private string m_brokerAddress = "localhost";

        [Tooltip("Port where the broker accepts connections")]
        [SerializeField] private int m_brokerPort = 1883;

        [Tooltip("Use encrypted connection")] [SerializeField]
        private bool m_isEncrypted;

        [UnityEngine.Header("Connection parameters")]
        [Tooltip("Connection to the broker is delayed by the the given milliseconds")]
        [SerializeField] [Suffix("ms")] private int m_connectionDelay = 500;

        [Tooltip("Connection timeout in milliseconds")]
        [SerializeField] [Suffix("ms")] private int m_timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;

        [Tooltip("Connect on startup")]
        [SerializeField] private bool m_autoConnect;

        [Tooltip("UserName for the MQTT broker. Keep blank if no user name is required.")]
        [SerializeField] private string m_MQTTUserName = string.Empty;

        [Tooltip("Password for the MQTT broker. Keep blank if no password is required.")]
        [SerializeField] private string m_MQTTPassword = string.Empty;

        private static MQTTBackend _MQTTBackend;

        private readonly List<MqttMsgPublishEventArgs> _messageQueue1 = new();
        private readonly List<MqttMsgPublishEventArgs> _messageQueue2 = new();
        private List<MqttMsgPublishEventArgs> _frontMessageQueue;
        private List<MqttMsgPublishEventArgs> _backMessageQueue;
        private bool _connectionClosed;
        private bool _connected;

        private static readonly Dictionary<string, List<Action<string, byte[]>>> _topicCallbacks = new();

        private static List<(string topic, byte qosLevel)> _pendingSubscriptions; // For anyone subscribing before we're ready and connected
        private static List<string> _pendingUnsubscriptions;
        private static List<(string topic, byte[] message, byte qosLevel, bool retain)> _pendingPublications;


        private Coroutine _connect;
        private Coroutine _disconnect;

        /// <summary>Gets or sets the broker hostname or IP address.</summary>
        public string BrokerAddress
        {
            get => m_brokerAddress;
            set => m_brokerAddress = value;
        }

        /// <summary>Gets or sets the broker port as a string. Invalid values are silently ignored.</summary>
        public string BrokerPortString
        {
            get => m_brokerPort.ToString();
            set => m_brokerPort = int.TryParse(value, out var port) ? port : m_brokerPort;
        }

        /// <summary>Gets or sets the broker port number.</summary>
        public int BrokerPort
        {
            get => m_brokerPort;
            set => m_brokerPort = value;
        }

        /// <summary>Gets or sets whether to use an SSL/TLS-encrypted connection to the broker.</summary>
        public bool IsEncrypted
        {
            get => m_isEncrypted;
            set => m_isEncrypted = value;
        }


        /// <summary>Gets a value indicating whether the MQTT backend is currently connected to the broker.</summary>
        public static bool IsConnected => _MQTTBackend is {IsConnected: true};
        /// <summary>Invoked on the main thread after a successful broker connection.</summary>
        public static Action OnConnected;
        /// <summary>Invoked on the main thread when the broker connection is lost or explicitly closed.</summary>
        public static Action OnDisconnected;


        /// <summary>Singleton instance. Only one <see cref="MQTTClient"/> may exist; extras are destroyed on Awake.</summary>
        public static MQTTClient Instance;


        /// <summary>
        ///     Initialize MQTT message queue
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);

                return;
            }

            Instance = this;

            _frontMessageQueue = _messageQueue1;
            _backMessageQueue = _messageQueue2;

            if (m_autoConnect)
            {
                Connect();
            }
        }


        /// <summary>
        ///     Initiates an asynchronous connection to the configured MQTT broker.
        ///     Does nothing if already connected. Any in-flight connect coroutine is
        ///     restarted. Pending subscriptions, unsubscriptions, and publications are
        ///     flushed automatically once connected.
        /// </summary>
        [ContextMenu(nameof(Connect))]
        public void Connect()
        {
            if (_MQTTBackend is {IsConnected: true})
            {
                return;
            }

            if (_connect != null)
            {
                StopCoroutine(_connect);
            }

            _connect = StartCoroutine(ConnectCR());
        }


        private IEnumerator ConnectCR()
        {
            yield return new WaitForSecondsRealtime(m_connectionDelay / 1000f);
            yield return new WaitForEndOfFrame();

            if (_MQTTBackend == null)
            {
                try
                {
                    _MQTTBackend = new MQTTBackend(m_brokerAddress, m_brokerPort, m_isEncrypted, null, null,
                        m_isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                }
                catch (Exception e)
                {
                    _MQTTBackend = null;
                    Debug.LogError($"Initialization failed: {e.Message}");

                    yield break;
                }
            }

            if (_MQTTBackend.IsConnected)
            {
                yield break;
            }

            var clientId = Guid.NewGuid().ToString();
            var connectResult = false;
            string errorMsg = null;

            // Run connect in a background thread
            var connectThread = new Thread(() =>
            {
                try
                {
                    _MQTTBackend.Settings.TimeoutOnConnection = m_timeoutOnConnection;
                    _MQTTBackend.Connect(clientId, m_MQTTUserName, m_MQTTPassword);
                    connectResult = _MQTTBackend.IsConnected;
                }
                catch (Exception e)
                {
                    connectResult = false;
                    errorMsg = e.Message;
                }
            });

            connectThread.Start();

            // Wait for thread to finish but yield each frame
            while (connectThread.IsAlive)
            {
                yield return null;
            }

            if (connectResult)
            {
                BackendIsConnected();
            }
            else
            {
                _MQTTBackend = null;
                Debug.LogError($"Failed to connect to {m_brokerAddress}:{m_brokerPort}\n{errorMsg}");
                OnDisconnected?.Invoke();
            }

            _connect = null;
        }


        private void BackendIsConnected()
        {
            _MQTTBackend.ConnectionClosed += OnMqttConnectionClosed;
            _MQTTBackend.MqttMsgPublishReceived += OnMqttMessageReceived;

            Debug.Log($"Connected to {m_brokerAddress}:{m_brokerPort}...");

            FlushPendingUnsubscriptions();

            FlushPendingSubscriptions();

            FlushPendingPublications();

            _connected = true;

            OnConnected?.Invoke();
        }


        private void FlushPendingUnsubscriptions()
        {
            if (_pendingUnsubscriptions == null || _pendingUnsubscriptions.Count == 0)
            {
                return;
            }

            foreach (var topic in _pendingUnsubscriptions)
            {
                // Debug.Log("Unsubscribing from pending topic: " + topic);
                _MQTTBackend.Unsubscribe(new[] {topic});
            }

            Debug.Log($"Flushed {_pendingUnsubscriptions.Count} pending unsubscriptions.");
            _pendingUnsubscriptions.Clear();
        }


        private void FlushPendingSubscriptions()
        {
            if (_pendingSubscriptions == null || _pendingSubscriptions.Count == 0)
            {
                return;
            }

            foreach (var (topic, qosLevel) in _pendingSubscriptions)
            {
                // Debug.Log("Subscribing to pending topic: " + topic + " with QoS level: " + qosLevel);
                _MQTTBackend.Subscribe(new[] {topic}, new[] {qosLevel});
            }

            Debug.Log($"Flushed {_pendingSubscriptions.Count} pending subscriptions.");
            _pendingSubscriptions.Clear();
        }


        private void FlushPendingPublications()
        {
            if (_pendingPublications == null || _pendingPublications.Count == 0)
            {
                return;
            }

            foreach (var (topic, message, qosLevel, retain) in _pendingPublications)
            {
                _MQTTBackend.Publish(topic, message, qosLevel, retain);
            }

            // Debug.Log($"Flushed {_pendingPublications.Count} pending publications.");
            _pendingPublications.Clear();
        }


        /// <summary>
        ///     Defaults to subscribing to all topics ("#") with QoS level 2 (exactly once).
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="topic"></param>
        /// <param name="qosLevel"></param>
        public static void Subscribe(Action<string, byte[]> callback, string topic = "#", byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
        {
            if (!_topicCallbacks.TryGetValue(topic, out var callbacks))
            {
                callbacks = new List<Action<string, byte[]>>();
                _topicCallbacks[topic] = callbacks;

                if (_MQTTBackend is {IsConnected: true})
                {
                    _MQTTBackend.Subscribe(new[] {topic}, new[] {qosLevel});
                    // Debug.Log($"Subscribed to topic: {topic} with QoS level: {qosLevel}");
                }
                else
                {
                    _pendingSubscriptions ??= new List<(string topic, byte qosLevel)>();

                    _pendingSubscriptions.Add((topic, qosLevel)); // Store pending subscriptions to be processed later
                    // Debug.Log($"Pending subscription to topic: {topic} with QoS level: {qosLevel}");
                }
            }
            else
            {
                // Debug.Log($"Already subscribed to topic: {topic} with QoS level: {qosLevel}");
            }


            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }


        /// <summary>
        ///     Removes a previously registered callback from the given topic.
        ///     When the last callback for a topic is removed, the broker subscription is also cancelled.
        ///     Safe to call while not connected — the unsubscription is queued and flushed on reconnect.
        /// </summary>
        /// <param name="callback">The callback to remove. Must match the instance passed to <see cref="Subscribe"/>.</param>
        /// <param name="topic">The MQTT topic to unsubscribe from. Defaults to <c>#</c> (all topics).</param>
        public static void Unsubscribe(Action<string, byte[]> callback, string topic = "#")
        {
            if (!_topicCallbacks.TryGetValue(topic, out var callbacks))
            {
                return;
            }

            callbacks.Remove(callback);

            if (callbacks.Count != 0)
            {
                return;
            }

            _topicCallbacks.Remove(topic);

            if (_MQTTBackend is {IsConnected: true})
            {
                _MQTTBackend.Unsubscribe(new[] {topic});
                // Debug.Log($"Unsubscribed from topic: {topic}");
            }
            else
            {
                _pendingUnsubscriptions ??= new List<string>();

                _pendingUnsubscriptions.Add(topic); // Store pending unsubscriptions to be processed later
                // Debug.Log($"Pending unsubscription from topic: {topic}");
            }
        }


        /// <summary>
        ///     Publishes a UTF-8 string payload to the given topic.
        ///     If not currently connected, the message is queued and sent on reconnect.
        /// </summary>
        /// <param name="topic">Target MQTT topic.</param>
        /// <param name="payload">String payload to publish.</param>
        /// <param name="qosLevel">QoS level (default: exactly-once).</param>
        /// <param name="retain">Whether the broker should retain the message for new subscribers.</param>
        public static void Publish(string topic, string payload, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, bool retain = false)
        {
            Publish(topic, Encoding.UTF8.GetBytes(payload), qosLevel, retain);
        }


        /// <summary>
        ///     Publishes a raw byte payload to the given topic.
        ///     If not currently connected, the message is queued and sent on reconnect.
        /// </summary>
        /// <param name="topic">Target MQTT topic.</param>
        /// <param name="payload">Raw byte payload to publish.</param>
        /// <param name="qosLevel">QoS level (default: exactly-once).</param>
        /// <param name="retain">Whether the broker should retain the message for new subscribers.</param>
        public static void Publish(string topic, byte[] payload, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, bool retain = false)
        {
            var mssg = Encoding.UTF8.GetString(payload);

            if (_MQTTBackend is {IsConnected: true})
            {
                _MQTTBackend.Publish(topic, payload, qosLevel, retain);
                // Debug.Log($"Published message to topic: {topic} with payload: {mssg} and QoS level: {qosLevel} (retain: {retain})");
            }
            else
            {
                _pendingPublications ??= new List<(string topic, byte[] message, byte qosLevel, bool retain)>();

                _pendingPublications.Add((topic, payload, qosLevel, retain)); // Store pending publications to be processed later
                // Debug.Log($"Pending publication to topic: {topic} with payload: {mssg} and QoS level: {qosLevel} (retain: {retain})");
            }
        }


        private void DecodeMessage(string topic, byte[] payload)
        {
            // Debug.Log($"Message received on topic: {topic} - {Encoding.UTF8.GetString(payload)}");

            foreach (var (filter, callbacks) in _topicCallbacks)
            {
                if (!TopicMatches(filter, topic))
                {
                    continue;
                }

                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(topic, payload);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in MQTT callback for topic {filter} (received: {topic}): {e}");
                    }
                }
            }
        }


        private static bool TopicMatches(string filter, string topic)
        {
            var filterLevels = filter.Split('/');
            var topicLevels = topic.Split('/');

            for (var i = 0; i < filterLevels.Length; i++)
            {
                if (i >= topicLevels.Length)
                {
                    return false;
                }

                if (filterLevels[i] == "#")
                {
                    return true;
                }

                if (filterLevels[i] == "+")
                {
                    continue;
                }

                if (filterLevels[i] != topicLevels[i])
                {
                    return false;
                }
            }

            return filterLevels.Length == topicLevels.Length;
        }


        /// <summary>
        ///     Processing of income messages and events is postponed here in the main thread.
        /// </summary>
        private void Update()
        {
            ProcessMqttEvents();
        }


        /// <summary>
        ///     Swap twice to process all messages even if new ones came in during the first swap
        /// </summary>
        private void ProcessMqttEvents()
        {
            // process messages in the main queue
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
            // process messages income in the meanwhile
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();

            if (_connectionClosed)
            {
                _connectionClosed = true;
                Debug.Log("Connection to the broker closed.");
                OnDisconnected?.Invoke();
            }
        }


        private void ProcessMqttMessageBackgroundQueue()
        {
            foreach (var msg in _backMessageQueue)
            {
                DecodeMessage(msg.Topic, msg.Message);
            }

            _backMessageQueue.Clear();
        }


        /// <summary>
        ///     Swap the message queues to continue receiving message when processing a queue.
        /// </summary>
        private void SwapMqttMessageQueues()
        {
            _frontMessageQueue = _frontMessageQueue == _messageQueue1 ? _messageQueue2 : _messageQueue1;
            _backMessageQueue = _backMessageQueue == _messageQueue1 ? _messageQueue2 : _messageQueue1;
        }


        private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
        {
            _frontMessageQueue.Add(msg);
        }


        private void OnMqttConnectionClosed(object sender, EventArgs e)
        {
            // Set unexpected connection closed only if connected (avoid event handling in case of controlled disconnection)
            _connectionClosed = _connected;
            _connected = false;
        }


        /// <summary>
        ///     Begins an asynchronous disconnection from the broker.
        ///     Clears all subscriptions, pending queues, and fires <see cref="OnDisconnected"/>.
        /// </summary>
        [ContextMenu(nameof(Disconnect))]
        public void Disconnect()
        {
            if (_disconnect != null)
            {
                StopCoroutine(_disconnect);
            }

            _disconnect = StartCoroutine(DisconnectCR());
        }


        private IEnumerator DisconnectCR()
        {
            yield return new WaitForEndOfFrame();
            DisconnectImmediately();

            _disconnect = null;
        }


        private void DisconnectImmediately()
        {
            _connected = false;


            if (_MQTTBackend != null)
            {
                _MQTTBackend.Disconnect();
                _MQTTBackend.MqttMsgPublishReceived -= OnMqttMessageReceived;
                _MQTTBackend.ConnectionClosed -= OnMqttConnectionClosed;
                _MQTTBackend = null;
            }

            _frontMessageQueue.Clear();
            _backMessageQueue.Clear();
            _pendingSubscriptions?.Clear();
            _pendingUnsubscriptions?.Clear();
            _pendingPublications?.Clear();

            foreach (var callbacks in _topicCallbacks.Values)
            {
                callbacks.Clear();
            }

            _topicCallbacks.Clear();

            FlushPendingUnsubscriptions();
            FlushPendingSubscriptions();
            FlushPendingPublications();

            OnDisconnected?.Invoke();

            Debug.Log("Disconnected from broker: " + m_brokerAddress + ":" + m_brokerPort);
        }


        /// <summary>
        ///     Disconnect before the application quits.
        /// </summary>
        private void OnApplicationQuit()
        {
            StopAllCoroutines();
            DisconnectImmediately();
        }


        #if ((!UNITY_EDITOR && UNITY_WSA_10_0 || !UNITY_EDITOR && UNITY_ANDROID))
        private void OnApplicationFocus(bool focus)
        {
            // On UWP 10 (HoloLeng) / Android we cannot tell whether the application actually got closed or just minimized.
            // (https://forum.unity.com/threads/onapplicationquit-and-ondestroy-are-not-called-on-uwp-10.462597/)
            if (focus)
            {
                Connect();
            }
            else
            {
                DisconnectImmediately();
            }
        }
        #endif
    }
}
