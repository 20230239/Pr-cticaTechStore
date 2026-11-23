using System.Data;
using Microsoft.Data.SqlClient;
using TechStore.Core;

namespace TechStore.AdoNet;

public sealed class AdoClienteRepository : IClienteRepository
{
    private const string Cols = "SELECT Id, Nombre, Email FROM dbo.Clientes";
    private readonly string _cs;
    public AdoClienteRepository(string connectionString) => _cs = connectionString;

    private static Cliente Map(SqlDataReader r) => new() { Id = r.GetInt32(0), Nombre = r.GetString(1), Email = r.GetString(2) };

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(Cols + " WHERE Id = @id", cn);
        cmd.Parameters.Add(Db.P("@id", SqlDbType.Int, id));
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<int> InsertarAsync(Cliente c)
    {
        const string sql = "INSERT INTO dbo.Clientes (Nombre, Email) OUTPUT INSERTED.Id VALUES (@n, @e)";
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(Db.P("@n", SqlDbType.NVarChar, c.Nombre, 150));
        cmd.Parameters.Add(Db.P("@e", SqlDbType.NVarChar, c.Email, 150));
        c.Id = (int)(await cmd.ExecuteScalarAsync())!;
        return c.Id;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(int pagina, int tamano)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(Cols + " ORDER BY Id OFFSET @off ROWS FETCH NEXT @n ROWS ONLY", cn);
        cmd.Parameters.Add(Db.P("@off", SqlDbType.Int, (pagina - 1) * tamano));
        cmd.Parameters.Add(Db.P("@n", SqlDbType.Int, tamano));
        var list = new List<Cliente>(tamano);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }
}
