using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Politicas;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Parámetros vigentes del negocio (G-38). Los valores por defecto son [Supuesto] (BRD 7.1).</summary>
public sealed class PoliticaComercial : RaizDeAgregado
{
    public int Id { get; private set; } = 1;
    /// <summary>Porcentaje del precio como anticipo mínimo, en porcentaje (30 = 30%).</summary>
    public decimal PorcentajeDeAnticipoMinimo { get; private set; }
    public int PlazoMaximoDeApartadoEnDias { get; private set; }
    public IReadOnlyList<MetodoDePago> MetodosAutorizados { get; private set; } = [];
    public IReadOnlyDictionary<CategoriaDeProducto, int> PlazoDeDevolucionEnDiasPorCategoria { get; private set; } =
        new Dictionary<CategoriaDeProducto, int>();

    private PoliticaComercial() { }

    public static PoliticaComercial Crear(decimal porcentajeDeAnticipoMinimo, int plazoMaximoDeApartadoEnDias,
        IEnumerable<MetodoDePago> metodosAutorizados, IReadOnlyDictionary<CategoriaDeProducto, int> plazoDeDevolucionEnDiasPorCategoria)
    {
        if (porcentajeDeAnticipoMinimo is <= 0 or > 100) throw new ArgumentException("El porcentaje de anticipo debe estar entre 0 y 100.");
        if (plazoMaximoDeApartadoEnDias <= 0) throw new ArgumentException("La vigencia del apartado debe ser positiva.");
        var metodos = metodosAutorizados.Distinct().ToList();
        if (metodos.Count == 0) throw new ArgumentException("Debe haber al menos un método de pago autorizado.");
        return new PoliticaComercial
        {
            PorcentajeDeAnticipoMinimo = porcentajeDeAnticipoMinimo,
            PlazoMaximoDeApartadoEnDias = plazoMaximoDeApartadoEnDias,
            MetodosAutorizados = metodos,
            PlazoDeDevolucionEnDiasPorCategoria = new Dictionary<CategoriaDeProducto, int>(plazoDeDevolucionEnDiasPorCategoria)
        };
    }

    /// <summary>Valores [Supuesto] del BRD 7.1: anticipo 30%, 7 días, devolución 15/30 días, Efectivo, Tarjeta y Transferencia.</summary>
    public static PoliticaComercial PorDefecto() => Crear(30m, 7,
        [MetodoDePago.Efectivo, MetodoDePago.Tarjeta, MetodoDePago.Transferencia],
        new Dictionary<CategoriaDeProducto, int>
        {
            [CategoriaDeProducto.Computadoras] = 15,
            [CategoriaDeProducto.Celulares] = 15,
            [CategoriaDeProducto.Accesorios] = 30,
            [CategoriaDeProducto.Componentes] = 30
        });

    public Dinero AnticipoMinimoSobre(Dinero precio) => precio.Porcentaje(PorcentajeDeAnticipoMinimo);

    public VigenciaDeApartado NuevaVigenciaDesde(DateTime ahora) => new(ahora, ahora.AddDays(PlazoMaximoDeApartadoEnDias));

    /// <summary>RN-VTA-002: lanza MetodoDePagoNoAutorizado si el pago no usa un método autorizado.</summary>
    public void ValidarPago(PagoRecibido pago)
    {
        var resultado = new EspecificacionDeMetodoDePagoAutorizado(MetodosAutorizados).EsSatisfechaPor(pago);
        if (!resultado.Cumple)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.MetodoDePagoNoAutorizado, resultado.Razon);
    }

    public CriterioDeDevolucion CriterioDeDevolucionVigente() => CriterioDeDevolucion.Estandar(PlazoDeDevolucionEnDiasPorCategoria);
}
