using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

var app = builder.Build();

var clientes = new Dictionary<int, int>
{
    {1,   1000 * 100},
    {2,    800 * 100},
    {3,  10000 * 100},
    {4, 100000 * 100},
    {5,   5000 * 100}
};

app.MapGet("/", () => "Hello World!");

app.MapGet("/clientes/{id:int}/extrato", async (int id, NpgsqlDataSource dataSource) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    SaldoDto saldo;

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = @"
                            SELECT ""Id"", ""SaldoInicial"" AS Total, ""Limite"" AS Limite, NOW() AS data_extrato
                            FROM public.""Clientes""
                            WHERE ""Id"" = $1;
                            ";

        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetDateTime(3));

        if (saldo.Id == 0)
            return Results.NotFound();
    }

    await using (var cmd = dataSource.CreateCommand())
    {
        cmd.CommandText = @"
                            SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
                            FROM public.""Transacoes""
                            WHERE ""ClienteId"" = $1
                            ORDER BY ""Id"" DESC
                            LIMIT 10;
                            ";

        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        var ultimasTransacoes = new List<TransacaoDto>();

        while (await reader.ReadAsync())
        {
            ultimasTransacoes.Add(new TransacaoDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return Results.Ok(new ExtratoDto(saldo, ultimasTransacoes));      
    }

    //------------------------------------

    // if (!clientes.ContainsKey(id))
    //     return Results.NotFound();

    // using var cmd = dataSource.CreateCommand();
    // cmd.CommandText = @"
    //     SELECT ""Id"", ""SaldoInicial"" AS Total, ""Limite"" AS Limite, NOW() AS data_extrato,
    //            (SELECT array_to_json(array_agg(row(""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm"")))
    //             FROM (
    //                 SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
    //                 FROM public.""Transacoes""
    //                 WHERE ""ClienteId"" = $1
    //                 ORDER BY ""Id"" DESC
    //                 LIMIT 10
    //             ) AS t) AS ultimas_transacoes
    //     FROM public.""Clientes""
    //     WHERE ""Id"" = $1;
    // ";

    // cmd.Parameters.AddWithValue(id);

    // using var reader = await cmd.ExecuteReaderAsync();
    // await reader.ReadAsync();

    // if (!reader.HasRows)
    //     return Results.NotFound();

    // var saldo = new SaldoDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetDateTime(3));
    // var ultimasTransacoes = JsonSerializer.Deserialize(reader.GetString(4), SourceGenerationContext.Default.ListTransacaoDto);

    // return Results.Ok(new ExtratoDto(saldo, ultimasTransacoes!));    
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
                            SELECT ""Id"", ""Limite"", ""SaldoInicial"" AS Saldo
                            FROM public.""Clientes""
                            WHERE ""Id"" = $1;
                            ";

        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var cliente = new ClienteDto(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));

        if (cliente.Id == 0)
            return Results.NotFound();
    }

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
// [JsonSerializable(typeof(List<TransacaoDto?>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

internal readonly record struct ClienteDto(int Id, int Limite, int Saldo);
internal readonly record struct ExtratoDto(SaldoDto Saldo, List<TransacaoDto> ultimas_transacoes);
internal readonly record struct SaldoDto(int Id, int Total, int Limite, DateTime data_extrato);
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