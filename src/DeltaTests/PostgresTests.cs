using Npgsql;

public class PostgresTests
{
    [Test]
    public async Task GetLastTimeStamp()
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
}