using FarmacorpPOS.Domain;
using FarmacorpPOS.Infrastructure.Repositories;

namespace FarmacorpPOS.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    IRepository<TipoProducto> Tipos { get; }
    IRepository<ExpProducto> Productos { get; }
    IRepository<ErpProducto> ErpProductos { get; }
    IRepository<CodigoBarra> CodigosBarras { get; }
    IRepository<Categoria> Categorias { get; }
    IRepository<ProductoCategoria> ProductosCategorias { get; }
    IRepository<VentaExpress> Ventas { get; }
    Task<int> SaveChangesAsync();
}
