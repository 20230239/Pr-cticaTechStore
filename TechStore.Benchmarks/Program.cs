using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using TechStore.AdoNet;
using TechStore.Core;
using TechStore.EFCore;

int reps = int.Parse(Arg("--reps") ?? "5");
bool quick = args.Contains("--quick");
string outFile = Arg("--out") ?? "resultados.csv";
string? Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

var cs = Config.ConnectionString;
var factory = EfFactory.Create(cs);
var techs = new List<Repos>
{
    new("ADO.NET",                new AdoProductoRepository(cs),             new AdoClienteRepository(cs),             new AdoVentaRepository(cs)),
    new("EF Core (tracking)",     new EfProductoRepository(factory, false),  new EfClienteRepository(factory, false),  new EfVentaRepository(factory)),
    new("EF Core (AsNoTracking)", new EfProductoRepository(factory, true),   new EfClienteRepository(factory, true),   new EfVentaRepository(factory)),
};

var inicio = DateTime.UtcNow;
long seq = 0;
string run = DateTime.UtcNow.ToString("mmss");
string NuevoSku() => $"BENCH-{run}-{Interlocked.Increment(ref seq):D8}";
List<Producto> Generar(int n) => Enumerable.Range(0, n).Select(i => new Producto
{
    Nombre = $"Producto benchmark {i}", Sku = NuevoSku(), Precio = 10 + i % 100, Stock = 100, CategoriaId = 1 + i % 10
}).ToList();

Task LimpiarProductos() => Sql.Exec("DELETE FROM dbo.Productos WHERE SKU LIKE 'BENCH-%'");
Task LimpiarVentas() => Sql.Exec("DELETE FROM dbo.Ventas WHERE Fecha >= @f", new SqlParameter("@f", inicio));

var escenarios = new List<Escenario>
{
    new("1a Insertar individual", new[] { 1_000, 10_000 },
        (r, n) => { var l = Generar(n); return Task.FromResult<Func<Task>>(async () => { foreach (var p in l) await r.P.InsertarAsync(p); }); },
        LimpiarProductos),

    new("1b Insertar en lote", new[] { 1_000, 10_000, 50_000, 100_000 },
        (r, n) => { var l = Generar(n); return Task.FromResult<Func<Task>>(() => r.P.InsertarLoteAsync(l)); },
        LimpiarProductos),

    new("2 Consulta por ID", new[] { 1_000, 10_000, 50_000 },
        (r, n) => { var rnd = new Random(42); var ids = Enumerable.Range(0, n).Select(_ => rnd.Next(1, 50_001)).ToArray();
                    return Task.FromResult<Func<Task>>(async () => { foreach (var id in ids) await r.P.ObtenerPorIdAsync(id); }); }),

    new("3a Busqueda filtrada (prefijo + pagina 50)", new[] { 100, 1_000, 5_000 },
        (r, n) => Task.FromResult<Func<Task>>(async () => { for (int i = 0; i < n; i++) await r.P.BuscarAsync("Laptops Modelo 1", null, 1, 50); })),

    new("3b Paginacion (lee N filas, paginas de 500)", new[] { 1_000, 10_000, 50_000, 100_000 },
        (r, n) => Task.FromResult<Func<Task>>(async () => { for (int pg = 1; pg <= n / 500 || pg == 1; pg++) await r.P.BuscarAsync(null, null, pg, 500); })),

    new("4 JOIN Productos+Categorias (N filas)", new[] { 1_000, 10_000, 50_000, 100_000 },
        (r, n) => Task.FromResult<Func<Task>>(() => r.P.ListarConCategoriaAsync(n))),

    new("5a Actualizacion masiva set-based (UPDATE / ExecuteUpdate)", new[] { 1_000, 10_000, 50_000, 100_000 },
        (r, n) => Task.FromResult<Func<Task>>(() => r.P.ActualizarPrecioRangoMasivoAsync(1, n, 1m)),
        null, new[] { "ADO.NET", "EF Core (AsNoTracking)" }),

    new("5b Actualizacion masiva por entidades (SaveChanges)", new[] { 1_000, 10_000, 50_000, 100_000 },
        (r, n) => Task.FromResult<Func<Task>>(() => r.P.ActualizarPrecioRangoAsync(1, n, 1m)),
        null, new[] { "EF Core (tracking)" }),

    new("6 Venta transaccional (3 items c/u)", new[] { 50, 500 },
        async (r, n) =>
        {
            await Sql.Exec("UPDATE dbo.Productos SET Stock = 1000000 WHERE Id <= 30");
            var rnd = new Random(7);
            var ventas = Enumerable.Range(0, n).Select(k =>
                Enumerable.Range(1, 30).OrderBy(j => rnd.Next()).Take(3).Select(id => new ItemVenta(id, 1)).ToList()).ToList();
            return async () => { foreach (var v in ventas) await r.V.RegistrarVentaAsync(1, v); };
        },
        LimpiarVentas),
};

