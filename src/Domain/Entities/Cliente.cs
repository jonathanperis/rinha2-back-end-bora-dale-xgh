namespace Domain.Entities;

public sealed record Cliente
{
    public int Id { get; set; }
    public int Limite { get; set; }
    public int SaldoInicial { get; set; }
    public List<Transacao> Transacoes { get; set; } = [];

    public static Cliente Create(int id, int limite, int saldoInicial)
    {
        return new Cliente
        {
            Id = id,
            Limite = limite,
            SaldoInicial = saldoInicial
        };
    }
}