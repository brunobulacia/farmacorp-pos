using FarmacorpPOS.Domain;

namespace FarmacorpPOS.Application.Reglas;

public class ReglasBase : IReglasNegocio
{
    public string Nombre => "BASE";

    public decimal CalcularPrecio(decimal costo) => costo * 1.50m;

    public decimal CalcularDescuento(ExpProducto producto, int cantidadCategorias) =>
        cantidadCategorias == 1 ? 0.30m : 0m;

    public bool ValidarStock(ErpProducto erp, int cantidad) =>
        erp.Stock >= cantidad;
}
