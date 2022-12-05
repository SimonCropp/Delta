
var sqlInstance = new SqlInstance<SampleDbContext>(constructInstance: builder => new(builder.Options));

await using var database = await sqlInstance.Build("WebApp");

#region UseDelta

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSqlServer<SampleDbContext>(database.ConnectionString);
var app = builder.Build();
app.UseDelta<SampleDbContext>();

#endregion

app.MapGet("/", () => "Hello World!");

#region UseDeltaMapGroup

app.MapGroup("/group")
    .UseDelta<SampleDbContext>()
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();