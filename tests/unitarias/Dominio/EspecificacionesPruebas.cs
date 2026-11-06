using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

public class EspecificacionesPruebas
{
    private static readonly IReadOnlyDictionary<CategoriaDeProducto, int> Plazos = Datos.Politica().PlazoDeDevolucionEnDiasPorCategoria;

    [Fact]
    public void ProductoApartable_cumple_con_Laptop_y_no_con_Teclado()
    {
        var spec = new EspecificacionDeProductoApartable();
        Assert.True(spec.EsSatisfechaPor(Datos.Laptop()).Cumple);
        var r = spec.EsSatisfechaPor(Datos.Teclado());
        Assert.False(r.Cumple);
        Assert.Contains("Teclado 04", r.Razon);
    }

    [Fact]
    public void UnidadDisponibleParaVenta_solo_cumple_en_estado_Disponible()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        var spec = new EspecificacionDeUnidadDisponibleParaVenta();
        Assert.True(spec.EsSatisfechaPor(unidad).Cumple);
        unidad.Apartar(new Folio("APT-1"), Datos.Suc1);
        Assert.False(spec.EsSatisfechaPor(unidad).Cumple);
    }

    [Fact]
    public void ApartadoVigente_depende_del_estado_y_de_la_fecha()
    {
        var laptop = Datos.Laptop();
        var apartado = Datos.Apartado(laptop, Datos.Unidad(laptop));
        Assert.True(new EspecificacionDeApartadoVigente(Datos.Ahora.AddDays(1)).EsSatisfechaPor(apartado).Cumple);
        Assert.False(new EspecificacionDeApartadoVigente(Datos.Ahora.AddDays(8)).EsSatisfechaPor(apartado).Cumple);
    }

    [Fact]
    public void MetodoDePagoAutorizado_rechaza_metodos_fuera_de_la_lista()
    {
        var spec = new EspecificacionDeMetodoDePagoAutorizado([MetodoDePago.Efectivo]);
        Assert.True(spec.EsSatisfechaPor(Datos.Pago(1m, MetodoDePago.Efectivo)).Cumple);
        Assert.False(spec.EsSatisfechaPor(Datos.Pago(1m, MetodoDePago.Tarjeta)).Cumple);
    }

    [Fact]
    public void ComprobanteValido_exige_venta_y_linea()
    {
        var laptop = Datos.Laptop();
        var venta = Datos.Venta(laptop, Datos.Unidad(laptop));
        var spec = new EspecificacionDeComprobanteValido();
        var linea = venta.Lineas[0];
        Assert.True(spec.EsSatisfechaPor(new SolicitudDeDevolucion(venta, linea, laptop, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora)).Cumple);
        Assert.False(spec.EsSatisfechaPor(new SolicitudDeDevolucion(null, null, laptop, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora)).Cumple);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(16, false)]
    public void DentroDePlazo_Computadoras_15_dias(int dias, bool cumple)
    {
        var laptop = Datos.Laptop();
        var venta = Datos.Venta(laptop, Datos.Unidad(laptop));
        var s = new SolicitudDeDevolucion(venta, venta.Lineas[0], laptop, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora.AddDays(dias));
        Assert.Equal(cumple, new EspecificacionDeDevolucionDentroDePlazo(Plazos).EsSatisfechaPor(s).Cumple);
    }

    [Fact]
    public void DentroDePlazo_Accesorios_30_dias_y_casos_sin_datos()
    {
        var teclado = Datos.Teclado();
        var venta = Datos.Venta(teclado, Datos.Unidad(teclado, null));
        var spec = new EspecificacionDeDevolucionDentroDePlazo(Plazos);
        Assert.True(spec.EsSatisfechaPor(new SolicitudDeDevolucion(venta, venta.Lineas[0], teclado, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora.AddDays(30))).Cumple);
        Assert.False(spec.EsSatisfechaPor(new SolicitudDeDevolucion(null, null, teclado, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora)).Cumple);
        var sinPlazo = new EspecificacionDeDevolucionDentroDePlazo(new Dictionary<CategoriaDeProducto, int>());
        Assert.False(sinPlazo.EsSatisfechaPor(new SolicitudDeDevolucion(venta, venta.Lineas[0], teclado, CondicionDelProducto.EmpaqueCerrado, Datos.Ahora)).Cumple);
    }

    [Theory]
    [InlineData(CondicionDelProducto.EmpaqueCerrado, true)]
    [InlineData(CondicionDelProducto.Defectuoso, true)]
    [InlineData(CondicionDelProducto.EmpaqueAbierto, false)]
    public void CondicionAceptable_empaque_abierto_es_excepcion(CondicionDelProducto condicion, bool cumple) =>
        Assert.Equal(cumple, new EspecificacionDeCondicionAceptable()
            .EsSatisfechaPor(new SolicitudDeDevolucion(null, null, Datos.Laptop(), condicion, Datos.Ahora)).Cumple);

    // ---- Composición Y, O, No (ADR-007) ----
    private static readonly IEspecificacion<Producto> Apartable = new EspecificacionDeProductoApartable();
    private static readonly IEspecificacion<Producto> ConSerie = new EspecificacionConSerie();

    private sealed class EspecificacionConSerie : IEspecificacion<Producto>
    {
        public ResultadoDeRegla EsSatisfechaPor(Producto p) =>
            p.RequiereSerie ? ResultadoDeRegla.Satisfecha() : ResultadoDeRegla.Incumplida("no lleva serie");
    }

    [Fact]
    public void Y_exige_ambas_y_reune_las_razones()
    {
        Assert.True(Apartable.Y(ConSerie).EsSatisfechaPor(Datos.Laptop()).Cumple);
        var r = Apartable.Y(ConSerie).EsSatisfechaPor(Datos.Teclado());
        Assert.False(r.Cumple);
        Assert.Contains("no se puede apartar", r.Razon);
        Assert.Contains("no lleva serie", r.Razon);
    }

    [Fact]
    public void O_basta_con_una()
    {
        var teclado = Datos.Teclado();
        Assert.True(Apartable.O(ConSerie).EsSatisfechaPor(Datos.Laptop()).Cumple);
        Assert.True(ConSerie.O(Apartable).EsSatisfechaPor(Datos.Laptop()).Cumple);
        var sinTeclado = Apartable.O(ConSerie).EsSatisfechaPor(teclado);
        Assert.False(sinTeclado.Cumple);
        Assert.Contains("no lleva serie", sinTeclado.Razon);
    }

    [Fact]
    public void No_invierte_el_resultado()
    {
        Assert.True(Apartable.No().EsSatisfechaPor(Datos.Teclado()).Cumple);
        Assert.False(Apartable.No().EsSatisfechaPor(Datos.Laptop()).Cumple);
    }

    [Fact]
    public void Composiciones_se_pueden_anidar()
    {
        // apartable Y (con serie O NO apartable): Laptop cumple; Teclado no (no apartable).
        var compuesta = Apartable.Y(ConSerie.O(Apartable.No()));
        Assert.True(compuesta.EsSatisfechaPor(Datos.Laptop()).Cumple);
        Assert.False(compuesta.EsSatisfechaPor(Datos.Teclado()).Cumple);
    }
}
