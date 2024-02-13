namespace Application.Common.Dtos;

public sealed record TransacaoDto
{
    public int Valor { get; set; }
    public char Tipo { get; set; }
    public string? Descricao { get; set; }
    public DateTime RealizadoEm { get; set; }
}
