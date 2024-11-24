SqlInstance sqlInstance = new(
    name: "DeltaWebApplication",
    buildTemplate: DbBuilder.Create);
await using var database = await sqlInstance.Build("WebApp");

var connectionString = database.ConnectionString;

#region UseDelta

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<SqlConnection>(_ => new(connectionString));
var app = builder.Build();
app.UseDelta(
    getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>());

#endregion

app.MapGet("/", () => "Hello World!");

#region UseDeltaMapGroup

app.MapGroup("/group")
    .UseDelta(
        getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>())
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();