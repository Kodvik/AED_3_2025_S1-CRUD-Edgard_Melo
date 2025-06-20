using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;
using AED_3_2025_S1_CRUD_Edgard_Melo.Indexing;

namespace AED_3_2025_S1_CRUD_Edgard_Melo
{
    public class CRUD<T> where T : class, IEntidade
    {
        private readonly string caminhoArquivoBinario;
        private readonly string caminhoArquivoUIDs;
        private readonly ArvoreBPlus arvoreBPlus;
        private readonly HashingEstendido hashingEstendido;
        private readonly object arquivoLock = new object();
        private readonly bool criptografiaAtivada;
        private readonly byte[] aesKey;
        private readonly byte[] aesIV;
        private readonly Dictionary<int, long> uidPositions = new Dictionary<int, long>();
        private readonly List<long> posicoesLivres = new List<long>();
        private int ultimoId;

        public int UltimoId => ultimoId;

        public CRUD(string basePath, bool criptografiaAtivada = false)
        {
            caminhoArquivoBinario = Path.Combine(basePath, "banco_de_dados.bin");
            caminhoArquivoUIDs = Path.Combine(basePath, "encrypted_uids.bin");
            arvoreBPlus = new ArvoreBPlus(basePath, 4);
            hashingEstendido = new HashingEstendido(basePath);
            this.criptografiaAtivada = criptografiaAtivada;

            // Inicializar chaves AES
            aesKey = Encoding.UTF8.GetBytes("ChaveAleatoriaCom32bytes12345678"); // 32 bytes para AES-256
            aesIV = Encoding.UTF8.GetBytes("ChaveAESde16byte"); // 16 bytes

            // Validar tamanhos das chaves
            if (criptografiaAtivada)
            {
                if (aesKey.Length != 16 && aesKey.Length != 24 && aesKey.Length != 32)
                    throw new ArgumentException($"Tamanho da chave AES inválido: {aesKey.Length} bytes. Deve ser 16, 24 ou 32 bytes.");
                if (aesIV.Length != 16)
                    throw new ArgumentException($"Tamanho do IV AES inválido: {aesIV.Length} bytes. Deve ser 16 bytes.");
            }

            CarregarDadosExistentes();
        }

