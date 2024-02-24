namespace Application.Commands;

public sealed class CreateTransacaoCommandHandler(IConnectionFactory connectionFactory) : IRequestHandler<CreateTransacaoCommand, CreateTransacaoCommandViewModel>
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;

    private const string sql1 = @"
                                SELECT ""Id"", ""Limite"", ""SaldoInicial""
                                FROM public.""Clientes""
                                WHERE ""Id"" = @Id;
                                ";

    private const string sql2 = @"
                                INSERT INTO public.""Transacoes"" (""Valor"", ""Tipo"", ""Descricao"", ""ClienteId"", ""RealizadoEm"")
                                VALUES (@Valor, @Tipo, @Descricao, @ClienteId, @RealizadoEm);
                                ";

    private const string sql3 = @"
                                UPDATE public.""Clientes""
                                SET ""SaldoInicial"" = ""SaldoInicial"" + @valorTransacao
                                WHERE ""Id"" = @Id
                                AND (""SaldoInicial"" + @valorTransacao >= ""Limite"" * -1 OR @valorTransacao > 0);
                                ";

    public async ValueTask<CreateTransacaoCommandViewModel> Handle(CreateTransacaoCommand request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var cliente = connection.QueryFirstOrDefault<Cliente>(sql1, new { request.Id });

        if (cliente is null)
            return new CreateTransacaoCommandViewModel(OperationResult.NotFound);

        connection.Execute(sql2, new
        {
            request.Transacao.Valor,
            request.Transacao.Tipo,
            request.Transacao.Descricao,
            ClienteId = request.Id,
            RealizadoEm = DateTime.UtcNow
        });

        var valorTransacao = request.Transacao.Tipo == 'c' ? request.Transacao.Valor : request.Transacao.Valor * -1;

        var result = connection.Execute(sql3, new { request.Id, valorTransacao });

        if (result == 0)
        {
            return new CreateTransacaoCommandViewModel(OperationResult.Failed);
        }

        cliente = connection.QueryFirstOrDefault<Cliente>(sql1, new { request.Id });

        return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente?.SaldoInicial, cliente?.Limite);
    }
}