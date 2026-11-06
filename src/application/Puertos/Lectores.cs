using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Aplicacion.Puertos;

/// <summary>Puertos de lectura (CQRS básico, ADR-004). Devuelven null si el producto o el folio no existen.</summary>
public interface ILectorDeInventario
{
    DisponibilidadDeProductoDto? DisponibilidadEnSucursal(ProductoId productoId, SucursalId sucursalId);
    IReadOnlyList<DisponibilidadPorSucursalDto>? DisponibilidadEnOtrasSucursales(ProductoId productoId, SucursalId sucursalActual);
}

public interface ILectorDeDevoluciones
{
    EstadoDeDevolucionDto? EstadoDeDevolucion(Folio folio);
    CriterioDeDevolucionDto CriterioVigente();
}
