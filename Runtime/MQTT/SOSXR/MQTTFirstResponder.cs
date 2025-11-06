using System.Text;
using MQTTUnity;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;
using UnityEngine.Events;


public class MQTTFirstResponder : MonoBehaviour
{
    [SerializeField] [HideInInspector] private bool m_toggleTopic;
    [SerializeField] [HideInInspector] private bool m_togglePayload;

    [Tooltip("Leave this blank (or #) for responding to all topics")]
    [EnableIf(nameof(m_toggleTopic))]
    [SerializeField] private string m_onlyRespondToThisTopic;

    [Tooltip("Leave this blank for responding to all payloads")]
    [EnableIf(nameof(m_togglePayload))]
    [SerializeField] private string m_onlyRespondToThisPayload;

    [SerializeField] private UnityEvent<string, string> m_onMessageReceived;


    private void OnValidate()
    {
        if (!m_toggleTopic)
        {
            m_onlyRespondToThisTopic = string.Empty;
        }

        if (!m_togglePayload)
        {
            m_onlyRespondToThisPayload = string.Empty;
        }
    }


    private void OnEnable()
    {
        MQTTClient.Subscribe(MessageReceived, m_onlyRespondToThisTopic);
    }


    private void MessageReceived(string topic, byte[] payload)
    {
        if (topic != m_onlyRespondToThisTopic && m_onlyRespondToThisTopic != "#" && !string.IsNullOrEmpty(m_onlyRespondToThisTopic))
        {
            this.Verbose($"We got something, but not the correct topic (incoming is {topic}).");

            return;
        }

        var payloadString = Encoding.UTF8.GetString(payload);

        if (payloadString != m_onlyRespondToThisPayload && m_onlyRespondToThisPayload != "#" && !string.IsNullOrEmpty(m_onlyRespondToThisPayload))
        {
            this.Verbose($"We got something, but not the correct payload (Payload received is {payloadString}).");

            return;
        }

        this.Info($"Payload received: {payloadString} on topic {topic}");

        m_onMessageReceived?.Invoke(topic, payloadString);
    }


    private void OnDisable()
    {
        MQTTClient.Unsubscribe(MessageReceived, m_onlyRespondToThisTopic);
    }
}