using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

public class ServicioDeDevolucionesPruebas
{
    private static (Escenario, string folioVenta) ConVenta()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1");
        var venta = e.Ventas.RegistrarVenta(new(Escenario.Suc1, [new LineaSolicitada(e.Laptop.Id, new NumeroDeSerie("SN-1"))],
            [Escenario.Pago(1000m)])).DebeSerExitoso<VentaRegistradaDto>();
        e.Almacen.Comprobantes.Clear();
        return (e, venta.Folio);
    }

    private static SolicitarDevolucionCommand Solicitar(Escenario e, string folioVenta, CondicionDelProducto condicion = CondicionDelProducto.EmpaqueCerrado,
        FormaDeCompensacion forma = FormaDeCompensacion.Reembolso, string serie = "SN-1") =>
        new(new Folio(folioVenta), e.Laptop.Id, new NumeroDeSerie(serie), new MotivoDeDevolucion("Falla", "No enciende"), condicion, forma);

    // ---- CU-04 ----
    [Fact]
    public void US10_CU04_Devolucion_estandar_se_acepta_y_la_unidad_regresa_a_Disponible()
    {
        var (e, venta) = ConVenta();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(5);
        var dto = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta)).DebeSerExitoso<EstadoDeDevolucionDto>();

        Assert.Equal("Aceptada", dto.Estado);
        Assert.Equal("Reembolso", dto.FormaDeCompensacion);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado); // RN-INV-004 por el manejador
        Assert.Contains(e.Almacen.Comprobantes, c => c.Tipo == TecnoStock.Aplicacion.Puertos.TipoDeOperacion.Devolucion);
    }

    [Fact]
    public void RN_INV_004_CU04_Devolucion_defectuosa_aceptada_deja_la_unidad_Defectuosa()
    {
        var (e, venta) = ConVenta();
        e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta, CondicionDelProducto.Defectuoso)).DebeSerExitoso<EstadoDeDevolucionDto>();
        Assert.Equal(EstadoDeUnidad.Defectuosa, e.Almacen.Unidades[0].Estado);
    }

    [Fact]
    public void US10_CU04_Fuera_de_plazo_queda_EnRevision_y_se_avisa_al_Supervisor()
    {
        var (e, venta) = ConVenta();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(16);
        var dto = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta)).DebeSerExitoso<EstadoDeDevolucionDto>();

        Assert.Equal("EnRevision", dto.Estado);
        Assert.Contains(e.Publicador.Publicados, x => x is DevolucionDerivadaASupervisor);
        Assert.Equal("Supervisor", Assert.Single(e.Almacen.Avisos).Destinatario);
        Assert.Equal(EstadoDeUnidad.Vendida, e.Almacen.Unidades[0].Estado);
        Assert.Empty(e.Almacen.Comprobantes);
    }

    [Fact]
    public void US10_CU04_Empaque_abierto_queda_EnRevision()
    {
        var (e, venta) = ConVenta();
        var dto = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta, CondicionDelProducto.EmpaqueAbierto))
            .DebeSerExitoso<EstadoDeDevolucionDto>();
        Assert.Equal("EnRevision", dto.Estado);
        Assert.Single(e.Almacen.Devoluciones);
    }

    [Fact]
    public void US10_CU04_Comprobante_no_valido_rechaza_de_forma_definitiva_y_no_crea_la_devolucion()
    {
        var (e, _) = ConVenta();
        e.Devoluciones.SolicitarDevolucion(Solicitar(e, "VTA-999999")).DebeRechazar(MotivoDeRechazo.ComprobanteNoValido);
        Assert.Empty(e.Almacen.Devoluciones);
    }

    [Fact]
    public void CU04_Linea_ajena_a_la_venta_y_producto_inexistente()
    {
        var (e, venta) = ConVenta();
        e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta, serie: "OTRA")).DebeRechazar(MotivoDeRechazo.LineaNoPerteneceALaVenta);
        var cmd = Solicitar(e, venta) with { ProductoId = new ProductoId("NO") };
        e.Devoluciones.SolicitarDevolucion(cmd).DebeRechazar(MotivoDeRechazo.NoEncontrado);
    }

    [Fact]
    public void US08_CU04_El_motivo_queda_asociado_al_folio_de_la_devolucion()
    {
        var (e, venta) = ConVenta();
        var dto = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta)).DebeSerExitoso<EstadoDeDevolucionDto>();
        var guardada = e.Almacen.Devoluciones[dto.Folio];
        Assert.Equal("Falla", guardada.Motivo.Causal);
        Assert.Equal("No enciende", guardada.Motivo.Detalle);
    }

    // ---- CU-05 ----
    private static (Escenario, string folioDevolucion) ConDevolucionEnRevision()
    {
        var (e, venta) = ConVenta();
        var dto = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta, CondicionDelProducto.EmpaqueAbierto))
            .DebeSerExitoso<EstadoDeDevolucionDto>();
        e.Almacen.Comprobantes.Clear();
        e.ComoRol(Rol.Supervisor);
        return (e, dto.Folio);
    }

    [Fact]
    public void US13_CU05_Autorizacion_acepta_registra_al_Supervisor_y_deja_la_unidad_Defectuosa()
    {
        var (e, folio) = ConDevolucionEnRevision();
        var dto = e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio(folio), true, "cliente frecuente"))
            .DebeSerExitoso<EstadoDeDevolucionDto>();

        Assert.Equal("Aceptada", dto.Estado);
        Assert.Equal("cliente frecuente", dto.Motivo);
        var d = e.Almacen.Devoluciones[folio];
        Assert.Equal("supervisor1", d.Decision!.Supervisor.UsuarioId);
        Assert.Equal(EstadoDeUnidad.Defectuosa, e.Almacen.Unidades[0].Estado); // empaque abierto: no vuelve a la venta
        Assert.Single(e.Almacen.Comprobantes);
    }

    [Fact]
    public void US13_CU05_Denegacion_marca_Rechazada_y_no_toca_la_unidad()
    {
        var (e, folio) = ConDevolucionEnRevision();
        var dto = e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio(folio), false, "producto usado"))
            .DebeSerExitoso<EstadoDeDevolucionDto>();
        Assert.Equal("Rechazada", dto.Estado);
        Assert.Equal(EstadoDeUnidad.Vendida, e.Almacen.Unidades[0].Estado);
        Assert.Contains(e.Publicador.Publicados, x => x is DevolucionRechazada);
    }

    [Fact]
    public void US13_CU05_Operador_sin_rol_de_Supervisor()
    {
        var (e, folio) = ConDevolucionEnRevision();
        e.ComoRol(Rol.Cajero);
        e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio(folio), true, "x")).DebeRechazar(MotivoDeRechazo.SoloSupervisorAutoriza);
        Assert.Equal(EstadoDeDevolucion.EnRevision, e.Almacen.Devoluciones[folio].Estado);
    }

    [Fact]
    public void US13_CU05_Devolucion_que_ya_fue_resuelta_o_inexistente()
    {
        var (e, folio) = ConDevolucionEnRevision();
        e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio(folio), false, "no")).DebeSerExitoso<EstadoDeDevolucionDto>();
        e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio(folio), true, "otra vez")).DebeRechazar(MotivoDeRechazo.TransicionDeEstadoInvalida);
        e.Devoluciones.AutorizarDevolucionExcepcional(new(new Folio("DEV-999999"), true, "x")).DebeRechazar(MotivoDeRechazo.NoEncontrado);
    }

    // ---- Consultas ----
    [Fact]
    public void US02_Consulta_del_estado_de_devolucion_con_motivo_y_compensacion()
    {
        var (e, venta) = ConVenta();
        var folio = e.Devoluciones.SolicitarDevolucion(Solicitar(e, venta, forma: FormaDeCompensacion.NotaDeCredito))
            .DebeSerExitoso<EstadoDeDevolucionDto>().Folio;
        var dto = e.ConsultasDevoluciones.ConsultarEstadoDeDevolucion(new(new Folio(folio)));
        Assert.Equal("Aceptada", dto.Estado);
        Assert.Equal("NotaDeCredito", dto.FormaDeCompensacion);
        Assert.Equal("Falla", dto.Motivo);
    }

    [Fact]
    public void US02_Folio_inexistente_responde_NoEncontrado()
    {
        var e = new Escenario();
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => e.ConsultasDevoluciones.ConsultarEstadoDeDevolucion(new(new Folio("DEV-1"))));
        Assert.Equal(MotivoDeRechazo.NoEncontrado, ex.Motivo);
    }

    [Fact]
    public void US16_Consulta_del_criterio_vigente_muestra_condiciones_y_plazos_por_categoria()
    {
        var e = new Escenario();
        var dto = e.ConsultasDevoluciones.ConsultarCriterioDeDevolucionVigente(new());
        Assert.Equal(3, dto.Condiciones.Count);
        Assert.Equal(15, dto.PlazoPorCategoria["Computadoras"]);
        Assert.Equal(30, dto.PlazoPorCategoria["Accesorios"]);
    }
}
