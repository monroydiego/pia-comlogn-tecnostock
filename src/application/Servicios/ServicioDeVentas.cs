using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>CU-02 RegistrarVenta (US06, US09, RN-VTA-001/002).</summary>
public sealed class ServicioDeVentas(
    IRepositorioDeVentas ventas, IRepositorioDeUnidades unidades, IRepositorioDeProductos productos,
    IRepositorioDePoliticas politicas, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj, IGeneradorDeFolios folios,
    IContextoDeUsuario contexto, TraductorDeRechazos traductor) : ServicioDeAplicacion(unidadDeTrabajo, traductor)
{
    public Resultado RegistrarVenta(RegistrarVentaCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var ahora = reloj.Ahora();
        var folio = folios.Siguiente(TipoDeOperacion.Venta);
        var lineas = new List<LineaDeVenta>();
        var unidadesDeLaVenta = new List<UnidadInventario>();

        foreach (var solicitada in cmd.Lineas)
        {
            var producto = productos.Obtener(solicitada.ProductoId)
                ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {solicitada.ProductoId} no existe.");
            var excluidas = unidadesDeLaVenta.Select(u => u.Id).ToList();
            var unidad = unidades.ObtenerUnidadParaVenta(solicitada.ProductoId, cmd.SucursalId, solicitada.Serie, excluidas)
                ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.SinExistenciaDisponible,
                    $"No hay unidades disponibles de {producto.Nombre} en la sucursal {cmd.SucursalId}.");

            lineas.Add(LineaDeVenta.Crear(producto, unidad, solicitada.Serie, producto.PrecioDeLista));
            unidadesDeLaVenta.Add(unidad);
        }

        var venta = Venta.Cerrar(folio, cmd.SucursalId, contexto.OperadorActual(), lineas, cmd.Pagos,
            politicas.ObtenerPoliticaVigente(), ahora);

        foreach (var unidad in unidadesDeLaVenta)
        {
            unidad.Vender(folio, cmd.SucursalId);
            unidades.Guardar(unidad);
        }
        ventas.Agregar(venta);
        return Resultado.Exito(VentaRegistradaDto.Desde(venta));
    });
}
