namespace Application.Queries;

public sealed record GetExtratoQueryViewModel(OperationResult OperationResult, SaldoDto? Saldo = default, List<TransacaoDto>? UltimasTransacoes = default);