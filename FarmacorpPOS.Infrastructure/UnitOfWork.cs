using FarmacorpPOS.Domain;
using FarmacorpPOS.Infrastructure.Repositories;

namespace FarmacorpPOS.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly FarmacorpDbContext _context;

    public IRepository<TipoProducto> Tipos { get; }
    public IRepository<ExpProducto> Productos { get; }
    public IRepository<ErpProducto> ErpProductos { get; }
    public IRepository<CodigoBarra> CodigosBarras { get; }
    public IRepository<Categoria> Categorias { get; }
    public IRepository<ProductoCategoria> ProductosCategorias { get; }
    public IRepository<VentaExpress> Ventas { get; }

    public UnitOfWork(FarmacorpDbContext context)
    {
        _context = context;
        Tipos = new Repository<TipoProducto>(context);
        Productos = new Repository<ExpProducto>(context);
        ErpProductos = new Repository<ErpProducto>(context);
        CodigosBarras = new Repository<CodigoBarra>(context);
        Categorias = new Repository<Categoria>(context);
        ProductosCategorias = new Repository<ProductoCategoria>(context);
        Ventas = new Repository<VentaExpress>(context);
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
