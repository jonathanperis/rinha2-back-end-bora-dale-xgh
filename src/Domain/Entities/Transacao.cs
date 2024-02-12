namespace Domain.Entities;

public sealed record Transacao
{
    public int Id { get; set; }
    public int Valor { get; set; }
    public int ClienteId { get; set; }
    public char Tipo { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime RealizadoEm { get; set; } = DateTime.UtcNow;
}