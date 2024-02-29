namespace WebApi.Dtos;

public readonly record struct TransacaoDto(int Valor, string Tipo, string Descricao)
{
    private readonly static string[] tipoTransacao = ["c", "d"];

    public bool Valida()
    {
        return tipoTransacao.Contains(Tipo)
            && !string.IsNullOrEmpty(Descricao)
            && Descricao.Length <= 10
            && Valor > 0;
    }
}