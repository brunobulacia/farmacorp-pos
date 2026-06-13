namespace FarmacorpPOS.Domain;

public class CodigoBarra
{
    public int IdCodigoBarra { get; set; }
    public string UniqueCodigo { get; set; } = string.Empty;
    public bool Activo { get; set; }

    public int IdProducto { get; set; }
    public ErpProducto ErpProducto { get; set; } = null!;
}
