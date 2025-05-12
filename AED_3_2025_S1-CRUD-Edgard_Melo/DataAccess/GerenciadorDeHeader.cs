using System.IO;

namespace AED_3_2025_S1_CRUD_Edgard_Melo.DataAccess
{
    public class GerenciadorDeCabecalho
    {
        private GerenciadorDeArquivo gerenciadorDeArquivo;

        public GerenciadorDeCabecalho(GerenciadorDeArquivo gerenciadorDeArquivo)
        {
            this.gerenciadorDeArquivo = gerenciadorDeArquivo;
        }

        public int ObterProximoId()
        {
            using (var fs = new FileStream(gerenciadorDeArquivo.CaminhoArquivo, FileMode.Open))
            {
                byte[] buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                return BitConverter.ToInt32(buffer, 0) + 1;
            }
        }

        public void AtualizarCabecalho(int novoUltimoId)
        {
            using (var fs = new FileStream(gerenciadorDeArquivo.CaminhoArquivo, FileMode.Open))
            {
                fs.Write(BitConverter.GetBytes(novoUltimoId), 0, 4);
            }
        }

        // metodo futuro para resetar o cabeçalho (para compactação) TP3
        public void ResetarCabecalho()
        {
            using (var fs = new FileStream(gerenciadorDeArquivo.CaminhoArquivo, FileMode.Open))
            {
                fs.Write(BitConverter.GetBytes(0), 0, 4);
            }
        }
    }
}