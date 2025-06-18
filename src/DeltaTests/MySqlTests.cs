using MySqlConnector;

public class MySqlTests
{
    [Test]
    public async Task GetLastTimeStamp()
    {
        await using var connection = new MySqlConnection(MySqlConnectionString.Value);
        await connection.OpenAsync();
        await MySqlDbBuilder.Create(connection);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             INSERT INTO Companies (Id, Content)
             VALUES ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();

        var timeStamp = await connection.GetLastTimeStamp();

        IsNotNull(timeStamp);
        IsNotEmpty(timeStamp);
    }
}