using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Servicios;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

/// <summary>US06, US07, US09 y RN-VTA-001/002.</summary>
public class VentaYConversionPruebas
{
    private static readonly ConversionDeApartado Conversion = new();

    private static MotivoDeRechazo Motivo(Action a) => Assert.Throws<ReglaDeNegocioViolada>(a).Motivo;

    // ----- Venta directa (CU-02) -----
    [Fact]
    public void US09_Venta_de_producto_serializable_cierra_y_conserva_la_serie()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop, "SN-1");
        var linea = LineaDeVenta.Crear(laptop, unidad, new NumeroDeSerie("sn-1"), laptop.PrecioDeLista);
        var venta = Venta.Cerrar(new Folio("VTA-1"), Datos.Suc1, Datos.Cajero, [linea], [Datos.Pago(1000m)], Datos.Politica(), Datos.Ahora);

        Assert.Equal(new NumeroDeSerie("SN-1"), venta.Lineas[0].Serie);
        Assert.Equal(1000m, venta.Total().Monto);
        var evento = Assert.IsType<VentaCerrada>(Assert.Single(venta.ExtraerEventos()));
        Assert.Equal(1000m, evento.Total.Monto);
    }

    [Fact]
    public void US09_Serie_no_registrada_bloquea_el_cierre_con_SerieRequerida()
    {
        var laptop = Datos.Laptop();
        Assert.Equal(MotivoDeRechazo.SerieRequerida,
            Motivo(() => LineaDeVenta.Crear(laptop, Datos.Unidad(laptop), null, laptop.PrecioDeLista)));
    }

    [Fact]
    public void US09_Serie_que_no_corresponde_a_la_unidad_se_rechaza_con_SerieNoCoincide()
    {
        var laptop = Datos.Laptop();
        Assert.Equal(MotivoDeRechazo.SerieNoCoincide,
            Motivo(() => LineaDeVenta.Crear(laptop, Datos.Unidad(laptop, "SN-1"), new NumeroDeSerie("SN-2"), laptop.PrecioDeLista)));
    }

    [Fact]
    public void Producto_sin_serie_no_exige_serie_en_la_linea()
    {
        var teclado = Datos.Teclado();
        var linea = LineaDeVenta.Crear(teclado, Datos.Unidad(teclado, null), null, teclado.PrecioDeLista);
        Assert.Null(linea.Serie);
    }

    [Fact]
    public void RN_VTA_002_Metodo_de_pago_no_autorizado_en_la_venta()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var politica = PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo], Datos.Politica().PlazoDeDevolucionEnDiasPorCategoria);
        var linea = LineaDeVenta.Crear(laptop, unidad, unidad.Serie, laptop.PrecioDeLista);
        Assert.Equal(MotivoDeRechazo.MetodoDePagoNoAutorizado,
            Motivo(() => Venta.Cerrar(new Folio("V"), Datos.Suc1, Datos.Cajero, [linea], [Datos.Pago(1000m, MetodoDePago.Transferencia)], politica, Datos.Ahora)));
    }

    [Fact]
    public void Cobro_multimedio_suma_varios_pagos_y_pagos_insuficientes_se_rechazan()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var linea = LineaDeVenta.Crear(laptop, unidad, unidad.Serie, laptop.PrecioDeLista);
        var venta = Venta.Cerrar(new Folio("V"), Datos.Suc1, Datos.Cajero, [linea],
            [Datos.Pago(600m, MetodoDePago.Tarjeta), Datos.Pago(400m)], Datos.Politica(), Datos.Ahora);
        Assert.Equal(2, venta.Pagos.Count);

        Assert.Equal(MotivoDeRechazo.SaldoInsuficiente,
            Motivo(() => Venta.Cerrar(new Folio("V2"), Datos.Suc1, Datos.Cajero, [linea], [Datos.Pago(999m)], Datos.Politica(), Datos.Ahora)));
    }

    [Fact]
    public void Venta_sin_lineas_o_sin_pagos_es_dato_invalido()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var linea = LineaDeVenta.Crear(laptop, unidad, unidad.Serie, laptop.PrecioDeLista);
        Assert.Throws<ArgumentException>(() => Venta.Cerrar(new Folio("V"), Datos.Suc1, Datos.Cajero, [], [Datos.Pago(1m)], Datos.Politica(), Datos.Ahora));
        Assert.Throws<ArgumentException>(() => Venta.Cerrar(new Folio("V"), Datos.Suc1, Datos.Cajero, [linea], [], Datos.Politica(), Datos.Ahora));
    }

    [Fact]
    public void ObtenerLinea_de_otro_producto_o_serie_lanza_LineaNoPerteneceALaVenta()
    {
        var laptop = Datos.Laptop();
        var venta = Datos.Venta(laptop, Datos.Unidad(laptop, "SN-1"));
        Assert.Same(venta.Lineas[0], venta.ObtenerLinea(laptop.Id, new NumeroDeSerie("SN-1")));
        Assert.Equal(MotivoDeRechazo.LineaNoPerteneceALaVenta, Motivo(() => venta.ObtenerLinea(laptop.Id, new NumeroDeSerie("SN-9"))));
        Assert.Equal(MotivoDeRechazo.LineaNoPerteneceALaVenta, Motivo(() => venta.ObtenerLinea(new ProductoId("OTRO"), new NumeroDeSerie("SN-1"))));
    }

    // ----- Conversión de apartado (CU-03, diagrama 04) -----
    private static (Producto, UnidadInventario, Apartado) Escenario()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop, "SN-1");
        return (laptop, unidad, Datos.Apartado(laptop, unidad, 1000m, 300m));
    }

    [Fact]
    public void US07_Conversion_de_apartado_a_venta_aplica_el_anticipo_y_la_unidad_pasa_a_Vendida()
    {
        var (laptop, unidad, apartado) = Escenario();
        var r = Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m, MetodoDePago.Tarjeta), new NumeroDeSerie("SN-1"),
            new Folio("VTA-9"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(2));

        Assert.True(r.EsExitosa());
        var venta = r.Venta!;
        Assert.Equal(apartado.Folio, venta.FolioDeApartadoOrigen);
        Assert.Equal(2, venta.Pagos.Count);
        Assert.Equal(1000m, venta.Total().Monto);
        Assert.Equal(EstadoDeApartado.Convertido, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Vendida, unidad.Estado);
        Assert.Contains(venta.ExtraerEventos(), e => e is VentaCerrada);
        Assert.Equal(0m, apartado.SaldoPendiente().Monto);
    }

    [Fact]
    public void US07_Apartado_vencido_se_marca_Vencido_libera_la_unidad_y_rechaza_con_ApartadoVencido()
    {
        var (laptop, unidad, apartado) = Escenario();
        var r = Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m), new NumeroDeSerie("SN-1"),
            new Folio("VTA-9"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(8));

        Assert.False(r.EsExitosa());
        Assert.Equal(MotivoDeRechazo.ApartadoVencido, r.Motivo);
        Assert.Equal(EstadoDeApartado.Vencido, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
        Assert.Contains(apartado.ExtraerEventos(), e => e is ApartadoVencido);
    }

    [Fact]
    public void US07_Saldo_insuficiente_rechaza_y_el_apartado_sigue_vigente_sin_cambios()
    {
        var (laptop, unidad, apartado) = Escenario();
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(699m),
            new NumeroDeSerie("SN-1"), new Folio("V"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(1)));
        Assert.Equal(MotivoDeRechazo.SaldoInsuficiente, ex.Motivo);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Apartada, unidad.Estado);
    }

    [Fact]
    public void US07_Metodo_de_pago_no_autorizado_en_el_saldo()
    {
        var (laptop, unidad, apartado) = Escenario();
        var politica = PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo], Datos.Politica().PlazoDeDevolucionEnDiasPorCategoria);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m, MetodoDePago.Tarjeta),
            new NumeroDeSerie("SN-1"), new Folio("V"), Datos.Cajero, politica, Datos.Ahora.AddDays(1)));
        Assert.Equal(MotivoDeRechazo.MetodoDePagoNoAutorizado, ex.Motivo);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
    }

    [Fact]
    public void US09_Conversion_con_serie_equivocada_no_deja_cambios_parciales()
    {
        var (laptop, unidad, apartado) = Escenario();
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m),
            new NumeroDeSerie("SN-99"), new Folio("V"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(1)));
        Assert.Equal(MotivoDeRechazo.SerieNoCoincide, ex.Motivo);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Apartada, unidad.Estado);
    }

    [Fact]
    public void Convertir_un_apartado_ya_convertido_es_transicion_invalida()
    {
        var (laptop, unidad, apartado) = Escenario();
        Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m), new NumeroDeSerie("SN-1"),
            new Folio("V1"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(1));
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => Conversion.Convertir(apartado, unidad, laptop, Datos.Pago(700m),
            new NumeroDeSerie("SN-1"), new Folio("V2"), Datos.Cajero, Datos.Politica(), Datos.Ahora.AddDays(1)));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
    }
}
