using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Infraestructura.Persistencia;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>US01, US03, US06, US07, US09, US11, US12 y US14 de punta a punta: HTTP, EF Core y SQL Server.</summary>
public sealed class ApartadosYVentasApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly FabricaDeApi _fabrica;
    private readonly HttpClient _http;

    public ApartadosYVentasApiPruebas(FabricaDeApi fabrica)
    {
        _fabrica = fabrica;
        _http = fabrica.CreateClient();
    }

    [Fact]
    public async Task OpenAPI_expone_todos_los_endpoints_del_diagrama_03()
    {
        var r = await _http.Get("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, r.Codigo);
        var rutas = r.Cuerpo.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToHashSet();
        foreach (var ruta in new[] { "/apartados", "/apartados/{folio}/convertir", "/apartados/{folio}/cancelar", "/apartados/vencer", "/ventas",
                     "/devoluciones", "/devoluciones/{folio}/resolucion", "/devoluciones/{folio}", "/unidades", "/disponibilidad/{productoId}",
                     "/disponibilidad/{productoId}/otras-sucursales", "/politica/devolucion" })
            Assert.Contains(ruta, rutas);
    }

    [Fact]
    public async Task US01_US03_Apartar_responde_201_con_folio_fecha_limite_y_saldo_y_bloquea_la_unidad()
    {
        var antes = await _http.Disponibles("TEC-P002");
        var r = await _http.Post("/apartados", Apartar("TEC-P002", 200m));

        Assert.Equal(HttpStatusCode.Created, r.Codigo);
        Assert.StartsWith("APT-", r.Texto("folio"));
        Assert.Equal(207m, r.Cuerpo.GetProperty("saldoPendiente").GetDecimal()); // precio 407 - anticipo 200
        Assert.True(r.Cuerpo.TryGetProperty("fechaLimite", out _));
        Assert.Equal(antes - 1, await _http.Disponibles("TEC-P002"));
    }

    [Fact]
    public async Task US01_Anticipo_insuficiente_responde_422_y_la_unidad_sigue_disponible()
    {
        var antes = await _http.Disponibles("TEC-P003");
        var r = await _http.Post("/apartados", Apartar("TEC-P003", 10m));
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("AnticipoInsuficiente", r.Texto("motivo"));
        Assert.Equal(antes, await _http.Disponibles("TEC-P003"));
    }

    [Fact]
    public async Task US01_Producto_no_apartable_responde_422()
    {
        var r = await _http.Post("/apartados", Apartar("TEC-P004", 300m)); // Teclado
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("ProductoNoApartable", r.Texto("motivo"));
    }

    [Fact]
    public async Task US01_Sin_existencia_responde_422_y_sugiere_consultar_otras_sucursales()
    {
        var r = await _http.Post("/apartados", Apartar("TEC-P011", 400m)); // deshabilitado: sin unidades
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("SinExistenciaDisponible", r.Texto("motivo"));
        Assert.Equal("ConsultarOtrasSucursales", r.Texto("alternativaSugerida"));
    }

    [Fact]
    public async Task Producto_inexistente_responde_404()
    {
        var r = await _http.Post("/apartados", Apartar("NO-EXISTE", 100m));
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
        Assert.Equal("NoEncontrado", r.Texto("motivo"));
    }

    [Fact]
    public async Task Datos_invalidos_responden_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Post("/apartados", Apartar("TEC-P001", -5m))).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Post("/apartados", Apartar("TEC-P001", 100m), rol: null)).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Post("/apartados", Apartar("TEC-P001", 100m), rol: "Gerente")).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Post("/apartados", Apartar("  ", 100m))).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Post("/apartados", new { productoId = "TEC-P001" })).Codigo);
    }

    [Fact]
    public async Task US11_Metodo_de_pago_fuera_del_catalogo_responde_400()
    {
        var r = await _http.Enviar(HttpMethod.Post, "/ventas",
            "{\"sucursalId\":\"SUC-01\",\"lineas\":[{\"productoId\":\"TEC-P004\"}],\"pagos\":[{\"metodo\":\"Cheque\",\"monto\":549}]}");
        Assert.Equal(HttpStatusCode.BadRequest, r.Codigo);
    }

    [Fact]
    public async Task Venta_de_producto_sin_serie_se_cierra_con_cualquier_unidad_disponible()
    {
        var antes = await _http.Disponibles("TEC-P004");
        var r = await _http.Post("/ventas", new
        {
            sucursalId = "SUC-01",
            lineas = new[] { new { productoId = "TEC-P004", serie = (string?)null }, new { productoId = "TEC-P004", serie = (string?)null } },
            pagos = new[] { Pago("Tarjeta", 500m), Pago("Efectivo", 598m) }
        });
        Assert.Equal(HttpStatusCode.Created, r.Codigo);
        Assert.Equal(1098m, r.Cuerpo.GetProperty("total").GetDecimal());
        Assert.Equal(antes - 2, await _http.Disponibles("TEC-P004"));
    }

    [Fact]
    public async Task US06_Vender_una_unidad_apartada_responde_409_UnidadApartada()
    {
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P005", 200m))).Texto("folio");
        var serie = await SerieApartada(apartado);

        var r = await _http.Post("/ventas", VentaConSerie("TEC-P005", serie, 620m));

        Assert.Equal(HttpStatusCode.Conflict, r.Codigo);
        Assert.Equal("UnidadApartada", r.Texto("motivo"));
        Assert.Equal("ElegirOtraUnidad", r.Texto("alternativaSugerida"));
    }

    [Fact]
    public async Task US07_US09_US12_Convertir_apartado_en_venta_aplica_el_anticipo_marca_la_unidad_vendida_y_emite_comprobante_con_serie()
    {
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P006", 300m))).Texto("folio");
        var serie = await SerieApartada(apartado);

        var r = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Tarjeta", 391m), serieCapturada = serie.ToLowerInvariant() });

        Assert.Equal(HttpStatusCode.OK, r.Codigo);
        Assert.Equal(691m, r.Cuerpo.GetProperty("total").GetDecimal());
        Assert.Equal(apartado, r.Texto("folioDeApartadoOrigen"));
        var folioVenta = r.Texto("folio");

        using var alcance = _fabrica.NuevoAlcance();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        var comprobante = await ctx.Comprobantes.SingleAsync(c => c.FolioDeOperacion == folioVenta);
        Assert.Contains(serie, comprobante.Detalle);
        Assert.Equal("usuario1", comprobante.Usuario);
        Assert.StartsWith("CMP-", comprobante.FolioDeComprobante);
    }

    [Fact]
    public async Task US07_Saldo_insuficiente_responde_422_y_el_apartado_sigue_vigente()
    {
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P007", 300m))).Texto("folio");
        var serie = await SerieApartada(apartado);

        var r = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Efectivo", 1m), serieCapturada = serie });
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("SaldoInsuficiente", r.Texto("motivo"));

        var segundo = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Efectivo", 462m), serieCapturada = serie });
        Assert.Equal(HttpStatusCode.OK, segundo.Codigo);
    }

    [Fact]
    public async Task US09_Convertir_con_serie_equivocada_o_faltante_responde_422()
    {
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P009", 300m))).Texto("folio");
        var equivocada = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Efectivo", 604m), serieCapturada = "OTRA" });
        Assert.Equal("SerieNoCoincide", equivocada.Texto("motivo"));
        var faltante = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Efectivo", 604m) });
        Assert.Equal("SerieRequerida", faltante.Texto("motivo"));
    }

    [Fact]
    public async Task Convertir_apartado_inexistente_responde_404()
    {
        var r = await _http.Post("/apartados/APT-999999/convertir", new { pagoDelSaldo = Pago("Efectivo", 1m), serieCapturada = "X" });
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
    }

    [Fact]
    public async Task US09_Venta_directa_exige_serie_y_cierra_con_ella()
    {
        var sinSerie = await _http.Post("/ventas", VentaConSerie("TEC-P013", null, 1188m));
        Assert.Equal((HttpStatusCode)422, sinSerie.Codigo);
        Assert.Equal("SerieRequerida", sinSerie.Texto("motivo"));

        var antes = await _http.Disponibles("TEC-P013");
        var ok = await _http.Post("/ventas", VentaConSerie("TEC-P013", Serie("TEC-P013", 1), 1188m));
        Assert.Equal(HttpStatusCode.Created, ok.Codigo);
        Assert.Equal(Serie("TEC-P013", 1), ok.Cuerpo.GetProperty("lineas")[0].GetProperty("serie").GetString());
        Assert.Equal(antes - 1, await _http.Disponibles("TEC-P013"));

        var otraVez = await _http.Post("/ventas", VentaConSerie("TEC-P013", Serie("TEC-P013", 1), 1188m));
        Assert.Equal((HttpStatusCode)422, otraVez.Codigo); // UnidadNoDisponible: ya está Vendida
        Assert.Equal("UnidadNoDisponible", otraVez.Texto("motivo"));
    }

    [Fact]
    public async Task Venta_con_pagos_insuficientes_responde_422_y_no_vende_la_unidad()
    {
        var antes = await _http.Disponibles("TEC-P014");
        var r = await _http.Post("/ventas", VentaConSerie("TEC-P014", Serie("TEC-P014", 1), 100m));
        Assert.Equal("SaldoInsuficiente", r.Texto("motivo"));
        Assert.Equal(antes, await _http.Disponibles("TEC-P014"));
    }

    [Fact]
    public async Task US14_Cancelar_apartado_solo_lo_hace_el_Supervisor_y_libera_la_unidad()
    {
        var antes = await _http.Disponibles("TEC-P015");
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P015", 400m))).Texto("folio");
        Assert.Equal(antes - 1, await _http.Disponibles("TEC-P015"));

        var vendedor = await _http.Post($"/apartados/{apartado}/cancelar", new { motivo = "x" }, rol: "Vendedor");
        Assert.Equal((HttpStatusCode)422, vendedor.Codigo);
        Assert.Equal("SoloSupervisorAutoriza", vendedor.Texto("motivo"));

        var supervisor = await _http.Post($"/apartados/{apartado}/cancelar", new { motivo = "el cliente lo pidió" }, rol: "Supervisor");
        Assert.Equal(HttpStatusCode.OK, supervisor.Codigo);
        Assert.Equal("Cancelado", supervisor.Texto("estado"));
        Assert.Equal(antes, await _http.Disponibles("TEC-P015"));

        var repetido = await _http.Post($"/apartados/{apartado}/cancelar", new { motivo = "otra vez" }, rol: "Supervisor");
        Assert.Equal(HttpStatusCode.Conflict, repetido.Codigo);
        Assert.Equal("TransicionDeEstadoInvalida", repetido.Texto("motivo"));
    }

    /// <summary>Serie de la unidad que quedó reservada por el apartado (se lee de la base de datos).</summary>
    private async Task<string> SerieApartada(string folioDeApartado)
    {
        using var alcance = _fabrica.NuevoAlcance();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        var folio = new TecnoStock.Dominio.ObjetosDeValor.Folio(folioDeApartado);
        var unidad = await ctx.Unidades.AsNoTracking().SingleAsync(u => u.FolioDeApartadoVigente == folio);
        return unidad.Serie!.Valor;
    }
}
