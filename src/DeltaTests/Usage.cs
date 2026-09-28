using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Npgsql;

[NotInParallel]
public class Usage :
    LocalDbTestBase
{
    public static void HostBuilderSqlServer(string connectionString)
    {
        #region UseDeltaHostBuilderSqlServer

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(_ =>
            {
                _.ConfigureServices(services => services.AddScoped(provider => new SqlConnection(connectionString)));
                _.Configure(app => app.UseDelta());
            })
            .Build();

        #endregion
    }

    public static void HostBuilderPostgres(string connectionString)
    {
        #region UseDeltaHostBuilderPostgres

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(_ =>
            {
                _.ConfigureServices(services => services.AddScoped(provider => new NpgsqlConnection(connectionString)));
                _.Configure(app => app.UseDelta());
            })
            .Build();

        #endregion
    }

    public static void Suffix(WebApplicationBuilder builder)
    {
        #region Suffix

        var app = builder.Build();
        app.UseDelta(suffix: httpContext => "MySuffix");

        #endregion
    }

    public static void ShouldExecute(WebApplicationBuilder builder)
    {
        #region ShouldExecute

        var app = builder.Build();
        app.UseDelta(
            shouldExecute: httpContext =>
            {
                var path = httpContext.Request.Path.ToString();
                return path.Contains("match");
            });

        #endregion
    }

    public static void SuffixWithAuth(WebApplicationBuilder builder)
    {
        #region SuffixWithAuth

        var app = builder.Build();

        // Authentication middleware must run before UseDelta
        // so that User claims are available to the suffix callback
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseDelta(
            suffix: httpContext =>
            {
                // Access user claims to create per-user cache keys
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantId = httpContext.User.FindFirst("TenantId")?.Value;
                return $"{userId}-{tenantId}";
            });

        #endregion
    }

    public static void AllowAnonymous(WebApplicationBuilder builder)
    {
        #region AllowAnonymous

        var app = builder.Build();

        // For endpoints that intentionally allow anonymous access
        // but still want a suffix for cache differentiation
        app.UseDelta(
            suffix: httpContext => httpContext.Request.Headers["X-Client-Version"].ToString(),
            allowAnonymous: true);

        #endregion
    }

    [Test]
    [MatrixDataSource]
    public async Task LastTimeStamp([Matrix] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        await AssertTimestamps(tracking, database, AddEntity);
    }

    static async Task AssertTimestamps(bool tracking, SqlDatabase database, Func<SqlConnection, Task> action)
    {
        var lsnTimeStamp = await GetLsnTimeStamp(database);
        await Assert.That(lsnTimeStamp).IsNotEmpty();
        await Assert.That(lsnTimeStamp).IsNotNull();

        var trackingTimeStamp = await GetTrackingTimeStamp(database);
        await Assert.That(trackingTimeStamp).IsNotEmpty();
        await Assert.That(trackingTimeStamp).IsNotNull();

        await action(database);

        if (tracking)
        {
            var newTackingTimeStamp = await GetTrackingTimeStamp(database);
            await Assert.That(newTackingTimeStamp).IsNotEmpty();
            await Assert.That(newTackingTimeStamp).IsNotNull();
            await Assert.That(trackingTimeStamp).IsNotEqualTo(newTackingTimeStamp);
        }

        var newLsnTimeStamp = await GetLsnTimeStamp(database);
        await Assert.That(newLsnTimeStamp).IsNotEmpty();
        await Assert.That(newLsnTimeStamp).IsNotNull();
        await Assert.That(lsnTimeStamp).IsNotEqualTo(newLsnTimeStamp);
    }

    [Test]
    [MatrixDataSource]
    public async Task LastTimeStampOnUpdate([Matrix] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        var companyGuid = await AddEntity(database);

        await AssertTimestamps(tracking, database, connection => UpdateEntity(connection, companyGuid));
    }

    static async Task UpdateEntity(SqlConnection connection, Guid id)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            update Companies
            set Content = 'New Content Value'
            where Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    [MatrixDataSource]
    public async Task LastTimeStampOnDelete([Matrix] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        var companyGuid = await AddEntity(database);

        await AssertTimestamps(tracking, database, connection => DeleteEntity(connection, companyGuid));
    }

    [Test]
    [MatrixDataSource]
    public async Task LastTimeStampReadTwice([Matrix] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database);
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database);
        await Assert.That(timeStamp).IsEqualTo(newTimeStamp);
    }

    static async Task<Guid> AddEntity(SqlConnection connection)
    {
        var id = Guid.NewGuid();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{id}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();
        return id;
    }

    static async Task DeleteEntity(SqlConnection connection, Guid id)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "delete From Companies where Id=@Id";
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task LastTimeStampOnTruncate()
    {
        await using var database = await LocalDb();

        await AddEntity(database);

        await AssertTimestamps(false, database, TruncateTable);
    }

    static Task<string> GetTrackingTimeStamp(SqlDatabase database) =>
        Execute(database, DeltaExtensions.ExecuteSqlTimeStamp);

    static Task<string> GetLsnTimeStamp(SqlDatabase database) =>
        Execute(database, DeltaExtensions.ExecuteSqlLsn);

    static async Task<string> Execute(SqlDatabase database, Func<DbCommand, Cancel, Task<string>> execute)
    {
        await using var command = database.Connection.CreateCommand();
        return await execute(command, Cancel.None);
    }

    static async Task TruncateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "truncate table Companies";
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    [MatrixDataSource]
    public async Task GetLastTimeStampSqlServer([Matrix] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        var connection = database.Connection;

        #region GetLastTimeStampConnection

        var timeStamp = await connection.GetLastTimeStamp();

        #endregion

        await Assert.That(timeStamp).IsNotNull();
    }

    [Test]
    public async Task GetDatabasesWithTracking()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.EnableTracking();

        #region GetDatabasesWithTracking

        var trackedDatabases = await sqlConnection.GetTrackedDatabases();
        foreach (var db in trackedDatabases)
        {
            Trace.WriteLine(db);
        }

        #endregion

        await Assert.That(trackedDatabases).IsNotEmpty();
    }

    [Test]
    public async Task GetTrackedTables()
    {
        var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.DisableTracking();

        #region SetTrackedTables

        await sqlConnection.SetTrackedTables(["Companies"]);

        #endregion

        #region GetTrackedTables

        var trackedTables = await sqlConnection.GetTrackedTables();
        foreach (var db in trackedTables)
        {
            Trace.WriteLine(db);
        }

        #endregion

        await Verify(sqlConnection.GetTrackedTables());
    }

    [Test]
    public async Task DuplicateSetTrackedTables()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables(["Companies"]);
        await connection.SetTrackedTables(["Companies"]);
    }

    [Test]
    public async Task SetTrackedTablesCaseInsensitive()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables(["Companies"]);
        await connection.SetTrackedTables(["COMPANIES"]);
        var trackedTables = await connection.GetTrackedTables();
        await Assert.That(trackedTables).Count().IsEqualTo(1);
    }

    [Test]
    public async Task EmptySetTrackedTables()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables([]);
    }

    [Test]
    public async Task SchemaWithTracking()
    {
        await using var database = await LocalDb();
        await database.Connection.EnableTracking();
        await database.Connection.SetTrackedTables(["Companies", "Employees"]);
        await Verify(await database.OpenNewConnection()).SchemaAsSql();
    }

    [Test]
    public async Task Schema()
    {
        await using var database = await LocalDb();
        await Verify(await database.OpenNewConnection()).SchemaAsSql();
    }

    [Test]
    public async Task DisableTracking()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(["Companies"]);

        #region DisableTracking

        await sqlConnection.DisableTracking();

        #endregion

        await Assert.That(await sqlConnection.IsTrackingEnabled()).IsFalse();
    }

    [Test]
    public async Task IsTrackingEnabled()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;

        #region EnableTracking

        await sqlConnection.EnableTracking();

        #endregion

        #region IsTrackingEnabled

        var isTrackingEnabled = await sqlConnection.IsTrackingEnabled();

        #endregion

        await Assert.That(isTrackingEnabled).IsTrue();
    }

    static void CustomDiscoveryConnectionSqlServer(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnectionSqlServer

        var application = webApplicationBuilder.Build();
        application.UseDelta(
            getConnection: httpContext =>
                httpContext.RequestServices.GetRequiredService<SqlConnection>());

        #endregion
    }
    static void CustomDiscoveryConnectionPostgres(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnectionPostgres

        var application = webApplicationBuilder.Build();
        application.UseDelta(
            getConnection: httpContext =>
                httpContext.RequestServices.GetRequiredService<NpgsqlConnection>());

        #endregion
    }

    static void CustomDiscoveryConnectionAndTransactionSqlServer(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnectionAndTransactionSqlServer

        var application = webApplicationBuilder.Build();
        application.UseDelta(
            getConnection: httpContext =>
            {
                var provider = httpContext.RequestServices;
                var connection = provider.GetRequiredService<SqlConnection>();
                var transaction = provider.GetService<SqlTransaction>();
                return new(connection, transaction);
            });

        #endregion
    }
    static void CustomDiscoveryConnectionAndTransactionPostgres(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnectionAndTransactionPostgres

        var application = webApplicationBuilder.Build();
        application.UseDelta(
            getConnection: httpContext =>
            {
                var provider = httpContext.RequestServices;
                var connection = provider.GetRequiredService<NpgsqlConnection>();
                var transaction = provider.GetService<NpgsqlTransaction>();
                return new(connection, transaction);
            });

        #endregion
    }
}