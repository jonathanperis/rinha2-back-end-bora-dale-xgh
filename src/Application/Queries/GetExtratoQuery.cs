namespace Application.Queries;

public sealed record GetExtratoQuery(int id) : IRequest<GetExtratoQueryViewModel>;