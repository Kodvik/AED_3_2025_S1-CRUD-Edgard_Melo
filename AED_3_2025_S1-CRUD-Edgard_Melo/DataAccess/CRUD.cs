using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CsvHelper;
using System.Globalization;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;
using AED_3_2025_S1_CRUD_Edgard_Melo.Indexing;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataAccess
{
    public class CRUD<T> where T : IEntidade, new()
    {
        private readonly string caminhoArquivoBinario;
        private readonly string caminhoCSV;
        private readonly ArvoreBPlus arvoreBPlus;
        private readonly HashingEstendido hashingEstendido;
        private readonly object arquivoLock = new object();
        private readonly List<long> posicoesLivres = new List<long>();
        private int ultimoId;

        public CRUD(string basePath, string caminhoCSV)
        {
            try
            {
                string dataPath = Path.Combine(basePath, "Data");
                Directory.CreateDirectory(dataPath);
                this.caminhoArquivoBinario = Path.Combine(dataPath, "banco_de_dados.bin");
                this.caminhoCSV = caminhoCSV;
                arvoreBPlus = new ArvoreBPlus(basePath, 4);
                hashingEstendido = new HashingEstendido(basePath);
                InicializarArquivo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar CRUD: {ex.Message}");
                throw;
            }
        }

        public int getUltimoID() => ultimoId;

        private void InicializarArquivo()
        {
            try
            {
                if (!File.Exists(caminhoArquivoBinario))
                {
                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Create))
                    {
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                    }
                }
                else
                {
                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                    {
                        byte[] buffer = new byte[4];
                        fs.Read(buffer, 0, 4);
                        ultimoId = BitConverter.ToInt32(buffer, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar arquivo binário: {ex.Message}");
                throw;
            }
        }

        public void Criar(T entidade)
        {
            if (entidade == null)
                throw new ArgumentNullException(nameof(entidade));

            var entidadeComId = entidade as IEntidade;
            if (entidadeComId == null)
                throw new ArgumentException("Entidade deve implementar IEntidadeComId");

            lock (arquivoLock) 
            {
                try
                {
                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        long posicao;
                        if (posicoesLivres.Any())
                        {
                            posicao = posicoesLivres.First();
                            posicoesLivres.Remove(posicao);
                        }
                        else
                        {
                            posicao = fs.Length;
                        }

                        byte[] dados = Serializar(entidade);
                        fs.Position = posicao;
                        fs.Write(dados, 0, dados.Length);

                        arvoreBPlus.Inserir(entidadeComId.UID, posicao);
                        hashingEstendido.Inserir(entidadeComId.UID, posicao);

                        if (entidadeComId.UID > ultimoId)
                        {
                            ultimoId = entidadeComId.UID;
                            byte[] cabecalho = BitConverter.GetBytes(ultimoId);
                            fs.Position = 0;
                            fs.Write(cabecalho, 0, cabecalho.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao criar entidade (UID: {entidadeComId.UID}): {ex.Message}");
                    throw;
                }
            }
        }

        public void ReinicializarUltimoId(int novoUltimoId)
        {
            ultimoId = novoUltimoId;
        }

        public T Ler(int id)
        {
            try
            {
                long posicao = arvoreBPlus.Buscar(id);
                if (posicao == -1)
                    return default;

                using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                {
                    fs.Seek(posicao, SeekOrigin.Begin);
                    int lapide = fs.ReadByte();
                    byte[] tamanhoBuffer = new byte[4];
                    fs.Read(tamanhoBuffer, 0, 4);
                    int tamanhoRegistro = BitConverter.ToInt32(tamanhoBuffer, 0);
                    byte[] registroBuffer = new byte[tamanhoRegistro];
                    fs.Read(registroBuffer, 0, tamanhoRegistro);

                    if (lapide == 1)
                        return Desserializar(registroBuffer);
                }
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler entidade (ID: {id}): {ex.Message}");
                return default;
            }
        }

        public void Atualizar(int id, T entidadeAtualizada)
        {
            try
            {
                long posicao = arvoreBPlus.Buscar(id);
                if (posicao == -1)
                    return;

                using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                {
                    fs.Seek(posicao, SeekOrigin.Begin);
                    fs.WriteByte(0);
                }

                entidadeAtualizada.UID = id;
                byte[] entidadeBytes = Serializar(entidadeAtualizada);
                byte[] tamanhoRegistro = BitConverter.GetBytes(entidadeBytes.Length);

                long novaPosicao;
                using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Append))
                {
                    novaPosicao = fs.Position;
                    fs.WriteByte(1);
                    fs.Write(tamanhoRegistro, 0, tamanhoRegistro.Length);
                    fs.Write(entidadeBytes, 0, entidadeBytes.Length);
                }

                arvoreBPlus.Inserir(id, novaPosicao);
                hashingEstendido.Inserir(id, novaPosicao);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar entidade (ID: {id}): {ex.Message}");
            }
        }

        public void Deletar(int id)
        {
            try
            {
                long posicao = arvoreBPlus.Buscar(id);
                if (posicao == -1)
                    return;

                using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                {
                    fs.Seek(posicao, SeekOrigin.Begin);
                    fs.WriteByte(0);
                }

                arvoreBPlus.Remover(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar entidade (ID: {id}): {ex.Message}");
            }
        }

        private void AtualizarCabecalho()
        {
            try
            {
                using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                {
                    fs.Write(BitConverter.GetBytes(ultimoId), 0, 4);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar cabeçalho: {ex.Message}");
                throw;
            }
        }

        private byte[] Serializar(T entidade)
        {
            return JsonSerializer.SerializeToUtf8Bytes(entidade);
        }

        private T Desserializar(byte[] dados)
        {
            return JsonSerializer.Deserialize<T>(dados);
        }

        public void ExportarParaCSV(string caminhoCSV)
        {
            try
            {
                var registros = new List<T>();
                for (int i = 1; i <= ultimoId; i++)
                {
                    var registro = Ler(i);
                    if (registro != null)
                        registros.Add(registro);
                }

                using (var writer = new StreamWriter(caminhoCSV))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(registros);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao exportar para CSV: {ex.Message}");
            }
        }
    }
}