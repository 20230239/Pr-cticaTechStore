using Microsoft.EntityFrameworkCore;
using TechStore.Core;

namespace TechStore.EFCore;

public sealed class EfProductoRepository : IProductoRepository
{
    private readonly Func<TechStoreDbContext> _factory;
    private readonly bool _noTracking;

    public EfProductoRepository(Func<TechStoreDbContext> factory, bool noTracking = false)
    {
        _factory = factory;
        _noTracking = noTracking;
    }

    private IQueryable<Producto> Q(TechStoreDbContext c) => _noTracking ? c.Productos.AsNoTracking() : c.Productos;

    public async Task<Producto?> ObtenerPorIdAsync(int id)
    {
        await using var c = _factory();
        return await Q(c).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Producto>> BuscarAsync(string? nombre, int? categoriaId, int pagina, int tamano)
    {
        await using var c = _factory();
        var q = Q(c);
        if (!string.IsNullOrWhiteSpace(nombre)) q = q.Where(p => p.Nombre.StartsWith(nombre));
        if (categoriaId.HasValue) q = q.Where(p => p.CategoriaId == categoriaId.Value);
        return await q.OrderBy(p => p.Id).Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();
    }

    public async Task<IReadOnlyList<ProductoConCategoria>> ListarConCategoriaAsync(int top)
    {
        await using var c = _factory();
        IQueryable<Producto> q = c.Productos;
        if (_noTracking) q = q.AsNoTracking();
        var lista = await q.Include(p => p.Categoria).OrderBy(p => p.Id).Take(top).ToListAsync();
        return lista.Select(p => new ProductoConCategoria(p.Id, p.Nombre, p.Sku, p.Precio, p.Stock, p.Categoria!.Nombre)).ToList();
    }

    public async Task<int> InsertarAsync(Producto producto)
    {
        await using var c = _factory();
        c.Productos.Add(producto);
        await c.SaveChangesAsync();
        return producto.Id;
    }

    public async Task InsertarLoteAsync(IReadOnlyList<Producto> productos)
    {
        await using var c = _factory();
        c.Productos.AddRange(productos);
        await c.SaveChangesAsync();
    }

    public async Task<int> ActualizarPrecioRangoAsync(int desdeId, int hastaId, decimal porcentaje)
    {
        var f = 1 + porcentaje / 100m;
        await using var c = _factory();
        var lista = await c.Productos.Where(p => p.Id >= desdeId && p.Id <= hastaId).ToListAsync();
        foreach (var p in lista) p.Precio = Math.Round(p.Precio * f, 2);
        return await c.SaveChangesAsync();
    }

    public async Task<int> ActualizarPrecioRangoMasivoAsync(int desdeId, int hastaId, decimal porcentaje)
    {
        var f = 1 + porcentaje / 100m;
        await using var c = _factory();
        return await c.Productos.Where(p => p.Id >= desdeId && p.Id <= hastaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, p => Math.Round(p.Precio * f, 2)));
    }
}
