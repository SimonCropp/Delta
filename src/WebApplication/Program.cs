using System.Data;

SqlInstance sqlInstance = new(
    name: "DeltaWebApplication",
    buildTemplate: DbBuilder.Create);
await using var database = await sqlInstance.Build("WebApp");

var connectionString = database.ConnectionString;

#region UseDelta

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped(_ => new SqlConnection(connectionString));
var app = builder.Build();
app.UseDelta(
    getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>());

#endregion

await using var command = database.Connection.CreateCommand();
command.CommandText =
    $"""
     insert into [Companies] (Id, Content)
     values ('{Guid.NewGuid()}', 'The company')
     """;
await command.ExecuteNonQueryAsync();

app.MapGet("/", async _ =>
{
    var connection = _.RequestServices.GetRequiredService<SqlConnection>();

    if (connection.State == ConnectionState.Closed)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "select * from Companies";
    await using var reader = await command.ExecuteReaderAsync();
    var builder = new StringBuilder("Results: ");
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
    .UseDelta(
        getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>())
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();