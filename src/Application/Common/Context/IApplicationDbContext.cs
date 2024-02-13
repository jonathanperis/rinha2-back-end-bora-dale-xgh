namespace Application.Common.Context;

public interface IApplicationDbContext
{
    DbSet<Cliente> Clientes { get; set; }
    DbSet<Transacao> Transacoes { get; set; }

    DatabaseFacade Database { get; } 

    Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default);
}