namespace FarmacorpPOS.Domain;

public class Categoria
{
    public int IdCategoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; }

    public int? IdCategoriaPadre { get; set; }
    public Categoria? CategoriaPadre { get; set; }

    public ICollection<ProductoCategoria> ProductosCategorias { get; set; } = new List<ProductoCategoria>();
}
