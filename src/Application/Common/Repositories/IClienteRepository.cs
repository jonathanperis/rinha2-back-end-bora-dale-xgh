namespace Application.Common.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> GetClienteAsync(int id);

    Task<SaldoDto?> GetSaldoClienteAsync(int id);
}
