namespace Application.Common.Repositories;

public interface ITransacaoRepository
{
    Task CreateTransacaoAsync(int Valor, char Tipo, string? Descricao, int ClienteId, DateTime RealizadoEm, NpgsqlConnection connection);

    Task<IEnumerable<TransacaoDto>?> ListTransacaoAsync(int ClienteId, NpgsqlConnection connection);
}
