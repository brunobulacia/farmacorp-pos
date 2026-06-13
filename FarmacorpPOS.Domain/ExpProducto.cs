namespace FarmacorpPOS.Domain;

public class ExpProducto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Observaciones { get; set; } = string.Empty;

    public int IdTipoProducto { get; set; }
    public TipoProducto TipoProducto { get; set; } = null!;

    public ErpProducto ErpProducto { get; set; } = null!;
    public ICollection<ProductoCategoria> ProductosCategorias { get; set; } = new List<ProductoCategoria>();
    public ICollection<VentaExpress> Ventas { get; set; } = new List<VentaExpress>();
}
