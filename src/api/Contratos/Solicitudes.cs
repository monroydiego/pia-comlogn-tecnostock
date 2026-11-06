using System.ComponentModel.DataAnnotations;
using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Api.Contratos;

/// <summary>Pago con método y monto en MXN.</summary>
public sealed record PagoSolicitud(MetodoDePago Metodo, decimal Monto)
{
    public PagoRecibido ComoPago() => new(Metodo, new Dinero(Monto));
}

public sealed record ApartarProductoSolicitud(string ProductoId, string SucursalId, string ClienteId, PagoSolicitud Anticipo)
{
    public ApartarProductoCommand ComoComando() => new(new ProductoId(Texto.Obligatorio(ProductoId, nameof(ProductoId))),
        new SucursalId(Texto.Obligatorio(SucursalId, nameof(SucursalId))), new ClienteId(Texto.Obligatorio(ClienteId, nameof(ClienteId))),
        Anticipo.ComoPago());
}

public sealed record ConvertirApartadoSolicitud(PagoSolicitud PagoDelSaldo, string? SerieCapturada)
{
    public ConvertirApartadoEnVentaCommand ComoComando(string folio) =>
        new(new Folio(folio), PagoDelSaldo.ComoPago(), Texto.Serie(SerieCapturada));
}

public sealed record CancelarApartadoSolicitud(string Motivo)
{
    public CancelarApartadoCommand ComoComando(string folio) => new(new Folio(folio), Motivo);
}

public sealed record LineaSolicitud(string ProductoId, string? Serie);

public sealed record RegistrarVentaSolicitud(string SucursalId, List<LineaSolicitud> Lineas, List<PagoSolicitud> Pagos)
{
    public RegistrarVentaCommand ComoComando() => new(new SucursalId(Texto.Obligatorio(SucursalId, nameof(SucursalId))),
        Lineas.Select(l => new LineaSolicitada(new ProductoId(Texto.Obligatorio(l.ProductoId, nameof(l.ProductoId))), Texto.Serie(l.Serie))).ToList(),
        Pagos.Select(p => p.ComoPago()).ToList());
}

public sealed record MotivoDeDevolucionSolicitud(string Causal, string? Detalle);

public sealed record SolicitarDevolucionSolicitud(string FolioDeVenta, string ProductoId, string? SerieCapturada,
    MotivoDeDevolucionSolicitud Motivo, CondicionDelProducto CondicionDelProducto, FormaDeCompensacion FormaDeCompensacion)
{
    public SolicitarDevolucionCommand ComoComando() => new(new Folio(FolioDeVenta), new ProductoId(Texto.Obligatorio(ProductoId, nameof(ProductoId))),
        Texto.Serie(SerieCapturada), new MotivoDeDevolucion(Motivo.Causal, Motivo.Detalle ?? string.Empty), CondicionDelProducto, FormaDeCompensacion);
}

public sealed record ResolverDevolucionSolicitud(bool Autoriza, string Motivo)
{
    public AutorizarDevolucionExcepcionalCommand ComoComando(string folio) => new(new Folio(folio), Autoriza, Motivo);
}

public sealed record RegistrarUnidadSolicitud(string ProductoId, string SucursalId, string? Serie)
{
    public RegistrarUnidadCommand ComoComando() => new(new ProductoId(Texto.Obligatorio(ProductoId, nameof(ProductoId))),
        new SucursalId(Texto.Obligatorio(SucursalId, nameof(SucursalId))), Texto.Serie(Serie));
}

internal static class Texto
{
    public static string Obligatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? throw new ArgumentException($"{campo} es obligatorio.") : valor.Trim();

    public static NumeroDeSerie? Serie(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : new NumeroDeSerie(valor);
}
