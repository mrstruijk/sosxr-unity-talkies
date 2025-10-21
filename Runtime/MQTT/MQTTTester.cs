using SOSXR.SeaShark;
using UnityEngine;

namespace MQTTUnity
{
    public class MQTTTester : MonoBehaviour
    {
        [SerializeField] private string m_debugTopic = "SOSXR/Test";
        [SerializeField] private string m_debugPayload = "Hello world!";


        private void OnEnable()
        {
            SubscribeToDebug();
        }


        [Button]
        private void SubscribeToDebug()
        {
            MQTTClient.Subscribe(null, m_debugTopic); // Subscribe to the test topic with a null callback to ensure the topic is registered
        }


        [Button]
        public void PublishDebug()
        {
            MQTTClient.Publish(m_debugTopic, m_debugPayload);
        }


        private void OnDisable()
        {
            UnsubscribeFromDebug();
        }


        [Button]
        private void UnsubscribeFromDebug()
        {
            MQTTClient.Unsubscribe(null, m_debugTopic);
        }
    }
}