namespace Application.Common.Repositories;

public interface IClienteRepository
{
    Task<ClienteDto> GetClienteAsync(int id);

    Task<SaldoDto> GetSaldoClienteAsync(int id);
}
