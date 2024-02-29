using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
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

app.MapGet("/clientes/{id:int}/extrato", async (int id, NpgsqlConnection connection) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    await using (connection)
    {
        await connection.OpenAsync();

        SaldoDto saldo;

        var cmd = connection.CreateCommand();
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

        var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
                            SELECT ""Valor"", ""Tipo"", ""Descricao"", ""RealizadoEm""
                            FROM public.""Transacoes""
                            WHERE ""ClienteId"" = $1
                            ORDER BY ""Id"" DESC
                            LIMIT 10;
                            ";

        cmd2.Parameters.AddWithValue(id);

        using var reader2 = await cmd2.ExecuteReaderAsync();

        var ultimasTransacoes = new List<TransacaoDto>();

        while (await reader2.ReadAsync())
        {
            ultimasTransacoes.Add(new TransacaoDto(reader2.GetInt32(0), reader2.GetString(1), reader2.GetString(2)));
        }

        return Results.Ok(new ExtratoDto(saldo, ultimasTransacoes));
    }
});

app.MapPost("/clientes/{id:int}/transacoes", async (int id, TransacaoDto transacao, NpgsqlConnection connection) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    await using (connection)
    {
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
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

        cmd = connection.CreateCommand();
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

        var valorTransacao = transacao.Tipo == "c" ? transacao.Valor : transacao.Valor * -1;

        cmd = connection.CreateCommand();
        cmd.CommandText = @"
                            UPDATE public.""Clientes""
                            SET ""SaldoInicial"" = ""SaldoInicial"" + $2
                            WHERE ""Id"" = $1
                            AND (""SaldoInicial"" + $2 >= ""Limite"" * -1 OR $2 > 0);
                            ";

        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(valorTransacao);

        var success = await cmd.ExecuteNonQueryAsync() == 1;

        if (!success)
        {
            return Results.UnprocessableEntity();
        }

        cmd = connection.CreateCommand();
        cmd.CommandText = @"
                            SELECT ""Id"", ""Limite"", ""SaldoInicial"" AS Saldo
                            FROM public.""Clientes""
                            WHERE ""Id"" = $1;
                            ";

        cmd.Parameters.AddWithValue(id);

        using var reader2 = await cmd.ExecuteReaderAsync();
        await reader2.ReadAsync();

        cliente = new ClienteDto(reader2.GetInt32(0), reader2.GetInt32(1), reader2.GetInt32(2));

        return Results.Ok(cliente);

        // cliente = _clienteRepository.GetCliente(request.Id, connection);

        // return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente);

        //----------------------------------------------        
    }
});

app.Run();

[JsonSerializable(typeof(ClienteDto))]
[JsonSerializable(typeof(ExtratoDto))]
[JsonSerializable(typeof(SaldoDto))]
[JsonSerializable(typeof(TransacaoDto))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

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