namespace Infrastructure.Common.Repositories;

[DapperAot]
internal sealed class TransacaoRepository() : ITransacaoRepository
{
    public void CreateTransacao(int Valor, string Tipo, string Descricao, int ClienteId, DateTime RealizadoEm, NpgsqlConnection connection)
    {
        const string sql = @"
                            INSERT INTO public.""Transacoes"" (""Valor"", ""Tipo"", ""Descricao"", ""ClienteId"", ""RealizadoEm"")
                            VALUES (@Valor, @Tipo, @Descricao, @ClienteId, @RealizadoEm);
                            ";

        connection.Execute(sql, new
        {
            Valor,
            Tipo,
            Descricao,
            ClienteId,
            RealizadoEm
        });
    }

    public IEnumerable<TransacaoDto> ListTransacao(int ClienteId, NpgsqlConnection connection)
    {
        const string sql = @"
                            SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
                            FROM public.""Transacoes""
                            WHERE ""ClienteId"" = @ClienteId
                            ORDER BY ""Id"" DESC
                            LIMIT 10;
                            ";

        return connection.Query<TransacaoDto>(sql, new { ClienteId });
    }
}
