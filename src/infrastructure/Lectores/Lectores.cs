using Microsoft.EntityFrameworkCore;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Infraestructura.Persistencia;

namespace TecnoStock.Infraestructura.Lectores;

/// <summary>Lado de lectura (Transaction Script, ADR-001/004): consulta sin reconstruir aggregates.</summary>
public sealed class LectorDeInventarioSql(ContextoDeDatos ctx) : ILectorDeInventario
{
    public DisponibilidadDeProductoDto? DisponibilidadEnSucursal(ProductoId productoId, SucursalId sucursalId)
    {
        if (!ctx.Productos.AsNoTracking().Any(p => p.Id == productoId)) return null;
        var cantidad = ctx.Unidades.AsNoTracking().Count(u => u.ProductoId == productoId && u.SucursalActual == sucursalId
                                                              && u.Estado == EstadoDeUnidad.Disponible);
        return new DisponibilidadDeProductoDto(productoId.Valor, sucursalId.Valor, cantidad);
    }

    public IReadOnlyList<DisponibilidadPorSucursalDto>? DisponibilidadEnOtrasSucursales(ProductoId productoId, SucursalId sucursalActual)
    {
        if (!ctx.Productos.AsNoTracking().Any(p => p.Id == productoId)) return null;
        return ctx.Unidades.AsNoTracking()
            .Where(u => u.ProductoId == productoId && u.SucursalActual != sucursalActual && u.Estado == EstadoDeUnidad.Disponible)
            .GroupBy(u => u.SucursalActual)
            .Select(g => new { Sucursal = g.Key, Cantidad = g.Count() })
            .ToList()
            .OrderBy(g => g.Sucursal.Valor)
            .Select(g => new DisponibilidadPorSucursalDto(g.Sucursal.Valor, g.Cantidad))
            .ToList();
    }
}

public sealed class LectorDeDevolucionesSql(ContextoDeDatos ctx) : ILectorDeDevoluciones
{
    public EstadoDeDevolucionDto? EstadoDeDevolucion(Folio folio)
    {
        var devolucion = ctx.Devoluciones.AsNoTracking().FirstOrDefault(d => d.Folio == folio);
        return devolucion is null ? null : EstadoDeDevolucionDto.Desde(devolucion);
    }

    public CriterioDeDevolucionDto CriterioVigente() =>
        CriterioDeDevolucionDto.Desde(ctx.Politicas.AsNoTracking().OrderBy(p => p.Id).First());
}
