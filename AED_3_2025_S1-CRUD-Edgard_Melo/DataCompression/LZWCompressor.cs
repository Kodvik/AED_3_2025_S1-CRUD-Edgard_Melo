using System;
using System.Collections.Generic;
using System.IO;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression
{
    public class LZWCompressor
    {
        private const int BitsPorCodigo = 12;
        private const int MaxCodigo = (1 << BitsPorCodigo) - 1; // 4095

        public void Compress(string inputPath, string outputPath)
        {
            try
            {
                byte[] inputBytes = File.ReadAllBytes(inputPath);
                Dictionary<string, int> dicionario = InicializarDicionario();
                List<int> codigos = new List<int>();
                string sequenciaAtual = "";

                foreach (byte b in inputBytes)
                {
                    string novaSequencia = sequenciaAtual + (char)b;
                    if (dicionario.ContainsKey(novaSequencia))
                    {
                        sequenciaAtual = novaSequencia;
                    }
                    else
                    {
                        codigos.Add(dicionario[sequenciaAtual]);
                        if (dicionario.Count < MaxCodigo)
                        {
                            dicionario.Add(novaSequencia, dicionario.Count);
                        }
                        sequenciaAtual = "" + (char)b;
                    }
                }

                if (!string.IsNullOrEmpty(sequenciaAtual))
                {
                    codigos.Add(dicionario[sequenciaAtual]);
                }

                using (var fs = new FileStream(outputPath, FileMode.Create))
                using (var writer = new BinaryWriter(fs))
                {
                    writer.Write(codigos.Count);
                    WriteCodigos(codigos, writer);
                }

                Console.WriteLine($"Arquivo comprimido com LZW: {inputPath} -> {outputPath}");
                Console.WriteLine($"Tamanho original: {inputBytes.Length} bytes, Tamanho comprimido: {new FileInfo(outputPath).Length} bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao comprimir com LZW ({inputPath}): {ex.Message}");
                throw;
            }
        }

        public void Decompress(string inputPath, string outputPath)
        {
            try
            {
                List<int> codigos;
                using (var fs = new FileStream(inputPath, FileMode.Open))
                using (var reader = new BinaryReader(fs))
                {
                    int count = reader.ReadInt32();
                    codigos = ReadCodigos(reader, count);
                }

                Dictionary<int, string> dicionario = InicializarDicionarioInverso();
                List<byte> outputBytes = new List<byte>();
                string sequenciaAtual = dicionario[codigos[0]];
                outputBytes.AddRange(sequenciaAtual.Select(c => (byte)c));

                for (int i = 1; i < codigos.Count; i++)
                {
                    int codigo = codigos[i];
                    string entrada;
                    if (dicionario.ContainsKey(codigo))
                    {
                        entrada = dicionario[codigo];
                    }
                    else
                    {
                        entrada = sequenciaAtual + sequenciaAtual[0];
                    }

                    outputBytes.AddRange(entrada.Select(c => (byte)c));
                    if (dicionario.Count < MaxCodigo)
                    {
                        dicionario.Add(dicionario.Count, sequenciaAtual + entrada[0]);
                    }
                    sequenciaAtual = entrada;
                }

                File.WriteAllBytes(outputPath, outputBytes.ToArray());
                Console.WriteLine($"Arquivo descomprimido com LZW: {inputPath} -> {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao descomprimir com LZW ({inputPath}): {ex.Message}");
                throw;
            }
        }

        private Dictionary<string, int> InicializarDicionario()
        {
            var dicionario = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
            {
                dicionario.Add(((char)i).ToString(), i);
            }
            return dicionario;
        }

        private Dictionary<int, string> InicializarDicionarioInverso()
        {
            var dicionario = new Dictionary<int, string>();
            for (int i = 0; i < 256; i++)
            {
                dicionario.Add(i, ((char)i).ToString());
            }
            return dicionario;
        }

        private void WriteCodigos(List<int> codigos, BinaryWriter writer)
        {
            byte[] buffer = new byte[codigos.Count * 2];
            int index = 0;
            foreach (int codigo in codigos)
            {
                buffer[index++] = (byte)(codigo >> 4);
                buffer[index++] = (byte)(codigo & 0xFF);
            }
            writer.Write(buffer);
        }

        private List<int> ReadCodigos(BinaryReader reader, int count)
        {
            List<int> codigos = new List<int>();
            byte[] buffer = reader.ReadBytes(count * 2);
            for (int i = 0; i < buffer.Length; i += 2)
            {
                int codigo = (buffer[i] << 4) | buffer[i + 1];
                codigos.Add(codigo);
            }
            return codigos;
        }
    }
}