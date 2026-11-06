using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

public class UnidadInventarioPruebas
{
    [Fact]
    public void US17_Alta_de_unidad_con_serie_queda_Disponible()
    {
        var laptop = Datos.Laptop();
        var unidad = UnidadInventario.Registrar(UnidadId.Nuevo(), laptop.Id, Datos.Suc1, new NumeroDeSerie("sn-77"));
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
        Assert.Equal(new NumeroDeSerie("SN-77"), unidad.Serie);
    }

    [Fact]
    public void US06_Intento_de_venta_de_unidad_apartada_se_bloquea_con_UnidadApartada()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        Datos.Apartado(laptop, unidad);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => unidad.Vender(new Folio("VTA-2"), Datos.Suc1));
        Assert.Equal(MotivoDeRechazo.UnidadApartada, ex.Motivo);
        Assert.Equal(EstadoDeUnidad.Apartada, unidad.Estado);
    }

    [Fact]
    public void Vender_una_unidad_vendida_o_defectuosa_lanza_UnidadNoDisponible()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        unidad.Vender(new Folio("VTA-1"), Datos.Suc1);
        Assert.Equal(EstadoDeUnidad.Vendida, unidad.Estado);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => unidad.Vender(new Folio("VTA-2"), Datos.Suc1));
        Assert.Equal(MotivoDeRechazo.UnidadNoDisponible, ex.Motivo);
    }

    [Fact]
    public void Vender_en_otra_sucursal_lanza_UnidadEnOtraSucursal()
    {
        var unidad = Datos.Unidad(Datos.Laptop(), "SN-5", Datos.Suc2);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => unidad.Vender(new Folio("VTA-2"), Datos.Suc1));
        Assert.Equal(MotivoDeRechazo.UnidadEnOtraSucursal, ex.Motivo);
    }

    [Fact]
    public void LiberarApartado_con_otro_folio_se_rechaza()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop);
        Datos.Apartado(laptop, unidad);
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => unidad.LiberarApartado(new Folio("OTRO")));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
    }

    [Fact]
    public void RN_INV_004_Devolucion_con_empaque_cerrado_regresa_a_Disponible()
    {
        var unidad = Datos.Unidad(Datos.Laptop());
        unidad.Vender(new Folio("VTA-1"), Datos.Suc1);
        unidad.RegistrarDevolucion(new Folio("DEV-1"), CondicionDelProducto.EmpaqueCerrado);
        Assert.Equal(EstadoDeUnidad.Disponible, unidad.Estado);
    }

    [Theory]
    [InlineData(CondicionDelProducto.EmpaqueAbierto)]
    [InlineData(CondicionDelProducto.Defectuoso)]
    public void RN_INV_004_Devolucion_con_empaque_abierto_o_defecto_queda_Defectuosa(CondicionDelProducto condicion)
    {
        var unidad = Datos.Unidad(Datos.Laptop());
        unidad.Vender(new Folio("VTA-1"), Datos.Suc1);
        unidad.RegistrarDevolucion(new Folio("DEV-1"), condicion);
        Assert.Equal(EstadoDeUnidad.Defectuosa, unidad.Estado);
    }

    [Fact]
    public void Registrar_devolucion_de_una_unidad_no_vendida_es_transicion_invalida()
    {
        var unidad = Datos.Unidad(Datos.Laptop());
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => unidad.RegistrarDevolucion(new Folio("DEV-1"), CondicionDelProducto.EmpaqueCerrado));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
    }

    [Fact]
    public void VenderDesdeApartado_valida_serie_y_folio()
    {
        var laptop = Datos.Laptop();
        var unidad = Datos.Unidad(laptop, "SN-1");
        var apartado = Datos.Apartado(laptop, unidad);

        Assert.Equal(MotivoDeRechazo.SerieRequerida,
            Assert.Throws<ReglaDeNegocioViolada>(() => unidad.VenderDesdeApartado(apartado.Folio, null)).Motivo);
        Assert.Equal(MotivoDeRechazo.SerieNoCoincide,
            Assert.Throws<ReglaDeNegocioViolada>(() => unidad.VenderDesdeApartado(apartado.Folio, new NumeroDeSerie("SN-2"))).Motivo);
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida,
            Assert.Throws<ReglaDeNegocioViolada>(() => unidad.VenderDesdeApartado(new Folio("OTRO"), new NumeroDeSerie("SN-1"))).Motivo);

        unidad.VenderDesdeApartado(apartado.Folio, new NumeroDeSerie("sn-1"));
        Assert.Equal(EstadoDeUnidad.Vendida, unidad.Estado);
        Assert.Null(unidad.FolioDeApartadoVigente);
    }

    [Fact]
    public void Unidad_sin_serie_se_vende_desde_apartado_sin_serie_capturada()
    {
        var teclado = Producto.Crear(new ProductoId("TEC-P099"), "TEC-P099", "Monitor X", CategoriaDeProducto.Accesorios, false, true, new Dinero(100m));
        var unidad = Datos.Unidad(teclado, null);
        var apartado = Datos.Apartado(teclado, unidad, 100m, 30m);
        unidad.VenderDesdeApartado(apartado.Folio, null);
        Assert.Equal(EstadoDeUnidad.Vendida, unidad.Estado);
    }
}
