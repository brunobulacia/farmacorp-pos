using FarmacorpPOS.Domain;
using Microsoft.EntityFrameworkCore;

namespace FarmacorpPOS.Infrastructure;

public class FarmacorpDbContext : DbContext
{
    public FarmacorpDbContext(DbContextOptions<FarmacorpDbContext> options) : base(options) { }

    public DbSet<TipoProducto> TiposProducto => Set<TipoProducto>();
    public DbSet<ExpProducto> ExpProductos => Set<ExpProducto>();
    public DbSet<ErpProducto> ErpProductos => Set<ErpProducto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<ProductoCategoria> ProductosCategorias => Set<ProductoCategoria>();
    public DbSet<CodigoBarra> CodigosBarras => Set<CodigoBarra>();
    public DbSet<VentaExpress> VentasExpress => Set<VentaExpress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoProducto>(e =>
        {
            e.ToTable("TiposProducto");
            e.HasKey(x => x.IdTipoProducto);
            e.Property(x => x.Descripcion).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<ExpProducto>(e =>
        {
            e.ToTable("ExpProductos");
            e.HasKey(x => x.IdProducto);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
            e.Property(x => x.Precio).HasColumnType("decimal(18,2)");
            e.Property(x => x.Observaciones).HasMaxLength(500);

            e.HasOne(x => x.TipoProducto)
                .WithMany(t => t.Productos)
                .HasForeignKey(x => x.IdTipoProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ErpProducto>(e =>
        {
            e.ToTable("ErpProductos");
            e.HasKey(x => x.IdProducto);
            e.Property(x => x.IdProducto).ValueGeneratedNever();
            e.Property(x => x.Costo).HasColumnType("decimal(18,2)");
            e.Property(x => x.UniqueCodigo).IsRequired().HasMaxLength(12);
            e.HasIndex(x => x.UniqueCodigo).IsUnique();

            e.HasOne(x => x.Producto)
                .WithOne(p => p.ErpProducto)
                .HasForeignKey<ErpProducto>(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.HasKey(x => x.IdCategoria);
            e.Property(x => x.Descripcion).IsRequired().HasMaxLength(100);

            e.HasOne(x => x.CategoriaPadre)
                .WithMany()
                .HasForeignKey(x => x.IdCategoriaPadre)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductoCategoria>(e =>
        {
            e.ToTable("ProductosCategorias");
            e.HasKey(x => x.IdDetalle);

            e.HasOne(x => x.Producto)
                .WithMany(p => p.ProductosCategorias)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Categoria)
                .WithMany(c => c.ProductosCategorias)
                .HasForeignKey(x => x.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CodigoBarra>(e =>
        {
            e.ToTable("CodigosBarras");
            e.HasKey(x => x.IdCodigoBarra);
            e.Property(x => x.UniqueCodigo).IsRequired().HasMaxLength(12);
            e.HasIndex(x => x.UniqueCodigo).IsUnique();

            e.HasOne(x => x.ErpProducto)
                .WithMany(p => p.CodigosBarras)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VentaExpress>(e =>
        {
            e.ToTable("VentaExpress");
            e.HasKey(x => x.Id);
            e.Property(x => x.Cliente).IsRequired().HasMaxLength(150);
            e.Property(x => x.Producto).HasMaxLength(150);
            e.Property(x => x.UniqueProducto).HasMaxLength(12);
            e.Property(x => x.Precio).HasColumnType("decimal(18,2)");
            e.Property(x => x.Descuento).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.ExpProducto)
                .WithMany(p => p.Ventas)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
