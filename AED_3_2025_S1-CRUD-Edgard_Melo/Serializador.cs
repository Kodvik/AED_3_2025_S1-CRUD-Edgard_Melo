using System.Text;

public class SerializadorEntidade<T> where T : IEntidade, new()
{
    public byte[] Serializar(T entidade)
    {
        // Serialização simples (pode ser adaptada)
        string serializado = $"{entidade.UID},{entidade.ToString()}";
        return Encoding.UTF8.GetBytes(serializado);
    }

    public T Desserializar(byte[] dados)
    {
        string serializado = Encoding.UTF8.GetString(dados);
        string[] campos = serializado.Split(',');

        T entidade = new T();
        entidade.UID = int.Parse(campos[0]);
        // Desserializar outros campos aqui
        return entidade;
    }
}
