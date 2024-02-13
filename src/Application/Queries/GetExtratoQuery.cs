namespace Application.Queries;

public sealed record GetExtratoQuery(int Id) : IRequest<GetExtratoQueryViewModel>;