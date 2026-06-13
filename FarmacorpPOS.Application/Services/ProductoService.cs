using FarmacorpPOS.Application.Reglas;
using FarmacorpPOS.Domain;
using FarmacorpPOS.Infrastructure;

namespace FarmacorpPOS.Application.Services;

public class ProductoService
{
    private readonly IUnitOfWork _uow;
    private readonly ReglasNegocioProvider _reglas;

    public ProductoService(IUnitOfWork uow, ReglasNegocioProvider reglas)
    {
        _uow = uow;
        _reglas = reglas;
    }

    public Task<IEnumerable<ExpProducto>> ListarProductosAsync() => _uow.Productos.GetAllAsync();
    public Task<IEnumerable<Categoria>> ListarCategoriasAsync() => _uow.Categorias.GetAllAsync();

    public async Task<Categoria> CrearCategoriaAsync(string descripcion, int? idPadre = null)
    {
        var cat = new Categoria { Descripcion = descripcion, Activo = true, IdCategoriaPadre = idPadre };
        await _uow.Categorias.AddAsync(cat);
        await _uow.SaveChangesAsync();
        return cat;
    }

    public async Task<ExpProducto> RegistrarProductoErpAsync(
        string nombre, DateTime fechaVencimiento, string observaciones,
        decimal costo, int stock)
    {
        var exp = new ExpProducto
        {
            Nombre = nombre,
            FechaVencimiento = fechaVencimiento,
            Observaciones = observaciones,
            Activo = true,
            TipoProducto = await ObtenerTipoPorDefectoAsync(),
            Precio = _reglas.Actual.CalcularPrecio(costo)
        };

        exp.ErpProducto = new ErpProducto
        {
            Costo = costo,
            Stock = stock,
            FechaRegistro = DateTime.Now,
            UniqueCodigo = await GenerarUniqueCodigoErpAsync()
        };

        await _uow.Productos.AddAsync(exp);
        await _uow.SaveChangesAsync();
        return exp;
    }

    public async Task<CodigoBarra> AsignarCodigoBarraAsync(int idProducto)
    {
        var erp = await _uow.ErpProductos.GetByIdAsync(idProducto)
            ?? throw new InvalidOperationException($"No existe un producto ERP con Id {idProducto}.");

        var codigo = new CodigoBarra
        {
            IdProducto = erp.IdProducto,
            Activo = true,
            UniqueCodigo = await GenerarUniqueCodigoBarraAsync()
        };

        await _uow.CodigosBarras.AddAsync(codigo);
        await _uow.SaveChangesAsync();
        return codigo;
    }

    public async Task<ProductoCategoria> AsignarCategoriaAsync(int idProducto, int idCategoria)
    {
        _ = await _uow.Productos.GetByIdAsync(idProducto)
            ?? throw new InvalidOperationException($"No existe el producto {idProducto}.");
        _ = await _uow.Categorias.GetByIdAsync(idCategoria)
            ?? throw new InvalidOperationException($"No existe la categoría {idCategoria}.");

        var detalle = new ProductoCategoria
        {
            IdProducto = idProducto,
            IdCategoria = idCategoria,
            FechaCreacion = DateTime.Now
        };

        await _uow.ProductosCategorias.AddAsync(detalle);
        await _uow.SaveChangesAsync();
        return detalle;
    }

    private static string NuevoCodigo() =>
        Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

    private async Task<string> GenerarUniqueCodigoErpAsync()
    {
        var existentes = (await _uow.ErpProductos.GetAllAsync())
            .Select(e => e.UniqueCodigo).ToHashSet();
        string codigo;
        do { codigo = NuevoCodigo(); } while (existentes.Contains(codigo));
        return codigo;
    }

    private async Task<string> GenerarUniqueCodigoBarraAsync()
    {
        var existentes = (await _uow.CodigosBarras.GetAllAsync())
            .Select(c => c.UniqueCodigo).ToHashSet();
        string codigo;
        do { codigo = NuevoCodigo(); } while (existentes.Contains(codigo));
        return codigo;
    }

    private async Task<TipoProducto> ObtenerTipoPorDefectoAsync()
    {
        var existente = (await _uow.Tipos.GetAllAsync())
            .FirstOrDefault(t => t.Descripcion == "General");
        if (existente != null) return existente;

        var tipo = new TipoProducto { Descripcion = "General" };
        await _uow.Tipos.AddAsync(tipo);
        return tipo;
    }
}
