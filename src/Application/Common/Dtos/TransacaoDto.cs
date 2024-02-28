namespace Application.Common.Dtos;

public readonly record struct TransacaoDto(int Valor, string? Tipo)
{
    public string Descricao { get; init; } = string.Empty;
}
