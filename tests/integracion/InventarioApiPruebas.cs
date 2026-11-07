using System.Net;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>US04, US05 y US17 de punta a punta.</summary>
public sealed class InventarioApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly HttpClient _http;

    public InventarioApiPruebas(FabricaDeApi fabrica) => _http = fabrica.CreateClient();

    [Fact]
    public async Task Datos_semilla_catalogo_de_20_productos_repartido_en_las_5_sucursales()
    {
        // TEC-P001 tiene 13 unidades: 3 en SUC-01, 3 en SUC-02, 3 en SUC-03, 2 en SUC-04 y 2 en SUC-05.
        Assert.Equal(3, await _http.Disponibles("TEC-P001", "SUC-01"));
        var otras = await _http.Get("/disponibilidad/TEC-P001/otras-sucursales?sucursalActual=SUC-01");
        Assert.Equal(4, otras.Cuerpo.GetArrayLength());
        Assert.Equal(10, otras.Cuerpo.EnumerateArray().Sum(s => s.GetProperty("cantidadDisponible").GetInt32()));
    }

    [Fact]
    public async Task US04_Producto_inexistente_responde_404()
    {
        var r = await _http.Get("/disponibilidad/NO-EXISTE?sucursalId=SUC-01");
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
        Assert.Equal("NoEncontrado", r.Texto("motivo"));
    }

    [Fact]
    public async Task US05_Producto_deshabilitado_sin_existencia_devuelve_lista_vacia()
    {
        var r = await _http.Get("/disponibilidad/TEC-P011/otras-sucursales?sucursalActual=SUC-01");
        Assert.Equal(HttpStatusCode.OK, r.Codigo);
        Assert.Equal(0, r.Cuerpo.GetArrayLength());
    }

    [Fact]
    public async Task Consulta_sin_sucursal_responde_400()
    {
        var r = await _http.Get("/disponibilidad/TEC-P001");
        Assert.Equal(HttpStatusCode.BadRequest, r.Codigo);
    }

    [Fact]
    public async Task US17_Alta_de_unidad_con_serie_responde_201_y_suma_a_la_disponibilidad()
    {
        var antes = await _http.Disponibles("TEC-P017", "SUC-05");
        var r = await _http.Post("/unidades", new { productoId = "TEC-P017", sucursalId = "SUC-05", serie = "sn-nueva-1" }, rol: "Almacen");
        Assert.Equal(HttpStatusCode.Created, r.Codigo);
        Assert.Equal("Disponible", r.Texto("estado"));
        Assert.Equal("SN-NUEVA-1", r.Texto("serie"));
        Assert.Equal(antes + 1, await _http.Disponibles("TEC-P017", "SUC-05"));
    }

    [Fact]
    public async Task US17_Sucursal_inexistente_responde_404_por_la_clave_foranea()
    {
        var r = await _http.Post("/unidades", new { productoId = "TEC-P001", sucursalId = "SUC-99", serie = "sn-sin-sucursal" }, rol: "Almacen");
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
        Assert.Contains("sucursal", r.Texto("detail"));
    }

    [Fact]
    public async Task US17_Serie_duplicada_responde_422()
    {
        var existente = Serie("TEC-P002", 1);
        var r = await _http.Post("/unidades", new { productoId = "TEC-P002", sucursalId = "SUC-02", serie = existente }, rol: "Almacen");
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("SerieDuplicada", r.Texto("motivo"));
    }

    [Fact]
    public async Task US17_Serie_faltante_responde_422_pero_un_producto_sin_serie_no_la_exige()
    {
        var sinSerie = await _http.Post("/unidades", new { productoId = "TEC-P001", sucursalId = "SUC-01" }, rol: "Almacen");
        Assert.Equal("SerieRequerida", sinSerie.Texto("motivo"));
        var teclado = await _http.Post("/unidades", new { productoId = "TEC-P004", sucursalId = "SUC-01" }, rol: "Almacen");
        Assert.Equal(HttpStatusCode.Created, teclado.Codigo);
    }
}
