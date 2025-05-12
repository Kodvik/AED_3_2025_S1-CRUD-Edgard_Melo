using System;
using System.IO;

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

        // Metodo futuro para compactação (a ser implementado) para o proximo TP
        public void CompactarArquivo()
        {
            // ainda preciso implementar
        }
    }
}