using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.ResourceDetectors.Host;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

Action<ResourceBuilder> appResourceBuilder =
    resource => resource
        .AddDetector(new ContainerResourceDetector())
        .AddDetector(new HostDetector());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(appResourceBuilder)
    .WithTracing(tracerBuilder => tracerBuilder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(meterBuilder => meterBuilder
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
        
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

var clientes = new Dictionary<int, int>
{
    {1,   1000 * 100},
    {2,    800 * 100},
    {3,  10000 * 100},
    {4, 100000 * 100},
    {5,   5000 * 100}
};

app.MapGet("/", () => "Hello World!!");

app.MapGet("/clientes/{id:int}/extrato", async (int id, [FromServices] NpgsqlDataSource dataSource) =>
{
    // if (!clientes.ContainsKey(id))
    //     return Results.NotFound();

    // SaldoDto saldo;

    // await using (var cmd = dataSource.CreateCommand())
    // {
    //     cmd.CommandText = @"
    //                         SELECT ""SaldoInicial"" AS Total, ""Limite"" AS Limite, NOW() AS data_extrato
    //                         FROM public.""Clientes""
    //                         WHERE ""Id"" = $1;
    //                         ";

    //     cmd.Parameters.AddWithValue(id);

    //     using var reader = await cmd.ExecuteReaderAsync();
    //     await reader.ReadAsync();

    //     saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2));
    // }

    // await using (var cmd = dataSource.CreateCommand())
    // {
    //     cmd.CommandText = @"
    //                         SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
    //                         FROM public.""Transacoes""
    //                         WHERE ""ClienteId"" = $1
    //                         ORDER BY ""Id"" DESC
    //                         LIMIT 10;
    //                         ";

    //     cmd.Parameters.AddWithValue(id);

    //     using var reader = await cmd.ExecuteReaderAsync();

    //     var ultimasTransacoes = new List<TransacaoDto>();

    //     while (await reader.ReadAsync())
    //     {
    //         ultimasTransacoes.Add(new TransacaoDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
    //     }

    //     return Results.Ok(new ExtratoDto(saldo, ultimasTransacoes));      
    // }   

    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    SaldoDto saldo;

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = "SELECT * FROM GetSaldoClienteById($1)";
        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.NotFound();

        saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2));
    }

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = "SELECT GetUltimasTransacoes($1)";
        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        Console.WriteLine($"Value: {reader.GetString(0)}");

        var ultimasTransacoes = JsonSerializer.Deserialize(reader.GetString(0), SourceGenerationContext.Default.ListTransacaoDto);
        return Results.Ok(new ExtratoDto(saldo, ultimasTransacoes));
    }
});

app.MapPost("/clientes/{id:int}/transacoes", async (int id, TransacaoDto transacao, NpgsqlDataSource dataSource) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = @"
                            INSERT INTO public.""Transacoes"" (""Valor"", ""Tipo"", ""Descricao"", ""ClienteId"", ""RealizadoEm"")
                            VALUES ($1, $2, $3, $4, $5);
                            ";

        cmd.Parameters.AddWithValue(transacao.Valor);
        cmd.Parameters.AddWithValue(transacao.Tipo!);
        cmd.Parameters.AddWithValue(transacao.Descricao);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = @"
                    UPDATE public.""Clientes""
                    SET ""SaldoInicial"" = ""SaldoInicial"" + $2
                    WHERE ""Id"" = $1
                    AND (""SaldoInicial"" + $2 >= ""Limite"" * -1 OR $2 > 0);
                    ";

        var valorTransacao = transacao.Tipo == "c" ? transacao.Valor : transacao.Valor * -1;

        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(valorTransacao);

        var success = await cmd.ExecuteNonQueryAsync() == 1;

        if (!success)
        {
            return Results.UnprocessableEntity();
        }
    }

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = @"
                            SELECT ""Id"", ""Limite"", ""SaldoInicial"" AS Saldo
                            FROM public.""Clientes""
                            WHERE ""Id"" = $1;
                            ";

        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var cliente = new ClienteDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));

        return Results.Ok(cliente); 
    }
});

app.Run();

[JsonSerializable(typeof(ClienteDto))]
[JsonSerializable(typeof(ExtratoDto))]
[JsonSerializable(typeof(SaldoDto))]
[JsonSerializable(typeof(TransacaoDto))]
[JsonSerializable(typeof(List<TransacaoDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

internal readonly record struct ClienteDto(int Id, int Limite, int Saldo);
internal readonly record struct ExtratoDto(SaldoDto Saldo, List<TransacaoDto>? ultimas_transacoes);
internal readonly record struct SaldoDto(int Total, int Limite, DateTime data_extrato);
internal readonly record struct TransacaoDto(int Valor, string Tipo, string Descricao)
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