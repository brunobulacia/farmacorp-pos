namespace FarmacorpPOS.Domain;

public class VentaExpress
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string UniqueProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public int IdProducto { get; set; }
    public ExpProducto ExpProducto { get; set; } = null!;
}
