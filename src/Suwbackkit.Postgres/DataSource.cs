using System.Data.Common;
using Npgsql;

namespace Suwbackkit.Postgres;

internal static class DataSource
{
    public const int MaxPoolSize = 5;
    public const int KeepaliveSeconds = 10;

    public static NpgsqlDataSource Create(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder { MaxPoolSize = MaxPoolSize, KeepAlive = KeepaliveSeconds };
        var configured = new DbConnectionStringBuilder { ConnectionString = connectionString };
        foreach (string keyword in configured.Keys)
        {
            builder[keyword] = configured[keyword];
        }
        return NpgsqlDataSource.Create(builder);
    }
}
