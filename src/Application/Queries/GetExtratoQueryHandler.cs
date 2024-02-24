namespace Application.Queries;

public sealed class GetExtratoQueryHandler(IConnectionFactory connectionFactory) : IRequestHandler<GetExtratoQuery, GetExtratoQueryViewModel>
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;

    private const string sql1 = @"
                                SELECT ""SaldoInicial"" AS Total, ""Limite"" AS Limite
                                FROM public.""Clientes""
                                WHERE ""Id"" = @Id;
                                ";

    private const string sql2 = @"
                                SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
                                FROM public.""Transacoes""
                                WHERE ""ClienteId"" = @ClienteId
                                ORDER BY ""Id"" DESC
                                LIMIT 10;
                                ";

    public async ValueTask<GetExtratoQueryViewModel> Handle(GetExtratoQuery request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var saldo = connection.QueryFirstOrDefault<SaldoDto>(sql1, new { request.Id });

        if (saldo is null)
            return new GetExtratoQueryViewModel(OperationResult.NotFound);

        var ultimasTransacoes = new List<TransacaoDto>(10);

        ultimasTransacoes = connection.Query<TransacaoDto>(sql2, new { ClienteId = request.Id }).ToList();
        
        return new GetExtratoQueryViewModel(OperationResult.Success, saldo, ultimasTransacoes);
    }
}