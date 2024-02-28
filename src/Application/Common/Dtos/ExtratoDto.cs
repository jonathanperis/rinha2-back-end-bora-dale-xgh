namespace Application.Common.Dtos;

public readonly record struct ExtratoDto(SaldoDto Saldo, List<TransacaoDto> ultimas_transacoes);