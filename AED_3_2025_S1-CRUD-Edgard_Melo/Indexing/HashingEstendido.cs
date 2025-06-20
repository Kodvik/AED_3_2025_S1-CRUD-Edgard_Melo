using System;
using System.Collections.Generic;
using System.IO;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Indexing
{
    public class HashingEstendido
    {
        private readonly string caminhoArquivo;
        private readonly Dictionary<int, long> tabelaHash;
        private readonly int tamanhoTabela;

        public HashingEstendido(string basePath)
        {
            caminhoArquivo = Path.Combine(basePath, "indices_hash.bin");
            tamanhoTabela = 1000; // Tamanho inicial da tabela
            tabelaHash = new Dictionary<int, long>();
            CarregarIndices();
        }

        public void Inserir(int chave, long posicao)
        {
            int hash = Math.Abs(chave % tamanhoTabela);
            if (tabelaHash.ContainsKey(hash))
            {
                // Resolução de colisão simples (encadeamento linear)
                int i = 1;
                while (tabelaHash.ContainsKey(hash + i))
                    i++;
                hash += i;
            }
            tabelaHash[hash] = posicao;
            SalvarIndices();
        }

        public void Remover(int chave)
        {
            int hash = Math.Abs(chave % tamanhoTabela);
            if (tabelaHash.ContainsKey(hash) && tabelaHash[hash] == chave)
            {
                tabelaHash.Remove(hash);
                SalvarIndices();
            }
            else
            {
                // Busca linear para colisões
                int i = 1;
                while (tabelaHash.ContainsKey(hash + i))
                {
                    if (tabelaHash[hash + i] == chave)
                    {
                        tabelaHash.Remove(hash + i);
                        SalvarIndices();
                        break;
                    }
                    i++;
                }
            }
        }

        public long Buscar(int chave)
        {
            int hash = Math.Abs(chave % tamanhoTabela);
            if (tabelaHash.ContainsKey(hash) && tabelaHash[hash] == chave)
                return tabelaHash[hash];

            int i = 1;
            while (tabelaHash.ContainsKey(hash + i))
            {
                if (tabelaHash[hash + i] == chave)
                    return tabelaHash[hash + i];
                i++;
            }
            return -1;
        }

        private void CarregarIndices()
        {
            if (File.Exists(caminhoArquivo))
            {
                using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
                using (var reader = new BinaryReader(fs))
                {
                    while (fs.Position < fs.Length)
                    {
                        int chave = reader.ReadInt32();
                        long posicao = reader.ReadInt64();
                        Inserir(chave, posicao);
                    }
                }
            }
        }

        private void SalvarIndices()
        {
            using (var fs = new FileStream(caminhoArquivo, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                foreach (var kvp in tabelaHash)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }
        }
    }
}