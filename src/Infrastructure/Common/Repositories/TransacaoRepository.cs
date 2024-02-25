namespace Infrastructure.Common.Repositories;

internal sealed class TransacaoRepository : ITransacaoRepository
{
    public async Task CreateTransacaoAsync(int Valor, char Tipo, string? Descricao, int ClienteId, DateTime RealizadoEm, NpgsqlConnection connection)
    {
        const string sql = @"
                            INSERT INTO public.""Transacoes"" (""Valor"", ""Tipo"", ""Descricao"", ""ClienteId"", ""RealizadoEm"")
                            VALUES (@Valor, @Tipo, @Descricao, @ClienteId, @RealizadoEm);
                            ";

        await connection.ExecuteAsync(sql, new
        {
            Valor,
            Tipo,
            Descricao,
            ClienteId,
            RealizadoEm
        });
    }

    public async Task<IEnumerable<TransacaoDto>?> ListTransacaoAsync(int ClienteId, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
                            FROM public.""Transacoes""
                            WHERE ""ClienteId"" = @ClienteId
                            ORDER BY ""Id"" DESC
                            LIMIT 10;
                            ";

        return await connection.QueryAsync<TransacaoDto>(sql, new { ClienteId });
    }
}
