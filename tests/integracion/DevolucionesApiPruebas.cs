using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Infraestructura.Persistencia;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>US02, US08, US10, US13 y US16 de punta a punta. El reloj de prueba se adelanta para forzar los plazos.</summary>
public sealed class DevolucionesApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly FabricaDeApi _fabrica;
    private readonly HttpClient _http;
    // Cada prueba usa una unidad distinta: la base de datos se comparte dentro del fixture.
    private static int _siguienteSerie;

    public DevolucionesApiPruebas(FabricaDeApi fabrica)
    {
        _fabrica = fabrica;
        _http = fabrica.CreateClient();
    }

    /// <summary>Vende una laptop con serie y regresa (folio de venta, serie).</summary>
    private async Task<(string Folio, string Serie)> VenderLaptop(string producto)
    {
        var n = Interlocked.Increment(ref _siguienteSerie);
        var serie = Serie(producto, n);
        var r = await _http.Post("/ventas", VentaConSerie(producto, serie, 10000m, SucursalDe(n)));
        Assert.Equal(HttpStatusCode.Created, r.Codigo);
        return (r.Texto("folio"), serie);
    }

    private static object Devolver(string folioVenta, string producto, string serie, string condicion = "EmpaqueCerrado", string forma = "Reembolso") => new
    {
        folioDeVenta = folioVenta, productoId = producto, serieCapturada = serie,
        motivo = new { causal = "Falla", detalle = "No enciende" }, condicionDelProducto = condicion, formaDeCompensacion = forma
    };

    private async Task<EstadoDeUnidad> LeerEstadoDeUnidad(string serie)
    {
        using var alcance = _fabrica.NuevoAlcance();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        return (await ctx.Unidades.AsNoTracking().SingleAsync(u => u.Serie == new NumeroDeSerie(serie))).Estado;
    }

    [Fact]
    public async Task US10_US02_Devolucion_estandar_se_acepta_consulta_el_estado_y_la_unidad_regresa_a_Disponible()
    {
        var (venta, serie) = await VenderLaptop("TEC-P001");
        Assert.Equal(EstadoDeUnidad.Vendida, await LeerEstadoDeUnidad(serie));

        var r = await _http.Post("/devoluciones", Devolver(venta, "TEC-P001", serie, forma: "NotaDeCredito"));

        Assert.Equal(HttpStatusCode.Created, r.Codigo);
        Assert.Equal("Aceptada", r.Texto("estado"));
        Assert.Equal("NotaDeCredito", r.Texto("formaDeCompensacion"));
        Assert.Equal(EstadoDeUnidad.Disponible, await LeerEstadoDeUnidad(serie)); // RN-INV-004 por el manejador

        var consulta = await _http.Get($"/devoluciones/{r.Texto("folio")}");
        Assert.Equal(HttpStatusCode.OK, consulta.Codigo);
        Assert.Equal("Aceptada", consulta.Texto("estado"));
        Assert.Equal("Falla", consulta.Texto("motivo")); // US08: el motivo queda asociado al folio

        using var alcance = _fabrica.NuevoAlcance();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        Assert.True(await ctx.Comprobantes.AnyAsync(c => c.Tipo == "Devolucion" && c.FolioDeOperacion == r.Texto("folio")));
    }

    [Fact]
    public async Task RN_INV_004_Devolucion_de_producto_defectuoso_deja_la_unidad_Defectuosa()
    {
        var (venta, serie) = await VenderLaptop("TEC-P001");
        var r = await _http.Post("/devoluciones", Devolver(venta, "TEC-P001", serie, "Defectuoso"));
        Assert.Equal("Aceptada", r.Texto("estado"));
        Assert.Equal(EstadoDeUnidad.Defectuosa, await LeerEstadoDeUnidad(serie));
    }

    [Fact]
    public async Task US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza()
    {
        var (venta, serie) = await VenderLaptop("TEC-P001");
        var solicitud = await _http.Post("/devoluciones", Devolver(venta, "TEC-P001", serie, "EmpaqueAbierto"));
        Assert.Equal("EnRevision", solicitud.Texto("estado"));
        var folio = solicitud.Texto("folio");
        Assert.Equal(EstadoDeUnidad.Vendida, await LeerEstadoDeUnidad(serie));

        var cajero = await _http.Post($"/devoluciones/{folio}/resolucion", new { autoriza = true, motivo = "x" }, rol: "Cajero");
        Assert.Equal((HttpStatusCode)422, cajero.Codigo);
        Assert.Equal("SoloSupervisorAutoriza", cajero.Texto("motivo"));

        var supervisor = await _http.Post($"/devoluciones/{folio}/resolucion", new { autoriza = true, motivo = "cliente frecuente" }, rol: "Supervisor");
        Assert.Equal(HttpStatusCode.OK, supervisor.Codigo);
        Assert.Equal("Aceptada", supervisor.Texto("estado"));
        Assert.Equal("cliente frecuente", supervisor.Texto("motivo"));
        Assert.Equal(EstadoDeUnidad.Defectuosa, await LeerEstadoDeUnidad(serie)); // empaque abierto: no vuelve a la venta

        var repetida = await _http.Post($"/devoluciones/{folio}/resolucion", new { autoriza = false, motivo = "otra vez" }, rol: "Supervisor");
        Assert.Equal(HttpStatusCode.Conflict, repetida.Codigo);
        Assert.Equal("TransicionDeEstadoInvalida", repetida.Texto("motivo"));

        using var alcance = _fabrica.NuevoAlcance();
        var devolucion = await alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>().Devoluciones.AsNoTracking()
            .SingleAsync(d => d.Folio == new Folio(folio));
        Assert.Equal("usuario1", devolucion.Decision!.Supervisor.UsuarioId); // la decisión se reconstruye desde SQL
        Assert.Contains(devolucion.Dictamen.Incumplimientos(), i => i.Contains("empaque abierto"));
    }

    [Fact]
    public async Task US13_Denegar_la_excepcion_marca_Rechazada_y_no_toca_la_unidad()
    {
        var (venta, serie) = await VenderLaptop("TEC-P001");
        var folio = (await _http.Post("/devoluciones", Devolver(venta, "TEC-P001", serie, "EmpaqueAbierto"))).Texto("folio");

        var r = await _http.Post($"/devoluciones/{folio}/resolucion", new { autoriza = false, motivo = "producto usado" }, rol: "Supervisor");
        Assert.Equal("Rechazada", r.Texto("estado"));
        Assert.Equal(EstadoDeUnidad.Vendida, await LeerEstadoDeUnidad(serie));
    }

    [Fact]
    public async Task US10_Comprobante_inexistente_responde_422_y_no_crea_la_devolucion()
    {
        var r = await _http.Post("/devoluciones", Devolver("VTA-999999", "TEC-P001", "SN-X"));
        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("ComprobanteNoValido", r.Texto("motivo"));
    }

    [Fact]
    public async Task Devolver_una_linea_que_no_es_de_la_venta_responde_422()
    {
        var (venta, _) = await VenderLaptop("TEC-P001");
        var r = await _http.Post("/devoluciones", Devolver(venta, "TEC-P001", "SN-OTRA"));
        Assert.Equal("LineaNoPerteneceALaVenta", r.Texto("motivo"));
    }

    [Fact]
    public async Task US02_Folio_de_devolucion_inexistente_responde_404()
    {
        var r = await _http.Get("/devoluciones/DEV-999999");
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
    }

    [Fact]
    public async Task US16_Criterio_vigente_incluye_condiciones_y_plazo_por_categoria()
    {
        var r = await _http.Get("/politica/devolucion");
        Assert.Equal(HttpStatusCode.OK, r.Codigo);
        Assert.Equal(3, r.Cuerpo.GetProperty("condiciones").GetArrayLength());
        Assert.Equal(15, r.Cuerpo.GetProperty("plazoPorCategoria").GetProperty("Computadoras").GetInt32());
        Assert.Equal(30, r.Cuerpo.GetProperty("plazoPorCategoria").GetProperty("Accesorios").GetInt32());
    }
}

