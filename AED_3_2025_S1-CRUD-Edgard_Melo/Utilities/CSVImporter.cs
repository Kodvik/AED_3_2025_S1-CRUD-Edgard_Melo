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
        }
    }
}