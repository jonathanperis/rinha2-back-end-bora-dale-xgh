namespace Domain.Entities;

public sealed record Cliente
{
    public int Id { get; set; }
    public int Limite { get; set; }
    public int SaldoInicial { get; set; }
    public List<Transacao> Transacoes { get; set; } = [];
}