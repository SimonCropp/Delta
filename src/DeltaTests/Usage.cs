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
    public async Task LastTimeStamp()
    {
        await using var database = await LocalDb();

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        Recording.Start();

        await AddEntity(database);

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(timeStamp, newTimeStamp);
    }

    [Test]
    public async Task LastTimeStampOnUpdate()
    {
        await using var database = await LocalDb();

        var companyGuid = await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);

        await UpdateEntity(database, companyGuid);

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(newTimeStamp, timeStamp);
    }

    static async Task UpdateEntity(SqlConnection connection, Guid id)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             update Companies
             set Content = 'New Content Value'
             where Id = @Id;
             """;
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task LastTimeStampOnDelete()
    {
        await using var database = await LocalDb();

        var companyGuid = await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);

        await DeleteEntity(database, companyGuid);

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(newTimeStamp, timeStamp);
    }

    [Test]
    public async Task LastTimeStampReadTwice()
    {
        await using var database = await LocalDb();

        await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
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
        command.CommandText = $"delete From Companies where Id=@Id";
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task LastTimeStampOnTruncate()
    {
        await using var database = await LocalDb();
        await AddEntity(database);

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);

        await TruncateTable(database);

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(newTimeStamp, timeStamp);
    }

    static async Task TruncateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "truncate table Companies";
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task GetLastTimeStampSqlServer()
    {
        await using var database = await LocalDb();

        var sqlConnection = database.Connection;

        #region GetLastTimeStampSqlConnection

        var timeStamp = await sqlConnection.GetLastTimeStamp();

        #endregion

        IsNotNull(timeStamp);
    }

    [Test]
    public async Task GetLastTimeStampPostgres()
    {
        await using var connection = new NpgsqlConnection(PostgresConnection.ConnectionString);
        await connection.OpenAsync();
        await PostgresDbBuilder.Create(connection);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             insert into "Companies"("Id", "Content")
             values ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();

        var timeStamp = await connection.GetLastTimeStamp();

        IsNotNull(timeStamp);
        IsNotEmpty(timeStamp);
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
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(["Companies"]);
        await sqlConnection.SetTrackedTables(["Companies"]);
    }

    [Test]
    public async Task EmptySetTrackedTables()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables([]);
    }

    [Test]
    public async Task Schema()
    {
        await using var database = await LocalDb();
        await Verify(database.Connection).SchemaAsSql();
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

    static void CustomDiscoveryConnection(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnection

        var application = webApplicationBuilder.Build();
        application.UseDelta(
            getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>());

        #endregion
    }

    static void CustomDiscoveryConnectionAndTransaction(WebApplicationBuilder webApplicationBuilder)
    {
        #region CustomDiscoveryConnectionAndTransaction

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
}