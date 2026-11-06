using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Estados;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

public class MaquinaDeEstadosPruebas
{
    [Theory]
    [InlineData(EstadoDeApartado.Vigente, EstadoDeApartado.Convertido, true)]
    [InlineData(EstadoDeApartado.Vigente, EstadoDeApartado.Vencido, true)]
    [InlineData(EstadoDeApartado.Vigente, EstadoDeApartado.Cancelado, true)]
    [InlineData(EstadoDeApartado.Vencido, EstadoDeApartado.Cancelado, false)]
    [InlineData(EstadoDeApartado.Convertido, EstadoDeApartado.Vigente, false)]
    [InlineData(EstadoDeApartado.Cancelado, EstadoDeApartado.Convertido, false)]
    public void Apartado_transiciones(EstadoDeApartado desde, EstadoDeApartado hacia, bool permitida) =>
        Assert.Equal(permitida, MaquinasDelDominio.Apartado.PuedeTransicionar(desde, hacia));

    [Theory]
    [InlineData(EstadoDeUnidad.Disponible, EstadoDeUnidad.Apartada, true)]
    [InlineData(EstadoDeUnidad.Disponible, EstadoDeUnidad.Vendida, true)]
    [InlineData(EstadoDeUnidad.Disponible, EstadoDeUnidad.EnTransito, true)]
    [InlineData(EstadoDeUnidad.Apartada, EstadoDeUnidad.Disponible, true)]
    [InlineData(EstadoDeUnidad.Apartada, EstadoDeUnidad.Vendida, true)]
    [InlineData(EstadoDeUnidad.EnTransito, EstadoDeUnidad.Disponible, true)]
    [InlineData(EstadoDeUnidad.Vendida, EstadoDeUnidad.Disponible, true)]
    [InlineData(EstadoDeUnidad.Vendida, EstadoDeUnidad.Defectuosa, true)]
    [InlineData(EstadoDeUnidad.Vendida, EstadoDeUnidad.Apartada, false)]
    [InlineData(EstadoDeUnidad.Defectuosa, EstadoDeUnidad.Disponible, false)]
    [InlineData(EstadoDeUnidad.Apartada, EstadoDeUnidad.EnTransito, false)]
    public void Unidad_transiciones(EstadoDeUnidad desde, EstadoDeUnidad hacia, bool permitida) =>
        Assert.Equal(permitida, MaquinasDelDominio.Unidad.PuedeTransicionar(desde, hacia));

    [Theory]
    [InlineData(EstadoDeDevolucion.EnRevision, EstadoDeDevolucion.Aceptada, true)]
    [InlineData(EstadoDeDevolucion.EnRevision, EstadoDeDevolucion.Rechazada, true)]
    [InlineData(EstadoDeDevolucion.Aceptada, EstadoDeDevolucion.Rechazada, false)]
    [InlineData(EstadoDeDevolucion.Rechazada, EstadoDeDevolucion.Aceptada, false)]
    public void Devolucion_transiciones(EstadoDeDevolucion desde, EstadoDeDevolucion hacia, bool permitida) =>
        Assert.Equal(permitida, MaquinasDelDominio.Devolucion.PuedeTransicionar(desde, hacia));

    [Fact]
    public void Transicionar_devuelve_el_destino_si_es_valida() =>
        Assert.Equal(EstadoDeApartado.Vencido,
            MaquinasDelDominio.Apartado.Transicionar(EstadoDeApartado.Vigente, EstadoDeApartado.Vencido));

    [Fact]
    public void Transicionar_invalida_lanza_TransicionDeEstadoInvalida()
    {
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() =>
            MaquinasDelDominio.Devolucion.Transicionar(EstadoDeDevolucion.Aceptada, EstadoDeDevolucion.Rechazada));
        Assert.Equal(MotivoDeRechazo.TransicionDeEstadoInvalida, ex.Motivo);
    }
}
