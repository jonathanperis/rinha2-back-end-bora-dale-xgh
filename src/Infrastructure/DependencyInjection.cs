namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ApplicationDbContext>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ItransacaoRepository, TransacaoRepository>();
    }
}