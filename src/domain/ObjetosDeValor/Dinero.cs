namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Importe en pesos mexicanos (MXN), inmutable.</summary>
public readonly record struct Dinero
{
    public decimal Monto { get; }
    public string Moneda { get; }

    public Dinero(decimal monto, string moneda = "MXN")
    {
        if (monto < 0) throw new ArgumentException("El monto no puede ser negativo.", nameof(monto));
        Monto = decimal.Round(monto, 2);
        Moneda = moneda;
    }

    public static Dinero Cero => new(0m);

    public Dinero Sumar(Dinero otro) => new(Monto + otro.Monto, Moneda);

    public Dinero Restar(Dinero otro) => new(Math.Max(0m, Monto - otro.Monto), Moneda);

    public bool EsMenorQue(Dinero otro) => Monto < otro.Monto;

    /// <summary>Calcula un porcentaje del monto; <paramref name="valor"/> es porcentual (30 = 30%).</summary>
    public Dinero Porcentaje(decimal valor) => new(Monto * valor / 100m, Moneda);

    public override string ToString() => $"{Monto:0.00} {Moneda}";
}
