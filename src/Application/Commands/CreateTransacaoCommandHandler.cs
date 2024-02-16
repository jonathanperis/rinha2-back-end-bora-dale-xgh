namespace Application.Commands;

public sealed class CreateTransacaoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateTransacaoCommand, CreateTransacaoCommandViewModel>
{
    private readonly IApplicationDbContext _context = context;

    public async ValueTask<CreateTransacaoCommandViewModel> Handle(CreateTransacaoCommand request, CancellationToken cancellationToken)
    {
        var valorTransacao = request.Transacao.Tipo == 'c' ? request.Transacao.Valor : request.Transacao.Valor * -1;
        
        // var cliente = await _clienteRepository.GetClienteAsync(request.Id);
        var cliente = await _context.Clientes.FindAsync([request.Id], cancellationToken: cancellationToken);

        if (cliente is null)
            return new CreateTransacaoCommandViewModel(OperationResult.NotFound);

        cliente.Transacoes.Add(new Transacao
        {
            Valor = request.Transacao.Valor,
            Tipo = request.Transacao.Tipo,
            Descricao = request.Transacao.Descricao,
            ClienteId = request.Id
        });

        // _context.Transacoes.Add(new Transacao
        // {
        //     Valor = request.Transacao.Valor,
        //     Tipo = request.Transacao.Tipo,
        //     Descricao = request.Transacao.Descricao,
        //     ClienteId = request.Id
        // });

        //var result = await _context.Clientes
        //    .Where(x => x.Id == request.Id)
        //    .Where(x => x.SaldoInicial + valorTransacao >= x.Limite * -1 || valorTransacao > 0)
        //    .ExecuteUpdateAsync(x =>
        //        x.SetProperty(e => e.SaldoInicial, e => e.SaldoInicial + valorTransacao));

        //if (result == 0)
        //{
        //    return UnprocessableEntity("Limite excedido");
        //}

        //--------------------------------------------------------------------------------------------------------

        var result = await _context.Clientes
                                    .Where(x => x.Id == request.Id)
                                    .Where(x => x.SaldoInicial + valorTransacao >= x.Limite * -1 || valorTransacao > 0)
                                    .FirstOrDefaultAsync(cancellationToken);

        if (result != null)
        {
            result.SaldoInicial += valorTransacao;
        }

        await _context.SaveChangesAsync(cancellationToken);

        cliente = await _context.Clientes.FindAsync([request.Id], cancellationToken: cancellationToken);

        return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente?.SaldoInicial, cliente?.Limite);
    }
}