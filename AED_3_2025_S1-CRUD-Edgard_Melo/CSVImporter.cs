using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;

public class CSVImporter
{
    // Método para importar registros diretamente usando cabeçalhos do CSV
    public List<RegistroDeRede> ImportarCSV(string caminhoCSV)
    {
        using (var reader = new StreamReader(caminhoCSV))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // Converte automaticamente os dados do CSV para a classe RegistroDeRede
            return new List<RegistroDeRede>(csv.GetRecords<RegistroDeRede>());
        }
    }
}
