namespace Infrastructure.Common.Repositories;

public sealed class ClienteRepository(ApplicationDbContext context) : IClienteRepository
{
    private static readonly Func<ApplicationDbContext, int, Task<ClienteDto>> GetCliente
    = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) => context.Clientes
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(y => new ClienteDto
                {
                    Id = y.Id,
                    Limite = y.Limite,
                    SaldoInicial = y.SaldoInicial,
                })
                .SingleOrDefault());

    private static readonly Func<ApplicationDbContext, int, Task<SaldoDto>> GetSaldoCliente
    = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) => context.Clientes
                .Where(x => x.Id == id)
                .Select(x => new SaldoDto
                {
                    Total = x.SaldoInicial,
                    Limite = x.Limite
                })
            .First());

    public async Task<ClienteDto> GetClienteAsync(int id)
    {
        return await GetCliente(context, id);
    }

    public async Task<SaldoDto> GetSaldoClienteAsync(int id)
    {
        return await GetSaldoCliente(context, id);
    }
}
