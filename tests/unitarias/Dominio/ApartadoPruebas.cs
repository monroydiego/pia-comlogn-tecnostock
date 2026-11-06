using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

/// <summary>US01, US03, US14, US20 a nivel de dominio.</summary>
public class ApartadoPruebas
{
    private static ReglaDeNegocioViolada Rechazo(Action accion) => Assert.Throws<ReglaDeNegocioViolada>(accion);

    [Fact]
    public void US01_Creacion_de_un_apartado()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var apartado = Datos.Apartado(laptop, unidad, 1000m, 300m);

        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Apartada, unidad.Estado);
        Assert.Equal(apartado.Folio, unidad.FolioDeApartadoVigente);
        Assert.Equal(unidad.Id, apartado.UnidadId);
        Assert.Equal(Datos.Suc1, apartado.SucursalId);
    }

    [Fact]
    public void US03_Confirmacion_con_folio_fecha_limite_y_saldo_pendiente()
    {
        var laptop = Datos.Laptop();
        var apartado = Datos.Apartado(laptop, Datos.Unidad(laptop), 1000m, 300m);

        var evento = Assert.IsType<ApartadoCreado>(Assert.Single(apartado.ExtraerEventos()));
        Assert.Equal(Datos.Ahora.AddDays(7), evento.FechaLimite);
        Assert.Equal(new Folio("APT-1"), evento.Folio);
        Assert.Equal(700m, apartado.SaldoPendiente().Monto);
        Assert.Empty(apartado.ExtraerEventos());
    }

    [Fact]
    public void US01_Anticipo_insuficiente_y_la_unidad_sigue_disponible()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var ex = Rechazo(() => Datos.Apartado(laptop, unidad, 1000m, 299.99m));
        Assert.Equal(MotivoDeRechazo.AnticipoInsuficiente, ex.Motivo);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
    }

    [Fact]
    public void US01_Anticipo_exactamente_en_el_minimo_se_acepta()
    {
        var laptop = Datos.Laptop();
        Assert.Equal(EstadoDeApartado.Vigente, Datos.Apartado(laptop, Datos.Unidad(laptop), 1000m, 300m).Estado);
    }

    [Fact]
    public void US01_Producto_no_apartable()
    {
        var teclado = Datos.Teclado();
        var ex = Rechazo(() => Datos.Apartado(teclado, Datos.Unidad(teclado, null), 500m, 200m));
        Assert.Equal(MotivoDeRechazo.ProductoNoApartable, ex.Motivo);
    }

    [Fact]
    public void US11_Metodo_de_pago_no_autorizado_en_el_anticipo_no_crea_el_apartado()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var politica = PoliticaComercial.Crear(30m, 7, [MetodoDePago.Efectivo], Datos.Politica().PlazoDeDevolucionEnDiasPorCategoria);
        var ex = Rechazo(() => Apartado.Crear(new Folio("APT-1"), Datos.Cliente, unidad, laptop, new Dinero(1000m),
            Datos.Pago(300m, MetodoDePago.Tarjeta), politica, Datos.Ahora));
        Assert.Equal(MotivoDeRechazo.MetodoDePagoNoAutorizado, ex.Motivo);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
    }

    [Fact]
    public void Apartar_una_unidad_de_otra_sucursal_se_rechaza()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop, "SN-X", Datos.Suc2);
        var ex = Rechazo(() => unidad.Apartar(new Folio("APT-9"), Datos.Suc1));
        Assert.Equal(MotivoDeRechazo.UnidadEnOtraSucursal, ex.Motivo);
    }

    [Fact]
    public void US20_Vencimiento_libera_la_unidad_y_registra_el_evento()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var apartado = Datos.Apartado(laptop, unidad);
        apartado.ExtraerEventos();

        var ahora = Datos.Ahora.AddDays(7);
        apartado.Vencer(ahora);
        unidad.LiberarApartado(apartado.Folio);

        Assert.Equal(EstadoDeApartado.Vencido, apartado.Estado);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
        Assert.Null(unidad.FolioDeApartadoVigente);
        var evento = Assert.IsType<ApartadoVencido>(Assert.Single(apartado.ExtraerEventos()));
        Assert.Equal(unidad.Id, evento.UnidadId);
    }

    [Fact]
    public void US20_Apartado_vigente_no_se_puede_vencer_antes_de_su_fecha()
    {
        var laptop = Datos.Laptop();
        var apartado = Datos.Apartado(laptop, Datos.Unidad(laptop));
        var ex = Rechazo(() => apartado.Vencer(Datos.Ahora.AddDays(3)));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
    }

    [Fact]
    public void US14_Cancelacion_manual_por_Supervisor_libera_la_unidad()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var apartado = Datos.Apartado(laptop, unidad);

        var decision = DecisionDeSupervisor.Registrar(Datos.Supervisor, true, "el cliente lo pidió", Datos.Ahora);
        apartado.Cancelar(decision);
        unidad.LiberarApartado(apartado.Folio);

        Assert.Equal(EstadoDeApartado.Cancelado, apartado.Estado);
        Assert.Equal("el cliente lo pidió", apartado.Cancelacion!.Motivo);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
    }

    [Fact]
    public void US14_Cancelacion_por_operador_sin_rol_de_Supervisor()
    {
        var ex = Rechazo(() => DecisionDeSupervisor.Registrar(Datos.Vendedor, true, "x", Datos.Ahora));
        Assert.Equal(MotivoDeRechazo.SoloSupervisorAutoriza, ex.Motivo);
    }

    [Fact]
    public void US14_Cancelar_un_apartado_que_no_esta_vigente()
    {
        var laptop = Datos.Laptop();
        var apartado = Datos.Apartado(laptop, Datos.Unidad(laptop));
        apartado.Vencer(Datos.Ahora.AddDays(8));
        var decision = DecisionDeSupervisor.Registrar(Datos.Supervisor, true, "x", Datos.Ahora);
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, Rechazo(() => apartado.Cancelar(decision)).Motivo);
    }

    [Fact]
    public void Cancelar_con_una_decision_que_no_autoriza_se_rechaza()
    {
        var laptop = Datos.Laptop();
        var apartado = Datos.Apartado(laptop, Datos.Unidad(laptop));
        var denegada = DecisionDeSupervisor.Registrar(Datos.Supervisor, false, "no procede", Datos.Ahora);
        Assert.Equal(MotivoDeRechazo.SoloSupervisorAutoriza, Rechazo(() => apartado.Cancelar(denegada)).Motivo);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
    }
}
