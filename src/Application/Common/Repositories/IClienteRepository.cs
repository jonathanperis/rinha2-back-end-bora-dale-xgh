namespace Application.Common.Repositories;

public interface IClienteRepository
{
    Task<SaldoDto?> GetSaldoTotalAsync(int Id, NpgsqlConnection connection);

    Task<ClienteDto?> GetClienteAsync(int Id, NpgsqlConnection connection);

    Task<bool> UpdateSaldoClienteAsync(int Id, int ValorTransacao, NpgsqlConnection connection);
}
