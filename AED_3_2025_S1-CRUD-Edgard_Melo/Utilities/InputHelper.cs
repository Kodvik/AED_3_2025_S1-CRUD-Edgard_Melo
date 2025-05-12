using System;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Utilities
{
    public static class InputHelper
    {
        public static RegistroDeRede ObterRegistroDoUsuario()
        {
            RegistroDeRede registro = new RegistroDeRede();
            try
            {
                // UID será gerado automaticamente pelo CRUD, não solicitado aqui
                registro.Timestamp = ObterDateTime("Timestamp (yyyy-MM-dd HH:mm:ss, pressione Enter para valor padrão): ") ?? DateTime.MinValue;
                registro.SourceIPAddress = ObterString("Source IP Address (pressione Enter para vazio): ");
                registro.DestinationIPAddress = ObterString("Destination IP Address (pressione Enter para vazio): ");
                registro.SourcePort = ObterInt("Source Port (pressione Enter para 0): ") ?? 0;
                registro.DestinationPort = ObterInt("Destination Port (pressione Enter para 0): ") ?? 0;
                registro.Protocol = ObterString("Protocol (pressione Enter para vazio): ");
                registro.PacketLength = ObterInt("Packet Length (pressione Enter para 0): ") ?? 0;
                registro.PacketType = ObterString("Packet Type (pressione Enter para vazio): ");
                registro.TrafficType = ObterString("Traffic Type (pressione Enter para vazio): ");
                registro.PayloadData = ObterString("Payload Data (pressione Enter para vazio): ");
                registro.MalwareIndicators = ObterString("Malware Indicators (pressione Enter para vazio): ");
                registro.AnomalyScores = ObterDouble("Anomaly Scores (pressione Enter para 0): ") ?? 0.0;
                registro.AlertsWarnings = ObterString("Alerts/Warnings (pressione Enter para vazio): ");
                registro.AttackType = ObterString("Attack Type (pressione Enter para vazio): ");
                registro.AttackSignature = ObterString("Attack Signature (pressione Enter para vazio): ");
                registro.ActionTaken = ObterString("Action Taken (pressione Enter para vazio): ");
                registro.SeverityLevel = ObterString("Severity Level (pressione Enter para vazio): ");
                registro.UserInformation = ObterString("User Information (pressione Enter para vazio): ");
                registro.DeviceInformation = ObterString("Device Information (pressione Enter para vazio): ");
                registro.NetworkSegment = ObterString("Network Segment (pressione Enter para vazio): ");
                registro.GeoLocationData = ObterString("Geo-location Data (pressione Enter para vazio): ");
                registro.ProxyInformation = ObterString("Proxy Information (pressione Enter para vazio): ");
                registro.FirewallLogs = ObterString("Firewall Logs (pressione Enter para vazio): ");
                registro.IDSIPSAlerts = ObterString("IDS/IPS Alerts (pressione Enter para vazio): ");
                registro.LogSource = ObterString("Log Source (pressione Enter para vazio): ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar entrada: {ex.Message}. Usando valores padrão.");
                // Continuar com valores padrão já atribuídos
            }
            return registro;
        }

        private static string? ObterString(string mensagem)
        {
            Console.Write(mensagem);
            string entrada = Console.ReadLine();
            return string.IsNullOrWhiteSpace(entrada) ? null : entrada;
        }

        private static int? ObterInt(string mensagem)
        {
            Console.Write(mensagem);
            string entrada = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(entrada))
                return null;
            if (int.TryParse(entrada, out int valor))
                return valor;
            Console.WriteLine("Entrada inválida, usando valor padrão (0).");
            return null;
        }

        private static double? ObterDouble(string mensagem)
        {
            Console.Write(mensagem);
            string entrada = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(entrada))
                return null;
            if (double.TryParse(entrada, out double valor))
                return valor;
            Console.WriteLine("Entrada inválida, usando valor padrão (0).");
            return null;
        }

        private static DateTime? ObterDateTime(string mensagem)
        {
            Console.Write(mensagem);
            string entrada = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(entrada))
                return null;
            if (DateTime.TryParse(entrada, out DateTime valor))
                return valor;
            Console.WriteLine("Entrada inválida, usando valor padrão (DateTime.MinValue).");
            return null;
        }
    }
}