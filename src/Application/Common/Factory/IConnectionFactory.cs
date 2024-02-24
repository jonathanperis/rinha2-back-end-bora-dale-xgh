namespace Application.Common.Factory;

public interface IConnectionFactory
{
    NpgsqlConnection CreateConnection();
}
