namespace FarmacorpPOS.Domain;

public class ErpProducto
{
    public int IdProducto { get; set; }
    public decimal Costo { get; set; }
    public string UniqueCodigo { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public int Stock { get; set; }

    public ExpProducto Producto { get; set; } = null!;
    public ICollection<CodigoBarra> CodigosBarras { get; set; } = new List<CodigoBarra>();
}
