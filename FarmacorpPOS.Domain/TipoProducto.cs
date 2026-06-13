namespace FarmacorpPOS.Domain;

public class TipoProducto
{
    public int IdTipoProducto { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<ExpProducto> Productos { get; set; } = new List<ExpProducto>();
}
