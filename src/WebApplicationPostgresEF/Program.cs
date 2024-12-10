DeltaExtensions.UseResponseDiagnostics = true;

var connectionString = PostgresConnection.ConnectionString;

#region UseDeltaPostgresEF

var builder = WebApplication.CreateBuilder();
builder.Services.AddDbContext<SampleDbContext>(
    _ => _.UseNpgsql(connectionString));
var app = builder.Build();
app.UseDelta<SampleDbContext>();

#endregion

using (var scope = app.Services.CreateScope())
{
    await using var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>()!;
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    context.Add(
        new Company
        {
            Content = "The company"
        });
    await context.SaveChangesAsync();
}

app.MapGet(
    "/",
    async _ =>
    {
        var result = new StringBuilder("Results: ");
        result.AppendLine();
        var dbContext = _.RequestServices.GetRequiredService<SampleDbContext>();
        result.AppendLine($"LastTimeStamp: {await dbContext.GetLastTimeStamp()}");
        result.AppendLine();
        foreach (var company in await dbContext.Companies.ToListAsync())
        {
            result.AppendLine($"Id: {company.Id}");
            result.AppendLine($"Content: {company.Content}");
        }

        await _.Response.WriteAsync(result.ToString());
    });

#region UseDeltaMapGroupEF

app.MapGroup("/group")
    .UseDelta<SampleDbContext>()
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();
