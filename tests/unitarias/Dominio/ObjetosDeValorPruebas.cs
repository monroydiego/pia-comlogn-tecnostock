using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

public class ObjetosDeValorPruebas
{
    [Fact]
    public void Dinero_suma_resta_y_porcentaje()
    {
        var cien = new Dinero(100m);
        Assert.Equal(150m, cien.Sumar(new Dinero(50m)).Monto);
        Assert.Equal(0m, cien.Restar(new Dinero(500m)).Monto);
        Assert.Equal(30m, cien.Porcentaje(30m).Monto);
        Assert.True(new Dinero(1m).EsMenorQue(cien));
        Assert.Equal(0m, Dinero.Cero.Monto);
        Assert.Equal("100.00 MXN", cien.ToString());
    }

    [Fact]
    public void Dinero_negativo_se_rechaza() => Assert.Throws<ArgumentException>(() => new Dinero(-1m));

    [Fact]
    public void NumeroDeSerie_se_normaliza_y_compara_por_valor()
    {
        Assert.Equal(new NumeroDeSerie(" sn-1 "), new NumeroDeSerie("SN-1"));
        Assert.Equal("SN-1", new NumeroDeSerie("sn-1").ToString());
        Assert.Throws<ArgumentException>(() => new NumeroDeSerie("  "));
    }

    [Fact]
    public void Folio_vacio_se_rechaza()
    {
        Assert.Throws<ArgumentException>(() => new Folio(""));
        Assert.Equal("A-1", new Folio(" A-1 ").ToString());
    }

    [Fact]
    public void Identificadores_tipados_se_representan_como_texto()
    {
        Assert.Equal("TEC-P001", new ProductoId("TEC-P001").ToString());
        Assert.Equal("SUC-01", new SucursalId("SUC-01").ToString());
        Assert.Equal("TEC-C001", new ClienteId("TEC-C001").ToString());
        Assert.NotEqual(UnidadId.Nuevo(), UnidadId.Nuevo());
        Assert.False(string.IsNullOrEmpty(UnidadId.Nuevo().ToString()));
    }

    [Fact]
    public void Operador_distingue_al_supervisor()
    {
        Assert.True(new Operador("u", Rol.Supervisor).EsSupervisor());
        Assert.False(new Operador("u", Rol.Cajero).EsSupervisor());
        Assert.Throws<ArgumentException>(() => new Operador(" ", Rol.Cajero));
    }

    [Fact]
    public void Vigencia_vence_al_alcanzar_la_fecha_limite()
    {
        var v = new VigenciaDeApartado(Datos.Ahora, Datos.Ahora.AddDays(7));
        Assert.False(v.HaVencido(Datos.Ahora.AddDays(6)));
        Assert.True(v.HaVencido(Datos.Ahora.AddDays(7)));
        Assert.Throws<ArgumentException>(() => new VigenciaDeApartado(Datos.Ahora, Datos.Ahora.AddDays(-1)));
    }

    [Fact]
    public void MotivoDeDevolucion_exige_causal() =>
        Assert.Throws<ArgumentException>(() => new MotivoDeDevolucion(" "));

    [Fact]
    public void Dictamen_lista_los_incumplimientos()
    {
        var d = new DictamenDeDevolucion([ResultadoDeRegla.Satisfecha(), ResultadoDeRegla.Incumplida("fuera de plazo")]);
        Assert.False(d.Cumple());
        Assert.Equal(["fuera de plazo"], d.Incumplimientos());
        Assert.True(new DictamenDeDevolucion([ResultadoDeRegla.Satisfecha()]).Cumple());
        Assert.Throws<ArgumentException>(() => new DictamenDeDevolucion([]));
    }

    [Fact]
    public void Decision_solo_la_registra_un_Supervisor()
    {
        var ok = DecisionDeSupervisor.Registrar(Datos.Supervisor, true, "caso especial", Datos.Ahora);
        Assert.True(ok.Autoriza);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => DecisionDeSupervisor.Registrar(Datos.Cajero, true, "x", Datos.Ahora));
        Assert.Equal(MotivoDeRechazo.SoloSupervisorAutoriza, ex.Motivo);
        Assert.Throws<ArgumentException>(() => DecisionDeSupervisor.Registrar(Datos.Supervisor, true, " ", Datos.Ahora));
    }

    [Fact]
    public void ReglaDeNegocioViolada_lleva_motivo_y_mensaje()
    {
        var ex = new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, "no existe");
        Assert.Equal(MotivoDeRechazo.NoEncontrado, ex.Motivo);
        Assert.Equal("no existe", ex.MensajeDeNegocio);
    }
}
