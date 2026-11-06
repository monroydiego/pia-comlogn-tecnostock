namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record NumeroDeSerie
{
    public string Valor { get; }

    public NumeroDeSerie(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("La serie no puede estar vacía.", nameof(valor));
        Valor = valor.Trim().ToUpperInvariant();
    }

    public override string ToString() => Valor;
}