        public void Criar(T entidade)
        {
            var stopwatch = Stopwatch.StartNew();
            if (entidade == null)
                throw new ArgumentNullException(nameof(entidade));

            var entidadeComId = entidade as IEntidade;
            if (entidadeComId == null)
                throw new ArgumentException("Entidade deve implementar IEntidade");

            lock (arquivoLock)
            {
                try
                {
                    entidadeComId.UID = ++ultimoId;

                    using (var fsData = new FileStream(caminhoArquivoBinario, FileMode.OpenOrCreate))
                    using (var fsUIDs = new FileStream(caminhoArquivoUIDs, FileMode.OpenOrCreate))
                    {
                        long posicao = posicoesLivres.Any() ? posicoesLivres.First() : fsData.Length;
                        if (posicoesLivres.Any())
                            posicoesLivres.Remove(posicao);

                        byte[] dados = Serializar(entidade);
                        byte[] dadosEncrypted = criptografiaAtivada ? EncryptAES(dados) : dados;
                        byte[] uidEncrypted = criptografiaAtivada ? EncryptRSA(entidadeComId.UID) : BitConverter.GetBytes(entidadeComId.UID);

                        fsData.Position = posicao;
                        fsData.WriteByte(1);
                        fsData.Write(BitConverter.GetBytes(dadosEncrypted.Length), 0, 4);
                        fsData.Write(dadosEncrypted, 0, dadosEncrypted.Length);

                        fsUIDs.Seek(0, SeekOrigin.Begin);
                        fsUIDs.Write(BitConverter.GetBytes(ultimoId), 0, 4);
                        fsUIDs.Seek(0, SeekOrigin.End);
                        long uidPosition = fsUIDs.Position;
                        fsUIDs.Write(BitConverter.GetBytes(uidEncrypted.Length), 0, 4);
                        fsUIDs.Write(uidEncrypted, 0, uidEncrypted.Length);
                        uidPositions[entidadeComId.UID] = uidPosition;

                        arvoreBPlus.Inserir(entidadeComId.UID, posicao);
                        hashingEstendido.Inserir(entidadeComId.UID, posicao);

                        fsData.Position = 0;
                        fsData.Write(BitConverter.GetBytes(ultimoId), 0, 4);
                    }
                    Console.WriteLine($"Criação de UID {entidadeComId.UID} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"Erro de criptografia ao criar entidade com UID {entidadeComId.UID}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao criar entidade com UID {entidadeComId.UID}: {ex.Message}");
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
        }

        public void CriarComUID(T entidade, int uid)
        {
            var stopwatch = Stopwatch.StartNew();
            if (entidade == null)
                throw new ArgumentNullException(nameof(entidade));
            if (uid < 0)
                throw new ArgumentException("UID não pode ser negativo.", nameof(uid));

            var entidadeComId = entidade as IEntidade;
            if (entidadeComId == null)
                throw new ArgumentException("Entidade deve implementar IEntidade");

            lock (arquivoLock)
            {
                try
                {
                    entidadeComId.UID = uid;
                    if (uid > ultimoId)
                        ultimoId = uid;

                    using (var fsData = new FileStream(caminhoArquivoBinario, FileMode.OpenOrCreate))
                    using (var fsUIDs = new FileStream(caminhoArquivoUIDs, FileMode.OpenOrCreate))
                    {
                        long posicao = posicoesLivres.Any() ? posicoesLivres.First() : fsData.Length;
                        if (posicoesLivres.Any())
                            posicoesLivres.Remove(posicao);

                        byte[] dados = Serializar(entidade);
                        byte[] dadosEncrypted = criptografiaAtivada ? EncryptAES(dados) : dados;
                        byte[] uidEncrypted = criptografiaAtivada ? EncryptRSA(entidadeComId.UID) : BitConverter.GetBytes(entidadeComId.UID);

                        fsData.Position = posicao;
                        fsData.WriteByte(1);
                        fsData.Write(BitConverter.GetBytes(dadosEncrypted.Length), 0, 4);
                        fsData.Write(dadosEncrypted, 0, dadosEncrypted.Length);

                        fsUIDs.Seek(0, SeekOrigin.Begin);
                        fsUIDs.Write(BitConverter.GetBytes(ultimoId), 0, 4);
                        fsUIDs.Seek(0, SeekOrigin.End);
                        long uidPosition = fsUIDs.Position;
                        fsUIDs.Write(BitConverter.GetBytes(uidEncrypted.Length), 0, 4);
                        fsUIDs.Write(uidEncrypted, 0, uidEncrypted.Length);
                        uidPositions[entidadeComId.UID] = uidPosition;

                        arvoreBPlus.Inserir(entidadeComId.UID, posicao);
                        hashingEstendido.Inserir(entidadeComId.UID, posicao);

                        fsData.Position = 0;
                        fsData.Write(BitConverter.GetBytes(ultimoId), 0, 4);
                    }
                    Console.WriteLine($"Criação de UID {entidadeComId.UID} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"Erro de criptografia ao criar entidade com UID {uid}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao criar entidade com UID {uid}: {ex.Message}");
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
        }

        public T Ler(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            lock (arquivoLock)
            {
                try
                {
                    long posicao = arvoreBPlus.Buscar(id);
                    Console.WriteLine($"Buscando UID {id} na posição {posicao}");
                    if (posicao == -1)
                    {
                        Console.WriteLine($"UID {id} não encontrado na árvore B+.");
                        return null;
                    }

                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                    {
                        fs.Seek(posicao, SeekOrigin.Begin);
                        byte lapide = (byte)fs.ReadByte();
                        Console.WriteLine($"Lápide do UID {id}: {lapide}");
                        if (lapide == 0)
                        {
                            Console.WriteLine($"UID {id} marcado como excluído.");
                            return null;
                        }

                        byte[] tamanhoBuffer = new byte[4];
                        fs.Read(tamanhoBuffer, 0, 4);
                        int tamanho = BitConverter.ToInt32(tamanhoBuffer, 0);
                        Console.WriteLine($"Tamanho dos dados do UID {id}: {tamanho}");
                        byte[] dados = new byte[tamanho];
                        fs.Read(dados, 0, tamanho);

                        byte[] dadosDecrypted = criptografiaAtivada ? DecryptAES(dados) : dados;
                        return Desserializar(dadosDecrypted);
                    }
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"Erro de criptografia ao ler UID {id}: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao ler UID {id}: {ex.Message}");
                    return null;
                }
                finally
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Leitura de UID {id} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }

        public List<T> LerConjunto(List<int> ids)
        {
            var stopwatch = Stopwatch.StartNew();
            var resultados = new List<T>();
            foreach (int id in ids)
            {
                var registro = Ler(id);
                if (registro != null)
                    resultados.Add(registro);
            }
            stopwatch.Stop();
            Console.WriteLine($"Leitura de conjunto concluída em {stopwatch.ElapsedMilliseconds}ms");
            return resultados;
        }

        public void Atualizar(T entidade)
        {
            var stopwatch = Stopwatch.StartNew();
            if (entidade == null)
                throw new ArgumentNullException(nameof(entidade));

            var entidadeComId = entidade as IEntidade;
            if (entidadeComId == null)
                throw new ArgumentException("Entidade deve implementar IEntidade");

            lock (arquivoLock)
            {
                try
                {
                    long posicao = arvoreBPlus.Buscar(entidadeComId.UID);
                    if (posicao == -1)
                        throw new Exception($"Registro com UID {entidadeComId.UID} não encontrado.");

                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                    {
                        byte[] dados = Serializar(entidade);
                        byte[] dadosEncrypted = criptografiaAtivada ? EncryptAES(dados) : dados;

                        fs.Seek(posicao, SeekOrigin.Begin);
                        fs.WriteByte(1);
                        fs.Write(BitConverter.GetBytes(dadosEncrypted.Length), 0, 4);
                        fs.Write(dadosEncrypted, 0, dadosEncrypted.Length);

                        if (criptografiaAtivada)
                        {
                            using (var fsUIDs = new FileStream(caminhoArquivoUIDs, FileMode.Open))
                            {
                                byte[] uidEncrypted = EncryptRSA(entidadeComId.UID);
                                fsUIDs.Seek(uidPositions[entidadeComId.UID], SeekOrigin.Begin);
                                fsUIDs.Write(BitConverter.GetBytes(uidEncrypted.Length), 0, 4);
                                fsUIDs.Write(uidEncrypted, 0, uidEncrypted.Length);
                            }
                        }
                    }
                    Console.WriteLine($"Atualização de UID {entidadeComId.UID} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"Erro de criptografia ao atualizar UID {entidadeComId.UID}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao atualizar UID {entidadeComId.UID}: {ex.Message}");
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
        }

        public void Deletar(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            lock (arquivoLock)
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

                    posicoesLivres.Add(posicao);
                    arvoreBPlus.Remover(id);
                    hashingEstendido.Remover(id);
                    Console.WriteLine($"UID {id} removido com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao deletar UID {id}: {ex.Message}");
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Deleção de UID {id} concluída em {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }

        public List<T> BuscarPadrao(string pattern)
        {
            var stopwatch = Stopwatch.StartNew();
            var resultados = new List<T>();
            for (int i = 0; i <= ultimoId; i++)
            {
                long posicao = arvoreBPlus.Buscar(i);
                if (posicao != -1)
                {
                    var registro = Ler(i);
                    if (registro != null)
                    {
                        var registroDeRede = registro as RegistroDeRede;
                        if (registroDeRede?.PayloadData != null && KMPSearch(registroDeRede.PayloadData, pattern))
                            resultados.Add(registro);
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Busca de padrão '{pattern}' concluída em {stopwatch.ElapsedMilliseconds}ms");
            return resultados;
        }

        public void CarregarDadosExistentes()
        {
            lock (arquivoLock)
            {
                try
                {
                    if (!File.Exists(caminhoArquivoBinario))
                    {
                        Console.WriteLine("Nenhum arquivo de dados encontrado.");
                        ultimoId = -1;
                        return;
                    }

                    using (var fs = new FileStream(caminhoArquivoBinario, FileMode.Open))
                    {
                        if (fs.Length >= 4)
                        {
                            byte[] buffer = new byte[4];
                            fs.Read(buffer, 0, 4);
                            ultimoId = BitConverter.ToInt32(buffer, 0);
                        }
                        else
                        {
                            ultimoId = -1;
                        }
                    }

                    if (File.Exists(caminhoArquivoUIDs))
                    {
                        using (var fs = new FileStream(caminhoArquivoUIDs, FileMode.Open))
                        {
                            byte[] buffer = new byte[4];
                            fs.Read(buffer, 0, 4);
                            int ultimoIdArquivo = BitConverter.ToInt32(buffer, 0);
                            if (ultimoIdArquivo > ultimoId)
                                ultimoId = ultimoIdArquivo;

                            long pos = 4;
                            while (pos < fs.Length)
                            {
                                fs.Seek(pos, SeekOrigin.Begin);
                                fs.Read(buffer, 0, 4);
                                int tamanho = BitConverter.ToInt32(buffer, 0);
                                byte[] uidData = new byte[tamanho];
                                fs.Read(uidData, 0, tamanho);
                                int uid = criptografiaAtivada ? DecryptRSA(uidData) : BitConverter.ToInt32(uidData, 0);
                                uidPositions[uid] = pos;
                                pos += 4 + tamanho;
                            }
                        }
                    }

                    Console.WriteLine($"Carregamento concluído. Último ID: {ultimoId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
                    ultimoId = -1;
                }
            }
        }

        private byte[] Serializar(T entidade)
        {
            var registro = entidade as RegistroDeRede;
            if (registro == null)
                throw new ArgumentException("Entidade deve ser do tipo RegistroDeRede");

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(registro.UID);
                writer.Write(registro.Timestamp.Ticks);
                writer.Write(registro.SourceIPAddress ?? "");
                writer.Write(registro.DestinationIPAddress ?? "");
                writer.Write(registro.SourcePort ?? 0);
                writer.Write(registro.DestinationPort ?? 0);
                writer.Write(registro.Protocol ?? "");
                writer.Write(registro.PacketLength ?? 0);
                writer.Write(registro.PacketType ?? "");
                writer.Write(registro.TrafficType ?? "");
                writer.Write(registro.PayloadData ?? "");
                writer.Write(registro.MalwareIndicators ?? "");
                writer.Write(registro.AnomalyScores ?? 0.0);
                writer.Write(registro.AlertsWarnings ?? "");
                writer.Write(registro.AttackType ?? "");
                writer.Write(registro.AttackSignature ?? "");
                writer.Write(registro.ActionTaken ?? "");
                writer.Write(registro.SeverityLevel ?? "");
                writer.Write(registro.UserInformation ?? "");
                writer.Write(registro.DeviceInformation ?? "");
                writer.Write(registro.NetworkSegment ?? "");
                writer.Write(registro.GeoLocationData?.Length ?? 0);
                foreach (var geo in registro.GeoLocationData ?? Array.Empty<string>())
                    writer.Write(geo);
                writer.Write(registro.ProxyInformation ?? "");
                writer.Write(registro.FirewallLogs ?? "");
                writer.Write(registro.IDSIPSAlerts ?? "");
                writer.Write(registro.LogSource ?? "");
                return ms.ToArray();
            }
        }

        private T Desserializar(byte[] dados)
        {
            using (var ms = new MemoryStream(dados))
            using (var reader = new BinaryReader(ms))
            {
                var registro = new RegistroDeRede
                {
                    UID = reader.ReadInt32(),
                    Timestamp = new DateTime(reader.ReadInt64()),
                    SourceIPAddress = reader.ReadString(),
                    DestinationIPAddress = reader.ReadString(),
                    SourcePort = reader.ReadInt32(),
                    DestinationPort = reader.ReadInt32(),
                    Protocol = reader.ReadString(),
                    PacketLength = reader.ReadInt32(),
                    PacketType = reader.ReadString(),
                    TrafficType = reader.ReadString(),
                    PayloadData = reader.ReadString(),
                    MalwareIndicators = reader.ReadString(),
                    AnomalyScores = reader.ReadDouble(),
                    AlertsWarnings = reader.ReadString(),
                    AttackType = reader.ReadString(),
                    AttackSignature = reader.ReadString(),
                    ActionTaken = reader.ReadString(),
                    SeverityLevel = reader.ReadString(),
                    UserInformation = reader.ReadString(),
                    DeviceInformation = reader.ReadString(),
                    NetworkSegment = reader.ReadString(),
                    GeoLocationData = new string[reader.ReadInt32()]
                };
                for (int i = 0; i < registro.GeoLocationData.Length; i++)
                    registro.GeoLocationData[i] = reader.ReadString();
                registro.ProxyInformation = reader.ReadString();
                registro.FirewallLogs = reader.ReadString();
                registro.IDSIPSAlerts = reader.ReadString();
                registro.LogSource = reader.ReadString();
                return registro as T;
            }
        }

        private byte[] EncryptAES(byte[] data)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = aesIV;
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Erro ao criptografar dados com AES: {ex.Message}", ex);
            }
        }

        private byte[] DecryptAES(byte[] data)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = aesIV;
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Erro ao descriptografar dados com AES: {ex.Message}", ex);
            }
        }

        private byte[] EncryptRSA(int uid)
        {
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    byte[] data = BitConverter.GetBytes(uid);
                    return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Erro ao criptografar UID com RSA: {ex.Message}", ex);
            }
        }

        private int DecryptRSA(byte[] data)
        {
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    byte[] decrypted = rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
                    return BitConverter.ToInt32(decrypted, 0);
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Erro ao descriptografar UID com RSA: {ex.Message}", ex);
            }
        }

        private bool KMPSearch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return false;

            int[] lps = ComputeLPSArray(pattern);
            int i = 0, j = 0;
            while (i < text.Length)
            {
                if (pattern[j] == text[i])
                {
                    i++;
                    j++;
                }
                if (j == pattern.Length)
                    return true;
                else if (i < text.Length && pattern[j] != text[i])
                {
                    if (j != 0)
                        j = lps[j - 1];
                    else
                        i++;
                }
            }
            return false;
        }

        private int[] ComputeLPSArray(string pattern)
        {
            int[] lps = new int[pattern.Length];
            int length = 0, i = 1;
            while (i < pattern.Length)
            {
                if (pattern[i] == pattern[length])
                {
                    length++;
                    lps[i] = length;
                    i++;
                }
                else
                {
                    if (length != 0)
                        length = lps[length - 1];
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }
            return lps;
        }
    }
}