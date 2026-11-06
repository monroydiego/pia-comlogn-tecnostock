using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Especificaciones;

/// <summary>RN-APT-004: solo los productos apartables pueden apartarse.</summary>
public sealed class EspecificacionDeProductoApartable : IEspecificacion<Producto>
{
    public ResultadoDeRegla EsSatisfechaPor(Producto producto) =>
        producto.PermiteApartado
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida($"El producto {producto.Nombre} no se puede apartar.");
}

/// <summary>RN-APT-003 y G-20: solo una unidad Disponible se puede vender o apartar.</summary>
public sealed class EspecificacionDeUnidadDisponibleParaVenta : IEspecificacion<UnidadInventario>
{
    public ResultadoDeRegla EsSatisfechaPor(UnidadInventario unidad) =>
        unidad.Estado == EstadoDeUnidad.Disponible
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida($"La unidad está {unidad.Estado} y no se puede vender.");
}

/// <summary>RN-APT-002: el apartado está Vigente y no ha alcanzado su fecha límite.</summary>
public sealed class EspecificacionDeApartadoVigente(DateTime ahora) : IEspecificacion<Apartado>
{
    public ResultadoDeRegla EsSatisfechaPor(Apartado apartado) =>
        apartado.Estado == EstadoDeApartado.Vigente && !apartado.Vigencia.HaVencido(ahora)
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida("El apartado no está vigente.");
}

/// <summary>RN-VTA-002: solo métodos de pago autorizados por la política.</summary>
public sealed class EspecificacionDeMetodoDePagoAutorizado(IEnumerable<MetodoDePago> autorizados) : IEspecificacion<PagoRecibido>
{
    private readonly HashSet<MetodoDePago> _autorizados = autorizados.ToHashSet();

    public ResultadoDeRegla EsSatisfechaPor(PagoRecibido pago) =>
        _autorizados.Contains(pago.Metodo)
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida($"El método de pago {pago.Metodo} no está autorizado.");
}

/// <summary>Datos que evalúan las reglas de devolución.</summary>
public sealed record SolicitudDeDevolucion(
    Venta? Venta, LineaDeVenta? Linea, Producto Producto, CondicionDelProducto Condicion, DateTime Ahora);

/// <summary>RN-DEV-001: la venta existe y la línea que se devuelve pertenece a ella.</summary>
public sealed class EspecificacionDeComprobanteValido : IEspecificacion<SolicitudDeDevolucion>
{
    public ResultadoDeRegla EsSatisfechaPor(SolicitudDeDevolucion solicitud) =>
        solicitud.Venta is not null && solicitud.Linea is not null
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida("El comprobante de compra no es válido.");
}

/// <summary>RN-DEV-002: la devolución procede dentro del plazo de la categoría (días naturales).</summary>
public sealed class EspecificacionDeDevolucionDentroDePlazo(IReadOnlyDictionary<CategoriaDeProducto, int> plazoEnDiasPorCategoria)
    : IEspecificacion<SolicitudDeDevolucion>
{
    public ResultadoDeRegla EsSatisfechaPor(SolicitudDeDevolucion solicitud)
    {
        if (solicitud.Venta is null) return ResultadoDeRegla.Incumplida("No hay venta contra la cual medir el plazo.");
        if (!plazoEnDiasPorCategoria.TryGetValue(solicitud.Producto.Categoria, out var plazo))
            return ResultadoDeRegla.Incumplida($"La categoría {solicitud.Producto.Categoria} no tiene plazo de devolución.");
        var dias = (solicitud.Ahora - solicitud.Venta.FechaDeCierre).TotalDays;
        return dias <= plazo
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida($"La compra excede el plazo de {plazo} días de la categoría {solicitud.Producto.Categoria}.");
    }
}

/// <summary>RN-DEV-003: la devolución estándar acepta empaque cerrado o defectuoso; empaque abierto es excepción.</summary>
public sealed class EspecificacionDeCondicionAceptable : IEspecificacion<SolicitudDeDevolucion>
{
    public ResultadoDeRegla EsSatisfechaPor(SolicitudDeDevolucion solicitud) =>
        solicitud.Condicion != CondicionDelProducto.EmpaqueAbierto
            ? ResultadoDeRegla.Satisfecha()
            : ResultadoDeRegla.Incumplida("El producto se devuelve con el empaque abierto.");
}
