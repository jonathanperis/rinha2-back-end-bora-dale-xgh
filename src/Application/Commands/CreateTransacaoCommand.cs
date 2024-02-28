namespace Application.Commands;

public readonly record struct CreateTransacaoCommand(int Id, TransacaoDto Transacao) : IRequest<CreateTransacaoCommandViewModel>;