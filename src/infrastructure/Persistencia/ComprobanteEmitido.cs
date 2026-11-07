using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia;

/// <summary>Comprobante emitido (US12), con folio único. Solo infraestructura.</summary>
public sealed class ComprobanteEmitido
{
    public string FolioDeComprobante { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FolioDeOperacion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = string.Empty;
    /// <summary>Una fila por renglón del comprobante (1FN).</summary>
    public List<ComprobanteLinea> Lineas { get; set; } = new();
}

public sealed class ComprobanteLinea
{
    public int Id { get; set; }
    public string FolioDeComprobante { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Texto { get; set; } = string.Empty;
}

/// <summary>Cliente de los datos de ejemplo del curso (actores.csv). El dominio solo usa su identificador; la FK garantiza que exista.</summary>
public sealed class ClienteSemilla
{
    public ClienteId Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsPreferente { get; set; }
    public bool Activo { get; set; }
    public string Ciudad { get; set; } = string.Empty;
}

/// <summary>Sucursal SUC-01 a SUC-05. [Supuesto]: los datos de ejemplo no traen sucursales; el nombre es genérico.</summary>
public sealed class SucursalSemilla
{
    public SucursalId Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
