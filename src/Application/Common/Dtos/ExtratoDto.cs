namespace Application.Common.Dtos;

public sealed record ExtratoDto
{
    public SaldoDto? Saldo { get; set; }
    public List<TransacaoDto>? UltimasTransacoes { get; set; }
}
