using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

public class ServicioDeVentasEInventarioPruebas
{
    private static RegistrarVentaCommand Venta(Escenario e, string? serie, decimal pago = 1000m, MetodoDePago metodo = MetodoDePago.Efectivo) =>
        new(Escenario.Suc1, [new LineaSolicitada(e.Laptop.Id, serie is null ? null : new NumeroDeSerie(serie))], [Escenario.Pago(pago, metodo)]);

    // ---- CU-02 ----
    [Fact]
    public void US09_CU02_Venta_de_producto_serializable_cierra_y_el_comprobante_incluye_la_serie()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1");
        var dto = e.Ventas.RegistrarVenta(Venta(e, "SN-1")).DebeSerExitoso<VentaRegistradaDto>();

        Assert.Equal("SN-1", dto.Lineas[0].Serie);
        Assert.Equal(EstadoDeUnidad.Vendida, e.Almacen.Unidades[0].Estado);
        Assert.Contains(Assert.Single(e.Almacen.Comprobantes).Detalle, d => d.Contains("SN-1"));
    }

    [Fact]
    public void US09_CU02_Serie_no_registrada_bloquea_el_cierre()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1");
        var rechazo = e.Ventas.RegistrarVenta(Venta(e, null)).DebeRechazar(MotivoDeRechazo.SerieRequerida);
        Assert.Equal(AlternativaSugerida.CapturarSerie, rechazo.Alternativa);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
    }

    [Fact]
    public void US09_CU02_Serie_que_no_existe_en_inventario_no_encuentra_unidad()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1");
        e.Ventas.RegistrarVenta(Venta(e, "SN-2")).DebeRechazar(MotivoDeRechazo.SinExistenciaDisponible);
    }

    [Fact]
    public void US06_CU02_Venta_de_una_unidad_apartada_se_bloquea_con_UnidadApartada()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-1");
        e.Apartados.ApartarProducto(new(e.Laptop.Id, Escenario.Suc1, Escenario.Cliente, Escenario.Pago(300m)))
            .DebeSerExitoso<ConfirmacionDeApartadoDto>();

        var rechazo = e.Ventas.RegistrarVenta(Venta(e, "SN-1")).DebeRechazar(MotivoDeRechazo.UnidadApartada);
        Assert.Equal(AlternativaSugerida.ElegirOtraUnidad, rechazo.Alternativa);
        Assert.Equal(EstadoDeUnidad.Apartada, e.Almacen.Unidades[0].Estado);
        Assert.Empty(e.Almacen.Ventas);
    }

    [Fact]
    public void US06_CU02_Sin_serie_para_producto_sin_serie_elige_una_unidad_disponible_distinta_por_linea()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Teclado, null);
        e.AgregarUnidad(e.Teclado, null);
        var cmd = new RegistrarVentaCommand(Escenario.Suc1,
            [new LineaSolicitada(e.Teclado.Id, null), new LineaSolicitada(e.Teclado.Id, null)], [Escenario.Pago(1000m)]);
        var dto = e.Ventas.RegistrarVenta(cmd).DebeSerExitoso<VentaRegistradaDto>();
        Assert.Equal(1000m, dto.Total);
        Assert.All(e.Almacen.Unidades, u => Assert.Equal(EstadoDeUnidad.Vendida, u.Estado));
    }

    [Fact]
    public void CU02_Sin_existencia_pagos_insuficientes_y_metodo_no_autorizado()
    {
        var e = new Escenario();
        e.Ventas.RegistrarVenta(Venta(e, "SN-1")).DebeRechazar(MotivoDeRechazo.SinExistenciaDisponible);
        e.AgregarUnidad(e.Laptop, "SN-1");
        e.Ventas.RegistrarVenta(Venta(e, "SN-1", 500m)).DebeRechazar(MotivoDeRechazo.SaldoInsuficiente);
        e.Almacen.Politica = TecnoStock.Dominio.Agregados.PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo],
            e.Almacen.Politica.PlazoDeDevolucionEnDiasPorCategoria);
        e.Ventas.RegistrarVenta(Venta(e, "SN-1", 1000m, MetodoDePago.Tarjeta)).DebeRechazar(MotivoDeRechazo.MetodoDePagoNoAutorizado);
        e.Ventas.RegistrarVenta(new(Escenario.Suc1, [new LineaSolicitada(new ProductoId("NO"), null)], [Escenario.Pago(1m)]))
            .DebeRechazar(MotivoDeRechazo.NoEncontrado);
        Assert.Equal(EstadoDeUnidad.Disponible, e.Almacen.Unidades[0].Estado);
    }

    // ---- CU-07 ----
    [Fact]
    public void US17_CU07_Alta_de_unidad_con_serie_queda_Disponible()
    {
        var e = new Escenario();
        var dto = e.Inventario.RegistrarUnidad(new(e.Laptop.Id, Escenario.Suc1, new NumeroDeSerie("sn-9"))).DebeSerExitoso<UnidadRegistradaDto>();
        Assert.Equal("Disponible", dto.Estado);
        Assert.Equal("SN-9", dto.Serie);
        Assert.Single(e.Almacen.Unidades);
    }

    [Fact]
    public void US17_CU07_Serie_duplicada()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "SN-9");
        e.Inventario.RegistrarUnidad(new(e.Laptop.Id, Escenario.Suc2, new NumeroDeSerie("SN-9"))).DebeRechazar(MotivoDeRechazo.SerieDuplicada);
        Assert.Single(e.Almacen.Unidades);
    }

    [Fact]
    public void US17_CU07_Serie_faltante()
    {
        var e = new Escenario();
        e.Inventario.RegistrarUnidad(new(e.Laptop.Id, Escenario.Suc1, null)).DebeRechazar(MotivoDeRechazo.SerieRequerida);
    }

    [Fact]
    public void CU07_Producto_sin_serie_se_registra_sin_ella_y_producto_inexistente_no_se_encuentra()
    {
        var e = new Escenario();
        e.Inventario.RegistrarUnidad(new(e.Teclado.Id, Escenario.Suc1, null)).DebeSerExitoso<UnidadRegistradaDto>();
        e.Inventario.RegistrarUnidad(new(new ProductoId("NO"), Escenario.Suc1, null)).DebeRechazar(MotivoDeRechazo.NoEncontrado);
    }

    // ---- Consultas ----
    [Fact]
    public void US04_Producto_con_existencia_muestra_la_cantidad_de_la_sucursal()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "A"); e.AgregarUnidad(e.Laptop, "B"); e.AgregarUnidad(e.Laptop, "C", Escenario.Suc2);
        var dto = e.ConsultasInventario.ConsultarDisponibilidadEnSucursal(new(e.Laptop.Id, Escenario.Suc1));
        Assert.Equal(2, dto.CantidadDisponible);
    }

    [Fact]
    public void US04_Producto_inexistente_responde_NoEncontrado()
    {
        var e = new Escenario();
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => e.ConsultasInventario.ConsultarDisponibilidadEnSucursal(new(new ProductoId("NO"), Escenario.Suc1)));
        Assert.Equal(MotivoDeRechazo.NoEncontrado, ex.Motivo);
        Assert.Throws<ReglaDeNegocioViolada>(() => e.ConsultasInventario.ConsultarDisponibilidadEnOtrasSucursales(new(new ProductoId("NO"), Escenario.Suc1)));
    }

    [Fact]
    public void US05_Sin_existencia_local_muestra_las_otras_sucursales_con_unidades()
    {
        var e = new Escenario();
        e.AgregarUnidad(e.Laptop, "A", Escenario.Suc2); e.AgregarUnidad(e.Laptop, "B", Escenario.Suc2);
        var lista = e.ConsultasInventario.ConsultarDisponibilidadEnOtrasSucursales(new(e.Laptop.Id, Escenario.Suc1));
        var d = Assert.Single(lista);
        Assert.Equal(("SUC-02", 2), (d.Sucursal, d.CantidadDisponible));
    }

    [Fact]
    public void US05_Sin_existencia_en_ninguna_sucursal_devuelve_lista_vacia()
    {
        var e = new Escenario();
        Assert.Empty(e.ConsultasInventario.ConsultarDisponibilidadEnOtrasSucursales(new(e.Laptop.Id, Escenario.Suc1)));
    }
}
