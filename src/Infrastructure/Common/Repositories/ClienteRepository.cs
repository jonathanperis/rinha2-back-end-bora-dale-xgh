namespace Infrastructure.Common.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    public async Task<ClienteDto?> GetClienteAsync(int Id, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""Id"", ""Limite"", ""SaldoInicial""
                            FROM public.""Clientes""
                            WHERE ""Id"" = @Id;
                            ";

        return await connection.QueryFirstOrDefaultAsync<ClienteDto>(sql, new { Id });
    }

    public async Task<SaldoDto?> GetSaldoTotalAsync(int Id, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""SaldoInicial"" AS Total, ""Limite"" AS Limite
                            FROM public.""Clientes""
                            WHERE ""Id"" = @Id;
                            ";

        return await connection.QueryFirstOrDefaultAsync<SaldoDto>(sql, new { Id });
    }

    public async Task<bool> UpdateSaldoClienteAsync(int Id, int ValorTransacao, NpgsqlConnection connection)
    {
        const string sql = @"
                            UPDATE public.""Clientes""
                            SET ""SaldoInicial"" = ""SaldoInicial"" + @ValorTransacao
                            WHERE ""Id"" = @Id
                            AND (""SaldoInicial"" + @ValorTransacao >= ""Limite"" * -1 OR @ValorTransacao > 0);
                            ";

        var result = await connection.ExecuteAsync(sql, new { Id, ValorTransacao });

        return result == 1;
    }
}
