namespace Application.Common.Dtos;

public sealed record TransacaoDto
{
    public int Valor { get; set; }    
    public string? Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
