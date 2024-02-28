namespace Application.Common.Repositories;

public interface ITransacaoRepository
{
    void CreateTransacao(int Valor, string Tipo, string Descricao, int ClienteId, DateTime RealizadoEm, NpgsqlConnection connection);

    IEnumerable<TransacaoDto> ListTransacao(int ClienteId, NpgsqlConnection connection);
}
