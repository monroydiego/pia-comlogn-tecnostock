using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>CU-04 SolicitarDevolucion y CU-05 AutorizarDevolucionExcepcional (diagrama 05).</summary>
public sealed class ServicioDeDevoluciones(
    IRepositorioDeVentas ventas, IRepositorioDeDevoluciones devoluciones, IRepositorioDeProductos productos,
    IRepositorioDePoliticas politicas, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj, IGeneradorDeFolios folios,
    IContextoDeUsuario contexto, TraductorDeRechazos traductor) : ServicioDeAplicacion(unidadDeTrabajo, traductor)
{
    /// <summary>
    /// Comprobante inexistente = rechazo definitivo (RN-DEV-001). Si incumple plazo o condición no se rechaza:
    /// la devolución queda EnRevision y se deriva al Supervisor (ADR-006).
    /// </summary>
    public Resultado SolicitarDevolucion(SolicitarDevolucionCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var ahora = reloj.Ahora();
        var venta = ventas.ObtenerPorFolio(cmd.FolioDeVenta)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.ComprobanteNoValido, $"El comprobante {cmd.FolioDeVenta} no existe.");
        var producto = productos.Obtener(cmd.ProductoId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {cmd.ProductoId} no existe.");

        var linea = venta.ObtenerLinea(cmd.ProductoId, cmd.SerieCapturada);
        var dictamen = politicas.ObtenerPoliticaVigente().CriterioDeDevolucionVigente()
            .Evaluar(venta, linea, producto, cmd.CondicionDelProducto, ahora);

        var devolucion = Devolucion.Solicitar(folios.Siguiente(TipoDeOperacion.Devolucion), venta, cmd.ProductoId,
            cmd.SerieCapturada, cmd.Motivo, cmd.CondicionDelProducto, cmd.FormaDeCompensacion, dictamen,
            contexto.OperadorActual(), ahora);

        devoluciones.Agregar(devolucion);
        return Resultado.Exito(EstadoDeDevolucionDto.Desde(devolucion));
    });

    public Resultado AutorizarDevolucionExcepcional(AutorizarDevolucionExcepcionalCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var ahora = reloj.Ahora();
        var devolucion = devoluciones.Obtener(cmd.FolioDeDevolucion)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"La devolución {cmd.FolioDeDevolucion} no existe.");

        var decision = DecisionDeSupervisor.Registrar(contexto.OperadorActual(), cmd.Autoriza, cmd.Motivo, ahora);
        devolucion.Resolver(decision, ahora);

        devoluciones.Guardar(devolucion);
        return Resultado.Exito(EstadoDeDevolucionDto.Desde(devolucion));
    });
}
