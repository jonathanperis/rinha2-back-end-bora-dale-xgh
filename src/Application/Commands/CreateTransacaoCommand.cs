namespace Application.Commands;

public sealed record CreateTransacaoCommand(int Id, TransacaoRequestDto Transacao) : IRequest<CreateTransacaoCommandViewModel>;