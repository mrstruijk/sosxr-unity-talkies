using System;
using System.Xml.Serialization;
using UnityEngine;


namespace MQTTUnity
{
    /// <summary>
    ///     Serializable settings for MQTT broker configuration.
    /// </summary>
    [Serializable]
    [XmlType(TypeName = "broker-settings")]
    public class BrokerSettings
    {
        /// <summary>Hostname or IP address of the MQTT broker.</summary>
        [Tooltip("Address of the host running the broker")]
        public string host = "localhost";

        /// <summary>Port number the broker listens on (default: 1883).</summary>
        [Tooltip("Port used to access the broker")]
        public int port = 1883;

        /// <summary>When <c>true</c>, the connection uses SSL/TLS encryption.</summary>
        [Tooltip("Encrypted access to the broker")]
        public bool encrypted;

        /// <summary>Optional fallback addresses tried when <see cref="host"/> is unreachable.</summary>
        [Tooltip("Optional alternate addresses, used if the previous host is not accessible")]
        public string[] alternateAddress;
    }
}