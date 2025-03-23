//IMPORTANTE... na proxima versao do arquivo... modularizar o CRUD
using System;
using System.IO;

public class CRUD<T> where T : IEntidade, new()
{
    private string caminhoArquivo;
    private int ultimoId; // Último ID usado

    public CRUD(string caminhoArquivo)
    {
        this.caminhoArquivo = caminhoArquivo;
        InicializarArquivo();
    }

    public int getUltimoID()
    {
        return ultimoId;
    }

    // Inicializa o arquivo e lê/escreve o cabeçalho
    private void InicializarArquivo()
    {
        if (!File.Exists(caminhoArquivo))
        {
            using (var fs = new FileStream(caminhoArquivo, FileMode.Create))
            {
                // Escreve o cabeçalho inicial (ultimoId = 0)
                fs.Write(BitConverter.GetBytes(0), 0, 4);
            }
        }
        else
        {
            using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
            {
                // Lê o cabeçalho para obter o último ID
                byte[] buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                ultimoId = BitConverter.ToInt32(buffer, 0);
            }
        }
    }

    // Método para criar um novo registro
    public int Criar(T entidade)
    {
        ultimoId++;
        entidade.UID = ultimoId; // Atribui o novo ID à entidade

        byte[] entidadeBytes = Serializar(entidade);
        byte[] tamanhoRegistro = BitConverter.GetBytes(entidadeBytes.Length);

        using (var fs = new FileStream(caminhoArquivo, FileMode.Append))
        {
            fs.WriteByte(1); // Lápide: registro válido
            fs.Write(tamanhoRegistro, 0, tamanhoRegistro.Length); // Tamanho do registro
            fs.Write(entidadeBytes, 0, entidadeBytes.Length); // Vetor de bytes do objeto
        }

        AtualizarCabecalho();
        return ultimoId;
    }

    // Método para ler um registro pelo ID
    public T Ler(int id)
    {
        using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
        {
            fs.Seek(4, SeekOrigin.Begin); // Pula o cabeçalho

            while (fs.Position < fs.Length)
            {
                int lápide = fs.ReadByte(); // Lê a lápide (1 ou 0)
                byte[] tamanhoBuffer = new byte[4];
                fs.Read(tamanhoBuffer, 0, 4);
                int tamanhoRegistro = BitConverter.ToInt32(tamanhoBuffer, 0);

                byte[] registroBuffer = new byte[tamanhoRegistro];
                fs.Read(registroBuffer, 0, tamanhoRegistro);

                if (lápide == 1) // Registro válido
                {
                    T entidade = Desserializar(registroBuffer);
                    if (entidade.UID == id)
                    {
                        return entidade;
                    }
                }
            }
        }
        return default; // Retorna null se o registro não for encontrado
    }

    // Método para atualizar um registro
    public void Atualizar(int id, T entidadeAtualizada)
    {
        using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
        {
            fs.Seek(4, SeekOrigin.Begin); // Pula o cabeçalho

            while (fs.Position < fs.Length)
            {
                long inicioRegistro = fs.Position;

                int lápide = fs.ReadByte();
                byte[] tamanhoBuffer = new byte[4];
                fs.Read(tamanhoBuffer, 0, 4);
                int tamanhoRegistro = BitConverter.ToInt32(tamanhoBuffer, 0);

                byte[] registroBuffer = new byte[tamanhoRegistro];
                fs.Read(registroBuffer, 0, tamanhoRegistro);

                if (lápide == 1) // Registro válido
                {
                    T entidade = Desserializar(registroBuffer);
                    if (entidade.UID == id)
                    {
                        // Marca o registro antigo como excluído
                        fs.Position = inicioRegistro;
                        fs.WriteByte(0); // Define a lápide como "excluído"

                        // Adiciona o novo registro no final
                        Criar(entidadeAtualizada);
                        return;
                    }
                }
            }
        }
    }

    // Método para deletar um registro
    public void Deletar(int id)
    {
        using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
        {
            fs.Seek(4, SeekOrigin.Begin); // Pula o cabeçalho

            while (fs.Position < fs.Length)
            {
                long inicioRegistro = fs.Position;

                int lápide = fs.ReadByte();
                byte[] tamanhoBuffer = new byte[4];
                fs.Read(tamanhoBuffer, 0, 4);
                int tamanhoRegistro = BitConverter.ToInt32(tamanhoBuffer, 0);

                byte[] registroBuffer = new byte[tamanhoRegistro];
                fs.Read(registroBuffer, 0, tamanhoRegistro);

                if (lápide == 1) // Registro válido
                {
                    T entidade = Desserializar(registroBuffer);
                    if (entidade.UID == id)
                    {
                        // Marca o registro como excluído
                        fs.Position = inicioRegistro;
                        fs.WriteByte(0); // Define a lápide como "excluído"
                        return;
                    }
                }
            }
        }
    }

    // Atualiza o cabeçalho com o último ID
    private void AtualizarCabecalho()
    {
        using (var fs = new FileStream(caminhoArquivo, FileMode.Open))
        {
            fs.Write(BitConverter.GetBytes(ultimoId), 0, 4); // Atualiza o último ID no cabeçalho
        }
    }

    // Serializa uma entidade para vetor de bytes
    private byte[] Serializar(T entidade)
    {
        // Pode usar JSON ou outra forma para serializar
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(entidade);
    }

    // Desserializa um vetor de bytes para uma entidade
    private T Desserializar(byte[] dados)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(dados);
    }
}