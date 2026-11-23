using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using TechStore.Core;

namespace TechStore.AdoNet;

public sealed class AdoProductoRepository : IProductoRepository
{
    private const string Cols = "SELECT Id, Nombre, SKU, Precio, Stock, CategoriaId FROM dbo.Productos";
    private readonly string _cs;
    public AdoProductoRepository(string connectionString) => _cs = connectionString;

    private static Producto Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0), Nombre = r.GetString(1), Sku = r.GetString(2),
        Precio = r.GetDecimal(3), Stock = r.GetInt32(4), CategoriaId = r.GetInt32(5)
    };

    public async Task<Producto?> ObtenerPorIdAsync(int id)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(Cols + " WHERE Id = @id", cn);
        cmd.Parameters.Add(Db.P("@id", SqlDbType.Int, id));
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<IReadOnlyList<Producto>> BuscarAsync(string? nombre, int? categoriaId, int pagina, int tamano)
    {
        var sql = new StringBuilder(Cols + " WHERE 1 = 1");
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand { Connection = cn };
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            sql.Append(" AND Nombre LIKE @nombre");
            cmd.Parameters.Add(Db.P("@nombre", SqlDbType.NVarChar, Db.EscapeLike(nombre) + "%", 150));
        }
        if (categoriaId.HasValue)
        {
            sql.Append(" AND CategoriaId = @cat");
            cmd.Parameters.Add(Db.P("@cat", SqlDbType.Int, categoriaId.Value));
        }
        sql.Append(" ORDER BY Id OFFSET @off ROWS FETCH NEXT @n ROWS ONLY");
        cmd.Parameters.Add(Db.P("@off", SqlDbType.Int, (pagina - 1) * tamano));
        cmd.Parameters.Add(Db.P("@n", SqlDbType.Int, tamano));
        cmd.CommandText = sql.ToString();

        var list = new List<Producto>(tamano);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<IReadOnlyList<ProductoConCategoria>> ListarConCategoriaAsync(int top)
    {
        const string sql = @"SELECT TOP (@n) p.Id, p.Nombre, p.SKU, p.Precio, p.Stock, c.Nombre
                             FROM dbo.Productos p
                             INNER JOIN dbo.Categorias c ON c.Id = p.CategoriaId
                             ORDER BY p.Id";
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(Db.P("@n", SqlDbType.Int, top));
        var list = new List<ProductoConCategoria>(Math.Min(top, 100_000));
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetString(5)));
        return list;
    }

    public async Task<int> InsertarAsync(Producto p)
    {
        const string sql = @"INSERT INTO dbo.Productos (Nombre, SKU, Precio, Stock, CategoriaId)
                             OUTPUT INSERTED.Id
                             VALUES (@nombre, @sku, @precio, @stock, @cat)";
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(Db.P("@nombre", SqlDbType.NVarChar, p.Nombre, 150));
        cmd.Parameters.Add(Db.P("@sku", SqlDbType.VarChar, p.Sku, 20));
        cmd.Parameters.Add(Db.Money("@precio", p.Precio));
        cmd.Parameters.Add(Db.P("@stock", SqlDbType.Int, p.Stock));
        cmd.Parameters.Add(Db.P("@cat", SqlDbType.Int, p.CategoriaId));
        p.Id = (int)(await cmd.ExecuteScalarAsync())!;
        return p.Id;
    }

    public async Task InsertarLoteAsync(IReadOnlyList<Producto> productos)
    {
        var table = new DataTable();
        table.Columns.Add("Nombre", typeof(string));
        table.Columns.Add("SKU", typeof(string));
        table.Columns.Add("Precio", typeof(decimal));
        table.Columns.Add("Stock", typeof(int));
        table.Columns.Add("CategoriaId", typeof(int));
        foreach (var p in productos) table.Rows.Add(p.Nombre, p.Sku, p.Precio, p.Stock, p.CategoriaId);

        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        using var bulk = new SqlBulkCopy(cn) { DestinationTableName = "dbo.Productos", BatchSize = 5000 };
        foreach (DataColumn c in table.Columns) bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
        await bulk.WriteToServerAsync(table);
    }

    public Task<int> ActualizarPrecioRangoAsync(int desdeId, int hastaId, decimal porcentaje) =>
        ActualizarPrecioRangoMasivoAsync(desdeId, hastaId, porcentaje);

    public async Task<int> ActualizarPrecioRangoMasivoAsync(int desdeId, int hastaId, decimal porcentaje)
    {
        const string sql = @"UPDATE dbo.Productos SET Precio = ROUND(Precio * @f, 2)
                             WHERE Id BETWEEN @d AND @h";
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 120 };
        cmd.Parameters.Add(Db.Money("@f", 1 + porcentaje / 100m, 10, 4));
        cmd.Parameters.Add(Db.P("@d", SqlDbType.Int, desdeId));
        cmd.Parameters.Add(Db.P("@h", SqlDbType.Int, hastaId));
        return await cmd.ExecuteNonQueryAsync();
    }
}
