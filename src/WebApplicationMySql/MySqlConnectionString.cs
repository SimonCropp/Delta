public static class MySqlConnectionString
{
    public static string Value;

    static MySqlConnectionString()
    {
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            Value = "Server=127.0.0.1;Port=3307;User ID=root;Password=password;Database=delta;";
            return;
        }

        Value = "Server=127.0.0.1;Port=3307;User ID=root;Password=password;Database=delta;";
    }
}