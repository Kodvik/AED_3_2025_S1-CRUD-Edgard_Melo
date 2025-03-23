using System.IO;

public class GerenciadorDeCabecalho
{
    private GerenciadorDeArquivo gerenciadorDeArquivo;

    public GerenciadorDeCabecalho(GerenciadorDeArquivo gerenciadorDeArquivo)
    {
        this.gerenciadorDeArquivo = gerenciadorDeArquivo;
    }

    // Obtém o próximo ID para novos registros
    public int ObterProximoId()
    {
        using (var fs = new FileStream(gerenciadorDeArquivo.CaminhoArquivo, FileMode.Open))
        {
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0) + 1;
        }
    }

    // Atualiza o cabeçalho com o novo último ID
    public void AtualizarCabecalho(int novoUltimoId)
    {
        using (var fs = new FileStream(gerenciadorDeArquivo.CaminhoArquivo, FileMode.Open))
        {
            fs.Write(BitConverter.GetBytes(novoUltimoId), 0, 4);
        }
    }
}