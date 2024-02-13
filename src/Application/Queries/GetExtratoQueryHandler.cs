namespace Application.Queries;

public sealed class GetExtratoQueryHandler(IClienteRepository clienteRepository, ItransacaoRepository transacaoRepository) : IRequestHandler<GetExtratoQuery, GetExtratoQueryViewModel>
{
    private readonly IClienteRepository _clienteRepository = clienteRepository;
    private readonly ItransacaoRepository _transacaoRepository = transacaoRepository;

    public async ValueTask<GetExtratoQueryViewModel> Handle(GetExtratoQuery request, CancellationToken cancellationToken)
    {
        var saldo = await _clienteRepository.GetSaldoClienteAsync(request.Id);

        if (saldo is null)
            return new GetExtratoQueryViewModel(OperationResult.NotFound);

        var ultimasTransacoes = new List<TransacaoDto>(10);

        await foreach (var transacao in _transacaoRepository.ListUltimasTransacoes(request.Id))
        {
            ultimasTransacoes.Add(transacao);
        }

        return new GetExtratoQueryViewModel(OperationResult.Success, saldo, ultimasTransacoes);
    }
}