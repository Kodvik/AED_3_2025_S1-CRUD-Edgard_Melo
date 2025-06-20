using System;
using System.Collections.Generic;
using System.IO;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;
using CsvHelper;
using System.Globalization;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Utilities
{
    public class CSVImporter
    {
        public List<RegistroDeRede> ImportarCSV(string caminhoCSV)
        {
            try
            {
                using (var reader = new StreamReader(caminhoCSV))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var registros = new List<RegistroDeRede>();
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        try
                        {
                            var registro = csv.GetRecord<RegistroDeRede>();
                            // Validar UID
                            if (registro.UID < 0)
                            {
                                Console.WriteLine($"UID inválido detectado: {registro.UID}. Ignorando registro.");
                                continue;
                            }
                            // Validar campos obrigatórios
                            if (string.IsNullOrWhiteSpace(registro.SourceIPAddress) || string.IsNullOrWhiteSpace(registro.DestinationIPAddress))
                            {
                                Console.WriteLine($"Registro com UID {registro.UID} possui SourceIPAddress ou DestinationIPAddress inválidos. Ignorando registro.");
                                continue;
                            }
                            Console.WriteLine($"Lido registro com UID: {registro.UID}");
                            registros.Add(registro);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao ler registro do CSV: {ex.Message}. Ignorando registro.");
                        }
                    }
                    // Verificar duplicatas
                    var duplicatas = registros.GroupBy(r => r.UID).Where(g => g.Count() > 1).Select(g => g.Key);
                    if (duplicatas.Any())
                    {
                        Console.WriteLine($"Aviso: Encontrados UIDs duplicados no CSV: {string.Join(", ", duplicatas)}");
                    }
                    return registros;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao importar CSV: {ex.Message}");
                return new List<RegistroDeRede>();
            }
        }
    }
}