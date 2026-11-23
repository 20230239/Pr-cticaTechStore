namespace TechStore.Core;

public interface IProductoRepository
{
    Task<Producto?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Producto>> BuscarAsync(string? nombre, int? categoriaId, int pagina, int tamano);
    Task<IReadOnlyList<ProductoConCategoria>> ListarConCategoriaAsync(int top);
    Task<int> InsertarAsync(Producto producto);
    Task InsertarLoteAsync(IReadOnlyList<Producto> productos);
    Task<int> ActualizarPrecioRangoAsync(int desdeId, int hastaId, decimal porcentaje);
    Task<int> ActualizarPrecioRangoMasivoAsync(int desdeId, int hastaId, decimal porcentaje);
}

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(Cliente cliente);
    Task<IReadOnlyList<Cliente>> ListarAsync(int pagina, int tamano);
}

public interface IVentaRepository
{
    Task<int> RegistrarVentaAsync(int clienteId, IReadOnlyList<ItemVenta> items);
    Task<Venta?> ObtenerAsync(int id);
}
