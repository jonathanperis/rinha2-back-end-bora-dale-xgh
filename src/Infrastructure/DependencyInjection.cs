namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IConnectionFactory, ConnectionFactory>();

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ITransacaoRepository, TransacaoRepository>();
    }
}