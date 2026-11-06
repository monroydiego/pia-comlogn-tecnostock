using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

public class PoliticaYProductoPruebas
{
    [Fact]
    public void Politica_por_defecto_trae_los_valores_supuestos_del_BRD()
    {
        var p = PoliticaComercial.PorDefecto();
        Assert.Equal(30m, p.PorcentajeDeAnticipoMinimo);
        Assert.Equal(7, p.PlazoMaximoDeApartadoEnDias);
        Assert.Equal(3, p.MetodosAutorizados.Count);
        Assert.Equal(300m, p.AnticipoMinimoSobre(new Dinero(1000m)).Monto);
        Assert.Equal(Datos.Ahora.AddDays(7), p.NuevaVigenciaDesde(Datos.Ahora).Hasta);
        Assert.Equal(15, p.PlazoDeDevolucionEnDiasPorCategoria[CategoriaDeProducto.Celulares]);
        Assert.Equal(30, p.PlazoDeDevolucionEnDiasPorCategoria[CategoriaDeProducto.Componentes]);
    }

    [Fact]
    public void Politica_es_configurable()
    {
        var p = PoliticaComercial.Crear(20m, 5, [MetodoDePago.Efectivo], new Dictionary<CategoriaDeProducto, int>());
        Assert.Equal(200m, p.AnticipoMinimoSobre(new Dinero(1000m)).Monto);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => p.ValidarPago(Datos.Pago(1m, MetodoDePago.Tarjeta)));
        Assert.Equal(MotivoDeRechazo.MetodoDePagoNoAutorizado, ex.Motivo);
        p.ValidarPago(Datos.Pago(1m));
    }

    [Theory]
    [InlineData(0, 7, 1)]
    [InlineData(101, 7, 1)]
    [InlineData(30, 0, 1)]
    [InlineData(30, 7, 0)]
    public void Politica_con_parametros_invalidos_se_rechaza(double porcentaje, int dias, int metodos)
    {
        var lista = metodos == 0 ? Array.Empty<MetodoDePago>() : [MetodoDePago.Efectivo];
        Assert.Throws<ArgumentException>(() =>
            PoliticaComercial.Crear((decimal)porcentaje, dias, lista, new Dictionary<CategoriaDeProducto, int>()));
    }

    [Fact]
    public void Producto_valida_sku_y_nombre()
    {
        Assert.Throws<ArgumentException>(() => Producto.Crear(new ProductoId("X"), " ", "n", CategoriaDeProducto.Accesorios, false, false, new Dinero(1m)));
        Assert.Throws<ArgumentException>(() => Producto.Crear(new ProductoId("X"), "s", " ", CategoriaDeProducto.Accesorios, false, false, new Dinero(1m)));
        var p = Datos.Laptop();
        Assert.True(p.RequiereSerie);
        Assert.True(p.PermiteApartado);
    }
}
