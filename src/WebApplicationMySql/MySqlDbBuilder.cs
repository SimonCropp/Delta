using MySqlConnector;

public static class MySqlDbBuilder
{
    public static async Task Create(MySqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """

            -- begin-snippet: MySqlSchema
            CREATE TABLE IF NOT EXISTS Companies (
                Id CHAR(36) NOT NULL PRIMARY KEY,
                Content TEXT
            );

            CREATE TABLE IF NOT EXISTS Employees (
                Id CHAR(36) NOT NULL PRIMARY KEY,
                CompanyId CHAR(36) NOT NULL,
                Content TEXT,
                Age INT NOT NULL,
                CONSTRAINT FK_Employees_Companies_CompanyId
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Employees_CompanyId
                ON Employees (CompanyId);
            -- end-snippet

            """;
        await command.ExecuteNonQueryAsync();
    }
}