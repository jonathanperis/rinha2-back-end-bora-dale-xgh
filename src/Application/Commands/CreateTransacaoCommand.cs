namespace Application.Commands;

public sealed record CreateTransacaoCommand(int Id, TransacaoRequest Transacao) : IRequest<CreateTransacaoCommandViewModel>;