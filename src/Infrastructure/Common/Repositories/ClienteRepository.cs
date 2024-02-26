namespace Infrastructure.Common.Repositories;

[DapperAot]
internal sealed class ClienteRepository : IClienteRepository
{
    public ClienteDto? GetCliente(int Id, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""Id"", ""Limite"", ""SaldoInicial"" AS Saldo
                            FROM public.""Clientes""
                            WHERE ""Id"" = @Id;
                            ";

        return connection.QueryFirstOrDefault<ClienteDto>(sql, new { Id });
    }

    public SaldoDto? GetSaldoTotal(int Id, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""SaldoInicial"" AS Total, ""Limite"" AS Limite
                            FROM public.""Clientes""
                            WHERE ""Id"" = @Id;
                            ";

        return connection.QueryFirstOrDefault<SaldoDto>(sql, new { Id });
    }

    public bool UpdateSaldoCliente(int Id, int ValorTransacao, NpgsqlConnection connection)
    {
        const string sql = @"
                            UPDATE public.""Clientes""
                            SET ""SaldoInicial"" = ""SaldoInicial"" + @ValorTransacao
                            WHERE ""Id"" = @Id
                            AND (""SaldoInicial"" + @ValorTransacao >= ""Limite"" * -1 OR @ValorTransacao > 0);
                            ";

        var result = connection.Execute(sql, new { Id, ValorTransacao });

        return result == 1;
    }
}
