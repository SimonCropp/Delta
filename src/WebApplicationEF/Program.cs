var sqlInstance = new SqlInstance<SampleDbContext>(constructInstance: builder => new(builder.Options));

await using var database = await sqlInstance.Build("WebAppEF");

#region UseDeltaEF

var builder = WebApplication.CreateBuilder();
builder.Services.AddSqlServer<SampleDbContext>(database.ConnectionString);
var app = builder.Build();
app.UseDelta<SampleDbContext>();

#endregion

app.MapGet("/", () => "Hello World!");

#region UseDeltaMapGroupEF

app.MapGroup("/group")
    .UseDelta<SampleDbContext>()
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();