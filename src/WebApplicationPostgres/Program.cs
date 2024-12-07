DeltaExtensions.UseResponseDiagnostics  = true;

var connectionString = PostgresConnection.ConnectionString;

#region UseDelta

var builder = WebApplication.CreateBuilder();
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));
var app = builder.Build();
app.UseDelta();

#endregion

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await PostgresDbBuilder.Create(connection);
    await using var command = connection.CreateCommand();
    command.CommandText =
        $"""
         insert into "Companies"("Id", "Content")
         values ('{Guid.NewGuid()}', 'The company')
         """;
    await command.ExecuteNonQueryAsync();
}

app.MapGet(
    "/",
    async _ =>
    {
        var connection = _.RequestServices.GetRequiredService<NpgsqlConnection>();

        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync();
        }

        var lastTimeStamp = await connection.GetLastTimeStamp();
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              select * from "Companies"
                              """;
        await using var reader = await command.ExecuteReaderAsync();
        var builder = new StringBuilder("Results: ");
        builder.AppendLine();
        builder.AppendLine($"LastTimeStamp: {lastTimeStamp}");
        builder.AppendLine();
        while (await reader.ReadAsync())
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                if (value is byte[] bytes)
                {
                    value = BitConverter.ToString(bytes);
                }

                builder.AppendLine($"{reader.GetName(i)}: {value}");
            }
        }

        await _.Response.WriteAsync(builder.ToString());
    });

#region UseDeltaMapGroup

app.MapGroup("/group")
    .UseDelta()
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();