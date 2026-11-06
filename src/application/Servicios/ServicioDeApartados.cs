using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;
using TecnoStock.Dominio.Servicios;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>CU-01, CU-03, CU-06 y CU-08.</summary>
public sealed class ServicioDeApartados(
    IRepositorioDeApartados apartados, IRepositorioDeUnidades unidades, IRepositorioDeProductos productos,
    IRepositorioDePoliticas politicas, IRepositorioDeVentas ventas, ConversionDeApartado conversion,
    IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj, IGeneradorDeFolios folios, IContextoDeUsuario contexto,
    TraductorDeRechazos traductor) : ServicioDeAplicacion(unidadDeTrabajo, traductor)
{
    /// <summary>CU-01 ApartarProducto (US01, US03, US11).</summary>
    public Resultado ApartarProducto(ApartarProductoCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var producto = productos.Obtener(cmd.ProductoId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {cmd.ProductoId} no existe.");

        var apartable = new EspecificacionDeProductoApartable().EsSatisfechaPor(producto);
        if (!apartable.Cumple)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.ProductoNoApartable, apartable.Razon);

        var unidad = unidades.ObtenerUnidadDisponible(cmd.ProductoId, cmd.SucursalId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.SinExistenciaDisponible,
                $"No hay unidades disponibles de {producto.Nombre} en la sucursal {cmd.SucursalId}.");

        var apartado = Apartado.Crear(folios.Siguiente(TipoDeOperacion.Apartado), cmd.ClienteId, unidad, producto,
            producto.PrecioDeLista, cmd.Anticipo, politicas.ObtenerPoliticaVigente(), reloj.Ahora());

        apartados.Agregar(apartado);
        unidades.Guardar(unidad);
        return Resultado.Exito(new ConfirmacionDeApartadoDto(apartado.Folio.Valor, apartado.Vigencia.Hasta, apartado.SaldoPendiente().Monto));
    });

    /// <summary>CU-03 ConvertirApartadoEnVenta (US07, US09). El Vendedor la inicia y el Cajero cobra el saldo (diagrama 04).</summary>
    public Resultado ConvertirApartadoEnVenta(ConvertirApartadoEnVentaCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var cajero = contexto.OperadorActual();
        var ahora = reloj.Ahora();
        var apartado = ObtenerApartado(cmd.FolioDeApartado);
        var unidad = unidades.Obtener(apartado.UnidadId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "La unidad del apartado no existe.");
        var producto = productos.Obtener(unidad.ProductoId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "El producto de la unidad no existe.");

        var resultado = conversion.Convertir(apartado, unidad, producto, cmd.PagoDelSaldo, cmd.SerieCapturada,
            folios.Siguiente(TipoDeOperacion.Venta), cajero, politicas.ObtenerPoliticaVigente(), ahora);

        apartados.Guardar(apartado);
        unidades.Guardar(unidad);

        if (!resultado.EsExitosa())
        {
            // Rechazo con efecto (ADR-006): el vencimiento se persiste aunque la venta se rechace.
            return Rechazo(resultado.Motivo!.Value, "El apartado ya venció; la unidad se liberó. Inicie una venta nueva.");
        }

        ventas.Agregar(resultado.Venta!);
        return Resultado.Exito(VentaRegistradaDto.Desde(resultado.Venta!));
    });

    /// <summary>CU-06 CancelarApartado (US14). Solo el Supervisor autoriza (RN-SUP-001).</summary>
    public Resultado CancelarApartado(CancelarApartadoCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var ahora = reloj.Ahora();
        var apartado = ObtenerApartado(cmd.FolioDeApartado);
        var decision = DecisionDeSupervisor.Registrar(contexto.OperadorActual(), true, cmd.Motivo, ahora);
        apartado.Cancelar(decision);

        var unidad = unidades.Obtener(apartado.UnidadId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "La unidad del apartado no existe.");
        unidad.LiberarApartado(apartado.Folio);

        apartados.Guardar(apartado);
        unidades.Guardar(unidad);
        return Resultado.Exito(new CancelacionDeApartadoDto(apartado.Folio.Valor, apartado.Estado.ToString()));
    });

    /// <summary>CU-08 VencerApartadosVencidos (US20), ejecutado por el actor Sistema.</summary>
    public Resultado VencerApartadosVencidos() => EjecutarEnUnidadDeTrabajo(() =>
    {
        var ahora = reloj.Ahora();
        var vencidos = apartados.ObtenerVigentesVencidos(ahora);
        foreach (var apartado in vencidos)
        {
            var unidad = unidades.Obtener(apartado.UnidadId)
                ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "La unidad del apartado no existe.");
            apartado.Vencer(ahora);
            unidad.LiberarApartado(apartado.Folio);
            apartados.Guardar(apartado);
            unidades.Guardar(unidad);
        }
        return Resultado.Exito(new ResumenDeVencimientoDto(vencidos.Count));
    });

    private Apartado ObtenerApartado(Folio folio) => apartados.Obtener(folio)
        ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El apartado {folio} no existe.");
}
