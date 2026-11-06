namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record Folio
{
    public string Valor { get; }

    public Folio(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("El folio no puede estar vacío.", nameof(valor));
        Valor = valor.Trim();
    }

    public override string ToString() => Valor;
}
