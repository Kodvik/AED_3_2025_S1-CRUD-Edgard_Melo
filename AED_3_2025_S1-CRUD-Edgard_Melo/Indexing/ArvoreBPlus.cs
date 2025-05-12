using System;
using System.Collections.Generic;
using System.IO;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.Indexing
{
    public class ArvoreBPlus
    {
        private readonly string caminhoArquivoIndices;
        private readonly int ordem;
        private long posicaoRaiz;
        private readonly List<(long Posicao, int Tamanho)> posicoesOcupadas = new List<(long, int)>();
        private readonly object arquivoLock = new object(); // Objeto para sincronização

        public ArvoreBPlus(string caminhoArquivoIndices, int ordem)
        {
            // Garantir que caminhoArquivoIndices inclua o subdiretório Data e o nome do arquivo
            string diretorioData = Path.Combine(Path.GetDirectoryName(caminhoArquivoIndices) ?? throw new ArgumentException("Diretório inválido"), "Data");
            caminhoArquivoIndices = Path.Combine(diretorioData, "indices_bplus.bin");

            Console.WriteLine($"Inicializando ArvoreBPlus com caminho: {caminhoArquivoIndices}");
            this.caminhoArquivoIndices = caminhoArquivoIndices;
            this.ordem = ordem;

            // Criar diretório Data, se não existir
            if (!Directory.Exists(diretorioData))
            {
                Directory.CreateDirectory(diretorioData);
                Console.WriteLine($"Diretório {diretorioData} criado com sucesso.");
            }

            if (!File.Exists(caminhoArquivoIndices))
            {
                Console.WriteLine($"Arquivo {caminhoArquivoIndices} não existe. Criando novo arquivo...");
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Create, FileAccess.Write))
                {
                    byte[] cabecalho = new byte[8];
                    BitConverter.GetBytes(8L).CopyTo(cabecalho, 0); // Posição inicial da raiz
                    fs.Write(cabecalho, 0, cabecalho.Length);
                    NoBPlus noInicial = new NoBPlus(true) { Posicao = 8 };
                    byte[] noData = SerializarNo(noInicial);
                    fs.Write(noData, 0, noData.Length);
                    Console.WriteLine($"Nó inicial escrito. Tamanho do arquivo: {fs.Length} bytes");
                    posicoesOcupadas.Add((8, 53)); // Reservar 53 bytes para nó inicial
                }
                posicaoRaiz = 8;
                Console.WriteLine($"Arquivo {caminhoArquivoIndices} criado com sucesso.");
            }
            else
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open, FileAccess.Read))
                {
                    byte[] cabecalho = new byte[8];
                    fs.Read(cabecalho, 0, 8);
                    posicaoRaiz = BitConverter.ToInt64(cabecalho, 0);
                    Console.WriteLine($"Arquivo {caminhoArquivoIndices} encontrado. Posição raiz: {posicaoRaiz}");
                }
                posicoesOcupadas.Add((8, 53));
                posicoesOcupadas.Add((posicaoRaiz, posicaoRaiz == 8 ? 53 : 49));
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

            int tamanhoReservado = 61; // Máximo para folha ou não-folha
            posicoesOcupadas.Add((novaPosicao, tamanhoReservado));
            Console.WriteLine($"Nova posição alocada: {novaPosicao}, Tamanho reservado: {tamanhoReservado} bytes");
            return novaPosicao;
        }

        private void AtualizarPosicaoOcupada(long posicao, int novoTamanho)
        {
            for (int i = 0; i < posicoesOcupadas.Count; i++)
            {
                if (posicoesOcupadas[i].Posicao == posicao)
                {
                    posicoesOcupadas[i] = (posicao, Math.Max(posicoesOcupadas[i].Tamanho, novoTamanho));
                    Console.WriteLine($"Posição {posicao} atualizada, Tamanho: {posicoesOcupadas[i].Tamanho} bytes");
                    return;
                }
            }
        }

        public void Inserir(int id, long posicaoRegistro)
        {
            try
            {
                Console.WriteLine($"Inserindo ID: {id}, Posição Registro: {posicaoRegistro}");
                NoBPlus noRaiz = LerNo(posicaoRaiz);
                if (noRaiz == null)
                {
                    Console.WriteLine($"Erro: Nó raiz na posição {posicaoRaiz} é nulo ou corrompido.");
                    throw new InvalidOperationException("Não foi possível ler o nó raiz.");
                }

                if (noRaiz.Chaves.Count >= ordem)
                {
                    Console.WriteLine("Raiz cheia, dividindo...");
                    NoBPlus novaRaiz = new NoBPlus(false) { Posicao = ObterNovaPosicao(49) };
                    novaRaiz.Filhos.Add(noRaiz.Posicao);
                    DividirNo(novaRaiz, 0, noRaiz);
                    posicaoRaiz = novaRaiz.Posicao;
                    AtualizarCabecalho();
                    if (!VerificarIntegridadeNoRaiz())
                    {
                        throw new InvalidOperationException("Nó raiz corrompido após divisão.");
                    }
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
                    // Procurar a posição correta para inserção
                    while (i >= 0 && no.Chaves[i] > id)
                    {
                        i--;
                    }
                    // Verificar se a chave já existe
                    if (i >= 0 && no.Chaves[i] == id)
                    {
                        // Chave duplicada encontrada
                        throw new InvalidOperationException($"Chave {id} já existe no nó folha (Posição: {no.Posicao}).");
                        // Alternativamente, atualizar a referência:
                        // no.Referencias[i] = posicaoRegistro;
                        // EscreverNo(no);
                        // return;
                    }
                    // Inserir a nova chave e referência na posição correta
                    no.Chaves.Insert(i + 1, id);
                    no.Referencias.Insert(i + 1, posicaoRegistro);
                    EscreverNo(no);
                }
                else
                {
                    // Procurar o filho apropriado
                    while (i >= 0 && no.Chaves[i] > id)
                    {
                        i--;
                    }
                    i++;
                    NoBPlus filho = LerNo(no.Filhos[i]);
                    if (filho == null)
                    {
                        Console.WriteLine($"Erro: Filho na posição {no.Filhos[i]} é nulo.");
                        throw new InvalidOperationException("Filho inválido.");
                    }
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
                    EscreverNo(no); // Re-escrever nó pai após modificações
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
            Console.WriteLine($"Dividindo nó (Posição: {filho.Posicao}, Chaves: [{string.Join(", ", filho.Chaves)}], ÉFolha: {filho.ÉFolha})");
            try
            {
                NoBPlus novoNo = new NoBPlus(filho.ÉFolha) { Posicao = ObterNovaPosicao(filho.ÉFolha ? 53 : 49) };
                int meio = filho.Chaves.Count / 2;
                int chavePromovida = filho.Chaves[meio];

                // Mover chaves e referências/filhos para o novo nó
                novoNo.Chaves.AddRange(filho.Chaves.GetRange(meio + (filho.ÉFolha ? 0 : 1), filho.Chaves.Count - meio - (filho.ÉFolha ? 0 : 1)));
                if (filho.ÉFolha)
                {
                    novoNo.Referencias.AddRange(filho.Referencias.GetRange(meio, filho.Referencias.Count - meio));
                }
                else
                {
                    novoNo.Filhos.AddRange(filho.Filhos.GetRange(meio + 1, filho.Filhos.Count - meio - 1));
                }

                // Remover chaves e referências/filhos do nó original
                filho.Chaves.RemoveRange(meio, filho.Chaves.Count - meio);
                if (filho.ÉFolha)
                {
                    filho.Referencias.RemoveRange(meio, filho.Referencias.Count - meio);
                }
                else
                {
                    filho.Filhos.RemoveRange(meio + 1, filho.Filhos.Count - meio - 1);
                }

                // Ajustar ponteiro para próximo nó, se for folha
                if (filho.ÉFolha)
                {
                    novoNo.Proximo = filho.Proximo;
                    filho.Proximo = novoNo.Posicao;
                }

                // Escrever nós modificados
                EscreverNo(novoNo);
                EscreverNo(filho);

                // Atualizar o nó pai
                pai.Chaves.Insert(indice, chavePromovida);
                pai.Filhos[indice] = filho.Posicao;
                pai.Filhos.Insert(indice + 1, novoNo.Posicao);
                EscreverNo(pai);

                // Se o nó pai for a raiz, atualizar a posição da raiz
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

        private long ObterProximaPosicaoLivre(int tamanhoDados)
        {
            lock (arquivoLock)
            {
                if (posicoesOcupadas.Count == 0)
                {
                    return 8; // Após o cabeçalho
                }

                posicoesOcupadas.Sort((a, b) => a.Posicao.CompareTo(b.Posicao));
                long ultimaPosicao = 8;

                foreach (var (Posicao, Tamanho) in posicoesOcupadas)
                {
                    if (Posicao > ultimaPosicao)
                    {
                        if (Posicao - ultimaPosicao >= tamanhoDados)
                        {
                            return ultimaPosicao;
                        }
                    }
                    ultimaPosicao = Math.Max(ultimaPosicao, Posicao + Tamanho);
                }

                return ultimaPosicao;
            }
        }

        public long Buscar(int id)
        {
            NoBPlus no = LerNo(posicaoRaiz);
            while (no != null && !no.ÉFolha)
            {
                int i = 0;
                while (i < no.Chaves.Count && id > no.Chaves[i])
                {
                    i++;
                }
                no = LerNo(no.Filhos[i]);
            }
            if (no == null) return -1;
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
            // Implementação de remoção (simplificada para o escopo)
        }

        private NoBPlus LerNo(long posicao)
        {
            if (posicao < 0)
            {
                Console.WriteLine($"Posição inválida para leitura: {posicao}");
                return null;
            }

            try
            {
                using (var fs = new FileStream(caminhoArquivoIndices, FileMode.Open, FileAccess.Read))
                {
                    if (posicao >= fs.Length)
                    {
                        Console.WriteLine($"Posição {posicao} fora do tamanho do arquivo ({fs.Length}).");
                        return null;
                    }

                    fs.Seek(posicao, SeekOrigin.Begin);
                    byte[] buffer = new byte[1];
                    fs.Read(buffer, 0, 1);
                    bool éFolha = buffer[0] == 1;

                    buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    int numChaves = BitConverter.ToInt32(buffer, 0);

                    if (numChaves > ordem || numChaves < 0)
                    {
                        Console.WriteLine($"Erro: numChaves ({numChaves}) inválido na posição {posicao}. Ordem: {ordem}.");
                        return null;
                    }

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
                        // Ler Proximo
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

                    var no = new NoBPlus(éFolha)
                    {
                        Posicao = posicao,
                        Chaves = chaves,
                        Referencias = referencias,
                        Filhos = filhos,
                        Proximo = proximo
                    };
                    Console.WriteLine($"Nó lido na posição {posicao}. ÉFolha: {no.ÉFolha}, Chaves: [{string.Join(", ", no.Chaves)}], " +
                                      $"Filhos: [{string.Join(", ", no.Filhos)}], Referências: [{string.Join(", ", no.Referencias)}], Proximo: {no.Proximo}");
                    return no;
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
                        long novaPosicao = no.Posicao == -1 ? ObterProximaPosicaoLivre(noData.Length) : no.Posicao;
                        if (novaPosicao + noData.Length > fs.Length)
                        {
                            fs.SetLength(novaPosicao + noData.Length);
                        }
                        fs.Position = novaPosicao;
                        fs.Write(noData, 0, noData.Length);
                        Console.WriteLine($"Nó escrito na posição {novaPosicao}. ÉFolha: {no.ÉFolha}, Chaves: [{string.Join(", ", no.Chaves)}], Tamanho dos dados: {noData.Length}, Tamanho do arquivo: {fs.Length} bytes, Intervalo: [{novaPosicao}, {novaPosicao + noData.Length})");
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
                if (noRaiz == null)
                {
                    Console.WriteLine($"Erro: Nó raiz na posição {posicaoRaiz} é nulo.");
                    return false;
                }
                if (noRaiz.Chaves.Count > ordem || noRaiz.Chaves.Count < 0)
                {
                    Console.WriteLine($"Erro: Nó raiz na posição {posicaoRaiz} tem numChaves inválido: {noRaiz.Chaves.Count}");
                    return false;
                }
                if (!noRaiz.ÉFolha && noRaiz.Filhos.Count != noRaiz.Chaves.Count + 1)
                {
                    Console.WriteLine($"Erro: Nó raiz não-folha na posição {posicaoRaiz} tem {noRaiz.Filhos.Count} filhos, esperado {noRaiz.Chaves.Count + 1}");
                    return false;
                }
                Console.WriteLine($"Integridade do nó raiz verificada: Posição: {posicaoRaiz}, ÉFolha: {noRaiz.ÉFolha}, Chaves: [{string.Join(", ", noRaiz.Chaves)}]");
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
                    // Escrever ÉFolha
                    ms.WriteByte((byte)(no.ÉFolha ? 1 : 0));

                    // Validar número de chaves
                    if (no.Chaves.Count > ordem)
                    {
                        throw new InvalidOperationException("Número de chaves excede ordem.");
                    }
                    if (no.ÉFolha && no.Referencias.Count != no.Chaves.Count)
                    {
                        throw new InvalidOperationException("Número de referências não corresponde às chaves.");
                    }
                    if (!no.ÉFolha && no.Filhos.Count != no.Chaves.Count + 1)
                    {
                        throw new InvalidOperationException("Número de filhos não corresponde às chaves + 1.");
                    }

                    // Escrever número de chaves
                    ms.Write(BitConverter.GetBytes(no.Chaves.Count), 0, 4);

                    // Escrever chaves
                    foreach (int chave in no.Chaves)
                    {
                        ms.Write(BitConverter.GetBytes(chave), 0, 4);
                    }

                    // Escrever referências (se folha) ou filhos (se não-folha)
                    if (no.ÉFolha)
                    {
                        foreach (long referencia in no.Referencias)
                        {
                            ms.Write(BitConverter.GetBytes(referencia), 0, 8);
                        }
                        // Escrever Proximo
                        ms.Write(BitConverter.GetBytes(no.Proximo), 0, 8);
                    }
                    else
                    {
                        foreach (long filho in no.Filhos)
                        {
                            ms.Write(BitConverter.GetBytes(filho), 0, 8);
                        }
                    }

                    byte[] data = ms.ToArray();
                    Console.WriteLine($"Serializando nó (Posição: {no.Posicao}, ÉFolha: {no.ÉFolha}, Chaves: [{string.Join(", ", no.Chaves)}], " +
                                      $"Tamanho dos dados: {data.Length} bytes, Filhos: [{string.Join(", ", no.Filhos)}], Referências: [{string.Join(", ", no.Referencias)}], Proximo: {no.Proximo}");
                    return data;
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
                    Console.WriteLine($"Cabeçalho atualizado. Posição Raiz: {posicaoRaiz}, Tamanho do arquivo: {fs.Length} bytes");
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

        public NoBPlus(bool éFolha)
        {
            ÉFolha = éFolha;
            Posicao = -1;
            Chaves = new List<int>();
            Referencias = new List<long>();
            Filhos = new List<long>();
            Proximo = 0;
        }
    }
}