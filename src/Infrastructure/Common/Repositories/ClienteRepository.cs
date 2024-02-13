namespace Infrastructure.Common.Repositories;

public sealed class ClienteRepository(ApplicationDbContext context) : IClienteRepository
{
    private static readonly Func<ApplicationDbContext, int, Task<Cliente?>> GetCliente
        = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) => context.Clientes
                .AsNoTracking()
                .SingleOrDefault(x => x.Id == id));

    private static readonly Func<ApplicationDbContext, int, Task<SaldoDto?>> GetSaldoCliente
        = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) => context.Clientes
                .Where(x => x.Id == id)
                .Select(x => new SaldoDto
                {
                    Total = x.SaldoInicial,
                    Limite = x.Limite
                })
                .FirstOrDefault());

    public async Task<Cliente?> GetClienteAsync(int id)
    {
        return await GetCliente(context, id);
    }

    public async Task<SaldoDto?> GetSaldoClienteAsync(int id)
    {
        return await GetSaldoCliente(context, id);
    }
}
