using System;
using System.Collections.Generic;
using System.IO;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Indexing
{
    public class HashingEstendido
    {
        private readonly string caminhoArquivoIndices;
        private int tamanhoTabela;
        private int profundidadeGlobal;
        private readonly int registrosPorBucket = 4;

        public HashingEstendido(string basePath)
        {
            try
            {
                string dataPath = Path.Combine(basePath, "Data");
                Directory.CreateDirectory(dataPath);
                this.caminhoArquivoIndices = Path.Combine(dataPath, "indices_hash.bin");
                tamanhoTabela = 1024;
                profundidadeGlobal = 10;
                InicializarArquivo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar HashingEstendido: {ex.Message}");
                throw;
            }
        }

        private void InicializarArquivo()
        {
            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Create, FileAccess.Write))
                {
                    // Escrever profundidade global
                    byte[] profundidadeBytes = BitConverter.GetBytes(profundidadeGlobal);
                    fs.Write(profundidadeBytes, 0, profundidadeBytes.Length);

                    // Escrever buckets iniciais (2^profundidadeGlobal buckets)
                    int numeroBuckets = 1 << profundidadeGlobal;
                    for (int i = 0; i < numeroBuckets; i++)
                    {
                        Bucket bucket = new Bucket { ProfundidadeLocal = profundidadeGlobal };
                        byte[] bucketBytes = SerializarBucket(bucket);
                        fs.Write(bucketBytes, 0, bucketBytes.Length);
                    }
                    Console.WriteLine($"Arquivo {caminhoArquivoIndices} criado com {numeroBuckets} buckets iniciais.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar arquivo {caminhoArquivoIndices}: {ex.Message}");
                throw;
            }
        }

        public void Inserir(int id, long posicaoRegistro)
        {
            try
            {
                Console.WriteLine($"Inserindo no Hash (ID: {id}, Posição: {posicaoRegistro})");
                int indice = id % tamanhoTabela;
                Bucket bucket = LerBucket(indice);
                if (bucket.Registros.Count < registrosPorBucket)
                {
                    bucket.Registros.Add((id, posicaoRegistro));
                    EscreverBucket(indice, bucket);
                }
                else
                {
                    DividirBucket(indice, id, posicaoRegistro);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inserir no Hash: {ex.Message}");
                throw;
            }
        }

        private void DividirBucket(int indice, int id, long posicaoRegistro)
        {
            try
            {
                Bucket bucket = LerBucket(indice);
                bucket.ProfundidadeLocal++;
                if (bucket.ProfundidadeLocal > profundidadeGlobal)
                {
                    DobrarTabela();
                    indice = id % tamanhoTabela;
                }

                Bucket novoBucket = new Bucket { ProfundidadeLocal = bucket.ProfundidadeLocal };
                List<(int, long)> registros = new List<(int, long)>(bucket.Registros) { (id, posicaoRegistro) };
                bucket.Registros.Clear();

                foreach (var reg in registros)
                {
                    int novoIndice = reg.Item1 % (1 << bucket.ProfundidadeLocal);
                    if (novoIndice == indice % (1 << bucket.ProfundidadeLocal))
                        bucket.Registros.Add(reg);
                    else
                        novoBucket.Registros.Add(reg);
                }

                EscreverBucket(indice, bucket);
                EscreverBucket(indice + (1 << (bucket.ProfundidadeLocal - 1)), novoBucket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao dividir bucket (Indice: {indice}): {ex.Message}");
                throw;
            }
        }

        private void DobrarTabela()
        {
            try
            {
                profundidadeGlobal++;
                int novoTamanho = tamanhoTabela * 2;
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(BitConverter.GetBytes(novoTamanho), 0, 4);
                    fs.Write(BitConverter.GetBytes(profundidadeGlobal), 0, 4);

                    fs.Seek(0, SeekOrigin.End);
                    for (int i = tamanhoTabela; i < novoTamanho; i++)
                    {
                        EscreverBucket(fs, LerBucket(i % tamanhoTabela));
                    }
                }
                tamanhoTabela = novoTamanho;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao dobrar tabela hash: {ex.Message}");
                throw;
            }
        }

        public long Buscar(int id)
        {
            try
            {
                int indice = id % tamanhoTabela;
                Bucket bucket = LerBucket(indice);
                foreach (var reg in bucket.Registros)
                {
                    if (reg.Item1 == id)
                        return reg.Item2;
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar ID {id} no Hash: {ex.Message}");
                return -1;
            }
        }

        private Bucket LerBucket(int indice)
        {
            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open))
                {
                    fs.Seek(8 + indice * (4 + 16 * registrosPorBucket), SeekOrigin.Begin);
                    byte[] buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    int profundidadeLocal = BitConverter.ToInt32(buffer, 0);

                    Bucket bucket = new Bucket { ProfundidadeLocal = profundidadeLocal };
                    for (int i = 0; i < registrosPorBucket; i++)
                    {
                        buffer = new byte[4];
                        fs.Read(buffer, 0, 4);
                        int id = BitConverter.ToInt32(buffer, 0);
                        buffer = new byte[8];
                        fs.Read(buffer, 0, 8);
                        long posicao = BitConverter.ToInt64(buffer, 0);
                        if (id != 0)
                            bucket.Registros.Add((id, posicao));
                    }
                    return bucket;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler bucket (Indice: {indice}): {ex.Message}");
                throw;
            }
        }

        private void EscreverBucket(int indice, Bucket bucket)
        {
            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open))
                {
                    fs.Seek(8 + indice * (4 + 16 * registrosPorBucket), SeekOrigin.Begin);
                    fs.Write(BitConverter.GetBytes(bucket.ProfundidadeLocal), 0, 4);
                    for (int i = 0; i < registrosPorBucket; i++)
                    {
                        if (i < bucket.Registros.Count)
                        {
                            fs.Write(BitConverter.GetBytes(bucket.Registros[i].Item1), 0, 4);
                            fs.Write(BitConverter.GetBytes(bucket.Registros[i].Item2), 0, 8);
                        }
                        else
                        {
                            fs.Write(BitConverter.GetBytes(0), 0, 4);
                            fs.Write(BitConverter.GetBytes(0L), 0, 8);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever bucket (Indice: {indice}): {ex.Message}");
                throw;
            }
        }

        private byte[] SerializarBucket(Bucket bucket)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    // Escrever profundidade local
                    ms.Write(BitConverter.GetBytes(bucket.ProfundidadeLocal), 0, 4);

                    // Escrever registros
                    for (int i = 0; i < registrosPorBucket; i++)
                    {
                        if (i < bucket.Registros.Count)
                        {
                            ms.Write(BitConverter.GetBytes(bucket.Registros[i].id), 0, 4);
                            ms.Write(BitConverter.GetBytes(bucket.Registros[i].posicao), 0, 8);
                        }
                        else
                        {
                            ms.Write(BitConverter.GetBytes(0), 0, 4);
                            ms.Write(BitConverter.GetBytes(0L), 0, 8);
                        }
                    }

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao serializar bucket: {ex.Message}");
                throw;
            }
        }

        private void EscreverBucket(FileStream fs, Bucket bucket)
        {
            try
            {
                fs.Write(BitConverter.GetBytes(bucket.ProfundidadeLocal), 0, 4);
                for (int i = 0; i < registrosPorBucket; i++)
                {
                    if (i < bucket.Registros.Count)
                    {
                        fs.Write(BitConverter.GetBytes(bucket.Registros[i].Item1), 0, 4);
                        fs.Write(BitConverter.GetBytes(bucket.Registros[i].Item2), 0, 8);
                    }
                    else
                    {
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                        fs.Write(BitConverter.GetBytes(0L), 0, 8);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever bucket no arquivo: {ex.Message}");
                throw;
            }
        }
    }

    public class Bucket
    {
        public int ProfundidadeLocal { get; set; }
        public List<(int id, long posicao)> Registros { get; set; } = new List<(int, long)>();
    }
}