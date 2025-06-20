using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

// Notas de Desenvolvimento:
// - Implementei a remoção parcial usando lápides no método Remover, marcando o nó como excluído sem remover completamente.
// - Mantive a integridade da árvore, apenas atualizando a lápide no arquivo de índices.
// - Adicionei medição de tempo em Remover.

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Indexing
{
    public class ArvoreBPlus
    {
        private readonly string caminhoArquivoIndices;
        private readonly int ordem;
        private long posicaoRaiz;
        private readonly List<(long Posicao, int Tamanho)> posicoesOcupadas = new List<(long, int)>();
        private readonly object arquivoLock = new object();

        public ArvoreBPlus(string caminhoArquivoIndices, int ordem)
        {
            string diretorioData = Path.Combine(caminhoArquivoIndices, "Data");
            caminhoArquivoIndices = Path.Combine(diretorioData, "indices_bplus.bin");
            this.caminhoArquivoIndices = caminhoArquivoIndices;
            this.ordem = ordem;

            if (!Directory.Exists(diretorioData))
            {
                Directory.CreateDirectory(diretorioData);
            }

            if (File.Exists(caminhoArquivoIndices))
            {
                try { File.Delete(caminhoArquivoIndices); }
                catch (Exception ex) { Console.WriteLine($"Erro ao excluir {caminhoArquivoIndices}: {ex.Message}"); throw; }
            }

            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Create, FileAccess.Write))
                {
                    byte[] cabecalho = new byte[8];
                    BitConverter.GetBytes(8L).CopyTo(cabecalho, 0);
                    fs.Write(cabecalho, 0, cabecalho.Length);
                    NoBPlus noInicial = new NoBPlus(true) { Posicao = 8, Lapide = 1 };
                    byte[] noData = SerializarNo(noInicial);
                    fs.Write(noData, 0, noData.Length);
                    posicoesOcupadas.Add((8, 53));
                }
                posicaoRaiz = 8;
                AtualizarCabecalho();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar arquivo de índices: {ex.Message}");
                throw;
            }
        }

        private long ObterNovaPosicao(int tamanho)
        {
            long novaPosicao;
            using (var fs = new FileStream(caminhoArquivoIndices, FileMode.OpenOrCreate, FileAccess.Read))
            {
                novaPosicao = fs.Length;
            }

            foreach (var (pos, tam) in posicoesOcupadas)
            {
                if (novaPosicao >= pos && novaPosicao < pos + tam)
                {
                    novaPosicao = pos + tam;
                }
            }

            int tamanhoReservado = 61;
            posicoesOcupadas.Add((novaPosicao, tamanhoReservado));
            return novaPosicao;
        }

        private void AtualizarPosicaoOcupada(long posicao, int novoTamanho)
        {
            for (int i = 0; i < posicoesOcupadas.Count; i++)
            {
                if (posicoesOcupadas[i].Posicao == posicao)
                {
                    posicoesOcupadas[i] = (posicao, Math.Max(posicoesOcupadas[i].Tamanho, novoTamanho));
                    return;
                }
            }
        }

        public void Inserir(int id, long posicaoRegistro)
        {
            try
            {
                NoBPlus noRaiz = LerNo(posicaoRaiz);
                if (noRaiz == null || noRaiz.Lapide == 0)
                    throw new InvalidOperationException("Nó raiz inválido ou excluído.");

                if (noRaiz.Chaves.Count >= ordem)
                {
                    NoBPlus novaRaiz = new NoBPlus(false) { Posicao = ObterNovaPosicao(49), Lapide = 1 };
                    novaRaiz.Filhos.Add(noRaiz.Posicao);
                    DividirNo(novaRaiz, 0, noRaiz);
                    posicaoRaiz = novaRaiz.Posicao;
                    AtualizarCabecalho();
                    noRaiz = LerNo(posicaoRaiz);
                }
                InserirNaoCheio(noRaiz, id, posicaoRegistro);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inserir na Árvore B+ (ID: {id}): {ex.Message}");
                throw;
            }
        }

        private void InserirNaoCheio(NoBPlus no, int id, long posicaoRegistro)
        {
            try
            {
                int i = no.Chaves.Count - 1;
                if (no.ÉFolha)
                {
                    while (i >= 0 && no.Chaves[i] > id)
                    {
                        i--;
                    }
                    if (i >= 0 && no.Chaves[i] == id)
                    {
                        no.Referencias[i] = posicaoRegistro;
                        EscreverNo(no);
                        return;
                    }
                    no.Chaves.Insert(i + 1, id);
                    no.Referencias.Insert(i + 1, posicaoRegistro);
                    EscreverNo(no);
                }
                else
                {
                    while (i >= 0 && no.Chaves[i] > id)
                    {
                        i--;
                    }
                    i++;
                    NoBPlus filho = LerNo(no.Filhos[i]);
                    if (filho == null || filho.Lapide == 0)
                        throw new InvalidOperationException("Filho inválido ou excluído.");
                    if (filho.Chaves.Count >= ordem)
                    {
                        DividirNo(no, i, filho);
                        if (id > no.Chaves[i])
                        {
                            i++;
                        }
                        filho = LerNo(no.Filhos[i]);
                    }
                    InserirNaoCheio(filho, id, posicaoRegistro);
                    EscreverNo(no);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inserir não cheio (Nó Posição: {no.Posicao}): {ex.Message}");
                throw;
            }
        }

        private void DividirNo(NoBPlus pai, int indice, NoBPlus filho)
        {
            try
            {
                NoBPlus novoNo = new NoBPlus(filho.ÉFolha) { Posicao = ObterNovaPosicao(filho.ÉFolha ? 53 : 49), Lapide = 1 };
                int meio = filho.Chaves.Count / 2;
                int chavePromovida = filho.Chaves[meio];

                novoNo.Chaves.AddRange(filho.Chaves.GetRange(meio + (filho.ÉFolha ? 0 : 1), filho.Chaves.Count - meio - (filho.ÉFolha ? 0 : 1)));
                if (filho.ÉFolha)
                {
                    novoNo.Referencias.AddRange(filho.Referencias.GetRange(meio, filho.Referencias.Count - meio));
                }
                else
                {
                    novoNo.Filhos.AddRange(filho.Filhos.GetRange(meio + 1, filho.Filhos.Count - meio - 1));
                }

                filho.Chaves.RemoveRange(meio, filho.Chaves.Count - meio);
                if (filho.ÉFolha)
                {
                    filho.Referencias.RemoveRange(meio, filho.Referencias.Count - meio);
                }
                else
                {
                    filho.Filhos.RemoveRange(meio + 1, filho.Filhos.Count - meio - 1);
                }

                if (filho.ÉFolha)
                {
                    novoNo.Proximo = filho.Proximo;
                    filho.Proximo = novoNo.Posicao;
                }

                EscreverNo(novoNo);
                EscreverNo(filho);

                pai.Chaves.Insert(indice, chavePromovida);
                pai.Filhos[indice] = filho.Posicao;
                pai.Filhos.Insert(indice + 1, novoNo.Posicao);
                EscreverNo(pai);

                if (pai.Posicao == posicaoRaiz)
                {
                    posicaoRaiz = pai.Posicao;
                    AtualizarCabecalho();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao dividir nó (Posição: {filho.Posicao}): {ex.Message}");
                throw;
            }
        }

        public long Buscar(int id)
        {
            NoBPlus no = LerNo(posicaoRaiz);
            while (no != null && !no.ÉFolha && no.Lapide == 1)
            {
                int i = 0;
                while (i < no.Chaves.Count && id > no.Chaves[i])
                {
                    i++;
                }
                no = LerNo(no.Filhos[i]);
            }
            if (no == null || no.Lapide == 0) return -1;
            for (int i = 0; i < no.Chaves.Count; i++)
            {
                if (no.Chaves[i] == id)
                {
                    return no.Referencias[i];
                }
            }
            return -1;
        }

        public void Remover(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                long posicao = Buscar(id);
                if (posicao == -1)
                {
                    Console.WriteLine($"ID {id} não encontrado para remoção.");
                    return;
                }

                // Encontrar o nó folha que contém o ID
                NoBPlus no = LerNo(posicaoRaiz);
                while (no != null && !no.ÉFolha && no.Lapide == 1)
                {
                    int i = 0;
                    while (i < no.Chaves.Count && id > no.Chaves[i])
                    {
                        i++;
                    }
                    no = LerNo(no.Filhos[i]);
                }
                if (no == null || no.Lapide == 0)
                {
                    Console.WriteLine($"Nó para ID {id} inválido ou excluído.");
                    return;
                }

                // Remover a chave e referência do nó
                int index = no.Chaves.IndexOf(id);
                if (index >= 0)
                {
                    no.Chaves.RemoveAt(index);
                    no.Referencias.RemoveAt(index);
                    no.Lapide = no.Chaves.Count > 0 ? (byte)1 : (byte)0;
                    EscreverNo(no);
                    Console.WriteLine($"UID {id} removido da árvore B+.");
                }
                else
                {
                    Console.WriteLine($"ID {id} não encontrado no nó folha.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover ID {id} na Árvore B+: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"Remoção de UID {id} concluída em {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private NoBPlus LerNo(long posicao)
        {
            if (posicao < 0)
                return null;

            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open, FileAccess.Read))
                {
                    if (posicao >= fs.Length)
                        return null;

                    fs.Seek(posicao, SeekOrigin.Begin);
                    byte lapide = (byte)fs.ReadByte();
                    if (lapide == 0) return null; // Ignora nós excluídos

                    bool éFolha = fs.ReadByte() == 1;

                    byte[] buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    int numChaves = BitConverter.ToInt32(buffer, 0);

                    List<int> chaves = new List<int>();
                    for (int i = 0; i < numChaves; i++)
                    {
                        buffer = new byte[4];
                        fs.Read(buffer, 0, 4);
                        chaves.Add(BitConverter.ToInt32(buffer, 0));
                    }

                    List<long> referencias = new List<long>();
                    List<long> filhos = new List<long>();
                    long proximo = 0;
                    if (éFolha)
                    {
                        for (int i = 0; i < numChaves; i++)
                        {
                            buffer = new byte[8];
                            fs.Read(buffer, 0, 8);
                            referencias.Add(BitConverter.ToInt64(buffer, 0));
                        }
                        buffer = new byte[8];
                        fs.Read(buffer, 0, 8);
                        proximo = BitConverter.ToInt64(buffer, 0);
                    }
                    else
                    {
                        for (int i = 0; i <= numChaves; i++)
                        {
                            buffer = new byte[8];
                            fs.Read(buffer, 0, 8);
                            filhos.Add(BitConverter.ToInt64(buffer, 0));
                        }
                    }

                    return new NoBPlus(éFolha)
                    {
                        Posicao = posicao,
                        Chaves = chaves,
                        Referencias = referencias,
                        Filhos = filhos,
                        Proximo = proximo,
                        Lapide = lapide
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler nó na posição {posicao}: {ex.Message}");
                return null;
            }
        }

        private void EscreverNo(NoBPlus no)
        {
            byte[] noData = SerializarNo(no);
            lock (arquivoLock)
            {
                try
                {
                    using (var fs = new FileStream(caminhoArquivoIndices, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        long novaPosicao = no.Posicao == -1 ? ObterNovaPosicao(noData.Length) : no.Posicao;
                        if (novaPosicao + noData.Length > fs.Length)
                        {
                            fs.SetLength(novaPosicao + noData.Length);
                        }
                        fs.Position = novaPosicao;
                        fs.Write(noData, 0, noData.Length);
                        if (no.Posicao != novaPosicao)
                        {
                            no.Posicao = novaPosicao;
                        }
                        AtualizarPosicaoOcupada(novaPosicao, noData.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever nó (Posição: {no.Posicao}): {ex.Message}");
                    throw;
                }
            }
        }

        private bool VerificarIntegridadeNoRaiz()
        {
            try
            {
                NoBPlus noRaiz = LerNo(posicaoRaiz);
                if (noRaiz == null || noRaiz.Lapide == 0)
                    return false;
                if (noRaiz.Chaves.Count > ordem || noRaiz.Chaves.Count < 0)
                    return false;
                if (!noRaiz.ÉFolha && noRaiz.Filhos.Count != noRaiz.Chaves.Count + 1)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar integridade do nó raiz: {ex.Message}");
                return false;
            }
        }

        private byte[] SerializarNo(NoBPlus no)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    ms.WriteByte(no.Lapide);
                    ms.WriteByte((byte)(no.ÉFolha ? 1 : 0));

                    if (no.Chaves.Count > ordem)
                        throw new InvalidOperationException("Número de chaves excede ordem.");
                    if (no.ÉFolha && no.Referencias.Count != no.Chaves.Count)
                        throw new InvalidOperationException("Número de referências não corresponde às chaves.");
                    if (!no.ÉFolha && no.Filhos.Count != no.Chaves.Count + 1)
                        throw new InvalidOperationException("Número de filhos não corresponde às chaves + 1.");

                    ms.Write(BitConverter.GetBytes(no.Chaves.Count), 0, 4);

                    foreach (int chave in no.Chaves)
                    {
                        ms.Write(BitConverter.GetBytes(chave), 0, 4);
                    }

                    if (no.ÉFolha)
                    {
                        foreach (long referencia in no.Referencias)
                        {
                            ms.Write(BitConverter.GetBytes(referencia), 0, 8);
                        }
                        ms.Write(BitConverter.GetBytes(no.Proximo), 0, 8);
                    }
                    else
                    {
                        foreach (long filho in no.Filhos)
                        {
                            ms.Write(BitConverter.GetBytes(filho), 0, 8);
                        }
                    }

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao serializar nó (Posição: {no.Posicao}): {ex.Message}");
                throw;
            }
        }

        private void AtualizarCabecalho()
        {
            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    byte[] posicaoRaizBytes = BitConverter.GetBytes(posicaoRaiz);
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(posicaoRaizBytes, 0, posicaoRaizBytes.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar cabeçalho: {ex.Message}");
                throw;
            }
        }
    }

    public class NoBPlus
    {
        public bool ÉFolha { get; set; }
        public long Posicao { get; set; }
        public List<int> Chaves { get; set; }
        public List<long> Referencias { get; set; }
        public List<long> Filhos { get; set; }
        public long Proximo { get; set; }
        public byte Lapide { get; set; } // Adicionado para suportar lápides

        public NoBPlus(bool éFolha)
        {
            ÉFolha = éFolha;
            Posicao = -1;
            Chaves = new List<int>();
            Referencias = new List<long>();
            Filhos = new List<long>();
            Proximo = 0;
            Lapide = 1; // Lápide inicial como ativa
        }
    }
}