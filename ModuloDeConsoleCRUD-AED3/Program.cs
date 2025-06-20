using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

namespace AED_3_2025_S1_CRUD_Edgard_Melo
{
    class Program
    {
        static readonly string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        static readonly CRUD<RegistroDeRede> crud = new CRUD<RegistroDeRede>(basePath, true);

        static void Main()
        {
            Console.WriteLine("Bem-vindo ao sistema CRUD!");
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            Console.WriteLine("Deseja importar dados de um arquivo CSV?");
            Console.WriteLine("1 - Sim");
            Console.WriteLine("2 - Não");
            Console.Write("Escolha uma opção: ");
            string opcao = Console.ReadLine();

            if (opcao == "1")
            {
                ImportarCSV();
            }
            else
            {
                crud.CarregarDadosExistentes();
            }

            bool continuar = true;
            while (continuar)
            {
                Console.Clear();
                Console.WriteLine("\nMenu CRUD:");
                Console.WriteLine("1 - Criar Registro");
                Console.WriteLine("2 - Pesquisar por ID");
                Console.WriteLine("3 - Atualizar Registro");
                Console.WriteLine("4 - Deletar Registro");                
                Console.WriteLine("5 - Listar Todos os Registros");
                Console.WriteLine("6 - Pesquisar por Conjunto de IDs");
                Console.WriteLine("7 - Pesquisar por Padrão (KMP)");
                Console.WriteLine("8 - Comprimir Arquivo");
                Console.WriteLine("9 - Descomprimir Arquivo");
                Console.WriteLine("0 - Sair");
                Console.Write("Escolha uma opção: ");
                opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        CriarRegistro();
                        break;
                    case "2":
                        PesquisarPorId();
                        break;
                    case "3":
                        AtualizarRegistro();
                        break;
                    case "4":
                        DeletarRegistro();
                        break;
                    case "5":
                        
                        ListarTodosRegistros();
                        break;
                    case "6":
                        PesquisarPorConjuntoIds();
                        break;
                    case "7":
                        PesquisarPorPadrao();
                        break;
                    case "8":
                        ComprimirArquivo();
                        break;
                    case "9":
                        DescomprimirArquivo();
                        break;
                    case "0":
                        continuar = false;
                        Console.WriteLine("Saindo do sistema. Até logo!");
                        break;
                    default:
                        Console.WriteLine("Opção inválida. Por favor, escolha uma opção entre 0 e 9.");
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void ImportarCSV()
        {
            Console.WriteLine("Por favor, insira o caminho completo do arquivo CSV:");
            string caminhoCSV = Console.ReadLine();
            if (!File.Exists(caminhoCSV))
            {
                Console.WriteLine($"Arquivo {caminhoCSV} não encontrado.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("A importação excluirá todos os dados existentes (.bin). Deseja continuar? (S/N)");
            string resposta = Console.ReadLine();
            if (resposta?.ToLower() != "s")
            {
                Console.WriteLine("Importação cancelada.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            LimparArquivosBin();
            var csvImporter = new CSVImporter();
            var registros = csvImporter.ImportarCSV(caminhoCSV);

            int importedCount = 0;
            foreach (var registro in registros)
            {
                try
                {
                    Console.WriteLine($"Criando registro (UID: {registro.UID})...");
                    crud.CriarComUID(registro, registro.UID);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao importar registro (UID: {registro.UID}): {ex.Message}");
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Importação bem-sucedida! Foram carregados {importedCount} registros.");
            Console.WriteLine($"Tempo de importação: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Registros importados com sucesso: {importedCount} de {registros.Count}");
            Console.WriteLine("Pressione qualquer tecla para continuar para o menu");
            Console.ReadKey();
        }

        private static void CriarRegistro()
        {
            Console.WriteLine("Digite os dados do novo registro:");
            var registro = InputHelper.ObterRegistroDoUsuario();
            try
            {
                crud.Criar(registro);
                Console.WriteLine("Registro criado com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar registro: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void ListarTodosRegistros()
        {
            Console.WriteLine("Lista de Registros:");
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i <= crud.UltimoId; i++)
            {
                try
                {
                    var registro = crud.Ler(i);
                    if (registro != null)
                    {
                        Console.WriteLine("-------------------------");
                        ExibirRegistro(registro);
                    }
                    Console.WriteLine($"Leitura de UID {i} concluída em {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Restart();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao ler UID {i}: {ex.Message}");
                }
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void AtualizarRegistro()
        {
            Console.Write("Digite o ID do registro a atualizar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                var registroExistente = crud.Ler(id);
                if (registroExistente == null)
                {
                    Console.WriteLine($"Registro com ID {id} não encontrado.");
                }
                else
                {
                    Console.WriteLine("Registro atual:");
                    ExibirRegistro(registroExistente);
                    Console.WriteLine("\nDigite os novos dados (pressione Enter para manter os valores atuais):");
                    var novoRegistro = InputHelper.ObterRegistroDoUsuario();
                    novoRegistro.UID = id;
                    crud.Atualizar(novoRegistro);
                    Console.WriteLine("Registro atualizado com sucesso!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar registro: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void DeletarRegistro()
        {
            Console.Write("Digite o ID do registro a deletar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                crud.Deletar(id);
                Console.WriteLine($"Registro com ID {id} deletado com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar registro: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PesquisarPorId()
        {
            Console.Write("Digite o ID do registro a pesquisar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var registro = crud.Ler(id);
                if (registro == null)
                {
                    Console.WriteLine($"Registro com ID {id} não encontrado.");
                }
                else
                {
                    Console.WriteLine("Registro encontrado:");
                    ExibirRegistro(registro);
                    Console.WriteLine($"Leitura de UID {id} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao pesquisar registro: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PesquisarPorConjuntoIds()
        {
            Console.Write("Digite os IDs separados por vírgula (ex: 1,2,3): ");
            string entrada = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(entrada))
            {
                Console.WriteLine("Entrada inválida.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            var ids = entrada.Split(',').Select(s => s.Trim()).Where(s => int.TryParse(s, out _)).Select(int.Parse).ToList();
            if (!ids.Any())
            {
                Console.WriteLine("Nenhum ID válido fornecido.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var registros = crud.LerConjunto(ids);
                if (!registros.Any())
                {
                    Console.WriteLine("Nenhum registro encontrado para os IDs fornecidos.");
                }
                else
                {
                    Console.WriteLine("Registros encontrados:");
                    foreach (var registro in registros)
                    {
                        Console.WriteLine("-------------------------");
                        ExibirRegistro(registro);
                    }
                }
                Console.WriteLine($"Pesquisa concluída em {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao pesquisar registros: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void PesquisarPorPadrao()
        {
            Console.Write("Digite o padrão a ser pesquisado no PayloadData: ");
            string padrao = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(padrao))
            {
                Console.WriteLine("Padrão inválido.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var registros = crud.BuscarPadrao(padrao);
                if (!registros.Any())
                {
                    Console.WriteLine($"Nenhum registro encontrado com o padrão '{padrao}'.");
                }
                else
                {
                    Console.WriteLine("Registros encontrados:");
                    foreach (var registro in registros)
                    {
                        Console.WriteLine("-------------------------");
                        ExibirRegistro(registro);
                    }
                }
                Console.WriteLine($"Pesquisa concluída em {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao pesquisar por padrão: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void ComprimirArquivo()
        {
            Console.Write("Digite a versão da compressão (ex: 1): ");
            if (!int.TryParse(Console.ReadLine(), out int versao))
            {
                Console.WriteLine("Versão inválida.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            try
            {
                var huffmanCompressor = new HuffmanCompressor();
                var lzwCompressor = new LZWCompressor();
                string huffmanOutput = Path.Combine(basePath, $"banco_de_dadosHuffmanCompressao{versao}.bin");
                string lzwOutput = Path.Combine(basePath, $"banco_de_dadosLZWCompressao{versao}.bin");

                var stopwatchHuffman = Stopwatch.StartNew();
                huffmanCompressor.Compress(Path.Combine(basePath, "banco_de_dados.bin"), huffmanOutput);
                stopwatchHuffman.Stop();

                var stopwatchLZW = Stopwatch.StartNew();
                lzwCompressor.Compress(Path.Combine(basePath, "banco_de_dados.bin"), lzwOutput);
                stopwatchLZW.Stop();

                Console.WriteLine($"Compressão concluída. Comparação:");
                Console.WriteLine($"Huffman - Tempo: {stopwatchHuffman.ElapsedMilliseconds}ms, Tamanho: {new FileInfo(huffmanOutput).Length} bytes");
                Console.WriteLine("Huffman salvo em: " + huffmanOutput);
                Console.WriteLine($"LZW - Tempo: {stopwatchLZW.ElapsedMilliseconds}ms, Tamanho: {new FileInfo(lzwOutput).Length} bytes");
                Console.WriteLine("LZW salvo em: " + lzwOutput);

                if (stopwatchHuffman.ElapsedMilliseconds < stopwatchLZW.ElapsedMilliseconds)
                    Console.WriteLine("Huffman foi mais rápido.");
                else
                    Console.WriteLine("LZW foi mais rápido.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao comprimir arquivo: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void DescomprimirArquivo()
        {
            Console.Write("Digite a versão da compressão a descomprimir (ex: 1): ");
            if (!int.TryParse(Console.ReadLine(), out int versao))
            {
                Console.WriteLine("Versão inválida.");
                Console.WriteLine("Pressione qualquer tecla para continuar");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Escolha o algoritmo de descompressão:");
            Console.WriteLine("1 - Huffman");
            Console.WriteLine("2 - LZW");
            Console.Write("Escolha uma opção: ");
            string algoritmo = Console.ReadLine();

            try
            {
                string inputPath;
                var compressor = algoritmo == "1" ? (ICompressor)new HuffmanCompressor() : new LZWCompressor();

                inputPath = Path.Combine(basePath, algoritmo == "1" ? $"banco_de_dadosHuffmanCompressao{versao}.bin" : $"banco_de_dadosLZWCompressao{versao}.bin");
                string outputPath = Path.Combine(basePath, "banco_de_dados.bin");

                if (!File.Exists(inputPath))
                {
                    Console.WriteLine($"Arquivo {inputPath} não encontrado.");
                    Console.WriteLine("Pressione qualquer tecla para continuar");
                    Console.ReadKey();
                    return;
                }

                var stopwatch = Stopwatch.StartNew();
                compressor.Decompress(inputPath, outputPath);
                stopwatch.Stop();
                Console.WriteLine($"Descompressão concluída em {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao descomprimir arquivo: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para continuar");
            Console.ReadKey();
        }

        private static void LimparArquivosBin()
        {
            try
            {
                string[] arquivos = { "banco_de_dados.bin", "indices_bplus.bin", "indices_hash.bin", "encrypted_uids.bin" };
                foreach (var arquivo in arquivos)
                {
                    string caminho = Path.Combine(basePath, arquivo);
                    if (File.Exists(caminho))
                    {
                        File.Delete(caminho);
                        Console.WriteLine($"Arquivo {caminho} excluído com sucesso.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao limpar arquivos binários: {ex.Message}");
            }
        }

        private static void ExibirRegistro(RegistroDeRede registro)
        {
            Console.WriteLine($"ID: {registro.UID}");
            Console.WriteLine($"Timestamp: {(registro.Timestamp != DateTime.MinValue ? registro.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") : "N/A")}");
            Console.WriteLine($"Source IP: {registro.SourceIPAddress ?? "N/A"}");
            Console.WriteLine($"Destination IP: {registro.DestinationIPAddress ?? "N/A"}");
            Console.WriteLine($"Source Port: {registro.SourcePort?.ToString() ?? "N/A"}");
            Console.WriteLine($"Destination Port: {registro.DestinationPort?.ToString() ?? "N/A"}");
            Console.WriteLine($"Protocol: {registro.Protocol ?? "N/A"}");
            Console.WriteLine($"Packet Length: {registro.PacketLength?.ToString() ?? "N/A"}");
            Console.WriteLine($"Packet Type: {registro.PacketType ?? "N/A"}");
            Console.WriteLine($"Traffic Type: {registro.TrafficType ?? "N/A"}");
            Console.WriteLine($"Payload Data: {registro.PayloadData ?? "N/A"}");
            Console.WriteLine($"Malware Indicators: {registro.MalwareIndicators ?? "N/A"}");
            Console.WriteLine($"Anomaly Scores: {registro.AnomalyScores?.ToString("F2") ?? "N/A"}"); // Correção do ToString
            Console.WriteLine($"Alerts/Warnings: {registro.AlertsWarnings ?? "N/A"}");
            Console.WriteLine($"Attack Type: {registro.AttackType ?? "N/A"}");
            Console.WriteLine($"Attack Signature: {registro.AttackSignature ?? "N/A"}");
            Console.WriteLine($"Action Taken: {registro.ActionTaken ?? "N/A"}");
            Console.WriteLine($"Severity Level: {registro.SeverityLevel ?? "N/A"}");
            Console.WriteLine($"User Information: {registro.UserInformation ?? "N/A"}");
            Console.WriteLine($"Device Information: {registro.DeviceInformation ?? "N/A"}");
            Console.WriteLine($"Network Segment: {registro.NetworkSegment ?? "N/A"}");
            Console.WriteLine($"Geo-location Data: {(registro.GeoLocationData != null && registro.GeoLocationData.Length > 0 ? string.Join(", ", registro.GeoLocationData) : "N/A")}");
            Console.WriteLine($"Proxy Information: {registro.ProxyInformation ?? "N/A"}");
            Console.WriteLine($"Firewall Logs: {registro.FirewallLogs ?? "N/A"}");
            Console.WriteLine($"IDS/IPS Alerts: {registro.IDSIPSAlerts ?? "N/A"}");
            Console.WriteLine($"Log Source: {registro.LogSource ?? "N/A"}");
        }
    }
}