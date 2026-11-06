using TecnoStock.Aplicacion.Consultas;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>Lado de lectura (Transaction Script, ADR-001/004). Lanza ReglaDeNegocioViolada(NoEncontrado) si el producto no existe.</summary>
public sealed class ConsultasDeInventario(ILectorDeInventario lector)
{
    public DisponibilidadDeProductoDto ConsultarDisponibilidadEnSucursal(ConsultarDisponibilidadEnSucursalQuery query) =>
        lector.DisponibilidadEnSucursal(query.ProductoId, query.SucursalId)
        ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {query.ProductoId} no existe en el catálogo.");

    public IReadOnlyList<DisponibilidadPorSucursalDto> ConsultarDisponibilidadEnOtrasSucursales(ConsultarDisponibilidadEnOtrasSucursalesQuery query) =>
        lector.DisponibilidadEnOtrasSucursales(query.ProductoId, query.SucursalActual)
        ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {query.ProductoId} no existe en el catálogo.");
}
