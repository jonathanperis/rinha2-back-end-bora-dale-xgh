namespace Application.Common.Dtos;

public sealed record SaldoDto
{
    public int Total { get; set; }
    public int Limite { get; set; }
    public DateTime DataExtrato { get; set; } = DateTime.UtcNow;
}
