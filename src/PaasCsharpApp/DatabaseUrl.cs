using Npgsql;

namespace PaasCsharpApp;

internal static class DatabaseUrl
{
    /// <summary>
    /// Переводит адрес вида postgresql://user:password@host:port/name в строку подключения Npgsql.
    /// Порт в адресе необязателен, Render его не указывает.
    /// </summary>
    public static string ToConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
        };
        if (!uri.IsDefaultPort && uri.Port > 0)
        {
            builder.Port = uri.Port;
        }

        if (userInfo.Length == 2)
        {
            builder.Password = Uri.UnescapeDataString(userInfo[1]);
        }

        return builder.ConnectionString;
    }
}
