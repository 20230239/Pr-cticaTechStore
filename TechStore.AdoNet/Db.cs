using System.Data;
using Microsoft.Data.SqlClient;

namespace TechStore.AdoNet;

internal static class Db
{
    public static SqlParameter P(string name, SqlDbType type, object? value, int size = 0)
    {
        var p = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static SqlParameter Money(string name, decimal value, byte precision = 10, byte scale = 2) =>
        new(name, SqlDbType.Decimal) { Precision = precision, Scale = scale, Value = value };

    public static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
