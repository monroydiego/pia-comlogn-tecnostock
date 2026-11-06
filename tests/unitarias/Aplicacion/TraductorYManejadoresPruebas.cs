using TecnoStock.Aplicacion.Comun;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

public class TraductorYManejadoresPruebas
{
    private readonly TraductorDeRechazos _t = new();

    [Theory]
    [InlineData(MotivoDeRechazo.SinExistenciaDisponible, AlternativaSugerida.ConsultarOtrasSucursales)]
    [InlineData(MotivoDeRechazo.UnidadApartada, AlternativaSugerida.ElegirOtraUnidad)]
    [InlineData(MotivoDeRechazo.UnidadNoDisponible, AlternativaSugerida.ElegirOtraUnidad)]
    [InlineData(MotivoDeRechazo.UnidadEnOtraSucursal, AlternativaSugerida.ElegirOtraUnidad)]
    [InlineData(MotivoDeRechazo.SerieRequerida, AlternativaSugerida.CapturarSerie)]
    [InlineData(MotivoDeRechazo.SerieNoCoincide, AlternativaSugerida.CapturarSerie)]
    [InlineData(MotivoDeRechazo.ApartadoVencido, AlternativaSugerida.IniciarVentaNueva)]
    [InlineData(MotivoDeRechazo.SaldoInsuficiente, AlternativaSugerida.Ninguna)]
    public void Traduce_cada_motivo_con_su_alternativa(MotivoDeRechazo motivo, AlternativaSugerida alternativa)
    {
        var r = _t.Traducir(new ReglaDeNegocioViolada(motivo, "mensaje"));
        Assert.Equal(motivo, r.Motivo);
        Assert.Equal("mensaje", r.MensajeDeNegocio);
        Assert.Equal(alternativa, r.Alternativa);
    }

    [Fact]
    public void Resultado_distingue_exito_y_rechazo()
    {
        var ok = Resultado.Exito(5);
        Assert.True(ok.Exitoso);
        Assert.Equal(5, ok.ValorComo<int>());
        var no = Resultado.Rechazar(_t.Traducir(MotivoDeRechazo.NoEncontrado, "x"));
        Assert.False(no.Exitoso);
        Assert.Throws<InvalidOperationException>(() => no.ValorComo<int>());
    }

    [Fact]
    public void Si_el_reingreso_falla_la_transaccion_propia_se_descarta()
    {
        var e = new Escenario();
        var manejador = new TecnoStock.Aplicacion.Eventos.ManejadorDeReingresoDeUnidades(
            new RepositorioDeUnidadesEnMemoria(e.Almacen), new UnidadDeTrabajoEnMemoria(e.Almacen, e.Publicador));
        var evento = new TecnoStock.Dominio.Eventos.DevolucionAceptada(new("DEV-1"), TecnoStock.Dominio.ObjetosDeValor.UnidadId.Nuevo(),
            new("VTA-1"), TecnoStock.Dominio.Enumeraciones.CondicionDelProducto.EmpaqueCerrado, e.Reloj.Actual);
        Assert.Throws<ReglaDeNegocioViolada>(() => manejador.AlAceptarseDevolucion(evento));
        Assert.Equal(1, e.Almacen.Descartes);
    }
}
