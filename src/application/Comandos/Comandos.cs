using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Aplicacion.Comandos;

// Commands (G-58): solicitudes que cambian el estado del sistema.

public sealed record ApartarProductoCommand(ProductoId ProductoId, SucursalId SucursalId, ClienteId ClienteId, PagoRecibido Anticipo);

public sealed record ConvertirApartadoEnVentaCommand(Folio FolioDeApartado, PagoRecibido PagoDelSaldo, NumeroDeSerie? SerieCapturada);

public sealed record CancelarApartadoCommand(Folio FolioDeApartado, string Motivo);

public sealed record LineaSolicitada(ProductoId ProductoId, NumeroDeSerie? Serie);

public sealed record RegistrarVentaCommand(SucursalId SucursalId, IReadOnlyList<LineaSolicitada> Lineas, IReadOnlyList<PagoRecibido> Pagos);

public sealed record SolicitarDevolucionCommand(Folio FolioDeVenta, ProductoId ProductoId, NumeroDeSerie? SerieCapturada,
    MotivoDeDevolucion Motivo, CondicionDelProducto CondicionDelProducto, FormaDeCompensacion FormaDeCompensacion);

public sealed record AutorizarDevolucionExcepcionalCommand(Folio FolioDeDevolucion, bool Autoriza, string Motivo);

public sealed record RegistrarUnidadCommand(ProductoId ProductoId, SucursalId SucursalId, NumeroDeSerie? Serie);
