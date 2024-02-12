namespace Application.Common.Dtos;

public record struct ClienteDto
{
    public int Id { get; set; }
    public int Limite { get; set; }
    public int SaldoInicial { get; set; }
}
