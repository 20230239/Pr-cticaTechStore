using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechStore.Core;

namespace TechStore.EFCore;

public class TechStoreDbContext : DbContext
{
    public TechStoreDbContext(DbContextOptions<TechStoreDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetalleVentas => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("UQ_Categorias_Nombre");
        });

        b.Entity<Producto>(e =>
        {
            e.ToTable("Productos", t =>
            {
                t.HasCheckConstraint("CK_Productos_Precio", "[Precio] > 0");
                t.HasCheckConstraint("CK_Productos_Stock", "[Stock] >= 0");
            });
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Sku).HasColumnName("SKU").HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.Precio).HasColumnType("decimal(10,2)");
            e.Property(x => x.Stock).HasDefaultValue(0);
            e.HasIndex(x => x.Sku).IsUnique().HasDatabaseName("UQ_Productos_SKU");
            e.HasIndex(x => x.CategoriaId).HasDatabaseName("IX_Productos_CategoriaId");
            e.HasIndex(x => x.Nombre).HasDatabaseName("IX_Productos_Nombre");
            e.HasOne(x => x.Categoria).WithMany(c => c.Productos)
             .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict)
             .HasConstraintName("FK_Productos_Categorias");
        });

        b.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Clientes_Email");
        });

        b.Entity<Venta>(e =>
        {
            e.ToTable("Ventas", t => t.HasCheckConstraint("CK_Ventas_Total", "[Total] >= 0"));
            e.Property(x => x.Total).HasColumnType("decimal(12,2)");
            e.Property(x => x.Fecha).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.ClienteId).HasDatabaseName("IX_Ventas_ClienteId");
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId)
             .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Ventas_Clientes");
        });

        b.Entity<DetalleVenta>(e =>
        {
            e.ToTable("DetalleVentas", t =>
            {
                t.HasCheckConstraint("CK_Detalle_Cantidad", "[Cantidad] > 0");
                t.HasCheckConstraint("CK_Detalle_Precio", "[PrecioUnitario] > 0");
            });
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(10,2)");
            e.HasIndex(x => x.VentaId).HasDatabaseName("IX_Detalle_VentaId");
            e.HasIndex(x => x.ProductoId).HasDatabaseName("IX_Detalle_ProductoId");
            e.HasOne<Venta>().WithMany(v => v.Detalles).HasForeignKey(x => x.VentaId)
             .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_Detalle_Ventas");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId)
             .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Detalle_Productos");
        });
    }
}

public static class EfFactory
{
    public static Func<TechStoreDbContext> Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TechStoreDbContext>().UseSqlServer(connectionString).Options;
        return () => new TechStoreDbContext(options);
    }
}

public class DesignTimeFactory : IDesignTimeDbContextFactory<TechStoreDbContext>
{
    public TechStoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TechStoreDbContext>()
            .UseSqlServer(Config.ConnectionString).Options;
        return new TechStoreDbContext(options);
    }
}
