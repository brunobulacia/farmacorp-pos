namespace FarmacorpPOS.Application.Reglas;

public enum TipoEstrategia { Base, GanaMax }

public class ReglasNegocioProvider
{
    public IReglasNegocio Actual { get; private set; } = new ReglasBase();

    public void Cambiar(TipoEstrategia tipo)
    {
        Actual = tipo switch
        {
            TipoEstrategia.GanaMax => new ReglasGanaMax(),
            _ => new ReglasBase()
        };
    }
}
