using Microsoft.EntityFrameworkCore;
using TechStore.Core;

namespace TechStore.EFCore;

public sealed class EfVentaRepository : IVentaRepository
{
    private readonly Func<TechStoreDbContext> _factory;
    public EfVentaRepository(Func<TechStoreDbContext> factory) => _factory = factory;

    public async Task<int> RegistrarVentaAsync(int clienteId, IReadOnlyList<ItemVenta> items)
    {
        if (items.Count == 0) throw new ArgumentException("La venta no tiene productos.", nameof(items));

        var lineas = items.GroupBy(i => i.ProductoId)
                          .Select(g => new ItemVenta(g.Key, g.Sum(x => x.Cantidad)))
                          .OrderBy(i => i.ProductoId).ToList();

        await using var ctx = _factory();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            var venta = new Venta { ClienteId = clienteId, Fecha = DateTime.UtcNow };
            foreach (var l in lineas)
            {
                var id = l.ProductoId;
                var cant = l.Cantidad;
                var filas = await ctx.Productos.Where(p => p.Id == id && p.Stock >= cant)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - cant));
                if (filas == 0) throw new StockInsuficienteException(id);

                var precio = await ctx.Productos.AsNoTracking().Where(p => p.Id == id).Select(p => p.Precio).FirstAsync();
                venta.Detalles.Add(new DetalleVenta { ProductoId = id, Cantidad = cant, PrecioUnitario = precio });
                venta.Total += precio * cant;
            }

            ctx.Ventas.Add(venta);
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return venta.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Venta?> ObtenerAsync(int id)
    {
        await using var c = _factory();
        return await c.Ventas.AsNoTracking().Include(v => v.Detalles).FirstOrDefaultAsync(v => v.Id == id);
    }
}
