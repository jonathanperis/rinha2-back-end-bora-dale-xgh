namespace Application.Common.Dtos;

public sealed record TransacaoDto
{
    [Range(1, int.MaxValue)]
    public int Valor { get; set; }

    [AllowedValues('c', 'd')]
    public char Tipo { get; set; }

    [Required]
    [MaxLength(10)]
    public string? Descricao { get; set; }
}
