namespace Application.Queries;

public sealed class GetExtratoQueryHandler(IConnectionFactory connectionFactory,
                                             IClienteRepository clienteRepository,
                                             ITransacaoRepository transacaoRepository) : IRequestHandler<GetExtratoQuery, GetExtratoQueryViewModel>
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;
    private readonly IClienteRepository _clienteRepository = clienteRepository;
    private readonly ITransacaoRepository _transacaoRepository = transacaoRepository;

    public async ValueTask<GetExtratoQueryViewModel> Handle(GetExtratoQuery request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var saldo = _clienteRepository.GetSaldoTotal(request.Id, connection);

        // if (saldo is null)
        //     return new GetExtratoQueryViewModel(OperationResult.NotFound);

        var ultimasTransacoes = _transacaoRepository.ListTransacao(request.Id, connection);
        
        return new GetExtratoQueryViewModel(OperationResult.Success, new ExtratoDto(saldo, ultimasTransacoes.ToList()));
    }
}