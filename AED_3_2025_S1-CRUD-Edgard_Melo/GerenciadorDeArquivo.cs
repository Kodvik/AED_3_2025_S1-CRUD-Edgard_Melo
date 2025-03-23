using System;
using System.IO;

public class GerenciadorDeArquivo
{
    public string CaminhoArquivo { get; private set; }

    public GerenciadorDeArquivo(string caminhoArquivo)
    {
        CaminhoArquivo = caminhoArquivo;
        InicializarArquivo();
    }

    // Método que inicializa o arquivo se ele não existir
    private void InicializarArquivo()
    {
        if (!File.Exists(CaminhoArquivo))
        {
            using (var fs = new FileStream(CaminhoArquivo, FileMode.Create))
            {
                fs.Write(BitConverter.GetBytes(0), 0, 4); // Escreve o último ID como 0
            }
        }
    }

    // Método para gravar um registro no arquivo
    public void GravarRegistro(byte[] dados, bool registroExcluido)
    {
        using (var fs = new FileStream(CaminhoArquivo, FileMode.Append))
        {
            fs.WriteByte(registroExcluido ? (byte)0 : (byte)1); // Lápide
            fs.Write(BitConverter.GetBytes(dados.Length), 0, 4); // Tamanho do registro
            fs.Write(dados, 0, dados.Length); // Dados do registro
        }
    }

    // Método para buscar um registro baseado no UID
    public byte[] BuscarRegistroPorId(int id)
    {
        using (var fs = new FileStream(CaminhoArquivo, FileMode.Open))
        {
            while (fs.Position < fs.Length)
            {
                long inicioRegistro = fs.Position;

                int lápide = fs.ReadByte(); // Lê o byte da lápide
                byte[] bufferTamanho = new byte[4];
                fs.Read(bufferTamanho, 0, 4);
                int tamanhoRegistro = BitConverter.ToInt32(bufferTamanho, 0);

                byte[] bufferRegistro = new byte[tamanhoRegistro];
                fs.Read(bufferRegistro, 0, tamanhoRegistro);

                // Obtém o UID do registro para comparação
                int idRegistro = BitConverter.ToInt32(bufferRegistro, 0);
                if (lápide == 1 && idRegistro == id)
                {
                    return bufferRegistro;
                }
            }
        }
        return null;
    }

    // Método para marcar um registro como excluído
    public void MarcarRegistroComoExcluido(int id)
    {
        using (var fs = new FileStream(CaminhoArquivo, FileMode.Open))
        {
            while (fs.Position < fs.Length)
            {
                long inicioRegistro = fs.Position;

                int lápide = fs.ReadByte();
                byte[] bufferTamanho = new byte[4];
                fs.Read(bufferTamanho, 0, 4);
                int tamanhoRegistro = BitConverter.ToInt32(bufferTamanho, 0);

                byte[] bufferRegistro = new byte[tamanhoRegistro];
                fs.Read(bufferRegistro, 0, tamanhoRegistro);

                int idRegistro = BitConverter.ToInt32(bufferRegistro, 0);
                if (lápide == 1 && idRegistro == id)
                {
                    fs.Position = inicioRegistro;
                    fs.WriteByte(0); // Define a lápide como excluído
                    return;
                }
            }
        }
    }
}