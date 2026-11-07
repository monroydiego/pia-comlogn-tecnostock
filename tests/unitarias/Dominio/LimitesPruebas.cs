using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

/// <summary>Los límites del dominio coinciden con las columnas de la base de datos: lo que se pasa, no revienta al guardar.</summary>
public class LimitesPruebas
{
    private static string Texto(int largo) => new('x', largo);

    [Fact]
    public void Folio_y_codigos_aceptan_hasta_30_caracteres_y_rechazan_mas()
    {
        Assert.Equal(30, new Folio(Texto(30)).Valor.Length);
        Assert.Equal(30, new ProductoId(Texto(30)).Valor.Length);
        Assert.Equal(30, new SucursalId(Texto(30)).Valor.Length);
        Assert.Equal(30, new ClienteId(Texto(30)).Valor.Length);
        Assert.Throws<ArgumentException>(() => new Folio(Texto(31)));
        Assert.Throws<ArgumentException>(() => new ProductoId(Texto(31)));
        Assert.Throws<ArgumentException>(() => new SucursalId(Texto(31)));
        Assert.Throws<ArgumentException>(() => new ClienteId(Texto(31)));
    }

    [Fact]
    public void Los_identificadores_no_pueden_estar_vacios()
    {
        Assert.Throws<ArgumentException>(() => new ProductoId(" "));
        Assert.Throws<ArgumentException>(() => new SucursalId(""));
        Assert.Throws<ArgumentException>(() => new ClienteId(null!));
    }

    [Fact]
    public void Serie_y_usuario_aceptan_hasta_60_caracteres()
    {
        Assert.Equal(60, new NumeroDeSerie(Texto(60)).Valor.Length);
        Assert.Throws<ArgumentException>(() => new NumeroDeSerie(Texto(61)));
        Assert.Equal(60, new Operador(Texto(60), Rol.Cajero).UsuarioId.Length);
        var ex = Assert.Throws<ArgumentException>(() => new Operador(Texto(61), Rol.Cajero));
        Assert.Contains("60", ex.Message);
    }

    [Fact]
    public void Motivo_de_devolucion_limita_causal_y_detalle()
    {
        _ = new MotivoDeDevolucion(Texto(100), Texto(500));
        Assert.Throws<ArgumentException>(() => new MotivoDeDevolucion(Texto(101)));
        Assert.Throws<ArgumentException>(() => new MotivoDeDevolucion("Falla", Texto(501)));
    }

    [Fact]
    public void Decision_del_Supervisor_limita_el_motivo_a_500_caracteres()
    {
        var supervisor = new Operador("super1", Rol.Supervisor);
        _ = DecisionDeSupervisor.Registrar(supervisor, true, Texto(500), DateTime.UtcNow);
        Assert.Throws<ArgumentException>(() => DecisionDeSupervisor.Registrar(supervisor, true, Texto(501), DateTime.UtcNow));
    }

    [Fact]
    public void Dinero_rechaza_importes_que_no_caben_en_decimal_18_2()
    {
        Assert.Equal(Limites.MontoMaximo, new Dinero(Limites.MontoMaximo).Monto);
        Assert.Throws<ArgumentException>(() => new Dinero(Limites.MontoMaximo + 1m));
        Assert.Throws<ArgumentException>(() => new Dinero(100_000_000_000_000_000_000m));
    }
}
