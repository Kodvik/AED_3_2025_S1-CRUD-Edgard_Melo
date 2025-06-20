// Notas de Desenvolvimento:
// - Substituí a serialização com ToString() por JsonSerializer, que já funciona no CRUD.
// - Isso resolve o problema de dados inválidos ao serializar RegistroDeRede.
// - Mantive a genericidade para suportar outras entidades no futuro.
using AED_3_2025_S1_CRUD_Edgard_Melo.Models;

public class SerializadorEntidade<T> where T : IEntidade, new()
{
    public byte[] Serializar(T entidade)
    {
        // Usando JsonSerializer para uma serialização robusta e padrão
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(entidade);
    }

    public T Desserializar(byte[] dados)
    {
        // Desserialização direta com JsonSerializer, assumindo que os dados estão bem formados
        return System.Text.Json.JsonSerializer.Deserialize<T>(dados);
    }
}