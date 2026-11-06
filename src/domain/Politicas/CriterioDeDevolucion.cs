using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Especificaciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Politicas;

/// <summary>Regla de devolución intercambiable (ADR-005); se apoya en una Specification (ADR-007).</summary>
public interface ReglaDeDevolucion
{
    ResultadoDeRegla Evaluar(Venta? venta, LineaDeVenta? linea, Producto producto, CondicionDelProducto condicion, DateTime ahora);
}

public abstract class ReglaBasadaEnEspecificacion(IEspecificacion<SolicitudDeDevolucion> especificacion) : ReglaDeDevolucion
{
    public ResultadoDeRegla Evaluar(Venta? venta, LineaDeVenta? linea, Producto producto, CondicionDelProducto condicion, DateTime ahora) =>
        especificacion.EsSatisfechaPor(new SolicitudDeDevolucion(venta, linea, producto, condicion, ahora));
}

public sealed class ReglaDeComprobanteVigente() : ReglaBasadaEnEspecificacion(new EspecificacionDeComprobanteValido());

public sealed class ReglaDePlazoPorCategoria(IReadOnlyDictionary<CategoriaDeProducto, int> plazoEnDiasPorCategoria)
    : ReglaBasadaEnEspecificacion(new EspecificacionDeDevolucionDentroDePlazo(plazoEnDiasPorCategoria))
{
    public IReadOnlyDictionary<CategoriaDeProducto, int> PlazoEnDiasPorCategoria { get; } = plazoEnDiasPorCategoria;
}

public sealed class ReglaDeCondicionDelProducto() : ReglaBasadaEnEspecificacion(new EspecificacionDeCondicionAceptable());

/// <summary>Conjunto de reglas de devolución (G-39). Evalúa todas y reúne el dictamen sin cortocircuitar.</summary>
public sealed class CriterioDeDevolucion
{
    private readonly List<ReglaDeDevolucion> _reglas;

    public CriterioDeDevolucion(IEnumerable<ReglaDeDevolucion> reglas)
    {
        _reglas = reglas.ToList();
        if (_reglas.Count == 0) throw new ArgumentException("El criterio requiere al menos una regla.", nameof(reglas));
    }

    public IReadOnlyList<ReglaDeDevolucion> Reglas => _reglas;

    public static CriterioDeDevolucion Estandar(IReadOnlyDictionary<CategoriaDeProducto, int> plazos) =>
        new([new ReglaDeComprobanteVigente(), new ReglaDePlazoPorCategoria(plazos), new ReglaDeCondicionDelProducto()]);

    public DictamenDeDevolucion Evaluar(Venta? venta, LineaDeVenta? linea, Producto producto, CondicionDelProducto condicion, DateTime ahora) =>
        new(_reglas.Select(r => r.Evaluar(venta, linea, producto, condicion, ahora)));
}
