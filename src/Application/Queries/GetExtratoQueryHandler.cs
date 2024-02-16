namespace Application.Queries;

public sealed class GetExtratoQueryHandler(IApplicationDbContext context) : IRequestHandler<GetExtratoQuery, GetExtratoQueryViewModel>
{
    private readonly IApplicationDbContext _context = context;

    public async ValueTask<GetExtratoQueryViewModel> Handle(GetExtratoQuery request, CancellationToken cancellationToken)
    {
        var saldo = await _context.Clientes.Where(x => x.Id == request.Id)
                .Select(x => new SaldoDto
                {
                    Total = x.SaldoInicial,
                    Limite = x.Limite
                })
                .FirstOrDefaultAsync(cancellationToken: cancellationToken); 

        if (saldo is null)
            return new GetExtratoQueryViewModel(OperationResult.NotFound);

        var ultimasTransacoes = new List<TransacaoDto>(10);

        var transacoes = _context.Transacoes.Where(x => x.ClienteId == request.Id)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Select(x => new TransacaoDto
                {
                    Valor = x.Valor,
                    Tipo = x.Tipo,
                    Descricao = x.Descricao,
                    RealizadoEm = x.RealizadoEm
                }).AsAsyncEnumerable();    

        await foreach (var transacao in transacoes)
        {
            ultimasTransacoes.Add(transacao);
        }

        return new GetExtratoQueryViewModel(OperationResult.Success, saldo, ultimasTransacoes);
    }
}