using Microsoft.AspNetCore.Mvc;
using Npgsql;
#if !EXTRAOPTIMIZE
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.ResourceDetectors.Host;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#endif
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

#if !EXTRAOPTIMIZE
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
#endif

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable("DATABASE_URL")!
);

builder.Services.AddHealthChecks();

var app = builder.Build();

#if !EXTRAOPTIMIZE
app.MapPrometheusScrapingEndpoint();
#endif

var clientes = new Dictionary<int, int>
{
    { 1, 100000 }, 
    { 2, 80000 }, 
    { 3, 1000000 }, 
    { 4, 10000000 }, 
    { 5, 500000 }
};

app.MapHealthChecks("/healthz");

app.MapGet("/clientes/{id:int}/extrato", async (int id, [FromServices] NpgsqlDataSource dataSource) =>
{
    if (!clientes.TryGetValue(id, out _))
        return Results.NotFound();
        
    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = "SELECT * FROM GetSaldoClienteById($1)";
        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.NotFound();

        var saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2));
        var jsonDoc = reader.GetFieldValue<JsonDocument>(3);
        var ultimasTransacoes = JsonSerializer.Deserialize<List<TransacaoDto>>(jsonDoc.RootElement, SourceGenerationContext.Default.ListTransacaoDto.Options);

        var extrato = new ExtratoDto(saldo, ultimasTransacoes);

        return Results.Ok(extrato);
    }
});

app.MapPost("/clientes/{id:int}/transacoes", async (int id, [FromBody] TransacaoDto transacao, [FromServices] NpgsqlDataSource dataSource) =>
{
    if (!clientes.TryGetValue(id, out int limite))
        return Results.NotFound();

    if (!IsTransacaoValid(transacao))
        return Results.UnprocessableEntity();

    await using (var cmd = dataSource.CreateCommand())
    {      
        cmd.CommandText = "SELECT InsertTransacao($1, $2, $3, $4)";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(transacao.Valor);
        cmd.Parameters.AddWithValue(transacao.Tipo);
        cmd.Parameters.AddWithValue(transacao.Descricao);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.UnprocessableEntity();

        var updatedSaldo = reader.GetInt32(0);

        return Results.Ok(new ClienteDto(id, limite, updatedSaldo)); 
    }
});

app.Run();

static bool IsTransacaoValid(TransacaoDto transacao)
{
    ReadOnlySpan<char> tipoC = "c";
    ReadOnlySpan<char> tipoD = "d";

    return (transacao.Tipo.AsSpan().SequenceEqual(tipoC) || transacao.Tipo.AsSpan().SequenceEqual(tipoD))
           && !string.IsNullOrEmpty(transacao.Descricao)
           && transacao.Descricao.Length <= 10
           && transacao.Valor > 0;    
}

[JsonSerializable(typeof(ClienteDto))]
[JsonSerializable(typeof(ExtratoDto))]
[JsonSerializable(typeof(SaldoDto))]
[JsonSerializable(typeof(TransacaoDto))]
[JsonSerializable(typeof(List<TransacaoDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

internal readonly record struct ClienteDto(int Id, int Limite, int Saldo);
internal readonly record struct ExtratoDto(SaldoDto Saldo, List<TransacaoDto>? ultimas_transacoes);
internal readonly record struct SaldoDto(int Total, int Limite, DateTime data_extrato);
internal readonly record struct TransacaoDto(int Valor, string Tipo, string Descricao);