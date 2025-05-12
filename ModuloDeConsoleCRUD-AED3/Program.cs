using System;
using System.Collections.Generic;
using System.IO;
using AED_3_2025_S1_CRUD_Edgard_Melo.DataAccess;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

namespace AED_3_2025_S1_CRUD_Edgard_Melo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bem-vindo ao sistema CRUD!");
            Console.WriteLine("Deseja importar dados de um arquivo CSV?");
            Console.WriteLine("1 - Sim");
            Console.WriteLine("2 - Não");
            Console.Write("Escolha uma opção: ");

            string caminhoCSV = string.Empty;
            string opcaoInicial = Console.ReadLine();
            string basePath = Directory.GetCurrentDirectory();
            string dataPath = Path.Combine(basePath, "Data");
            var crud = new CRUD<RegistroDeRede>(basePath, caminhoCSV);

            if (opcaoInicial == "1")
            {
                Console.WriteLine("Por favor, insira o caminho completo do arquivo CSV:");
                caminhoCSV = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(caminhoCSV) || !File.Exists(caminhoCSV))
                {
                    Console.WriteLine("Caminho inválido ou arquivo não encontrado. Continuando sem importar.");
                }
                else
                {
                    try
                    {
                        // Confirmar limpeza dos arquivos .bin
                        Console.WriteLine("A importação excluirá todos os dados existentes (.bin). Deseja continuar? (S/N)");
                        if (Console.ReadLine()?.ToUpper() != "S")
                        {
                            Console.WriteLine("Importação cancelada.");
                            Console.WriteLine("Pressione qualquer tecla para continuar para o menu");
                            Console.ReadKey();
                            return;
                        }

                        // Limpar arquivos .bin
                        LimparArquivosBin(dataPath);

                        // Reinicializar CRUD após limpeza para recriar índices
                        crud = new CRUD<RegistroDeRede>(basePath, caminhoCSV);

                        var importer = new CSVImporter();
                        var registros = importer.ImportarCSV(caminhoCSV);
                        Console.WriteLine($"Importação bem-sucedida! Foram carregados {registros.Count} registros.");

                        // Reinicializar ultimoId com base nos UIDs do CSV
                        int maxUid = registros.Any() ? registros.Max(r => r.UID) : 0;
                        crud.ReinicializarUltimoId(maxUid);
                        Console.WriteLine($"ultimoId reinicializado para: {maxUid}");

                        foreach (var registro in registros)
                        {
                            try
                            {
                                Console.WriteLine($"Criando registro (UID: {registro.UID})...");
                                crud.Criar(registro);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao criar registro (UID: {registro.UID}): {ex.Message}");
                            }
                        }
                        Console.WriteLine("Os registros foram salvos no banco de dados binário!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao importar CSV: {ex.Message}");
                    }
                }
            }

            Console.WriteLine("Pressione qualquer tecla para continuar para o menu");
            Console.ReadKey();

            bool continuar = true;
            while (continuar)
            {
                Console.Clear();
                Console.WriteLine("\nMenu CRUD:");
                Console.WriteLine("0 - Sair");
                Console.WriteLine("1 - Criar");
                Console.WriteLine("2 - Ler");
                Console.WriteLine("3 - Atualizar");
                Console.WriteLine("4 - Excluir");
                Console.WriteLine("5 - Listar Todos os Registros");
                Console.WriteLine("6 - Exportar para CSV");
                Console.Write("Escolha a opção desejada: ");

                string opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        Console.Clear();
                        var novoRegistro = InputHelper.ObterRegistroDoUsuario();
                        try
                        {
                            crud.Criar(novoRegistro);
                            Console.WriteLine("Registro criado com sucesso!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao criar registro: {ex.Message}");
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "2":
                        Console.Clear();
                        Console.Write("Digite o ID do registro que deseja ler: ");
                        if (int.TryParse(Console.ReadLine(), out int idLer))
                        {
                            try
                            {
                                var registro = crud.Ler(idLer);
                                if (registro != null)
                                {
                                    Console.WriteLine("\nRegistro Encontrado:");
                                    ExibirRegistro(registro);
                                }
                                else
                                {
                                    Console.WriteLine("Registro não encontrado.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao ler registro: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "3":
                        Console.Clear();
                        Console.Write("Digite o ID do registro que deseja atualizar: ");
                        if (int.TryParse(Console.ReadLine(), out int idAtualizar))
                        {
                            var registroAtualizado = InputHelper.ObterRegistroDoUsuario();
                            try
                            {
                                crud.Atualizar(idAtualizar, registroAtualizado);
                                Console.WriteLine("Registro atualizado com sucesso!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao atualizar registro: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "4":
                        Console.Clear();
                        Console.Write("Digite o ID do registro que deseja excluir: ");
                        if (int.TryParse(Console.ReadLine(), out int idExcluir))
                        {
                            try
                            {
                                crud.Deletar(idExcluir);
                                Console.WriteLine("Registro excluído com sucesso!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao excluir registro: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "5":
                        Console.Clear();
                        Console.WriteLine("\nLista de Registros:");
                        ListarRegistros(crud);
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "6":
                        Console.Clear();
                        Console.Write("Digite o caminho completo para o arquivo CSV de exportação: ");
                        string caminhoExportacao = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(caminhoExportacao))
                        {
                            Console.WriteLine("Caminho inválido. Exportação cancelada.");
                        }
                        else
                        {
                            try
                            {
                                crud.ExportarParaCSV(caminhoExportacao);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao exportar para CSV: {ex.Message}");
                            }
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;

                    case "0":
                        continuar = false;
                        Console.WriteLine("Saindo do sistema...");
                        break;

                    default:
                        Console.Clear();
                        Console.WriteLine("Opção inválida. Tente novamente.");
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void LimparArquivosBin(string dataPath)
        {
            string[] arquivosBin = {
                Path.Combine(dataPath, "indices_bplus.bin"),
                Path.Combine(dataPath, "banco_de_dados.bin"),
                Path.Combine(dataPath, "indices_hash.bin")
            };

            foreach (var arquivo in arquivosBin)
            {
                try
                {
                    if (File.Exists(arquivo))
                    {
                        File.Delete(arquivo);
                        Console.WriteLine($"Arquivo {arquivo} excluído com sucesso.");
                    }
                    else
                    {
                        Console.WriteLine($"Arquivo {arquivo} não existe. Nenhuma ação necessária.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao excluir arquivo {arquivo}: {ex.Message}");
                    throw;
                }
            }

            // Recriar diretório Data, se necessário
            if (!Directory.Exists(dataPath))
            {
                try
                {
                    Directory.CreateDirectory(dataPath);
                    Console.WriteLine($"Diretório {dataPath} criado com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao criar diretório {dataPath}: {ex.Message}");
                    throw;
                }
            }
        }

        private static void ExibirRegistro(RegistroDeRede registro)
        {
            Console.WriteLine($"ID: {registro.UID}");
            Console.WriteLine($"Timestamp: {registro.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Não fornecido"}");
            Console.WriteLine($"Source IP: {registro.SourceIPAddress ?? "Não fornecido"}");
            Console.WriteLine($"Destination IP: {registro.DestinationIPAddress ?? "Não fornecido"}");
            Console.WriteLine($"Source Port: {registro.SourcePort?.ToString() ?? "Não fornecido"}");
            Console.WriteLine($"Destination Port: {registro.DestinationPort?.ToString() ?? "Não fornecido"}");
            Console.WriteLine($"Protocol: {registro.Protocol ?? "Não fornecido"}");
            Console.WriteLine($"Packet Length: {registro.PacketLength?.ToString() ?? "Não fornecido"}");
            Console.WriteLine($"Packet Type: {registro.PacketType ?? "Não fornecido"}");
            Console.WriteLine($"Traffic Type: {registro.TrafficType ?? "Não fornecido"}");
            Console.WriteLine($"Payload Data: {registro.PayloadData ?? "Não fornecido"}");
            Console.WriteLine($"Malware Indicators: {registro.MalwareIndicators ?? "Não fornecido"}");
            Console.WriteLine($"Anomaly Scores: {registro.AnomalyScores?.ToString() ?? "Não fornecido"}");
            Console.WriteLine($"Alerts/Warnings: {registro.AlertsWarnings ?? "Não fornecido"}");
            Console.WriteLine($"Attack Type: {registro.AttackType ?? "Não fornecido"}");
            Console.WriteLine($"Attack Signature: {registro.AttackSignature ?? "Não fornecido"}");
            Console.WriteLine($"Action Taken: {registro.ActionTaken ?? "Não fornecido"}");
            Console.WriteLine($"Severity Level: {registro.SeverityLevel ?? "Não fornecido"}");
            Console.WriteLine($"User Information: {registro.UserInformation ?? "Não fornecido"}");
            Console.WriteLine($"Device Information: {registro.DeviceInformation ?? "Não fornecido"}");
            Console.WriteLine($"Network Segment: {registro.NetworkSegment ?? "Não fornecido"}");
            Console.WriteLine($"Geo-location Data: {registro.GeoLocationData ?? "Não fornecido"}");
            Console.WriteLine($"Proxy Information: {registro.ProxyInformation ?? "Não fornecido"}");
            Console.WriteLine($"Firewall Logs: {registro.FirewallLogs ?? "Não fornecido"}");
            Console.WriteLine($"IDS/IPS Alerts: {registro.IDSIPSAlerts ?? "Não fornecido"}");
            Console.WriteLine($"Log Source: {registro.LogSource ?? "Não fornecido"}");
        }

        private static void ListarRegistros(CRUD<RegistroDeRede> crud)
        {
            try
            {
                int ultimoID = crud.getUltimoID();
                for (int i = 1; i <= ultimoID; i++)
                {
                    var registro = crud.Ler(i);
                    if (registro != null)
                    {
                        Console.WriteLine("\n-------------------------");
                        ExibirRegistro(registro);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao listar registros: {ex.Message}");
            }
        }
    }
}