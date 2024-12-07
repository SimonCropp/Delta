static class PostgresConnection
{
    public static string ConnectionString;

    static PostgresConnection()
    {
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            ConnectionString = "User ID=postgres;Password=Password12!;Host=localhost;Port=5432;Database=delta";
            return;
        }

        ConnectionString = "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=delta";
    }
}