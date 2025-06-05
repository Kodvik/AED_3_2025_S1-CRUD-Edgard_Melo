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
        // Método comentado para evitar duplicação de código
        /*public List<RegistroDeRede> ImportarCSV(string caminhoCSV)
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
                            registros.Add(registro);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao ler registro do CSV: {ex.Message}. Ignorando registro.");
                        }
                    }
                    return registros;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao importar CSV: {ex.Message}");
                return new List<RegistroDeRede>();
            }
        }*/
    }
}