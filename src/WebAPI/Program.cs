var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});

var app = builder.Build();

//var sampleTodos = new Todo[] {
//    new(1, "Walk the dog"),
//    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
//    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
//    new(4, "Clean the bathroom"),
//    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
//};

//var todosApi = app.MapGroup("/todos");
//todosApi.MapGet("/", () => sampleTodos);
//todosApi.MapGet("/{id}", (int id) =>
//    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
//        ? Results.Ok(todo)
//        : Results.NotFound());

app.MapGet("/extrato/{id:int}", async (int id, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new GetExtratoQuery(id), cancellationToken);

    return result.OperationResult switch
    {
        Application.Common.Models.OperationResult.NotFound => Results.NotFound(),
        Application.Common.Models.OperationResult.Success => Results.Ok(new { result.Saldo, result.UltimasTransacoes }),
        _ => Results.UnprocessableEntity(),
    };
});

app.MapPost("/transacoes/{id:int}", async (int id, TransacaoRequestDto transacao, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new CreateTransacaoCommand(id, transacao), cancellationToken);

    return result.OperationResult switch
    {
        Application.Common.Models.OperationResult.NotFound => Results.NotFound(),
        Application.Common.Models.OperationResult.Success => Results.Ok(new { result.Saldo, result.Limite }),
        _ => Results.UnprocessableEntity(),
    };
});

app.Run();

//public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

//[JsonSerializable(typeof(Todo[]))]
//internal partial class AppJsonSerializerContext : JsonSerializerContext
//{

//}
