using System.Text;
using MQTTUnity;
using SOSXR.EnhancedLogger;
using UnityEngine;
using UnityEngine.Events;


public class MQTTFirstResponder : MonoBehaviour
{
    [Tooltip("Leave this blank (or #) for responding to all topics")]
    [SerializeField] private string m_topic;
    [Tooltip("Leave this blank for responding to all payloads")]
    [SerializeField] private string m_payload;
    [SerializeField] private UnityEvent<string> m_onMessageReceived;


    private void OnEnable()
    {
        MQTTClient.Subscribe(MessageReceived, m_topic);
    }


    private void MessageReceived(string topic, byte[] payload)
    {
        if (topic != m_topic && m_topic != "#" && !string.IsNullOrEmpty(m_topic))
        {
            this.Verbose($"We got something, but not the correct topic (incoming is {topic}).");

            return;
        }

        var payloadString = Encoding.UTF8.GetString(payload);

        if (string.IsNullOrEmpty(payloadString))
        {
            this.Error("Something has gone terribly wrong, we got a message, but the payload string is empty. Cannot continue.");

            return;
        }

        if (payloadString != m_payload && m_payload != "#" && !string.IsNullOrEmpty(m_payload))
        {
            this.Verbose($"We got something, but not the correct payload (Payload received is {payloadString}).");

            return;
        }

        this.Info($"Payload received: {payloadString} on topic {topic}");
        m_onMessageReceived?.Invoke(payloadString);
    }


    private void OnDisable()
    {
        MQTTClient.Unsubscribe(MessageReceived, m_topic);
    }
}