namespace TecnoStock.Dominio.Comun;

/// <summary>
/// Longitudes y rangos máximos de los datos. Son invariantes del dominio y, a la vez, el tamaño de las columnas de la
/// base de datos (la infraestructura los usa al mapear), de modo que una entrada demasiado grande se rechaza con 400
/// y nunca llega a reventar el guardado.
/// </summary>
public static class Limites
{
    public const int Codigo = 30;          // productos, sucursales, clientes y folios
    public const int Serie = 60;
    public const int Usuario = 60;
    public const int Causal = 100;
    public const int DetalleDeMotivo = 500;
    public const int MotivoDeDecision = 500;
    /// <summary>Mayor importe que cabe en decimal(18,2).</summary>
    public const decimal MontoMaximo = 9_999_999_999_999_999.99m;

    internal static string Texto(string? valor, int maximo, string nombre)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException($"{nombre} es obligatorio.");
        var limpio = valor.Trim();
        if (limpio.Length > maximo) throw new ArgumentException($"{nombre} no puede tener más de {maximo} caracteres.");
        return limpio;
    }

    internal static string TextoOpcional(string? valor, int maximo, string nombre)
    {
        var limpio = valor?.Trim() ?? string.Empty;
        if (limpio.Length > maximo) throw new ArgumentException($"{nombre} no puede tener más de {maximo} caracteres.");
        return limpio;
    }
}
