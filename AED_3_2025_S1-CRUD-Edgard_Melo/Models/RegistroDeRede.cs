﻿namespace AED_3_2025_S1_CRUD_Edgard_Melo.Models
{
    public class RegistroDeRede : IEntidade
    {
        public int UID { get; set; } // Mapeia "UID"
        public DateTime? Timestamp { get; set; } // Mapeia "Timestamp"
        public string? SourceIPAddress { get; set; } // Mapeia "Source IP Address"
        public string? DestinationIPAddress { get; set; } // Mapeia "Destination IP Address"
        public int? SourcePort { get; set; } // Mapeia "Source Port"
        public int? DestinationPort { get; set; } // Mapeia "Destination Port"
        public string? Protocol { get; set; } // Mapeia "Protocol"
        public int? PacketLength { get; set; } // Mapeia "Packet Length"
        public string? PacketType { get; set; } // Mapeia "Packet Type"
        public string? TrafficType { get; set; } // Mapeia "Traffic Type"
        public string? PayloadData { get; set; } // Mapeia "Payload Data"
        public string? MalwareIndicators { get; set; } // Mapeia "Malware Indicators"
        public double? AnomalyScores { get; set; } // Mapeia "Anomaly Scores"
        public string? AlertsWarnings { get; set; } // Mapeia "Alerts/Warnings"
        public string? AttackType { get; set; } // Mapeia "Attack Type"
        public string? AttackSignature { get; set; } // Mapeia "Attack Signature"
        public string? ActionTaken { get; set; } // Mapeia "Action Taken"
        public string? SeverityLevel { get; set; } // Mapeia "Severity Level"
        public string? UserInformation { get; set; } // Mapeia "User Information"
        public string? DeviceInformation { get; set; } // Mapeia "Device Information"
        public string? NetworkSegment { get; set; } // Mapeia "Network Segment"
        public string? GeoLocationData { get; set; } // Mapeia "Geo-location Data"
        public string? ProxyInformation { get; set; } // Mapeia "Proxy Information"
        public string? FirewallLogs { get; set; } // Mapeia "Firewall Logs"
        public string? IDSIPSAlerts { get; set; } // Mapeia "IDS/IPS Alerts"
        public string? LogSource { get; set; } // Mapeia "Log Source"
    }
}