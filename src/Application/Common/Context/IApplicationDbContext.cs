namespace Application.Common.Context;

public interface IApplicationDbContext
{
    DbSet<Cliente> Clientes { get; set; }
    DbSet<Transacao> Transacoes { get; set; }

    Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default);
}