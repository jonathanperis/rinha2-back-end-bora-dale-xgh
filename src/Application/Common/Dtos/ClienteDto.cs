namespace Application.Common.Dtos;

public sealed record ClienteDto
{
    public int Limite { get; set; }

    public int Saldo { get; set; }
}
