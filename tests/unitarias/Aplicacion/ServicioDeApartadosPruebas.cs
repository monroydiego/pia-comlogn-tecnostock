using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

public class ServicioDeApartadosPruebas
{
    private static ApartarProductoCommand Apartar(Escenario e, decimal anticipo = 300m, MetodoDePago metodo = MetodoDePago.Efectivo) =>
        new(e.Laptop.Id, Escenario.Suc1, Escenario.Cliente, Escenario.Pago(anticipo, metodo));

    private static Escenario ConLaptop(string serie = "SN-1")
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, serie);
        return e;
    }

    // ---- CU-01 ----
    [Fact]
    public void US01_CU01_Creacion_de_un_apartado_reserva_la_unidad()
    {
        var e = ConLaptop();
        var dto = e.Apartados.ApartarProducto(Apartar(e)).DebeSerExitoso<ConfirmacionDeApartadoDto>();

        Assert.Equal(EstadoDeUnidad.Apartada, e.Almacen.Unidades[0].Estado);
        Assert.Single(e.Almacen.Apartados);
        Assert.Equal(1, e.Almacen.Confirmaciones);
        Assert.Equal(0, e.Almacen.Descartes);
        Assert.NotEmpty(dto.Folio);
    }

    [Fact]
    public void US03_CU01_Confirmacion_muestra_folio_fecha_limite_y_saldo_pendiente()
    {
        var e = ConLaptop();
        var dto = e.Apartados.ApartarProducto(Apartar(e, 400m)).DebeSerExitoso<ConfirmacionDeApartadoDto>();
        Assert.Equal("APT-000001", dto.Folio);
        Assert.Equal(e.Reloj.Actual.AddDays(7), dto.FechaLimite);
        Assert.Equal(600m, dto.SaldoPendiente);
    }

    [Fact]
    public void US01_CU01_Anticipo_insuficiente_rechaza_y_la_unidad_sigue_disponible()
    {
        var e = ConLaptop();
        e.Apartados.ApartarProducto(Apartar(e, 100m)).DebeRechazar(MotivoDeRechazo.AnticipoInsuficiente);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
        Assert.Empty(e.Almacen.Apartados);
        Assert.Equal(1, e.Almacen.Descartes);
    }

    [Fact]
    public void US01_CU01_Producto_no_apartable()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Teclado, null);
        var r = e.Apartados.ApartarProducto(new(e.Teclado.Id, Escenario.Suc1, Escenario.Cliente, Escenario.Pago(200m)));
        r.DebeRechazar(MotivoDeRechazo.ProductoNoApartable);
    }

    [Fact]
    public void US01_CU01_Sin_existencia_sugiere_consultar_otras_sucursales()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1", Escenario.Suc2);
        var rechazo = e.Apartados.ApartarProducto(Apartar(e)).DebeRechazar(MotivoDeRechazo.SinExistenciaDisponible);
        Assert.Equal(AlternativaSugerida.ConsultarOtrasSucursales, rechazo.Alternativa);
    }

    [Fact]
    public void US11_CU01_Metodo_de_pago_no_autorizado_no_crea_el_apartado()
    {
        var e = ConLaptop();
        e.Almacen.Politica = TecnoStock.Dominio.Agregados.PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo],
            e.Almacen.Politica.PlazoDeDevolucionEnDiasPorCategoria);
        e.Apartados.ApartarProducto(Apartar(e, 300m, MetodoDePago.Tarjeta)).DebeRechazar(MotivoDeRechazo.MetodoDePagoNoAutorizado);
        Assert.Empty(e.Almacen.Apartados);
    }

    [Fact]
    public void US11_CU01_El_apartado_se_crea_solo_despues_de_aceptar_el_pago()
    {
        var e = ConLaptop();
        e.Apartados.ApartarProducto(Apartar(e, 300m, MetodoDePago.Transferencia)).DebeSerExitoso<ConfirmacionDeApartadoDto>();
        Assert.Equal(MetodoDePago.Transferencia, e.Almacen.Apartados.Values.Single().Anticipo.Metodo);
    }

    [Fact]
    public void CU01_Producto_inexistente_responde_NoEncontrado()
    {
        var e = new Escenario();
        e.Apartados.ApartarProducto(new(new ProductoId("NO-EXISTE"), Escenario.Suc1, Escenario.Cliente, Escenario.Pago(1m)))
            .DebeRechazar(MotivoDeRechazo.NoEncontrado);
    }

    [Fact]
    public void US12_Apartado_creado_emite_comprobante_con_folio_usuario_y_fecha()
    {
        var e = ConLaptop();
        e.Apartados.ApartarProducto(Apartar(e)).DebeSerExitoso<ConfirmacionDeApartadoDto>();
        var c = Assert.Single(e.Almacen.Comprobantes);
        Assert.Equal("CMP-000001", c.FolioDeComprobante.Valor);
        Assert.Equal("cajero1", c.Usuario);
        Assert.Equal(e.Reloj.Actual, c.Fecha);
    }

    [Fact]
    public void US12_Operacion_rechazada_no_emite_comprobante()
    {
        var e = ConLaptop();
        e.Apartados.ApartarProducto(Apartar(e, 1m)).DebeRechazar(MotivoDeRechazo.AnticipoInsuficiente);
        Assert.Empty(e.Almacen.Comprobantes);
        Assert.Empty(e.Publicador.Publicados);
    }

    // ---- CU-03 ----
    private static (Escenario, string) ConApartado(string serie = "SN-1")
    {
        var e = ConLaptop(serie);
        var folio = e.Apartados.ApartarProducto(Apartar(e)).DebeSerExitoso<ConfirmacionDeApartadoDto>().Folio;
        e.Almacen.Comprobantes.Clear();
        return (e, folio);
    }

    private static ConvertirApartadoEnVentaCommand Convertir(string folio, decimal saldo = 700m, string? serie = "SN-1",
        MetodoDePago metodo = MetodoDePago.Tarjeta) =>
        new(new Folio(folio), Escenario.Pago(saldo, metodo), serie is null ? null : new NumeroDeSerie(serie));

    [Fact]
    public void US07_CU03_Conversion_de_apartado_a_venta_la_unidad_pasa_a_Vendida()
    {
        var (e, folio) = ConApartado();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(3);
        var venta = e.Apartados.ConvertirApartadoEnVenta(Convertir(folio)).DebeSerExitoso<VentaRegistradaDto>();

        Assert.Equal(1000m, venta.Total);
        Assert.Equal(folio, venta.FolioDeApartadoOrigen);
        Assert.Equal(EstadoDeUnidad.Vendida, e.Almacen.Unidades[0].Estado);
        Assert.Equal(EstadoDeApartado.Convertido, e.Almacen.Apartados[folio].Estado);
        Assert.Single(e.Almacen.Ventas);
    }

    [Fact]
    public void US09_CU03_El_comprobante_de_la_conversion_incluye_la_serie()
    {
        var (e, folio) = ConApartado("SN-777");
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio, serie: "sn-777")).DebeSerExitoso<VentaRegistradaDto>();
        var c = Assert.Single(e.Almacen.Comprobantes);
        Assert.Equal(TecnoStock.Aplicacion.Puertos.TipoDeOperacion.Venta, c.Tipo);
        Assert.Contains(c.Detalle, d => d.Contains("SN-777"));
    }

    [Fact]
    public void US07_CU03_Apartado_vencido_se_persiste_libera_la_unidad_y_rechaza()
    {
        var (e, folio) = ConApartado();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(8);
        var rechazo = e.Apartados.ConvertirApartadoEnVenta(Convertir(folio)).DebeRechazar(MotivoDeRechazo.ApartadoVencido);

        Assert.Equal(AlternativaSugerida.IniciarVentaNueva, rechazo.Alternativa);
        Assert.Equal(EstadoDeApartado.Vencido, e.Almacen.Apartados[folio].Estado);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
        Assert.Empty(e.Almacen.Ventas);
        Assert.Equal(2, e.Almacen.Confirmaciones); // apartado + vencimiento con efecto
        Assert.Contains(e.Publicador.Publicados, x => x is ApartadoVencido);
        Assert.Contains(e.Almacen.Avisos, a => a.Destinatario == "Vendedor");
    }

    [Fact]
    public void US07_CU03_Saldo_insuficiente_el_apartado_sigue_vigente()
    {
        var (e, folio) = ConApartado();
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio, 100m)).DebeRechazar(MotivoDeRechazo.SaldoInsuficiente);
        Assert.Equal(EstadoDeApartado.Vigente, e.Almacen.Apartados[folio].Estado);
        Assert.Equal(EstadoDeUnidad.Apartada, e.Almacen.Unidades[0].Estado);
        Assert.Empty(e.Almacen.Ventas);
    }

    [Fact]
    public void US07_CU03_Metodo_de_pago_no_autorizado_en_el_saldo()
    {
        var (e, folio) = ConApartado();
        e.Almacen.Politica = TecnoStock.Dominio.Agregados.PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo],
            e.Almacen.Politica.PlazoDeDevolucionEnDiasPorCategoria);
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio, metodo: MetodoDePago.Tarjeta)).DebeRechazar(MotivoDeRechazo.MetodoDePagoNoAutorizado);
    }

    [Fact]
    public void US09_CU03_Serie_que_no_corresponde_o_faltante()
    {
        var (e, folio) = ConApartado();
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio, serie: "OTRA")).DebeRechazar(MotivoDeRechazo.SerieNoCoincide);
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio, serie: null)).DebeRechazar(MotivoDeRechazo.SerieRequerida);
        Assert.Equal(EstadoDeApartado.Vigente, e.Almacen.Apartados[folio].Estado);
    }

    [Fact]
    public void CU03_Apartado_inexistente_o_ya_convertido()
    {
        var (e, folio) = ConApartado();
        e.Apartados.ConvertirApartadoEnVenta(Convertir("APT-999999")).DebeRechazar(MotivoDeRechazo.NoEncontrado);
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio)).DebeSerExitoso<VentaRegistradaDto>();
        e.Apartados.ConvertirApartadoEnVenta(Convertir(folio)).DebeRechazar(MotivoDeRechazo.TransicionDeEstadoInvalida);
    }

    // ---- CU-06 ----
    [Fact]
    public void US14_CU06_Cancelacion_manual_por_Supervisor_libera_la_unidad_y_registra_el_motivo()
    {
        var (e, folio) = ConApartado();
        e.ComoRol(Rol.Supervisor);
        var dto = e.Apartados.CancelarApartado(new(new Folio(folio), "El cliente lo solicitó")).DebeSerExitoso<CancelacionDeApartadoDto>();
        Assert.Equal("Cancelado", dto.Estado);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
        Assert.Equal("El cliente lo solicitó", e.Almacen.Apartados[folio].Cancelacion!.Motivo);
    }

    [Fact]
    public void US14_CU06_Operador_sin_rol_de_Supervisor_no_cancela()
    {
        var (e, folio) = ConApartado();
        e.ComoRol(Rol.Vendedor);
        e.Apartados.CancelarApartado(new(new Folio(folio), "x")).DebeRechazar(MotivoDeRechazo.SoloSupervisorAutoriza);
        Assert.Equal(EstadoDeApartado.Vigente, e.Almacen.Apartados[folio].Estado);
    }

    [Fact]
    public void US14_CU06_Apartado_que_no_esta_vigente()
    {
        var (e, folio) = ConApartado();
        e.ComoRol(Rol.Supervisor);
        e.Apartados.CancelarApartado(new(new Folio(folio), "x")).DebeSerExitoso<CancelacionDeApartadoDto>();
        e.Apartados.CancelarApartado(new(new Folio(folio), "otra vez")).DebeRechazar(MotivoDeRechazo.TransicionDeEstadoInvalida);
        e.Apartados.CancelarApartado(new(new Folio("APT-999999"), "x")).DebeRechazar(MotivoDeRechazo.NoEncontrado);
    }

    // ---- CU-08 ----
    [Fact]
    public void US20_CU08_Vence_los_apartados_cuya_fecha_limite_paso_y_libera_la_unidad()
    {
        var (e, folio) = ConApartado();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(7);
        var resumen = e.Apartados.VencerApartadosVencidos().DebeSerExitoso<ResumenDeVencimientoDto>();

        Assert.Equal(1, resumen.ApartadosVencidos);
        Assert.Equal(EstadoDeApartado.Vencido, e.Almacen.Apartados[folio].Estado);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
        Assert.Contains(e.Publicador.Publicados, x => x is ApartadoVencido);
        Assert.Single(e.Almacen.Avisos);
    }

    [Fact]
    public void US20_CU08_Apartado_vigente_no_se_modifica()
    {
        var (e, folio) = ConApartado();
        e.Reloj.Actual = e.Reloj.Actual.AddDays(3);
        Assert.Equal(0, e.Apartados.VencerApartadosVencidos().DebeSerExitoso<ResumenDeVencimientoDto>().ApartadosVencidos);
        Assert.Equal(EstadoDeApartado.Vigente, e.Almacen.Apartados[folio].Estado);
        Assert.Equal(EstadoDeUnidad.Apartada, e.Almacen.Unidades[0].Estado);
    }

    [Fact]
    public void Una_excepcion_inesperada_descarta_la_transaccion_y_se_propaga()
    {
        var e = ConLaptop();
        var cmd = new ApartarProductoCommand(e.Laptop.Id, Escenario.Suc1, Escenario.Cliente, null!);
        Assert.ThrowsAny<Exception>(() => e.Apartados.ApartarProducto(cmd));
        Assert.Equal(1, e.Almacen.Descartes);
        Assert.Equal(0, e.Almacen.Confirmaciones);
    }
}
