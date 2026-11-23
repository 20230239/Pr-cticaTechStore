using Microsoft.EntityFrameworkCore;
using TechStore.Core;

namespace TechStore.EFCore;

public sealed class EfClienteRepository : IClienteRepository
{
    private readonly Func<TechStoreDbContext> _factory;
    private readonly bool _noTracking;

    public EfClienteRepository(Func<TechStoreDbContext> factory, bool noTracking = false)
    {
        _factory = factory;
        _noTracking = noTracking;
    }

    private IQueryable<Cliente> Q(TechStoreDbContext c) => _noTracking ? c.Clientes.AsNoTracking() : c.Clientes;

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        await using var c = _factory();
        return await Q(c).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> InsertarAsync(Cliente cliente)
    {
        await using var c = _factory();
        c.Clientes.Add(cliente);
        await c.SaveChangesAsync();
        return cliente.Id;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(int pagina, int tamano)
    {
        await using var c = _factory();
        return await Q(c).OrderBy(x => x.Id).Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();
    }
}
