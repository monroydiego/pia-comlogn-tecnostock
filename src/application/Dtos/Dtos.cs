using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Aplicacion.Dtos;

public sealed record DisponibilidadDeProductoDto(string Producto, string Sucursal, int CantidadDisponible);

public sealed record DisponibilidadPorSucursalDto(string Sucursal, int CantidadDisponible);

public sealed record EstadoDeDevolucionDto(string Folio, string Estado, string? Motivo, string? FormaDeCompensacion)
{
    /// <summary>Motivo = la decisión del Supervisor si existe; si no, la causal que capturó el Vendedor.</summary>
    public static EstadoDeDevolucionDto Desde(Devolucion d) => new(d.Folio.Valor, d.Estado.ToString(),
        d.Decision?.Motivo ?? d.Motivo.Causal,
        d.Estado == EstadoDeDevolucion.Aceptada ? d.FormaDeCompensacion.ToString() : null);
}

public sealed record CriterioDeDevolucionDto(IReadOnlyList<string> Condiciones, IReadOnlyDictionary<string, int> PlazoPorCategoria)
{
    public static CriterioDeDevolucionDto Desde(PoliticaComercial politica) => new(
        ["Comprobante de compra válido", "Dentro del plazo de la categoría", "Empaque cerrado o producto defectuoso"],
        politica.PlazoDeDevolucionEnDiasPorCategoria.ToDictionary(p => p.Key.ToString(), p => p.Value));
}

public sealed record ConfirmacionDeApartadoDto(string Folio, DateTime FechaLimite, decimal SaldoPendiente);

public sealed record LineaRegistradaDto(string ProductoId, string? Serie, decimal PrecioUnitario);

public sealed record VentaRegistradaDto(string Folio, decimal Total, IReadOnlyList<LineaRegistradaDto> Lineas, string? FolioDeApartadoOrigen)
{
    public static VentaRegistradaDto Desde(Venta v) => new(v.Folio.Valor, v.Total().Monto,
        v.Lineas.Select(l => new LineaRegistradaDto(l.ProductoId.Valor, l.Serie?.Valor, l.PrecioUnitario.Monto)).ToList(),
        v.FolioDeApartadoOrigen?.Valor);
}

public sealed record UnidadRegistradaDto(Guid UnidadId, string ProductoId, string Sucursal, string? Serie, string Estado);

public sealed record ResumenDeVencimientoDto(int ApartadosVencidos);

public sealed record CancelacionDeApartadoDto(string Folio, string Estado);
