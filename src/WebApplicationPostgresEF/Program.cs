DeltaExtensions.UseResponseDiagnostics = true;

var connectionString = PostgresConnection.ConnectionString;

#region UseDeltaEF

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
        var builder = new StringBuilder("Results: ");
        builder.AppendLine();
        var dbContext = _.RequestServices.GetRequiredService<SampleDbContext>();
        builder.AppendLine($"LastTimeStamp: {await dbContext.GetLastTimeStamp()}");
        builder.AppendLine();
        foreach (var company in await dbContext.Companies.ToListAsync())
        {
            builder.AppendLine($"Id: {company.Id}");
            builder.AppendLine($"Content: {company.Content}");
        }

        await _.Response.WriteAsync(builder.ToString());
    });

#region UseDeltaMapGroupEF

app.MapGroup("/group")
    .UseDelta<SampleDbContext>()
    .MapGet("/", () => "Hello Group!");

#endregion

app.Run();
