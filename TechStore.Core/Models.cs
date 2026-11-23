namespace TechStore.Core;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public List<Producto> Productos { get; set; } = new();
}

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
}

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
}

public class Venta
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = new();
}

public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public record ItemVenta(int ProductoId, int Cantidad);

public record ProductoConCategoria(int Id, string Nombre, string Sku, decimal Precio, int Stock, string Categoria);

public class StockInsuficienteException : Exception
{
    public int ProductoId { get; }
    public StockInsuficienteException(int productoId)
        : base($"Stock insuficiente o producto inexistente (Id={productoId}).") => ProductoId = productoId;
}
