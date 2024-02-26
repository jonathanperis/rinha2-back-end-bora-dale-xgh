namespace Application.Common.Dtos;

public sealed record ExtratoDto
{
    public SaldoDto? Saldo { get; set; }

    [JsonPropertyName("ultimas_transacoes")]
    public List<TransacaoDto>? UltimasTransacoes { get; set; }
}
