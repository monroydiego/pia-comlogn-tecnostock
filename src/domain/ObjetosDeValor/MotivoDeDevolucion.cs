namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record MotivoDeDevolucion
{
    public string Causal { get; }
    public string Detalle { get; }

    public MotivoDeDevolucion(string causal, string detalle = "")
    {
        if (string.IsNullOrWhiteSpace(causal)) throw new ArgumentException("La causal es obligatoria.", nameof(causal));
        Causal = causal.Trim();
        Detalle = detalle?.Trim() ?? string.Empty;
    }
}
