using System.Net;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>
/// Entradas fuera de rango o con valores que no existen. Antes de endurecer el dominio, varias terminaban en un 500
/// (cadenas más largas que la columna, montos que no caben en decimal(18,2)) y una condición numérica inválida se aceptaba.
/// Todas deben responder 400 y no dejar rastro.
/// </summary>
public sealed class EntradasInvalidasApiPruebas : IClassFixture<FabricaDeApi>
{
    private static string Largo(int n) => new('x', n);
    private readonly HttpClient _http;

    public EntradasInvalidasApiPruebas(FabricaDeApi fabrica) => _http = fabrica.CreateClient();

    private static void Es400(Respuesta r, string? contiene = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.Codigo);
        Assert.Equal("DatosInvalidos", r.Texto("title"));
        if (contiene is not null) Assert.Contains(contiene, r.Texto("detail"));
    }

    [Fact]
    public async Task Un_cliente_con_mas_de_30_caracteres_responde_400()
    {
        var antes = await _http.Disponibles("TEC-P001");
        Es400(await _http.PostJson("/apartados",
            $"{{\"productoId\":\"TEC-P001\",\"sucursalId\":\"SUC-01\",\"clienteId\":\"{Largo(40)}\",\"anticipo\":{{\"metodo\":\"Efectivo\",\"monto\":101}}}}"), "30");
        Assert.Equal(antes, await _http.Disponibles("TEC-P001"));
    }

    [Fact]
    public async Task Una_serie_de_mas_de_60_caracteres_responde_400_en_la_venta_y_en_el_alta()
    {
        Es400(await _http.PostJson("/ventas",
            $"{{\"sucursalId\":\"SUC-01\",\"lineas\":[{{\"productoId\":\"TEC-P001\",\"serie\":\"{Largo(100)}\"}}],\"pagos\":[{{\"metodo\":\"Efectivo\",\"monto\":336}}]}}"), "60");
        Es400(await _http.PostJson("/unidades",
            $"{{\"productoId\":\"TEC-P001\",\"sucursalId\":\"SUC-01\",\"serie\":\"{Largo(100)}\"}}", "Almacen"), "60");
    }

    [Fact]
    public async Task Un_producto_o_sucursal_con_mas_de_30_caracteres_responde_400()
    {
        Es400(await _http.PostJson("/unidades", $"{{\"productoId\":\"{Largo(40)}\",\"sucursalId\":\"SUC-01\",\"serie\":\"A1\"}}", "Almacen"));
        Es400(await _http.PostJson("/unidades", $"{{\"productoId\":\"TEC-P001\",\"sucursalId\":\"{Largo(40)}\",\"serie\":\"A1\"}}", "Almacen"));
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.Get($"/devoluciones/{Largo(40)}")).Codigo);
    }

    [Fact]
    public async Task Un_monto_que_no_cabe_en_la_base_responde_400_en_apartados_y_ventas()
    {
        Es400(await _http.PostJson("/apartados",
            "{\"productoId\":\"TEC-P001\",\"sucursalId\":\"SUC-01\",\"clienteId\":\"TEC-C001\",\"anticipo\":{\"metodo\":\"Efectivo\",\"monto\":100000000000000000000}}"), "monto");
        Es400(await _http.PostJson("/ventas",
            "{\"sucursalId\":\"SUC-01\",\"lineas\":[{\"productoId\":\"TEC-P004\"}],\"pagos\":[{\"metodo\":\"Efectivo\",\"monto\":100000000000000000000}]}"), "monto");
    }

    [Fact]
    public async Task Un_usuario_de_mas_de_60_caracteres_en_el_encabezado_responde_400()
    {
        var r = await _http.Enviar(HttpMethod.Post, "/ventas", new
        {
            sucursalId = "SUC-01", lineas = new[] { new { productoId = "TEC-P004", serie = (string?)null } }, pagos = new[] { Pago("Efectivo", 549m) }
        }, rol: "Cajero", usuario: Largo(80));
        Es400(r, "60");
    }

    [Fact]
    public async Task Una_causal_o_un_detalle_demasiado_largos_responden_400_y_no_crean_la_devolucion()
    {
        var venta = (await _http.Post("/ventas", VentaConSerie("TEC-P001", Serie("TEC-P001", 1), 336m))).Texto("folio");
        string Cuerpo(string motivo) => $"{{\"folioDeVenta\":\"{venta}\",\"productoId\":\"TEC-P001\",\"serieCapturada\":\"{Serie("TEC-P001", 1)}\",\"motivo\":{motivo},\"condicionDelProducto\":\"EmpaqueCerrado\",\"formaDeCompensacion\":\"Reembolso\"}}";
        Es400(await _http.PostJson("/devoluciones", Cuerpo($"{{\"causal\":\"{Largo(150)}\"}}")), "100");
        Es400(await _http.PostJson("/devoluciones", Cuerpo($"{{\"causal\":\"Falla\",\"detalle\":\"{Largo(600)}\"}}")), "500");
        var valida = await _http.PostJson("/devoluciones", Cuerpo("{\"causal\":\"Falla\"}"));
        Assert.Equal(HttpStatusCode.Created, valida.Codigo); // la unidad no quedó afectada por los intentos rechazados
    }

    [Theory]
    [InlineData("\"condicionDelProducto\":99", "\"formaDeCompensacion\":\"Reembolso\"")]
    [InlineData("\"condicionDelProducto\":\"99\"", "\"formaDeCompensacion\":\"Reembolso\"")]
    [InlineData("\"condicionDelProducto\":\"Roto\"", "\"formaDeCompensacion\":\"Reembolso\"")]
    [InlineData("\"condicionDelProducto\":\"EmpaqueCerrado\"", "\"formaDeCompensacion\":99")]
    public async Task Un_valor_de_lista_inexistente_o_numerico_responde_400_y_no_se_acepta_como_valido(string condicion, string forma)
    {
        var r = await _http.PostJson("/devoluciones",
            $"{{\"folioDeVenta\":\"VTA-000001\",\"productoId\":\"TEC-P001\",\"serieCapturada\":\"SN-TEC-P001-001\",\"motivo\":{{\"causal\":\"Falla\"}},{condicion},{forma}}}");
        Assert.Equal(HttpStatusCode.BadRequest, r.Codigo);
    }

    [Fact]
    public async Task Un_metodo_de_pago_numerico_o_fuera_del_catalogo_responde_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.PostJson("/apartados",
            "{\"productoId\":\"TEC-P002\",\"sucursalId\":\"SUC-02\",\"clienteId\":\"TEC-C001\",\"anticipo\":{\"metodo\":99,\"monto\":200}}")).Codigo);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.PostJson("/apartados",
            "{\"productoId\":\"TEC-P002\",\"sucursalId\":\"SUC-02\",\"clienteId\":\"TEC-C001\",\"anticipo\":{\"metodo\":\"Cheque\",\"monto\":200}}")).Codigo);
    }

    [Fact]
    public async Task Un_motivo_de_mas_de_500_caracteres_en_cancelaciones_y_resoluciones_responde_400_sin_cambiar_el_estado()
    {
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P003", 200m))).Texto("folio");
        Es400(await _http.PostJson($"/apartados/{apartado}/cancelar", $"{{\"motivo\":\"{Largo(600)}\"}}", "Supervisor"), "500");
        var correcta = await _http.Post($"/apartados/{apartado}/cancelar", new { motivo = "ok" }, rol: "Supervisor");
        Assert.Equal(HttpStatusCode.OK, correcta.Codigo); // seguía vigente

        var venta = (await _http.Post("/ventas", VentaConSerie("TEC-P001", Serie("TEC-P001", 6), 336m, SucursalDe(6)))).Texto("folio");
        var devolucion = (await _http.PostJson("/devoluciones",
            $"{{\"folioDeVenta\":\"{venta}\",\"productoId\":\"TEC-P001\",\"serieCapturada\":\"{Serie("TEC-P001", 6)}\",\"motivo\":{{\"causal\":\"Falla\"}},\"condicionDelProducto\":\"EmpaqueAbierto\",\"formaDeCompensacion\":\"Reembolso\"}}")).Texto("folio");
        Es400(await _http.PostJson($"/devoluciones/{devolucion}/resolucion", $"{{\"autoriza\":true,\"motivo\":\"{Largo(600)}\"}}", "Supervisor"), "500");
        var resuelta = await _http.Post($"/devoluciones/{devolucion}/resolucion", new { autoriza = true, motivo = "ok" }, rol: "Supervisor");
        Assert.Equal("Aceptada", resuelta.Texto("estado")); // seguía EnRevision
    }
}
