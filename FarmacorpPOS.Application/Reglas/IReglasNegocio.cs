using FarmacorpPOS.Domain;

namespace FarmacorpPOS.Application.Reglas;

public interface IReglasNegocio
{
    string Nombre { get; }
    decimal CalcularPrecio(decimal costo);
    decimal CalcularDescuento(ExpProducto producto, int cantidadCategorias);
    bool ValidarStock(ErpProducto erp, int cantidad);
}
