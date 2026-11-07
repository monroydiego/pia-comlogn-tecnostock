using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record NumeroDeSerie
{
    public string Valor { get; }

    public NumeroDeSerie(string valor)
    {
        Valor = Limites.Texto(valor, Limites.Serie, "La serie").ToUpperInvariant();
    }

    public override string ToString() => Valor;
}
