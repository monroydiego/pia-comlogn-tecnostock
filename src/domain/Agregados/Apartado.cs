using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.Estados;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Reserva de una unidad con anticipo (G-01).</summary>
public sealed class Apartado : RaizDeAgregado
{
    public Folio Folio { get; private set; } = null!;
    public ClienteId ClienteId { get; private set; }
    public SucursalId SucursalId { get; private set; }
    public UnidadId UnidadId { get; private set; }
    public Dinero PrecioAcordado { get; private set; }
    public PagoRecibido Anticipo { get; private set; } = null!;
    public VigenciaDeApartado Vigencia { get; private set; } = null!;
    public EstadoDeApartado Estado { get; private set; }
    public DecisionDeSupervisor? Cancelacion { get; private set; }

    private Apartado() { }

    /// <summary>
    /// CU-01. Valida todo antes de modificar la unidad: producto apartable (RN-APT-004), método de pago
    /// (RN-VTA-002), anticipo mínimo (RN-APT-001) y bloqueo de la unidad (RN-APT-003).
    /// </summary>
    public static Apartado Crear(Folio folio, ClienteId clienteId, UnidadInventario unidad, Producto producto,
        Dinero precio, PagoRecibido anticipo, PoliticaComercial politica, DateTime ahora)
    {
        var apartable = new EspecificacionDeProductoApartable().EsSatisfechaPor(producto);
        if (!apartable.Cumple)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.ProductoNoApartable, apartable.Razon);

        politica.ValidarPago(anticipo);
        var minimo = politica.AnticipoMinimoSobre(precio);
        if (anticipo.Monto.EsMenorQue(minimo))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.AnticipoInsuficiente,
                $"El anticipo debe ser al menos {minimo}.");

        var sucursal = unidad.SucursalActual;
        unidad.Apartar(folio, sucursal);

        var vigencia = politica.NuevaVigenciaDesde(ahora);
        var apartado = new Apartado
        {
            Folio = folio, ClienteId = clienteId, SucursalId = sucursal, UnidadId = unidad.Id,
            PrecioAcordado = precio, Anticipo = anticipo, Vigencia = vigencia, Estado = EstadoDeApartado.Vigente
        };
        apartado.Registrar(new ApartadoCreado(folio, vigencia.Hasta, ahora));
        return apartado;
    }

    public bool EstaVigente(DateTime ahora) => new EspecificacionDeApartadoVigente(ahora).EsSatisfechaPor(this).Cumple;

    public Dinero SaldoPendiente() =>
        Estado == EstadoDeApartado.Convertido ? Dinero.Cero : PrecioAcordado.Restar(Anticipo.Monto);

    /// <summary>Valida el pago del saldo sin modificar el apartado.</summary>
    public void ValidarLiquidacion(PagoRecibido pagoSaldo, PoliticaComercial politica)
    {
        if (!MaquinasDelDominio.Apartado.PuedeTransicionar(Estado, EstadoDeApartado.Convertido))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.TransicionDeEstadoInvalida, $"El apartado está {Estado}.");
        politica.ValidarPago(pagoSaldo);
        if (pagoSaldo.Monto.EsMenorQue(SaldoPendiente()))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SaldoInsuficiente,
                $"El pago es menor al saldo pendiente de {SaldoPendiente()}.");
    }

    /// <summary>US07: el apartado queda Convertido al cubrirse el saldo con un método autorizado.</summary>
    public void Liquidar(PagoRecibido pagoSaldo, PoliticaComercial politica, DateTime ahora)
    {
        ValidarLiquidacion(pagoSaldo, politica);
        Estado = MaquinasDelDominio.Apartado.Transicionar(Estado, EstadoDeApartado.Convertido);
    }

    /// <summary>RN-APT-002: pasa a Vencido al alcanzarse la fecha límite.</summary>
    public void Vencer(DateTime ahora)
    {
        if (Estado == EstadoDeApartado.Vigente && !Vigencia.HaVencido(ahora))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.TransicionDeEstadoInvalida, "El apartado aún no alcanza su fecha límite.");
        Estado = MaquinasDelDominio.Apartado.Transicionar(Estado, EstadoDeApartado.Vencido);
        Registrar(new ApartadoVencido(Folio, UnidadId, ahora));
    }

    /// <summary>US14 / RN-SUP-001: cancelación autorizada por un Supervisor.</summary>
    public void Cancelar(DecisionDeSupervisor decision)
    {
        if (!decision.Autoriza)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SoloSupervisorAutoriza, "La cancelación requiere la autorización del Supervisor.");
        Estado = MaquinasDelDominio.Apartado.Transicionar(Estado, EstadoDeApartado.Cancelado);
        Cancelacion = decision;
    }
}
