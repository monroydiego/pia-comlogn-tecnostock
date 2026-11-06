using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.Estados;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Politicas;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

/// <summary>US02, US08, US10, US13, US16.</summary>
public class DevolucionPruebas
{
    private static Devolucion Solicitar(Producto producto, Venta venta, CondicionDelProducto condicion, DateTime ahora)
    {
        var linea = venta.Lineas[0];
        var dictamen = Datos.Politica().CriterioDeDevolucionVigente().Evaluar(venta, linea, producto, condicion, ahora);
        return Devolucion.Solicitar(new Folio("DEV-1"), venta, producto.Id, linea.Serie,
            new MotivoDeDevolucion("Falla", "No enciende"), condicion, FormaDeCompensacion.NotaDeCredito, dictamen, Datos.Cajero, ahora);
    }

    private static (Producto, Venta) VentaDeLaptop()
    {
        var laptop = Datos.Laptop();
        return (laptop, Datos.Venta(laptop, Datos.Unidad(laptop, "SN-1")));
    }

    [Fact]
    public void US10_Devolucion_estandar_se_acepta_sin_intervencion_del_Supervisor()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora.AddDays(5));
        Assert.Equal(EstadoDeDevolucion.Aceptada, dev.Estado);
        Assert.Null(dev.Decision);
        var evento = Assert.IsType<DevolucionAceptada>(Assert.Single(dev.ExtraerEventos()));
        Assert.Equal(venta.Lineas[0].UnidadId, evento.UnidadId);
        Assert.Equal(CondicionDelProducto.EmpaqueCerrado, evento.Condicion);
    }

    [Fact]
    public void US10_Devolucion_fuera_de_plazo_queda_EnRevision_y_se_deriva()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora.AddDays(16));
        Assert.Equal(EstadoDeDevolucion.EnRevision, dev.Estado);
        var evento = Assert.IsType<DevolucionDerivadaASupervisor>(Assert.Single(dev.ExtraerEventos()));
        Assert.Contains(evento.Incumplimientos, i => i.Contains("plazo"));
    }

    [Fact]
    public void US10_Empaque_abierto_queda_EnRevision_y_se_deriva()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueAbierto, Datos.Ahora.AddDays(2));
        Assert.Equal(EstadoDeDevolucion.EnRevision, dev.Estado);
        Assert.Contains(dev.Dictamen.Incumplimientos(), i => i.Contains("empaque abierto"));
    }

    [Fact]
    public void US10_Producto_defectuoso_dentro_de_plazo_se_acepta()
    {
        var (laptop, venta) = VentaDeLaptop();
        Assert.Equal(EstadoDeDevolucion.Aceptada, Solicitar(laptop, venta, CondicionDelProducto.Defectuoso, Datos.Ahora.AddDays(2)).Estado);
    }

    [Fact]
    public void US10_Dictamen_reune_todos_los_incumplimientos_sin_cortocircuitar()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dictamen = Datos.Politica().CriterioDeDevolucionVigente()
            .Evaluar(venta, venta.Lineas[0], laptop, CondicionDelProducto.EmpaqueAbierto, Datos.Ahora.AddDays(40));
        Assert.Equal(2, dictamen.Incumplimientos().Count);
    }

    [Fact]
    public void US10_Comprobante_no_valido_el_dictamen_incumple()
    {
        var laptop = Datos.Laptop();
        var dictamen = Datos.Politica().CriterioDeDevolucionVigente().Evaluar(null, null, laptop, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora);
        Assert.False(dictamen.Cumple());
        Assert.Contains(dictamen.Incumplimientos(), i => i.Contains("comprobante"));
    }

    [Fact]
    public void Devolver_una_linea_que_no_pertenece_a_la_venta_lanza_LineaNoPerteneceALaVenta()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dictamen = new DictamenDeDevolucion([ResultadoDeRegla.Satisfecha()]);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => Devolucion.Solicitar(new Folio("DEV-2"), venta, laptop.Id,
            new NumeroDeSerie("OTRA"), new MotivoDeDevolucion("x"), CondicionDelProducto.EmpaqueCerrado,
            FormaDeCompensacion.Reembolso, dictamen, Datos.Cajero, Datos.Ahora));
        Assert.Equal(MotivoDeRechazo.LineaNoPerteneceALaVenta, ex.Motivo);
    }

    [Fact]
    public void US13_Autorizacion_de_excepcion_acepta_y_registra_al_Supervisor()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueAbierto, Datos.Ahora.AddDays(2));
        dev.ExtraerEventos();

        var decision = DecisionDeSupervisor.Registrar(Datos.Supervisor, true, "cliente frecuente", Datos.Ahora);
        dev.Resolver(decision, Datos.Ahora);

        Assert.Equal(EstadoDeDevolucion.Aceptada, dev.Estado);
        Assert.Equal("super1", dev.Decision!.Supervisor.UsuarioId);
        Assert.IsType<DevolucionAceptada>(Assert.Single(dev.ExtraerEventos()));
    }

    [Fact]
    public void US13_Denegacion_de_excepcion_marca_Rechazada()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueAbierto, Datos.Ahora.AddDays(2));
        dev.ExtraerEventos();

        dev.Resolver(DecisionDeSupervisor.Registrar(Datos.Supervisor, false, "producto usado", Datos.Ahora), Datos.Ahora);

        Assert.Equal(EstadoDeDevolucion.Rechazada, dev.Estado);
        var evento = Assert.IsType<DevolucionRechazada>(Assert.Single(dev.ExtraerEventos()));
        Assert.Equal("producto usado", evento.Motivo);
    }

    [Fact]
    public void US13_Resolver_una_devolucion_ya_resuelta_lanza_TransicionDeEstadoInvalida()
    {
        var (laptop, venta) = VentaDeLaptop();
        var dev = Solicitar(laptop, venta, CondicionDelProducto.EmpaqueAbierto, Datos.Ahora.AddDays(2));
        var decision = DecisionDeSupervisor.Registrar(Datos.Supervisor, true, "ok", Datos.Ahora);
        dev.Resolver(decision, Datos.Ahora);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => dev.Resolver(decision, Datos.Ahora));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
    }

    [Fact]
    public void US16_Criterio_vigente_expone_las_tres_reglas_y_los_plazos()
    {
        var politica = Datos.Politica();
        var criterio = politica.CriterioDeDevolucionVigente();
        Assert.Collection(criterio.Reglas,
            r => Assert.IsType<ReglaDeComprobanteVigente>(r),
            r => Assert.Equal(15, ((ReglaDePlazoPorCategoria)r).PlazoEnDiasPorCategoria[CategoriaDeProducto.Computadoras]),
            r => Assert.IsType<ReglaDeCondicionDelProducto>(r));
        Assert.Equal(30, politica.PlazoDeDevolucionEnDiasPorCategoria[CategoriaDeProducto.Accesorios]);
        Assert.Throws<ArgumentException>(() => new CriterioDeDevolucion([]));
    }
}
