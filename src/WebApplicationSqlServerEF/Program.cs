var sqlInstance = new SqlInstance<SampleDbContext>(constructInstance: builder => new(builder.Options));

await using var database = await sqlInstance.Build("WebAppEF");

var connectionString = database.ConnectionString;

#region UseDeltaSQLServerEF

var builder = WebApplication.CreateBuilder();
builder.Services.AddSqlServer<SampleDbContext>(connectionString);

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
        var result = new StringBuilder("Results: ");
        result.AppendLine();
        var dbContext = _.RequestServices.GetRequiredService<SampleDbContext>();
        result.AppendLine($"LastTimeStamp: {await dbContext.GetLastTimeStamp()}");
        result.AppendLine();
        foreach (var company in await dbContext.Companies.ToListAsync())
        {
            result.AppendLine($"Id: {company.Id}");
            result.AppendLine($"RowVersion: {company.RowVersion}");
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