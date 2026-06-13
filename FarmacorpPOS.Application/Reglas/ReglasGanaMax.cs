using FarmacorpPOS.Domain;

namespace FarmacorpPOS.Application.Reglas;

public class ReglasGanaMax : IReglasNegocio
{
    public string Nombre => "GANAMAX";

    public decimal CalcularPrecio(decimal costo) => costo * 1.80m;

    public decimal CalcularDescuento(ExpProducto producto, int cantidadCategorias) =>
        cantidadCategorias == 1 ? 0.10m : 0m;

    public bool ValidarStock(ErpProducto erp, int cantidad) =>
        erp.Stock >= cantidad && (erp.Stock - cantidad) > 10;
}
