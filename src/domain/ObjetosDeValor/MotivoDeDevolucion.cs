using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record MotivoDeDevolucion
{
    public string Causal { get; }
    public string Detalle { get; }

    public MotivoDeDevolucion(string causal, string detalle = "")
    {
        Causal = Limites.Texto(causal, Limites.Causal, "La causal");
        Detalle = Limites.TextoOpcional(detalle, Limites.DetalleDeMotivo, "El detalle");
    }
}
