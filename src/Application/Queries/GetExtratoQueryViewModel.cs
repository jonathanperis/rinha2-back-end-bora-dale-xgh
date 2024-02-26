namespace Application.Queries;

public sealed record GetExtratoQueryViewModel(OperationResult OperationResult, ExtratoDto? Extrato = default);