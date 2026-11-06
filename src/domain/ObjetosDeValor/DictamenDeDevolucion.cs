namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Resultado de evaluar el criterio de devolución (G-40).</summary>
public sealed record DictamenDeDevolucion
{
    public IReadOnlyList<ResultadoDeRegla> Resultados { get; }

    public DictamenDeDevolucion(IEnumerable<ResultadoDeRegla> resultados)
    {
        Resultados = resultados.ToList();
        if (Resultados.Count == 0) throw new ArgumentException("El dictamen requiere al menos un resultado.", nameof(resultados));
    }

    public bool Cumple() => Resultados.All(r => r.Cumple);

    public IReadOnlyList<string> Incumplimientos() =>
        Resultados.Where(r => !r.Cumple).Select(r => r.Razon).ToList();
}
