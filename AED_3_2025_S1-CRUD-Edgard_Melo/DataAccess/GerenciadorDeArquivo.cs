using System;
using System.IO;
using AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

// Notas de Desenvolvimento:
// - Ajustei a nomenclatura dos arquivos compactados para seguir o padrão nomeArquivoNomeAlgoritmoCompressaoX (ex.: banco_de_dadosLZW1.bin).
// - Mantive a lógica de compactação, mas agora com versão 1 como exemplo (pode incrementar dinamicamente depois).

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataAccess
{
    public class GerenciadorDeArquivo
    {
        public string CaminhoArquivo { get; private set; }

        public GerenciadorDeArquivo(string caminhoArquivo)
        {
            CaminhoArquivo = caminhoArquivo;
            InicializarArquivo();
        }

        private void InicializarArquivo()
        {
            if (!File.Exists(CaminhoArquivo))
            {
                using (var fs = new FileStream(CaminhoArquivo, FileMode.Create))
                {
                    fs.Write(BitConverter.GetBytes(0), 0, 4); // cabeçalho inicial (último ID)
                }
            }
        }

        public void Gravar(byte[] dados, long posicao = -1)
        {
            try
            {
                using (var fs = new FileStream(CaminhoArquivo, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    if (posicao == -1)
                    {
                        fs.Seek(0, SeekOrigin.End);
                    }
                    else
                    {
                        fs.Seek(posicao, SeekOrigin.Begin);
                    }
                    byte[] lapide = { 0 }; // Registro válido
                    byte[] tamanho = BitConverter.GetBytes(dados.Length);
                    fs.Write(lapide, 0, lapide.Length);
                    fs.Write(tamanho, 0, tamanho.Length);
                    fs.Write(dados, 0, dados.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar no arquivo: {ex.Message}");
            }
        }

        public void MarcarRegistroComoExcluido(long posicao)
        {
            using (var fs = new FileStream(CaminhoArquivo, FileMode.Open))
            {
                fs.Seek(posicao, SeekOrigin.Begin);
                fs.WriteByte(0); // marca como excluído
            }
        }

        public void CompactarArquivo(string metodoCompactacao)
        {
            try
            {
                string diretorioSaida = Path.Combine(Path.GetDirectoryName(CaminhoArquivo), "Compressed");
                Directory.CreateDirectory(diretorioSaida);
                string caminhoSaida = Path.Combine(diretorioSaida, $"{Path.GetFileNameWithoutExtension(CaminhoArquivo)}{metodoCompactacao}1.bin");

                if (metodoCompactacao.ToLower() == "lzw")
                {
                    var compressor = new LZWCompressor();
                    compressor.Compress(CaminhoArquivo, caminhoSaida);
                }
                else if (metodoCompactacao.ToLower() == "huffman")
                {
                    var compressor = new HuffmanCompressor();
                    compressor.Compress(CaminhoArquivo, caminhoSaida);
                }
                else
                {
                    throw new ArgumentException("Método de compactação inválido. Use 'LZW' ou 'Huffman'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao compactar arquivo {CaminhoArquivo} com {metodoCompactacao}: {ex.Message}");
                throw;
            }
        }
    }
}