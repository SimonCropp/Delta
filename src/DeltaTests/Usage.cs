using System.Data.Common;
using Npgsql;

public class Usage :
    LocalDbTestBase
{
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

    [Test]
    public async Task LastTimeStamp([Values] bool tracking)
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
        IsNotEmpty(lsnTimeStamp);
        IsNotNull(lsnTimeStamp);

        var trackingTimeStamp = await GetTrackingTimeStamp(database);
        IsNotEmpty(trackingTimeStamp);
        IsNotNull(trackingTimeStamp);

        await action(database);

        if (tracking)
        {
            var newTackingTimeStamp = await GetTrackingTimeStamp(database);
            IsNotEmpty(newTackingTimeStamp);
            IsNotNull(newTackingTimeStamp);
            AreNotEqual(newTackingTimeStamp, trackingTimeStamp);
        }

        var newLsnTimeStamp = await GetLsnTimeStamp(database);
        IsNotEmpty(newLsnTimeStamp);
        IsNotNull(newLsnTimeStamp);
        AreNotEqual(newLsnTimeStamp, lsnTimeStamp);
    }

    [Test]
    public async Task LastTimeStampOnUpdate([Values] bool tracking)
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
    public async Task LastTimeStampOnDelete([Values] bool tracking)
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
    public async Task LastTimeStampReadTwice([Values] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database);
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database);
        AreEqual(newTimeStamp, timeStamp);
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

    static async Task<string> GetLsnTimeStamp(SqlDatabase database) =>
        await Execute(database, DeltaExtensions.ExecuteSqlLsn);

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
    public async Task GetLastTimeStampSqlServer([Values] bool tracking)
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

        IsNotNull(timeStamp);
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

        IsNotEmpty(trackedDatabases);
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

        IsFalse(await sqlConnection.IsTrackingEnabled());
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

        IsTrue(isTrackingEnabled);
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