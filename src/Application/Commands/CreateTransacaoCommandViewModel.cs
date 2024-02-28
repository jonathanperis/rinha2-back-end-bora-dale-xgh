namespace Application.Commands;

public readonly record struct CreateTransacaoCommandViewModel(OperationResult OperationResult, ClienteDto Cliente = default);