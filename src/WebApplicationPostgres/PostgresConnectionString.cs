public static class PostgresConnectionString
{
    public static string Value;

    static PostgresConnectionString()
    {
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            Value = "User ID=postgres;Password=Password12!;Host=localhost;Port=5432;Database=delta";
            return;
        }

        Value = "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=delta";
    }
}