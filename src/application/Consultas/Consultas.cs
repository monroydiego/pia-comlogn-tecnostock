using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Aplicacion.Consultas;

// Queries (G-59): solo consultan y devuelven DTO de lectura.

public sealed record ConsultarDisponibilidadEnSucursalQuery(ProductoId ProductoId, SucursalId SucursalId);

public sealed record ConsultarDisponibilidadEnOtrasSucursalesQuery(ProductoId ProductoId, SucursalId SucursalActual);

public sealed record ConsultarEstadoDeDevolucionQuery(Folio FolioDeDevolucion);

public sealed record ConsultarCriterioDeDevolucionVigenteQuery;
