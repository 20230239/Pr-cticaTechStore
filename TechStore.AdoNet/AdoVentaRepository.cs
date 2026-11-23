using System.Data;
using Microsoft.Data.SqlClient;
using TechStore.Core;

namespace TechStore.AdoNet;

public sealed class AdoVentaRepository : IVentaRepository
{
    private readonly string _cs;
    public AdoVentaRepository(string connectionString) => _cs = connectionString;

    public async Task<int> RegistrarVentaAsync(int clienteId, IReadOnlyList<ItemVenta> items)
    {
        if (items.Count == 0) throw new ArgumentException("La venta no tiene productos.", nameof(items));

        var lineas = items.GroupBy(i => i.ProductoId)
                          .Select(g => new ItemVenta(g.Key, g.Sum(x => x.Cantidad)))
                          .OrderBy(i => i.ProductoId).ToList();

        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
        try
        {
            decimal total = 0;
            var precios = new Dictionary<int, decimal>();

            const string sqlStock = @"UPDATE dbo.Productos SET Stock = Stock - @cant
                                      OUTPUT INSERTED.Precio
                                      WHERE Id = @id AND Stock >= @cant";
            foreach (var l in lineas)
            {
                await using var cmd = new SqlCommand(sqlStock, cn, tx);
                cmd.Parameters.Add(Db.P("@cant", SqlDbType.Int, l.Cantidad));
                cmd.Parameters.Add(Db.P("@id", SqlDbType.Int, l.ProductoId));
                var precio = await cmd.ExecuteScalarAsync();
                if (precio is null) throw new StockInsuficienteException(l.ProductoId);
                precios[l.ProductoId] = (decimal)precio;
                total += (decimal)precio * l.Cantidad;
            }

            int ventaId;
            await using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Ventas (ClienteId, Fecha, Total) OUTPUT INSERTED.Id VALUES (@c, @f, @t)", cn, tx))
            {
                cmd.Parameters.Add(Db.P("@c", SqlDbType.Int, clienteId));
                cmd.Parameters.Add(Db.P("@f", SqlDbType.DateTime2, DateTime.UtcNow));
                cmd.Parameters.Add(Db.Money("@t", total, 12, 2));
                ventaId = (int)(await cmd.ExecuteScalarAsync())!;
            }

            foreach (var l in lineas)
            {
                await using var cmd = new SqlCommand(
                    @"INSERT INTO dbo.DetalleVentas (VentaId, ProductoId, Cantidad, PrecioUnitario)
                      VALUES (@v, @p, @c, @pu)", cn, tx);
                cmd.Parameters.Add(Db.P("@v", SqlDbType.Int, ventaId));
                cmd.Parameters.Add(Db.P("@p", SqlDbType.Int, l.ProductoId));
                cmd.Parameters.Add(Db.P("@c", SqlDbType.Int, l.Cantidad));
                cmd.Parameters.Add(Db.Money("@pu", precios[l.ProductoId]));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return ventaId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Venta?> ObtenerAsync(int id)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        Venta? venta;
        await using (var cmd = new SqlCommand("SELECT Id, ClienteId, Fecha, Total FROM dbo.Ventas WHERE Id = @id", cn))
        {
            cmd.Parameters.Add(Db.P("@id", SqlDbType.Int, id));
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            venta = new Venta { Id = r.GetInt32(0), ClienteId = r.GetInt32(1), Fecha = r.GetDateTime(2), Total = r.GetDecimal(3) };
        }
        await using (var cmd = new SqlCommand(
            "SELECT Id, VentaId, ProductoId, Cantidad, PrecioUnitario FROM dbo.DetalleVentas WHERE VentaId = @id", cn))
        {
            cmd.Parameters.Add(Db.P("@id", SqlDbType.Int, id));
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                venta.Detalles.Add(new DetalleVenta
                {
                    Id = r.GetInt32(0), VentaId = r.GetInt32(1), ProductoId = r.GetInt32(2),
                    Cantidad = r.GetInt32(3), PrecioUnitario = r.GetDecimal(4)
                });
        }
        return venta;
    }
}
