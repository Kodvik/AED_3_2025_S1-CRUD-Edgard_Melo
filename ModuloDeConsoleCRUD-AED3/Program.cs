using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization; // Tive que adicionar por precisar para o csvhelper
using System.IO;
using CsvHelper;
using Microsoft.Win32;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Bem-vindo ao sistema CRUD!");
        Console.WriteLine("Por favor, insira o caminho completo do arquivo CSV:");

        // Solicita ao usuário o caminho do arquivo CSV
        string caminhoCSV = Console.ReadLine();
        string caminhoDeTrabalho = Path.GetDirectoryName(caminhoCSV);

        // Valida se o caminho foi fornecido
        if (string.IsNullOrWhiteSpace(caminhoCSV))
        {
            Console.WriteLine("Caminho inválido. Por favor, insira um caminho válido.");
            return;
        }

        try
        {
            // Importa os registros do CSV usando CsvHelper
            var registros = ImportarRegistrosDoCSV(caminhoCSV);

            Console.WriteLine($"Importação bem-sucedida! Foram carregados {registros.Count} registros.");

            Console.WriteLine("Aguarde o processamento do banco de dados.");

            // Inicializa o sistema CRUD e o arquivo binário
            string caminhoArquivoBin = Path.Combine(caminhoDeTrabalho, "banco_de_dados.bin");
            var crud = new CRUD<RegistroDeRede>(caminhoArquivoBin);

            // Salva os registros importados no arquivo binário
            foreach (var registro in registros)
            {
                crud.Criar(registro);
            }

            Console.WriteLine("Os registros foram salvos no arquivo binário com sucesso!");
            Console.WriteLine("Pressione qualquer botao para continuar para o menu");

            Console.ReadLine();

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
                Console.Write("Escolha a opção desejada: ");

                string opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1": // Criar
                        Console.Clear();
                        RegistroDeRede novoRegistro = ObterRegistroDoUsuario();
                        crud.Criar(novoRegistro);
                        Console.WriteLine("Registro criado com sucesso!");
                        break;

                    case "2": // Ler
                        Console.Write("Digite o ID do registro que deseja ler: ");
                        if (int.TryParse(Console.ReadLine(), out int idLer))
                        {
                            var registro = crud.Ler(idLer);
                            if (registro != null)
                            {
                                Console.Clear();
                                Console.WriteLine("\nRegistro Encontrado:");
                                Console.WriteLine($"User Information: {registro.UserInformation}");
                                Console.WriteLine($"Time Stamp: {registro.Timestamp}");
                                Console.WriteLine($"Source IP: {registro.SourceIPAddress}");
                                Console.WriteLine($"Destination IP: {registro.DestinationIPAddress}");
                                Console.WriteLine($"Payload Data:  {registro.PayloadData}");
                            }
                            else
                            {
                                Console.WriteLine("Registro não encontrado.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        Console.WriteLine("Pressione qualquer tecla para continuar");
                        Console.Read();
                        break;

                    case "3": // Atualizar
                        Console.Clear();
                        Console.Write("Digite o ID do registro que deseja atualizar: ");
                        if (int.TryParse(Console.ReadLine(), out int idAtualizar))
                        {
                            RegistroDeRede registroAtualizado = ObterRegistroDoUsuario();
                            crud.Atualizar(idAtualizar, registroAtualizado);
                            Console.WriteLine("Registro atualizado com sucesso!");
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        break;

                    case "4": // Excluir
                        Console.Clear();
                        Console.Write("Digite o ID do registro que deseja excluir: ");
                        if (int.TryParse(Console.ReadLine(), out int idExcluir))
                        {
                            crud.Deletar(idExcluir);
                            Console.WriteLine("Registro excluído com sucesso!");
                        }
                        else
                        {
                            Console.WriteLine("ID inválido.");
                        }
                        break;

                    case "5": // Listar todos os registros
                        Console.Clear();
                        Console.WriteLine("\nLista de Registros:");
                        ListarRegistros(crud);
                        break;

                    case "0": // Sair
                        continuar = false;
                        Console.WriteLine("Saindo do sistema...");
                        break;

                    default:
                        Console.Clear();
                        Console.WriteLine("Opção inválida. Tente novamente.");
                        break;
                }
            }
        }

        catch (Exception ex)
        {
            // Lida com qualquer exceção durante o processo
            Console.WriteLine($"Erro ao processar o arquivo CSV: {ex.Message}");
        }
    }

    //vou testar usar o conceito de summary e param, se funcionar LEMBRAR DE USAR MAIS NO FUTURO!!!!
    /// <summary>
    /// Importa registros de um arquivo CSV utilizando CsvHelper.
    /// </summary>
    /// <param name="caminhoCSV">O caminho do arquivo CSV fornecido pelo usuário.</param>
    /// <returns>Uma lista de objetos do tipo RegistroDeRede.</returns
    private static List<RegistroDeRede> ImportarRegistrosDoCSV(string caminhoCSV)
    {
        using (var reader = new StreamReader(caminhoCSV))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // Converte automaticamente os dados do CSV para o tipo RegistroDeRede
            return new List<RegistroDeRede>(csv.GetRecords<RegistroDeRede>());
        }
    }


    // Método para criar um novo registro com os dados fornecidos pelo usuário
    private static RegistroDeRede ObterRegistroDoUsuario()
    {
        RegistroDeRede registro = new RegistroDeRede();

        //Tenho que otimizar isso ainda, uma lista muito grande de items a coletar
        //posso talvez modularizar a função em um .cs separado

        //IMPORTANTE: tenho que melhorar o sistema de tratamento de entradas invalidas ao inves de 'parar' o sistema.

        Console.Write("Timestamp (yyyy-MM-dd HH:mm:ss): ");
        registro.Timestamp = DateTime.Parse(Console.ReadLine());
        Console.Write("Source IP Address: ");
        registro.SourceIPAddress = Console.ReadLine();
        Console.Write("Destination IP Address: ");
        registro.DestinationIPAddress = Console.ReadLine();
        Console.Write("Source Port: ");
        registro.SourcePort = int.Parse(Console.ReadLine());
        Console.Write("Destination Port: ");
        registro.DestinationPort = int.Parse(Console.ReadLine());
        Console.Write("Protocol: ");
        registro.Protocol = Console.ReadLine();
        Console.Write("Packet Length: ");
        registro.PacketLength = int.Parse(Console.ReadLine());
        Console.Write("Packet Type: ");
        registro.PacketType = Console.ReadLine();
        Console.Write("Traffic Type: ");
        registro.TrafficType = Console.ReadLine();
        Console.Write("Payload Data: ");
        registro.PayloadData = Console.ReadLine();
        Console.Write("Malware Indicators: ");
        registro.MalwareIndicators = Console.ReadLine();
        Console.Write("Anomaly Scores: ");
        registro.AnomalyScores = Convert.ToDouble(Console.ReadLine());
        Console.Write("Alerts Warnings: ");
        registro.AlertsWarnings = Console.ReadLine();
        Console.Write("Attack Type: ");
        registro.AttackType = Console.ReadLine();
        Console.Write("Attack Signature: ");
        registro.AttackSignature = Console.ReadLine();
        Console.Write("Action Taken: ");
        registro.ActionTaken = Console.ReadLine();
        Console.Write("SeverityLevel: ");
        registro.SeverityLevel = Console.ReadLine();
        Console.Write("User Information: ");
        registro.UserInformation = Console.ReadLine();
        Console.Write("Device Information: ");
        registro.DeviceInformation = Console.ReadLine();
        Console.Write("Network Segment: ");
        registro.NetworkSegment = Console.ReadLine();
        Console.Write("Geo Location Data: ");
        registro.GeoLocationData = Console.ReadLine();
        Console.Write("Proxy Information: ");
        registro.ProxyInformation = Console.ReadLine();
        Console.Write("Firewall Logs: ");
        registro.FirewallLogs = Console.ReadLine();
        Console.Write("IDS IPS Alerts: ");
        registro.IDSIPSAlerts = Console.ReadLine();
        Console.Write("Log Source: ");
        registro.LogSource = Console.ReadLine();

        return registro;
    }

    // Método para listar todos os registros, talvez deva modularizar essa função também
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
                    Console.WriteLine($"Registro {registro.UID}: ");
                    //Preciso colocar o restante dos itens a serem impressos aqui ainda.
                    //usando somente o ID como teste de output por enquanto.
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar registros: {ex.Message}");
        }

    }
}
//AINDA PRECISO IMPLEMENTAR UMA FORMA DE EXPORTAR O CSV PARA ATUALIZAR!
