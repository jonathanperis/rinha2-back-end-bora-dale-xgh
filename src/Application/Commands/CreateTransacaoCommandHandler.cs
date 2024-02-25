namespace Application.Commands;

public sealed class CreateTransacaoCommandHandler(IConnectionFactory connectionFactory,
                                                    IClienteRepository clienteRepository,
                                                    ITransacaoRepository transacaoRepository) : IRequestHandler<CreateTransacaoCommand, CreateTransacaoCommandViewModel>
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;
    private readonly IClienteRepository _clienteRepository = clienteRepository;
    private readonly ITransacaoRepository _transacaoRepository = transacaoRepository;

    public async ValueTask<CreateTransacaoCommandViewModel> Handle(CreateTransacaoCommand request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var cliente = await _clienteRepository.GetClienteAsync(request.Id, connection);

        if (cliente is null)
            return new CreateTransacaoCommandViewModel(OperationResult.NotFound);

        await _transacaoRepository.CreateTransacaoAsync(
                        request.Transacao.Valor,
                        request.Transacao.Tipo,
                        request.Transacao.Descricao,
                        request.Id,
                        DateTime.UtcNow,
                        connection);

        var valorTransacao = request.Transacao.Tipo == 'c' ? request.Transacao.Valor : request.Transacao.Valor * -1;

        var success = await _clienteRepository.UpdateSaldoClienteAsync(request.Id, valorTransacao, connection);

        if (!success)
        {
            return new CreateTransacaoCommandViewModel(OperationResult.Failed);
        }

        cliente = await _clienteRepository.GetClienteAsync(request.Id, connection);

        await transaction.CommitAsync(cancellationToken);

        return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente?.SaldoInicial, cliente?.Limite);
    }
}