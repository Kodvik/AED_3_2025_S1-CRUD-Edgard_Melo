using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression
{
    public class HuffmanCompressor : ICompressor
    {
        private class HuffmanNode
        {
            public byte Data { get; set; }
            public long Frequency { get; set; }
            public HuffmanNode Left { get; set; }
            public HuffmanNode Right { get; set; }
            public bool IsLeaf => Left == null && Right == null;
        }

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

            var frequencyTable = BuildFrequencyTable(inputData);
            var huffmanTree = BuildHuffmanTree(frequencyTable);
            var huffmanCodes = BuildHuffmanCodes(huffmanTree);

            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(outputStream))
            {
                WriteHeader(writer, frequencyTable);
                WriteCompressedData(writer, inputData, huffmanCodes);
            }
        }

        public void Decompress(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputPath);

            using (var inputStream = new FileStream(inputPath, FileMode.Open))
            using (var reader = new BinaryReader(inputStream))
            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            {
                var frequencyTable = ReadHeader(reader);
                var huffmanTree = BuildHuffmanTree(frequencyTable);
                DecodeData(reader, outputStream, huffmanTree, inputStream.Length);
            }
        }

        private Dictionary<byte, long> BuildFrequencyTable(byte[] data)
        {
            var frequencyTable = new Dictionary<byte, long>();
            foreach (byte b in data)
            {
                if (frequencyTable.ContainsKey(b))
                    frequencyTable[b]++;
                else
                    frequencyTable[b] = 1;
            }
            return frequencyTable;
        }

        private HuffmanNode BuildHuffmanTree(Dictionary<byte, long> frequencyTable)
        {
            var nodes = frequencyTable.Select(kvp => new HuffmanNode
            {
                Data = kvp.Key,
                Frequency = kvp.Value
            }).ToList();

            while (nodes.Count > 1)
            {
                nodes = nodes.OrderBy(n => n.Frequency).ToList();
                var left = nodes[0];
                var right = nodes[1];
                var parent = new HuffmanNode
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);
                nodes.Add(parent);
            }

            return nodes.FirstOrDefault();
        }

        private Dictionary<byte, string> BuildHuffmanCodes(HuffmanNode root)
        {
            var huffmanCodes = new Dictionary<byte, string>();
            BuildHuffmanCodesRecursive(root, "", huffmanCodes);
            return huffmanCodes;
        }

        private void BuildHuffmanCodesRecursive(HuffmanNode node, string code, Dictionary<byte, string> huffmanCodes)
        {
            if (node == null)
                return;

            if (node.IsLeaf)
            {
                huffmanCodes[node.Data] = code.Length > 0 ? code : "0";
            }
            else
            {
                BuildHuffmanCodesRecursive(node.Left, code + "0", huffmanCodes);
                BuildHuffmanCodesRecursive(node.Right, code + "1", huffmanCodes);
            }
        }

        private void WriteHeader(BinaryWriter writer, Dictionary<byte, long> frequencyTable)
        {
            writer.Write(frequencyTable.Count);
            foreach (var kvp in frequencyTable)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        private Dictionary<byte, long> ReadHeader(BinaryReader reader)
        {
            var frequencyTable = new Dictionary<byte, long>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                byte data = reader.ReadByte();
                long frequency = reader.ReadInt64();
                frequencyTable[data] = frequency;
            }
            return frequencyTable;
        }

        private void WriteCompressedData(BinaryWriter writer, byte[] inputData, Dictionary<byte, string> huffmanCodes)
        {
            byte currentByte = 0;
            int bitCount = 0;

            foreach (byte b in inputData)
            {
                string code = huffmanCodes[b];
                foreach (char bit in code)
                {
                    currentByte = (byte)((currentByte << 1) | (bit == '1' ? 1 : 0));
                    bitCount++;
                    if (bitCount == 8)
                    {
                        writer.Write(currentByte);
                        currentByte = 0;
                        bitCount = 0;
                    }
                }
            }

            if (bitCount > 0)
            {
                currentByte <<= (8 - bitCount);
                writer.Write(currentByte);
            }
        }

        private void DecodeData(BinaryReader reader, FileStream outputStream, HuffmanNode root, long inputLength)
        {
            HuffmanNode currentNode = root;
            long bytesRead = 4 + GetHeaderSize(root);

            while (bytesRead < inputLength)
            {
                byte b = reader.ReadByte();
                bytesRead++;

                for (int i = 7; i >= 0 && bytesRead <= inputLength; i--)
                {
                    int bit = (b >> i) & 1;
                    currentNode = bit == 0 ? currentNode.Left : currentNode.Right;

                    if (currentNode.IsLeaf)
                    {
                        outputStream.WriteByte(currentNode.Data);
                        currentNode = root;
                    }
                }
            }
        }

        private int GetHeaderSize(HuffmanNode root)
        {
            int count = 0;
            CountLeaves(root, ref count);
            return 4 + count * (1 + 8);
        }

        private void CountLeaves(HuffmanNode node, ref int count)
        {
            if (node == null)
                return;
            if (node.IsLeaf)
                count++;
            CountLeaves(node.Left, ref count);
            CountLeaves(node.Right, ref count);
        }
    }
}