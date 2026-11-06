namespace TecnoStock.Infraestructura.Persistencia;

/// <summary>Comprobante emitido (US12), con folio único. Solo infraestructura.</summary>
public sealed class ComprobanteEmitido
{
    public string FolioDeComprobante { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FolioDeOperacion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

/// <summary>Cliente de los datos de ejemplo del curso (actores.csv). Se siembra como referencia; el dominio solo usa su identificador.</summary>
public sealed class ClienteSemilla
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsPreferente { get; set; }
    public bool Activo { get; set; }
    public string Ciudad { get; set; } = string.Empty;
}
