using System;
using System.Collections.Generic;
using System.IO;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression
{
    public class LZWCompressor : ICompressor
    {
        public void Compress(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputPath);

            byte[] inputData = File.ReadAllBytes(inputPath);
            if (inputData.Length == 0)
            {
                File.Create(outputPath).Dispose();
                return;
            }

            var dictionary = InitializeDictionary();
            List<int> compressedData = CompressData(inputData, dictionary);

            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(outputStream))
            {
                foreach (int code in compressedData)
                {
                    writer.Write(code);
                }
            }
        }

        public void Decompress(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputPath);

            List<int> compressedData = ReadCompressedData(inputPath);
            var dictionary = InitializeReverseDictionary();
            byte[] decompressedData = DecompressData(compressedData, dictionary);

            File.WriteAllBytes(outputPath, decompressedData);
        }

        private Dictionary<string, int> InitializeDictionary()
        {
            var dictionary = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
            {
                dictionary[((char)i).ToString()] = i;
            }
            return dictionary;
        }

        private List<int> CompressData(byte[] inputData, Dictionary<string, int> dictionary)
        {
            string current = "";
            List<int> compressedData = new List<int>();
            int nextCode = 256;

            foreach (byte b in inputData)
            {
                string currentPlusByte = current + (char)b;
                if (dictionary.ContainsKey(currentPlusByte))
                {
                    current = currentPlusByte;
                }
                else
                {
                    compressedData.Add(dictionary[current]);
                    dictionary[currentPlusByte] = nextCode++;
                    current = ((char)b).ToString();
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                compressedData.Add(dictionary[current]);
            }

            return compressedData;
        }

        private List<int> ReadCompressedData(string inputPath)
        {
            List<int> compressedData = new List<int>();
            using (var inputStream = new FileStream(inputPath, FileMode.Open))
            using (var reader = new BinaryReader(inputStream))
            {
                while (inputStream.Position < inputStream.Length)
                {
                    compressedData.Add(reader.ReadInt32());
                }
            }
            return compressedData;
        }

        private Dictionary<int, string> InitializeReverseDictionary()
        {
            var dictionary = new Dictionary<int, string>();
            for (int i = 0; i < 256; i++)
            {
                dictionary[i] = ((char)i).ToString();
            }
            return dictionary;
        }

        private byte[] DecompressData(List<int> compressedData, Dictionary<int, string> dictionary)
        {
            using (var outputStream = new MemoryStream())
            {
                int nextCode = 256;
                string current = compressedData.Count > 0 ? dictionary[compressedData[0]] : "";
                byte[] currentBytes = StringToByteArray(current);
                outputStream.Write(currentBytes, 0, currentBytes.Length);

                foreach (int code in compressedData.Skip(1))
                {
                    string entry;
                    if (dictionary.ContainsKey(code))
                    {
                        entry = dictionary[code];
                    }
                    else if (code == nextCode)
                    {
                        entry = current + current[0];
                    }
                    else
                    {
                        throw new InvalidDataException("Código inválido encontrado durante a descompressão.");
                    }

                    byte[] entryBytes = StringToByteArray(entry);
                    outputStream.Write(entryBytes, 0, entryBytes.Length);

                    dictionary[nextCode++] = current + entry[0];
                    current = entry;
                }

                return outputStream.ToArray();
            }
        }

        private byte[] StringToByteArray(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = (byte)str[i];
            }
            return bytes;
        }
    }
}