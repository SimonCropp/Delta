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
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        Recording.Start();
        await using var command = database.Connection.CreateCommand();
        command.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(timeStamp, newTimeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionOnUpdate()
    {
        await using var database = await LocalDb();
        await using var connection = database.Connection;
        await connection.EnableTracking();

        var companyGuid = Guid.NewGuid();
        await using var addDataCommand = connection.CreateCommand();
        addDataCommand.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{companyGuid}', 'The company')
             """;
        await addDataCommand.ExecuteNonQueryAsync();

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);

        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = $"""
                                     UPDATE [Companies]
                                     SET [Content] = 'New Content Value'
                                     WHERE [Id] = @Id;
                                     """;
        updateCommand.Parameters.AddWithValue("@Id", companyGuid);
        await updateCommand.ExecuteNonQueryAsync();

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(newTimeStamp, timeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionOnDelete()
    {
        await using var database = await LocalDb();
        await using var connection = database.Connection;
        await connection.EnableTracking();

        var companyGuid = Guid.NewGuid();
        await using var addDataCommand = connection.CreateCommand();
        addDataCommand.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{companyGuid}', 'The company')
             """;
        await addDataCommand.ExecuteNonQueryAsync();

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);

        await using var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = $"delete From Companies where Id=@Id";
        deleteCommand.Parameters.AddWithValue("@Id", companyGuid);
        await deleteCommand.ExecuteNonQueryAsync();

        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(newTimeStamp, timeStamp);
    }

    [Test]
    public async Task GetLastTimeStampSqlServer()
    {
        await using var database = await LocalDb();

        var sqlConnection = database.Connection;

        #region GetLastTimeStampSqlConnection

        var timeStamp = await sqlConnection.GetLastTimeStamp();

        #endregion

        var timeStamp2 = await sqlConnection.GetLastTimeStamp();

        IsNotNull(timeStamp);
    }


    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        var connection = database.Connection;
        await connection.EnableTracking();
        var timeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
        AreNotEqual(timeStamp, newTimeStamp);
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

        var webApplication = webApplicationBuilder.Build();
        webApplication.UseDelta(
            getConnection: httpContext =>
            {
                var provider = httpContext.RequestServices;
                var sqlConnection = provider.GetRequiredService<SqlConnection>();
                var sqlTransaction = provider.GetService<SqlTransaction>();
                return new(sqlConnection, sqlTransaction);
            });

        #endregion
    }
}