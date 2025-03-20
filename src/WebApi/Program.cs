using Microsoft.AspNetCore.Mvc;
using Npgsql;
#if !EXTRAOPTIMIZE
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
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
// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: builder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")!,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);

// Configure OpenTelemetry tracing & metrics with auto-start using the
// AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(tpb =>
    {
        tpb
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        // Use IConfiguration binding for AspNetCore instrumentation options.
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));

        tpb.AddOtlpExporter(otlpOptions =>
        {
            // Use IConfiguration binding for AspNetCore instrumentation options.
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
        });
    })
    .WithMetrics(mpb =>
    {
        mpb
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()      
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        mpb.AddOtlpExporter(otlpOptions =>
        {
            // Use IConfiguration binding for AspNetCore instrumentation options.
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
        });
    });

// Clear default logging providers used by WebApplication host.
builder.Logging.ClearProviders();

// Configure OpenTelemetry Logging.
builder.Logging.AddOpenTelemetry(options =>
{
    // Note: See appsettings.json Logging:OpenTelemetry section for configuration.

    var resourceBuilder = ResourceBuilder.CreateDefault();
    configureResource(resourceBuilder);
    options.SetResourceBuilder(resourceBuilder);

    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;

    options.AddOtlpExporter(otlpOptions =>
    {
        // Use IConfiguration binding for AspNetCore instrumentation options.
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
    });

    // Add the Console exporter for local debugging.
    // options.AddConsoleExporter();    
});
#endif

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable("DATABASE_URL")!
);

builder.Services.AddHealthChecks();

var app = builder.Build();

#if !EXTRAOPTIMIZE
// app.MapPrometheusScrapingEndpoint();
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

#if !EXTRAOPTIMIZE
app.MapGet("/clientes/{id:int}/extrato", async (int id, [FromServices] ILogger<Program> logger, [FromServices] NpgsqlDataSource dataSource) =>
#else
app.MapGet("/clientes/{id:int}/extrato", async (int id, [FromServices] NpgsqlDataSource dataSource) =>
#endif
{
    if (!clientes.TryGetValue(id, out _))
        return Results.NotFound();
        
    await using (var cmd = dataSource.CreateCommand())
    {
#if !EXTRAOPTIMIZE
        logger.LogInformation("Starting GetSaldoClienteById for clientId: {id}", id);
#endif

        cmd.CommandText = "SELECT * FROM GetSaldoClienteById($1)";
        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.NotFound();

        var saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2));
        var jsonDoc = reader.GetFieldValue<JsonDocument>(3);
        var ultimasTransacoes = JsonSerializer.Deserialize<List<TransacaoDto>>(jsonDoc.RootElement, SourceGenerationContext.Default.ListTransacaoDto.Options);

        var extrato = new ExtratoDto(saldo, ultimasTransacoes);

#if !EXTRAOPTIMIZE
        logger.LogInformation("Finished GetSaldoClienteById for clientId: {id}", id);
#endif

        return Results.Ok(extrato);
    }
});

#if !EXTRAOPTIMIZE
app.MapPost("/clientes/{id:int}/transacoes", async (int id, [FromBody] TransacaoDto transacao, [FromServices] ILogger<Program> logger, [FromServices] NpgsqlDataSource dataSource) =>
#else
app.MapPost("/clientes/{id:int}/transacoes", async (int id, [FromBody] TransacaoDto transacao, [FromServices] NpgsqlDataSource dataSource) =>
#endif
{
    if (!clientes.TryGetValue(id, out int limite))
        return Results.NotFound();

    if (!IsTransacaoValid(transacao))
        return Results.UnprocessableEntity();

    await using (var cmd = dataSource.CreateCommand())
    {     
#if !EXTRAOPTIMIZE
        logger.LogInformation("Starting InsertTransacao for clientId: {id}", id);
#endif   

        cmd.CommandText = "SELECT InsertTransacao($1, $2, $3, $4)";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(transacao.Valor);
        cmd.Parameters.AddWithValue(transacao.Tipo);
        cmd.Parameters.AddWithValue(transacao.Descricao);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.UnprocessableEntity();

        var updatedSaldo = reader.GetInt32(0);

#if !EXTRAOPTIMIZE
        logger.LogInformation("Finished InsertTransacao for clientId: {id}", id);
#endif

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