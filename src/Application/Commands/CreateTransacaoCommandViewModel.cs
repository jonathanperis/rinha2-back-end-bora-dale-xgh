namespace Application.Commands;

public sealed record CreateTransacaoCommandViewModel(OperationResult OperationResult, int Saldo = default, int Limite = default);