namespace Application.Queries;

public sealed class GetExtratoQueryHandler(IApplicationDbContext context, IClienteRepository clienteRepository) : IRequestHandler<GetExtratoQuery, GetExtratoQueryViewModel>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IClienteRepository _clienteRepository = clienteRepository;

    public async ValueTask<GetExtratoQueryViewModel> Handle(GetExtratoQuery request, CancellationToken cancellationToken)
    {
        //var recurso = await _context.Recursos
        //    .AsNoTracking()
        //    .Where(x => x.Id == request.Id && x.Ativo)
        //    .Select(x => x.MapToDto())
        //    .FirstOrDefaultAsync(cancellationToken);

        //if (recurso is null)
        //{
        //    return new GetExtratoQueryViewModel { OperationResult = OperationResult.NotFound };
        //}

        return new GetExtratoQueryViewModel { OperationResult = OperationResult.Success };
    }
}