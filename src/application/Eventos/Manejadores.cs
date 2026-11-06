using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.Repositorios;

namespace TecnoStock.Aplicacion.Eventos;

/// <summary>US12: emite un comprobante con folio único cuando la operación se confirma.</summary>
public sealed class ManejadorDeComprobantes(
    IEmisorDeComprobantes emisor, IGeneradorDeFolios folios, IContextoDeUsuario contexto, IRepositorioDeVentas ventas)
{
    public void AlCrearseApartado(ApartadoCreado e) => Emitir(TipoDeOperacion.Apartado, e.Folio, e.OcurridoEn,
        [$"Apartado {e.Folio}", $"Fecha límite: {e.FechaLimite:yyyy-MM-dd}"]);

    public void AlCerrarseVenta(VentaCerrada e)
    {
        var detalle = new List<string> { $"Venta {e.Folio}", $"Total: {e.Total}" };
        var venta = ventas.ObtenerPorFolio(e.Folio);
        if (venta is not null)
            detalle.AddRange(venta.Lineas.Select(l => $"{l.ProductoId} serie {l.Serie?.Valor ?? "(sin serie)"} {l.PrecioUnitario}"));
        Emitir(TipoDeOperacion.Venta, e.Folio, e.OcurridoEn, detalle);
    }

    public void AlAceptarseDevolucion(DevolucionAceptada e) => Emitir(TipoDeOperacion.Devolucion, e.Folio, e.OcurridoEn,
        [$"Devolución {e.Folio} de la venta {e.FolioDeVenta}", $"Condición: {e.Condicion}"]);

    private void Emitir(TipoDeOperacion tipo, Dominio.ObjetosDeValor.Folio folioDeOperacion, DateTime fecha, IReadOnlyList<string> detalle) =>
        emisor.Emitir(new Comprobante(folios.Siguiente(TipoDeOperacion.Comprobante), tipo, folioDeOperacion, fecha,
            contexto.OperadorActual().UsuarioId, detalle));
}

/// <summary>RN-INV-004: reingresa a Disponible o marca Defectuosa la unidad de una devolución aceptada, en transacción propia.</summary>
public sealed class ManejadorDeReingresoDeUnidades(IRepositorioDeUnidades unidades, IUnidadDeTrabajo unidadDeTrabajo)
{
    public void AlAceptarseDevolucion(DevolucionAceptada e)
    {
        unidadDeTrabajo.Iniciar();
        try
        {
            var unidad = unidades.Obtener(e.UnidadId)
                ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "La unidad devuelta no existe.");
            unidad.RegistrarDevolucion(e.Folio, e.Condicion);
            unidades.Guardar(unidad);
            unidadDeTrabajo.Confirmar();
        }
        catch
        {
            unidadDeTrabajo.Descartar();
            throw;
        }
    }
}

/// <summary>Avisos por canal por validar (US13, US20).</summary>
public sealed class ManejadorDeAvisos(INotificador notificador)
{
    public void AlDerivarseDevolucion(DevolucionDerivadaASupervisor e) =>
        notificador.Avisar("Supervisor", $"La devolución {e.Folio} está en revisión: {string.Join("; ", e.Incumplimientos)}");

    public void AlVencerseApartado(ApartadoVencido e) =>
        notificador.Avisar("Vendedor", $"El apartado {e.Folio} venció y su unidad volvió a estar disponible.");
}
