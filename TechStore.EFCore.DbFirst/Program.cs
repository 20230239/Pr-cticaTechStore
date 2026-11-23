#if DBFIRST_GENERADO
using Microsoft.EntityFrameworkCore;
using TechStore.EFCore.DbFirst.Models;

var cs = Environment.GetEnvironmentVariable("TECHSTORE_CONN")
         ?? "Server=localhost;Database=TechStoreDB;Trusted_Connection=True;TrustServerCertificate=True;";
var options = new DbContextOptionsBuilder<TechStoreDbFirstContext>().UseSqlServer(cs).Options;
using var db = new TechStoreDbFirstContext(options);
Console.WriteLine($"[Database First] Productos:  {await db.Productos.CountAsync():N0}");
Console.WriteLine($"[Database First] Categorias: {await db.Categorias.CountAsync():N0}");
#else
Console.WriteLine("Modelo Database First aun no generado. Ejecuta scaffold.sh o scaffold.ps1 (ver README).");
#endif
