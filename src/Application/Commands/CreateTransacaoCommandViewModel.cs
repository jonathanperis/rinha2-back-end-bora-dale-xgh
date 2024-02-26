namespace Application.Commands;

public sealed record CreateTransacaoCommandViewModel(OperationResult OperationResult, ClienteDto? Cliente = default);