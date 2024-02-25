var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/clientes/{id:int}/transacoes", async (int id, TransacaoDto transacao, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new CreateTransacaoCommand(id, transacao), cancellationToken);

    return result.OperationResult switch
    {
        Application.Common.Models.OperationResult.NotFound => Results.NotFound(),
        Application.Common.Models.OperationResult.Failed => Results.UnprocessableEntity(),
        Application.Common.Models.OperationResult.Success => Results.Ok(new { result.Saldo, result.Limite }),
        _ => Results.NoContent(),
    };
});

app.MapGet("/clientes/{id:int}/extrato", async (int id, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new GetExtratoQuery(id), cancellationToken);

    return result.OperationResult switch
    {
        Application.Common.Models.OperationResult.NotFound => Results.NotFound(),
        Application.Common.Models.OperationResult.Failed => Results.UnprocessableEntity(),
        Application.Common.Models.OperationResult.Success => Results.Ok(new { result.Saldo, ultimas_transacoes = result.UltimasTransacoes }),
        _ => Results.NoContent(),
    };
});

app.Run();