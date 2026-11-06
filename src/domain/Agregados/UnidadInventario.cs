using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.Estados;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Pieza física individual con identidad propia (G-29, ADR-002).</summary>
public sealed class UnidadInventario : RaizDeAgregado
{
    public UnidadId Id { get; private set; }
    public ProductoId ProductoId { get; private set; }
    public SucursalId SucursalActual { get; private set; }
    public NumeroDeSerie? Serie { get; private set; }
    public EstadoDeUnidad Estado { get; private set; }
    public Folio? FolioDeApartadoVigente { get; private set; }

    private UnidadInventario() { }

    public static UnidadInventario Registrar(UnidadId id, ProductoId productoId, SucursalId sucursal, NumeroDeSerie? serie) =>
        new() { Id = id, ProductoId = productoId, SucursalActual = sucursal, Serie = serie, Estado = EstadoDeUnidad.Disponible };

    private static readonly EspecificacionDeUnidadDisponibleParaVenta Disponible = new();

    private void ValidarDisponibleEn(SucursalId sucursal)
    {
        if (!Disponible.EsSatisfechaPor(this).Cumple)
        {
            throw Estado == EstadoDeUnidad.Apartada
                ? new ReglaDeNegocioViolada(MotivoDeRechazo.UnidadApartada, "La unidad está reservada por un apartado vigente.")
                : new ReglaDeNegocioViolada(MotivoDeRechazo.UnidadNoDisponible, $"La unidad no está disponible (estado {Estado}).");
        }
        if (SucursalActual != sucursal)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.UnidadEnOtraSucursal, $"La unidad está en la sucursal {SucursalActual}.");
    }

    /// <summary>RN-APT-003: bloquea la unidad para el apartado.</summary>
    public void Apartar(Folio folioApartado, SucursalId sucursal)
    {
        ValidarDisponibleEn(sucursal);
        Estado = MaquinasDelDominio.Unidad.Transicionar(Estado, EstadoDeUnidad.Apartada);
        FolioDeApartadoVigente = folioApartado;
    }

    public void LiberarApartado(Folio folioApartado)
    {
        if (Estado != EstadoDeUnidad.Apartada || FolioDeApartadoVigente != folioApartado)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.TransicionDeEstadoInvalida, "La unidad no está apartada por ese folio.");
        Estado = MaquinasDelDominio.Unidad.Transicionar(Estado, EstadoDeUnidad.Disponible);
        FolioDeApartadoVigente = null;
    }

    /// <summary>US06 y G-20: una unidad apartada o no disponible no se puede vender.</summary>
    public void Vender(Folio folioVenta, SucursalId sucursal)
    {
        ValidarDisponibleEn(sucursal);
        Estado = MaquinasDelDominio.Unidad.Transicionar(Estado, EstadoDeUnidad.Vendida);
    }

    /// <summary>Valida sin modificar que el apartado propietario puede vender su unidad (RN-APT-003, RN-VTA-001).</summary>
    public void ValidarVentaDesdeApartado(Folio folioApartado, NumeroDeSerie? serieCapturada)
    {
        if (Estado != EstadoDeUnidad.Apartada || FolioDeApartadoVigente != folioApartado)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.TransicionDeEstadoInvalida, "La unidad no está apartada por ese folio.");
        if (Serie is null) return;
        if (serieCapturada is null)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieRequerida, "Debe capturarse el número de serie.");
        if (serieCapturada != Serie)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieNoCoincide, "La serie capturada no corresponde a la unidad apartada.");
    }

    public void VenderDesdeApartado(Folio folioApartado, NumeroDeSerie? serieCapturada)
    {
        ValidarVentaDesdeApartado(folioApartado, serieCapturada);
        Estado = MaquinasDelDominio.Unidad.Transicionar(Estado, EstadoDeUnidad.Vendida);
        FolioDeApartadoVigente = null;
    }

    /// <summary>RN-INV-004: vuelve a Disponible solo con empaque cerrado; en otro caso queda Defectuosa.</summary>
    public void RegistrarDevolucion(Folio folioDevolucion, CondicionDelProducto condicion)
    {
        var destino = condicion == CondicionDelProducto.EmpaqueCerrado ? EstadoDeUnidad.Disponible : EstadoDeUnidad.Defectuosa;
        Estado = MaquinasDelDominio.Unidad.Transicionar(Estado, destino);
    }
}
