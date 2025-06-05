using System;
using System.IO;
using AED_3_2025_S1_CRUD_Edgard_Melo.DataCompression;
using AED_3_2025_S1_CRUD_Edgard_Melo.Utilities;

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

        public void GravarRegistro(byte[] dados, bool registroExcluido)
        {
            using (var fs = new FileStream(CaminhoArquivo, FileMode.Append))
            {
                fs.WriteByte(registroExcluido ? (byte)0 : (byte)1);
                fs.Write(BitConverter.GetBytes(dados.Length), 0, 4);
                fs.Write(dados, 0, dados.Length);
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
                string extensao = metodoCompactacao.ToLower() == "lzw" ? ".lzw" : ".huff";
                string caminhoSaida = Path.Combine(diretorioSaida, Path.GetFileNameWithoutExtension(CaminhoArquivo) + extensao);

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