
var sqlInstance = new SqlInstance<SampleDbContext>(constructInstance: builder => new(builder.Options));

await using var database = await sqlInstance.Build("WebApp");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSqlServer<SampleDbContext>(database.ConnectionString);
var app = builder.Build();
app.UseDelta<SampleDbContext>();

app.MapGet("/", () => "Hello World!");

app.MapGroup("/group")
    .UseDelta<SampleDbContext>()
    .MapGet("/", () => "Hello Group!");

app.Run();