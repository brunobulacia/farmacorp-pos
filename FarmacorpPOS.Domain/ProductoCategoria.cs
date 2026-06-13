namespace FarmacorpPOS.Domain;

public class ProductoCategoria
{
    public int IdDetalle { get; set; }
    public DateTime FechaCreacion { get; set; }

    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }

    public ExpProducto Producto { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}
