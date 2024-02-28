namespace Application.Queries;

public readonly record struct GetExtratoQuery(int Id) : IRequest<GetExtratoQueryViewModel>;