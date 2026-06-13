using FarmacorpPOS.Application.Reglas;
using FarmacorpPOS.Domain;
using FarmacorpPOS.Infrastructure;

namespace FarmacorpPOS.Application.Services;

public class VentaService
{
    private readonly IUnitOfWork _uow;
    private readonly ReglasNegocioProvider _reglas;

    public VentaService(IUnitOfWork uow, ReglasNegocioProvider reglas)
    {
        _uow = uow;
        _reglas = reglas;
    }

    public async Task<VentaExpress> RegistrarVentaAsync(int idProducto, string cliente, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.");

        var producto = await _uow.Productos.GetByIdAsync(idProducto)
            ?? throw new InvalidOperationException($"No existe el producto {idProducto}.");

        var erp = await _uow.ErpProductos.GetByIdAsync(idProducto)
            ?? throw new InvalidOperationException($"El producto {idProducto} no tiene datos ERP (costo/stock).");

        var reglas = _reglas.Actual;

        if (!reglas.ValidarStock(erp, cantidad))
            throw new InvalidOperationException(
                $"Stock insuficiente para la estrategia {reglas.Nombre}. Stock actual: {erp.Stock}.");

        var cantidadCategorias = (await _uow.ProductosCategorias.GetAllAsync())
            .Count(pc => pc.IdProducto == idProducto);
        var descuento = reglas.CalcularDescuento(producto, cantidadCategorias);

        var precio = producto.Precio;
        var total = (precio * cantidad) * (1 - descuento);

        var venta = new VentaExpress
        {
            IdProducto = idProducto,
            Cliente = cliente,
            Producto = producto.Nombre,
            Fecha = DateTime.Now,
            Cantidad = cantidad,
            Precio = precio,
            Descuento = descuento,
            Total = total,
            UniqueProducto = erp.UniqueCodigo
        };

        erp.Stock -= cantidad;
        _uow.ErpProductos.Update(erp);

        await _uow.Ventas.AddAsync(venta);
        await _uow.SaveChangesAsync();
        return venta;
    }
}
