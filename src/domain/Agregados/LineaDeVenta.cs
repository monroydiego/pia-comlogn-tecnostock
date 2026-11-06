using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Renglón de una venta con el producto, la unidad exacta y la serie capturada (G-34).</summary>
public sealed class LineaDeVenta
{
    public Guid Id { get; private set; }
    public ProductoId ProductoId { get; private set; }
    public UnidadId UnidadId { get; private set; }
    public NumeroDeSerie? Serie { get; private set; }
    public Dinero PrecioUnitario { get; private set; }

    private LineaDeVenta() { }

    /// <summary>RN-VTA-001 (G-19): un producto serializable exige la serie y debe coincidir con la unidad.</summary>
    public static LineaDeVenta Crear(Producto producto, UnidadInventario unidad, NumeroDeSerie? serieCapturada, Dinero precio)
    {
        if (producto.RequiereSerie)
        {
            if (serieCapturada is null)
                throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieRequerida, $"El producto {producto.Nombre} requiere número de serie.");
            if (unidad.Serie is null || serieCapturada != unidad.Serie)
                throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieNoCoincide, "La serie capturada no pertenece a la unidad seleccionada.");
        }
        return new LineaDeVenta
        {
            Id = Guid.NewGuid(), ProductoId = producto.Id, UnidadId = unidad.Id,
            Serie = producto.RequiereSerie ? serieCapturada : null, PrecioUnitario = precio
        };
    }
}
