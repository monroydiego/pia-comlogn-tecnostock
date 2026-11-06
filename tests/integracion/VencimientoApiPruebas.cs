using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Infraestructura.Persistencia;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>US20 y RN-APT-002: el reloj de prueba se adelanta para vencer apartados.</summary>
public sealed class VencimientoApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly FabricaDeApi _fabrica;
    private readonly HttpClient _http;

    public VencimientoApiPruebas(FabricaDeApi fabrica)
    {
        _fabrica = fabrica;
        _http = fabrica.CreateClient();
    }

    private async Task<EstadoDeApartado> EstadoDe(string folio)
    {
        using var alcance = _fabrica.NuevoAlcance();
        return (await alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>().Apartados.AsNoTracking()
            .SingleAsync(a => a.Folio == new Folio(folio))).Estado;
    }

    [Fact]
    public async Task US20_Vencer_libera_las_unidades_de_los_apartados_vencidos_y_no_toca_los_vigentes()
    {
        var antes = await _http.Disponibles("TEC-P001");
        var viejo = (await _http.Post("/apartados", Apartar("TEC-P001", 120m))).Texto("folio");
        _fabrica.Reloj.Avanzar(5);
        var reciente = (await _http.Post("/apartados", Apartar("TEC-P001", 120m))).Texto("folio");
        Assert.Equal(antes - 2, await _http.Disponibles("TEC-P001"));

        _fabrica.Reloj.Avanzar(3); // el primero lleva 8 días (vencido), el segundo 3 (vigente)
        var r = await _http.Post("/apartados/vencer", rol: null); // lo ejecuta el actor Sistema: no requiere operador

        Assert.Equal(HttpStatusCode.OK, r.Codigo);
        Assert.Equal(1, r.Cuerpo.GetProperty("apartadosVencidos").GetInt32());
        Assert.Equal(EstadoDeApartado.Vencido, await EstadoDe(viejo));
        Assert.Equal(EstadoDeApartado.Vigente, await EstadoDe(reciente));
        Assert.Equal(antes - 1, await _http.Disponibles("TEC-P001"));

        var otraVez = await _http.Post("/apartados/vencer", rol: null);
        Assert.Equal(0, otraVez.Cuerpo.GetProperty("apartadosVencidos").GetInt32());
    }

    [Fact]
    public async Task US07_Convertir_un_apartado_vencido_responde_422_y_el_vencimiento_queda_guardado()
    {
        var antes = await _http.Disponibles("TEC-P003");
        var apartado = (await _http.Post("/apartados", Apartar("TEC-P003", 150m))).Texto("folio");
        _fabrica.Reloj.Avanzar(8);

        var r = await _http.Post($"/apartados/{apartado}/convertir", new { pagoDelSaldo = Pago("Efectivo", 328m), serieCapturada = "X" });

        Assert.Equal((HttpStatusCode)422, r.Codigo);
        Assert.Equal("ApartadoVencido", r.Texto("motivo"));
        Assert.Equal("IniciarVentaNueva", r.Texto("alternativaSugerida"));
        Assert.Equal(EstadoDeApartado.Vencido, await EstadoDe(apartado)); // rechazo con efecto (ADR-006)
        Assert.Equal(antes, await _http.Disponibles("TEC-P003"));
    }
}
