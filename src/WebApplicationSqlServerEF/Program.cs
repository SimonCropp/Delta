DeltaExtensions.UseResponseDiagnostics = true;

var sqlInstance = new SqlInstance<SampleDbContext>(constructInstance: builder => new(builder.Options));

await using var database = await sqlInstance.Build("WebAppEF");

#region UseDeltaEF

var builder = WebApplication.CreateBuilder();
builder.Services.AddSqlServer<SampleDbContext>(database.ConnectionString);
var app = builder.Build();
app.UseDelta<SampleDbContext>();

#endregion

var context = database.Context;
context.Add(
    new Company
    {
        Content = "The company"
    });
await context.SaveChangesAsync();

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
            builder.AppendLine($"RowVersion: {company.RowVersion}");
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