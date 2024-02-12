namespace Application.Commands;

public sealed class CreateTransacaoCommandHandler(IApplicationDbContext context, IClienteRepository clienteRepository) : IRequestHandler<CreateTransacaoCommand, CreateTransacaoCommandViewModel>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IClienteRepository _clienteRepository = clienteRepository;

    public async ValueTask<CreateTransacaoCommandViewModel> Handle(CreateTransacaoCommand request, CancellationToken cancellationToken)
    {
        var valorTransacao = request.Transacao.Tipo == 'c' ? request.Transacao.Valor : request.Transacao.Valor * -1;
        var cliente = await _clienteRepository.GetClienteAsync(request.Id);
        
        using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        _context.Transacoes.Add(new Transacao
        {
            Valor = request.Transacao.Valor,
            Tipo = request.Transacao.Tipo,
            Descricao = request.Transacao.Descricao,
            ClienteId = request.Id
        });

        var result = await _context.Clientes
            .Where(x => x.Id == request.Id)
            .Where(x => x.SaldoInicial + valorTransacao >= x.Limite * -1 || valorTransacao > 0)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(e => e.SaldoInicial, e => e.SaldoInicial + valorTransacao));

        //if (result == 0)
        //{
        //    return UnprocessableEntity("Limite excedido");
        //}

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        cliente = await _clienteRepository.GetClienteAsync(request.Id);

        return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente.SaldoInicial, cliente.Limite);
    }
}