namespace Application.Common.Repositories;

public interface IClienteRepository
{
    SaldoDto GetSaldoTotal(int Id, NpgsqlConnection connection);

    ClienteDto GetCliente(int Id, NpgsqlConnection connection);

    bool UpdateSaldoCliente(int Id, int ValorTransacao, NpgsqlConnection connection);
}
