namespace Application.Common.Dtos;

public record struct TransacaoDto
{
    public int Valor { get; set; }
    public char Tipo { get; set; }
    public string? Descricao { get; set; }
    public DateTime RealizadoEm { get; set; }
}
