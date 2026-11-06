using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Operación en la que se cobra y entrega al menos una unidad (G-33).</summary>
public sealed class Venta : RaizDeAgregado
{
    private readonly List<LineaDeVenta> _lineas = new();
    private readonly List<PagoRecibido> _pagos = new();

    public Folio Folio { get; private set; } = null!;
    public SucursalId SucursalId { get; private set; }
    public Operador Cajero { get; private set; } = null!;
    public IReadOnlyList<LineaDeVenta> Lineas => _lineas;
    public IReadOnlyList<PagoRecibido> Pagos => _pagos;
    public Folio? FolioDeApartadoOrigen { get; private set; }
    public DateTime FechaDeCierre { get; private set; }

    private Venta() { }

    /// <summary>CU-02: valida métodos de pago autorizados (RN-VTA-002) y que los pagos cubran el total.</summary>
    public static Venta Cerrar(Folio folio, SucursalId sucursal, Operador cajero, IEnumerable<LineaDeVenta> lineas,
        IEnumerable<PagoRecibido> pagos, PoliticaComercial politica, DateTime ahora)
    {
        var listaDeLineas = lineas.ToList();
        var listaDePagos = pagos.ToList();
        if (listaDeLineas.Count == 0) throw new ArgumentException("La venta requiere al menos una línea.", nameof(lineas));
        if (listaDePagos.Count == 0) throw new ArgumentException("La venta requiere al menos un pago.", nameof(pagos));

        foreach (var pago in listaDePagos) politica.ValidarPago(pago);

        var total = listaDeLineas.Aggregate(Dinero.Cero, (acc, l) => acc.Sumar(l.PrecioUnitario));
        var cobrado = listaDePagos.Aggregate(Dinero.Cero, (acc, p) => acc.Sumar(p.Monto));
        if (cobrado.EsMenorQue(total))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SaldoInsuficiente, $"Los pagos ({cobrado}) no cubren el total ({total}).");

        return Construir(folio, sucursal, cajero, listaDeLineas, listaDePagos, null, total, ahora);
    }

    /// <summary>CU-03 (G-37): el anticipo se aplica como parte del pago y el saldo lo cobra el Cajero.</summary>
    public static Venta CerrarDesdeApartado(Folio folio, Apartado apartado, UnidadInventario unidad, Producto producto,
        PagoRecibido pagoSaldo, Operador cajero, DateTime ahora)
    {
        var linea = LineaDeVenta.Crear(producto, unidad, unidad.Serie, apartado.PrecioAcordado);
        return Construir(folio, apartado.SucursalId, cajero, [linea], [apartado.Anticipo, pagoSaldo],
            apartado.Folio, apartado.PrecioAcordado, ahora);
    }

    private static Venta Construir(Folio folio, SucursalId sucursal, Operador cajero, List<LineaDeVenta> lineas,
        List<PagoRecibido> pagos, Folio? apartadoOrigen, Dinero total, DateTime ahora)
    {
        var venta = new Venta
        {
            Folio = folio, SucursalId = sucursal, Cajero = cajero,
            FolioDeApartadoOrigen = apartadoOrigen, FechaDeCierre = ahora
        };
        venta._lineas.AddRange(lineas);
        venta._pagos.AddRange(pagos);
        venta.Registrar(new VentaCerrada(folio, total, ahora));
        return venta;
    }

    public Dinero Total() => _lineas.Aggregate(Dinero.Cero, (acc, l) => acc.Sumar(l.PrecioUnitario));

    /// <summary>Busca la línea del producto y la serie; si no existe lanza LineaNoPerteneceALaVenta.</summary>
    public LineaDeVenta ObtenerLinea(ProductoId productoId, NumeroDeSerie? serie) =>
        _lineas.FirstOrDefault(l => l.ProductoId == productoId && l.Serie == serie)
        ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.LineaNoPerteneceALaVenta,
            "El producto o la serie no pertenecen a esa venta.");
}