/// <summary>Plazos de devolución: aquí se adelanta el reloj, por eso va en su propio fixture.</summary>
public sealed class DevolucionesFueraDePlazoApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly FabricaDeApi _fabrica;
    private readonly HttpClient _http;

    public DevolucionesFueraDePlazoApiPruebas(FabricaDeApi fabrica)
    {
        _fabrica = fabrica;
        _http = fabrica.CreateClient();
    }

    [Theory]
    [InlineData("TEC-P001", "Computadoras", 15, 16)] // Laptop: 15 días
    [InlineData("TEC-P002", "Accesorios", 30, 31)]   // Monitor: 30 días
    public async Task US10_El_plazo_depende_de_la_categoria_dentro_se_acepta_y_fuera_queda_EnRevision(string producto, string categoria, int dentro, int fuera)
    {
        var serieUno = Serie(producto, 1);
        var serieDos = Serie(producto, 2);
        var ventaUno = (await _http.Post("/ventas", VentaConSerie(producto, serieUno, 10000m, SucursalDe(1)))).Texto("folio");
        var ventaDos = (await _http.Post("/ventas", VentaConSerie(producto, serieDos, 10000m, SucursalDe(2)))).Texto("folio");

        _fabrica.Reloj.Avanzar(dentro);
        var enPlazo = await _http.Post("/devoluciones", new
        {
            folioDeVenta = ventaUno, productoId = producto, serieCapturada = serieUno, motivo = new { causal = "Falla" },
            condicionDelProducto = "EmpaqueCerrado", formaDeCompensacion = "Reembolso"
        });
        Assert.Equal("Aceptada", enPlazo.Texto("estado"));

        _fabrica.Reloj.Avanzar(fuera - dentro);
        var fueraDePlazo = await _http.Post("/devoluciones", new
        {
            folioDeVenta = ventaDos, productoId = producto, serieCapturada = serieDos, motivo = new { causal = "Falla" },
            condicionDelProducto = "EmpaqueCerrado", formaDeCompensacion = "Reembolso"
        });
        Assert.Equal("EnRevision", fueraDePlazo.Texto("estado"));
        Assert.NotEmpty(categoria);
    }
}