var resultados = new List<Resultado>();
Console.WriteLine($"TechStore benchmarks | repeticiones={reps} | quick={quick}");
foreach (var esc in escenarios)
{
    var vols = quick ? esc.Volumenes.Take(2).ToArray() : esc.Volumenes;
    foreach (var vol in vols)
        foreach (var t in techs.Where(t => esc.Solo is null || esc.Solo.Contains(t.Nombre)))
        {
            var warm = await esc.Crear(t, vol); await warm(); if (esc.Limpiar != null) await esc.Limpiar();

            var tiempos = new List<double>(); var mem = new List<double>();
            for (int i = 0; i < reps; i++)
            {
                var accion = await esc.Crear(t, vol);
                GC.Collect(); GC.WaitForPendingFinalizers();
                long m0 = GC.GetTotalAllocatedBytes(true);
                var sw = Stopwatch.StartNew();
                await accion();
                sw.Stop();
                mem.Add((GC.GetTotalAllocatedBytes(true) - m0) / 1024.0);
                tiempos.Add(sw.Elapsed.TotalMilliseconds);
                if (esc.Limpiar != null) await esc.Limpiar();
            }
            var res = new Resultado(esc.Nombre, t.Nombre, vol, tiempos.Average(), tiempos.Min(), Desv(tiempos), mem.Average());
            resultados.Add(res);
            Console.WriteLine($"{res.Escenario,-62} {res.Tecnologia,-24} n={res.Volumen,7:N0}  {res.MediaMs,10:N1} ms (±{res.DesvMs:N1})  {res.KbAsignados / 1024,9:N1} MB");
        }
}

var sb = new StringBuilder("Escenario,Tecnologia,Volumen,MediaMs,MinMs,DesvMs,KbAsignados\n");
foreach (var r in resultados)
    sb.AppendLine(string.Join(',', $"\"{r.Escenario}\"", $"\"{r.Tecnologia}\"", r.Volumen,
        r.MediaMs.ToString("F3", CultureInfo.InvariantCulture), r.MinMs.ToString("F3", CultureInfo.InvariantCulture),
        r.DesvMs.ToString("F3", CultureInfo.InvariantCulture), r.KbAsignados.ToString("F1", CultureInfo.InvariantCulture)));
File.WriteAllText(outFile, sb.ToString());
Console.WriteLine($"\nResultados guardados en {Path.GetFullPath(outFile)}");

static double Desv(List<double> v) { var m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / Math.Max(1, v.Count - 1)); }

record Repos(string Nombre, IProductoRepository P, IClienteRepository C, IVentaRepository V);
record Escenario(string Nombre, int[] Volumenes, Func<Repos, int, Task<Func<Task>>> Crear, Func<Task>? Limpiar = null, string[]? Solo = null);
record Resultado(string Escenario, string Tecnologia, int Volumen, double MediaMs, double MinMs, double DesvMs, double KbAsignados);

static class Sql
{
    public static async Task Exec(string sql, params SqlParameter[] ps)
    {
        await using var cn = new SqlConnection(Config.ConnectionString);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 300 };
        cmd.Parameters.AddRange(ps);
        await cmd.ExecuteNonQueryAsync();
    }
}
