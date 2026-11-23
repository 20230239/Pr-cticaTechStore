using TechStore.AdoNet;
using TechStore.Core;
using TechStore.EFCore;

var cs = Config.ConnectionString;
var factory = EfFactory.Create(cs);
var ado = (Nombre: "ADO.NET", P: (IProductoRepository)new AdoProductoRepository(cs), V: (IVentaRepository)new AdoVentaRepository(cs));
var ef  = (Nombre: "EF Core", P: (IProductoRepository)new EfProductoRepository(factory, true), V: (IVentaRepository)new EfVentaRepository(factory));
var techs = new[] { ado, ef };

while (true)
{
    Console.WriteLine("\n=== TechStore Demo ===");
    Console.WriteLine("1) Venta exitosa con ADO.NET y con EF Core");
    Console.WriteLine("2) Fallo forzado: demostrar ROLLBACK (stock insuficiente en el 2º producto)");
    Console.WriteLine("3) Búsqueda paginada: 'Laptops Modelo 1' (ambas tecnologías)");
    Console.WriteLine("0) Salir");
    Console.Write("> ");
    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            foreach (var t in techs)
            {
                Console.WriteLine($"\n--- {t.Nombre} ---");
                var antes = await t.P.ObtenerPorIdAsync(1);
                var id = await t.V.RegistrarVentaAsync(1, new[] { new ItemVenta(1, 2), new ItemVenta(2, 1) });
                var v = await t.V.ObtenerAsync(id);
                var despues = await t.P.ObtenerPorIdAsync(1);
                Console.WriteLine($"Venta #{id}  total={v!.Total:C}  líneas={v.Detalles.Count}");
                Console.WriteLine($"Stock producto 1: {antes!.Stock} -> {despues!.Stock}");
            }
            break;

        case "2":
            foreach (var t in techs)
            {
                Console.WriteLine($"\n--- {t.Nombre} ---");
                var antes = await t.P.ObtenerPorIdAsync(3);
                try
                {
                    await t.V.RegistrarVentaAsync(1, new[] { new ItemVenta(3, 1), new ItemVenta(4, 1_000_000) });
                    Console.WriteLine("¡No debería llegar aquí!");
                }
                catch (StockInsuficienteException ex)
                {
                    Console.WriteLine($"Error esperado: {ex.Message}");
                }
                var despues = await t.P.ObtenerPorIdAsync(3);
                Console.WriteLine($"Stock producto 3: antes={antes!.Stock} después={despues!.Stock}  =>  " +
                                  (antes.Stock == despues.Stock ? "ROLLBACK correcto" : "ERROR: no se revirtió"));
            }
            break;

        case "3":
            foreach (var t in techs)
            {
                var r = await t.P.BuscarAsync("Laptops Modelo 1", null, 1, 5);
                Console.WriteLine($"\n--- {t.Nombre} (primeros {r.Count}) ---");
                foreach (var p in r) Console.WriteLine($"{p.Id,6}  {p.Sku,-11} {p.Nombre,-28} {p.Precio,10:C}  stock={p.Stock}");
            }
            break;

        case "0": return;
    }
}
