namespace Infrastructure.Common.Factory;

internal sealed class ConnectionFactory(IConfiguration configuration) : IConnectionFactory
{
    private readonly IConfiguration _configuration = configuration;

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(
                        _configuration.GetConnectionString("DefaultConnection"));
    }
}
