namespace Application.Common.Dtos;

public sealed record TransacaoDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Valor { get; set; }
    
    [Required]
    [AllowedValues('c', 'd')]
    public char Tipo { get; set; }

    [Required]
    [MaxLength(10)]
    public string Descricao { get; set; } = string.Empty;
}
