using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Estados;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Solicitud de devolución de una línea vendida (G-09, G-10).</summary>
public sealed class Devolucion : RaizDeAgregado
{
    public Folio Folio { get; private set; } = null!;
    public Folio FolioDeVenta { get; private set; } = null!;
    public UnidadId UnidadDevuelta { get; private set; }
    public MotivoDeDevolucion Motivo { get; private set; } = null!;
    public CondicionDelProducto Condicion { get; private set; }
    public FormaDeCompensacion FormaDeCompensacion { get; private set; }
    // Los resultados de cada regla se guardan como filas hijas (1FN); el dictamen se reconstruye a partir de ellos.
    private readonly List<ResultadoDeRegla> _resultadosDelDictamen = new();
    public IReadOnlyList<ResultadoDeRegla> ResultadosDelDictamen => _resultadosDelDictamen;
    public DictamenDeDevolucion Dictamen => new(_resultadosDelDictamen);
    public DecisionDeSupervisor? Decision { get; private set; }
    public EstadoDeDevolucion Estado { get; private set; }
    public Operador Cajero { get; private set; } = null!;
    public DateTime FechaDeSolicitud { get; private set; }

    private Devolucion() { }

    /// <summary>
    /// CU-04. Nace EnRevision; si el dictamen cumple el criterio estándar transita a Aceptada,
    /// si no, queda derivada al Supervisor (rechazo con derivación, ADR-006).
    /// </summary>
    public static Devolucion Solicitar(Folio folio, Venta venta, ProductoId productoId, NumeroDeSerie? serie,
        MotivoDeDevolucion motivo, CondicionDelProducto condicion, FormaDeCompensacion forma,
        DictamenDeDevolucion dictamen, Operador operador, DateTime ahora)
    {
        var linea = venta.ObtenerLinea(productoId, serie);
        var devolucion = new Devolucion
        {
            Folio = folio, FolioDeVenta = venta.Folio, UnidadDevuelta = linea.UnidadId, Motivo = motivo,
            Condicion = condicion, FormaDeCompensacion = forma, Cajero = operador,
            FechaDeSolicitud = ahora, Estado = EstadoDeDevolucion.EnRevision
        };

        devolucion._resultadosDelDictamen.AddRange(dictamen.Resultados);

        if (dictamen.Cumple())
        {
            devolucion.Estado = MaquinasDelDominio.Devolucion.Transicionar(devolucion.Estado, EstadoDeDevolucion.Aceptada);
            devolucion.Registrar(new DevolucionAceptada(folio, linea.UnidadId, venta.Folio, condicion, ahora));
        }
        else
        {
            devolucion.Registrar(new DevolucionDerivadaASupervisor(folio, dictamen.Incumplimientos(), ahora));
        }
        return devolucion;
    }

    /// <summary>CU-05 / RN-SUP-002: solo desde EnRevision; el Supervisor autoriza o deniega.</summary>
    public void Resolver(DecisionDeSupervisor decision, DateTime ahora)
    {
        var destino = decision.Autoriza ? EstadoDeDevolucion.Aceptada : EstadoDeDevolucion.Rechazada;
        Estado = MaquinasDelDominio.Devolucion.Transicionar(Estado, destino);
        Decision = decision;
        Registrar(decision.Autoriza
            ? new DevolucionAceptada(Folio, UnidadDevuelta, FolioDeVenta, Condicion, ahora)
            : new DevolucionRechazada(Folio, decision.Motivo, ahora));
    }
}
