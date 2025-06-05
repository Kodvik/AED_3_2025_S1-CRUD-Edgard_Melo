using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Utilities
{
    public class HuffmanCompressor
    {
        private class NoHuffman
        {
            public byte? Valor { get; set; }
            public long Frequencia { get; set; }
            public NoHuffman Esquerda { get; set; }
            public NoHuffman Direita { get; set; }
        }

        public void Compress(string inputPath, string outputPath)
        {
            try
            {
                byte[] inputBytes = File.ReadAllBytes(inputPath);
                if (inputBytes.Length == 0)
                {
                    File.WriteAllBytes(outputPath, new byte[0]);
                    Console.WriteLine($"Arquivo vazio comprimido: {inputPath} -> {outputPath}");
                    return;
                }

                // Calcular frequências
                long[] frequencias = new long[256];
                foreach (byte b in inputBytes)
                {
                    frequencias[b]++;
                }

                // Logar bytes com frequência > 0
                Console.WriteLine("Frequências dos bytes:");
                for (int i = 0; i < 256; i++)
                {
                    if (frequencias[i] > 0)
                        Console.WriteLine($"Byte {i}: {frequencias[i]} ocorrências");
                }

                // Construir árvore de Huffman
                NoHuffman raiz = ConstruirArvoreHuffman(frequencias);
                Dictionary<byte, string> codigos = GerarCodigosHuffman(raiz);

                // Logar códigos gerados
                Console.WriteLine("Códigos Huffman gerados:");
                foreach (var kvp in codigos)
                {
                    Console.WriteLine($"Byte {kvp.Key}: {kvp.Value}");
                }

                // Comprimir dados
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    // Serializar árvore
                    SerializarArvore(raiz, writer);
                    // Escrever tamanho dos dados originais
                    writer.Write((long)inputBytes.Length);
                    // Comprimir dados
                    string bits = string.Concat(inputBytes.Select(b =>
                    {
                        if (!codigos.ContainsKey(b))
                        {
                            throw new KeyNotFoundException($"Byte {b} não encontrado no dicionário de códigos.");
                        }
                        return codigos[b];
                    }));
                    byte[] compressedBytes = ConverterBitsParaBytes(bits);
                    writer.Write(compressedBytes);

                    File.WriteAllBytes(outputPath, ms.ToArray());
                }

                Console.WriteLine($"Arquivo comprimido com Huffman: {inputPath} -> {outputPath}");
                Console.WriteLine($"Tamanho original: {inputBytes.Length} bytes, Tamanho comprimido: {new FileInfo(outputPath).Length} bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao comprimir com Huffman ({inputPath}): {ex.Message}");
                throw;
            }
        }

        public void Decompress(string inputPath, string outputPath)
        {
            try
            {
                using (var fs = new FileStream(inputPath, FileMode.Open))
                using (var reader = new BinaryReader(fs))
                {
                    // Ler árvore
                    NoHuffman raiz = DesserializarArvore(reader);
                    // Ler tamanho dos dados originais
                    long tamanhoOriginal = reader.ReadInt64();
                    // Ler dados comprimidos
                    byte[] compressedBytes = reader.ReadBytes((int)(fs.Length - fs.Position));
                    string bits = ConverterBytesParaBits(compressedBytes);

                    // Descomprimir dados
                    List<byte> outputBytes = new List<byte>();
                    NoHuffman atual = raiz;
                    foreach (char bit in bits)
                    {
                        atual = bit == '0' ? atual.Esquerda : atual.Direita;
                        if (atual.Valor.HasValue)
                        {
                            outputBytes.Add(atual.Valor.Value);
                            atual = raiz;
                            if (outputBytes.Count == tamanhoOriginal)
                                break;
                        }
                    }

                    File.WriteAllBytes(outputPath, outputBytes.ToArray());
                    Console.WriteLine($"Arquivo descomprimido com Huffman: {inputPath} -> {outputPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao descomprimir com Huffman ({inputPath}): {ex.Message}");
                throw;
            }
        }

        private NoHuffman ConstruirArvoreHuffman(long[] frequencias)
        {
            var heap = new PriorityQueue<NoHuffman, long>();
            for (int i = 0; i < 256; i++)
            {
                if (frequencias[i] > 0)
                {
                    heap.Enqueue(new NoHuffman { Valor = (byte)i, Frequencia = frequencias[i] }, frequencias[i]);
                }
            }

            if (heap.Count == 0)
            {
                throw new InvalidOperationException("Nenhum byte com frequência maior que zero.");
            }

            while (heap.Count > 1)
            {
                heap.TryDequeue(out var esq, out var freqEsq);
                heap.TryDequeue(out var dir, out var freqDir);

                var pai = new NoHuffman
                {
                    Frequencia = freqEsq + freqDir,
                    Esquerda = esq,
                    Direita = dir
                };

                heap.Enqueue(pai, pai.Frequencia);
            }

            heap.TryDequeue(out var raiz, out _);
            return raiz;
        }

        private Dictionary<byte, string> GerarCodigosHuffman(NoHuffman raiz)
        {
            var codigos = new Dictionary<byte, string>();
            if (raiz.Valor.HasValue)
            {
                // Caso especial: apenas um byte no arquivo
                codigos[raiz.Valor.Value] = "0";
            }
            else
            {
                GerarCodigosHuffmanRecursivo(raiz, "", codigos);
            }
            return codigos;
        }

        private void GerarCodigosHuffmanRecursivo(NoHuffman no, string codigo, Dictionary<byte, string> codigos)
        {
            if (no.Valor.HasValue)
            {
                codigos[no.Valor.Value] = codigo.Length > 0 ? codigo : "0";
                return;
            }
            if (no.Esquerda != null)
                GerarCodigosHuffmanRecursivo(no.Esquerda, codigo + "0", codigos);
            if (no.Direita != null)
                GerarCodigosHuffmanRecursivo(no.Direita, codigo + "1", codigos);
        }

        private void SerializarArvore(NoHuffman no, BinaryWriter writer)
        {
            if (no.Valor.HasValue)
            {
                writer.Write((byte)1);
                writer.Write(no.Valor.Value);
            }
            else
            {
                writer.Write((byte)0);
                SerializarArvore(no.Esquerda, writer);
                SerializarArvore(no.Direita, writer);
            }
        }

        private NoHuffman DesserializarArvore(BinaryReader reader)
        {
            byte isFolha = reader.ReadByte();
            if (isFolha == 1)
            {
                return new NoHuffman { Valor = reader.ReadByte() };
            }
            var no = new NoHuffman
            {
                Esquerda = DesserializarArvore(reader),
                Direita = DesserializarArvore(reader)
            };
            return no;
        }

        private byte[] ConverterBitsParaBytes(string bits)
        {
            int padding = (8 - (bits.Length % 8)) % 8;
            bits += new string('0', padding);
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < bits.Length; i += 8)
            {
                string byteStr = bits.Substring(i, Math.Min(8, bits.Length - i));
                bytes.Add(Convert.ToByte(byteStr, 2));
            }
            return bytes.ToArray();
        }

        private string ConverterBytesParaBits(byte[] bytes)
        {
            return string.Concat(bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        }

        // Implementação de PriorityQueue para .NET 8
        private class PriorityQueue<TElement, TPriority>
        {
            private readonly List<(TElement Element, TPriority Priority)> _elements = new List<(TElement, TPriority)>();
            private readonly IComparer<TPriority> _comparer;

            public PriorityQueue()
            {
                _comparer = Comparer<TPriority>.Default;
            }

            public int Count => _elements.Count;

            public void Enqueue(TElement element, TPriority priority)
            {
                _elements.Add((element, priority));
                int i = _elements.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_comparer.Compare(_elements[parent].Priority, _elements[i].Priority) <= 0)
                        break;
                    (_elements[i], _elements[parent]) = (_elements[parent], _elements[i]);
                    i = parent;
                }
            }

            public bool TryDequeue(out TElement element, out TPriority priority)
            {
                if (_elements.Count == 0)
                {
                    element = default;
                    priority = default;
                    return false;
                }

                element = _elements[0].Element;
                priority = _elements[0].Priority;
                _elements[0] = _elements[^1];
                _elements.RemoveAt(_elements.Count - 1);

                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;
                    int smallest = i;

                    if (left < _elements.Count && _comparer.Compare(_elements[left].Priority, _elements[smallest].Priority) < 0)
                        smallest = left;
                    if (right < _elements.Count && _comparer.Compare(_elements[right].Priority, _elements[smallest].Priority) < 0)
                        smallest = right;

                    if (smallest == i)
                        break;

                    (_elements[i], _elements[smallest]) = (_elements[smallest], _elements[i]);
                    i = smallest;
                }

                return true;
            }
        }
    }
}