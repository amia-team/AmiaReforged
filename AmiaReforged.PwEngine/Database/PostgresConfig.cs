﻿using NLog;

namespace AmiaReforged.PwEngine.Database;

public static class PostgresConfig
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static string Host { get; set; } = GetHost();

    public static int Port { get; set; } = GetPort();

    public static string Database { get; } = GetDatabase();

    public static string Username { get; } = GetUsername();

    public static string Password { get; } = GetPassword();

    private static string GetHost()
    {
        string host = Environment.GetEnvironmentVariable(variable: "POSTGRES_HOST")!;
        if (!string.IsNullOrEmpty(host)) return host;
        Log.Warn(message: "POSTGRES_HOST environment variable not set, defaulting to localhost");
        host = "localhost";

        return host;
    }

    private static int GetPort()
    {
        string port = Environment.GetEnvironmentVariable(variable: "POSTGRES_PORT")!;
        if (!string.IsNullOrEmpty(port)) return Convert.ToInt32(port);
        Log.Warn(message: "POSTGRES_PORT environment variable not set, defaulting to 5432");

        port = "5432";

        return int.Parse(port);
    }

    private static string GetDatabase()
    {
        string database = Environment.GetEnvironmentVariable(variable: "POSTGRES_DB")!;
        if (!string.IsNullOrEmpty(database)) return database;
        Log.Warn(message: "POSTGRES_DATABASE environment variable not set, defaulting to amia");
        database = "amia";

        return database;
    }

    private static string GetUsername()
    {
        string username = Environment.GetEnvironmentVariable(variable: "POSTGRES_USER")!;
        if (!string.IsNullOrEmpty(username)) return username;
        Log.Warn(message: "POSTGRES_USERNAME environment variable not set, defaulting to amia");
        username = "amia";

        return username;
    }

    private static string GetPassword()
    {
        string password = Environment.GetEnvironmentVariable(variable: "POSTGRES_PASSWORD")!;
        if (!string.IsNullOrEmpty(password)) return password;
        Log.Warn(message: "POSTGRES_PASSWORD environment variable not set, defaulting to amia");
        password = "amia";

        return password;
    }
}