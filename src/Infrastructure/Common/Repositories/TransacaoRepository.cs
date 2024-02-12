namespace Infrastructure.Common.Repositories;

public sealed class TransacaoRepository(ApplicationDbContext context) : ItransacaoRepository
{
    private static readonly Func<ApplicationDbContext, int, IAsyncEnumerable<TransacaoDto>> GetUltimasTransacoes
    = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) => context.Transacoes
                .Where(x => x.ClienteId == id)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Select(x => new TransacaoDto
                {
                    Valor = x.Valor,
                    Tipo = x.Tipo,
                    Descricao = x.Descricao,
                    RealizadoEm = x.RealizadoEm
                }));

    public IAsyncEnumerable<TransacaoDto> ListUltimasTransacoes(int id)
    {
        return GetUltimasTransacoes(context, id);
    }
}
