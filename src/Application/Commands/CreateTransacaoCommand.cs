namespace Application.Commands;

public sealed record CreateTransacaoCommand(int Id, TransacaoDto Transacao) : IRequest<CreateTransacaoCommandViewModel>;