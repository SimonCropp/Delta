public static class MySqlConnectionString
{
    public static string Value;

    static MySqlConnectionString()
    {
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            Value = "User ID=postgres;Password=Password12!;Host=localhost;Port=5432;Database=delta";
            return;
        }

        Value = "Server=127.0.0.1:3307;Database=delta;User Id=root;Password=password;";
    }
}