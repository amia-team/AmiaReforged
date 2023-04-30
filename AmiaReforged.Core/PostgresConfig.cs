using NLog;
using NLog.Fluent;

namespace AmiaReforged.Core;

public static class PostgresConfig
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static string Host { get; set; } = GetHost();

    private static string GetHost()
    {
        string host = Environment.GetEnvironmentVariable("POSTGRES_HOST")!;
        if (!string.IsNullOrEmpty(host)) return host;
        Log.Warn("POSTGRES_HOST environment variable not set, defaulting to localhost");
        host = "localhost";

        return host;
    }

    public static int Port { get; set; } = GetPort();

    private static int GetPort()
    {
        string port = Environment.GetEnvironmentVariable("POSTGRES_PORT")!;
        if (!string.IsNullOrEmpty(port)) return Convert.ToInt32(port);
        Log.Warn("POSTGRES_PORT environment variable not set, defaulting to 5432");

        port = "5432";

        return int.Parse(port);
    }

    public static string Database { get; } = GetDatabase();

    private static string GetDatabase()
    {
        string database = Environment.GetEnvironmentVariable("POSTGRES_DB")!;
        if (!string.IsNullOrEmpty(database)) return database;
        Log.Warn("POSTGRES_DATABASE environment variable not set, defaulting to amia");
        database = "amia";

        return database;
    }

    public static string Username { get; } = GetUsername();

    private static string GetUsername()
    {
        string username = Environment.GetEnvironmentVariable("POSTGRES_USER")!;
        if (!string.IsNullOrEmpty(username)) return username;
        Log.Warn("POSTGRES_USERNAME environment variable not set, defaulting to amia");
        username = "amia";

        return username;
    }

    public static string Password { get; } = GetPassword();

    private static string GetPassword()
    {
        string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")!;
        if (!string.IsNullOrEmpty(password)) return password;
        Log.Warn("POSTGRES_PASSWORD environment variable not set, defaulting to amia");
        password = "amia";

        return password;
    }
}