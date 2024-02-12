namespace Application.Common.Repositories;

public interface ItransacaoRepository
{
    IAsyncEnumerable<TransacaoDto> ListUltimasTransacoes(int id);
}
