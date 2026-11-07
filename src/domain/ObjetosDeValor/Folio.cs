using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record Folio
{
    public string Valor { get; }

    public Folio(string valor)
    {
        Valor = Limites.Texto(valor, Limites.Codigo, "El folio");
    }

    public override string ToString() => Valor;
}
