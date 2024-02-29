using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApi.Dtos;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// builder.Services.AddNpgsqlDataSource(
//     builder.Configuration.GetConnectionString("DefaultConnection")!
// );

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

app.MapPost("/clientes/{id:int}/transacoes", async (int id, TransacaoDto transacao) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    // if (transacao.Tipo != "c" && transacao.Tipo != "d")
    //     return Results.UnprocessableEntity();
    // if (!int.TryParse(transacao.Valor.ToString(), out var valor))
    //     return Results.UnprocessableEntity();
    // if (string.IsNullOrEmpty(transacao.Descricao) || transacao.Descricao.Length > 10)
    //     return Results.UnprocessableEntity();

    //----------------------------------------------

    await using var dataSource = NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("DefaultConnection")!);

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

    // await using var connection = _connectionFactory.CreateConnection();
    // connection.Open();

    // var cliente = _clienteRepository.GetCliente(request.Id, connection);

    // if (cliente.Id == 0)
    //     return new CreateTransacaoCommandViewModel(OperationResult.NotFound);

    //----------------------------------------------

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

    // _transacaoRepository.CreateTransacao(
    //                 request.Transacao.Valor,
    //                 request.Transacao.Tipo!,
    //                 request.Transacao.Descricao,
    //                 request.Id,
    //                 DateTime.UtcNow,
    //                 connection);

    //----------------------------------------------

    await using (var cmd = dataSource.CreateCommand())
    {
        var valorTransacao = transacao.Tipo == "c" ? transacao.Valor : transacao.Valor * -1;

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
    }

    // var valorTransacao = request.Transacao.Tipo == "c" ? request.Transacao.Valor : request.Transacao.Valor * -1;

    // var success = _clienteRepository.UpdateSaldoCliente(request.Id, valorTransacao, connection);

    // if (!success)
    // {
    //     return new CreateTransacaoCommandViewModel(OperationResult.Failed);
    // }

    //----------------------------------------------

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

    // cliente = _clienteRepository.GetCliente(request.Id, connection);

    // return new CreateTransacaoCommandViewModel(OperationResult.Success, cliente);

    //----------------------------------------------
});

app.MapGet("/clientes/{id:int}/extrato", async (int id) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    await using var dataSource = NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("DefaultConnection")!);

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

    // await using var connection = _connectionFactory.CreateConnection();
    // connection.Open();

    // var saldo = _clienteRepository.GetSaldoTotal(request.Id, connection);

    // if (saldo.Id == 0)
    //     return new GetExtratoQueryViewModel(OperationResult.NotFound);

    //----------------------------------------------

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

    // var ultimasTransacoes = _transacaoRepository.ListTransacao(request.Id, connection);
    
    // return new GetExtratoQueryViewModel(OperationResult.Success, new ExtratoDto(saldo, ultimasTransacoes.ToList()));

    //----------------------------------------------
});

app.Run();

[JsonSerializable(typeof(ClienteDto))]
[JsonSerializable(typeof(SaldoDto))]
[JsonSerializable(typeof(TransacaoDto))]
[JsonSerializable(typeof(ExtratoDto))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }