namespace Application.Queries;

public readonly record struct GetExtratoQueryViewModel(OperationResult OperationResult, ExtratoDto Extrato = default);