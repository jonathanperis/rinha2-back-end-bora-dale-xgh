namespace Infrastructure.Common.Context;

public sealed class ApplicationDbContext(IConfiguration configuration) : DbContext
{
    private readonly IConfiguration? _configuration = configuration;

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Transacao> Transacoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CreateSeedData();

        modelBuilder.ApplyConfiguration(new ClienteMap());
        modelBuilder.ApplyConfiguration(new TransacaoMap());

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseNpgsql(_configuration?.GetConnectionString("DefaultConnection"))
                .EnableDetailedErrors();
        }
    }

    public async new Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken) > 0 ? OperationResult.Success : OperationResult.Failed;
    }

    private static void CreateSeedData()
    {
        ClienteMap.Clientes = 
        [
            Cliente.Create(1, 100000, 0),
            Cliente.Create(2, 80000, 0),
            Cliente.Create(3, 1000000, 0),
            Cliente.Create(4, 10000000, 0),
            Cliente.Create(5, 500000, 0)
        ];
    }
}