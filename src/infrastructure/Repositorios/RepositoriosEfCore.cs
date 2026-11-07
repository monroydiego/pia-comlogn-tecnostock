using Microsoft.EntityFrameworkCore;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;
using TecnoStock.Infraestructura.Persistencia;

namespace TecnoStock.Infraestructura.Repositorios;

public sealed class RepositorioDeApartadosEfCore(ContextoDeDatos ctx) : IRepositorioDeApartados
{
    public Apartado? Obtener(Folio folio) => ctx.Apartados.Find(folio);

    public IReadOnlyList<Apartado> ObtenerVigentesVencidos(DateTime ahora) =>
        ctx.Apartados.Where(a => a.Estado == EstadoDeApartado.Vigente && a.Vigencia.Hasta <= ahora).ToList();

    public void Agregar(Apartado apartado) => ctx.Apartados.Add(apartado);

    public void Guardar(Apartado apartado)
    {
        if (ctx.Entry(apartado).State == EntityState.Detached) ctx.Apartados.Update(apartado);
    }
}

public sealed class RepositorioDeUnidadesEfCore(ContextoDeDatos ctx) : IRepositorioDeUnidades
{
    public UnidadInventario? Obtener(UnidadId unidadId) => ctx.Unidades.Find(unidadId);

    public UnidadInventario? ObtenerPorSerie(NumeroDeSerie serie) => ctx.Unidades.FirstOrDefault(u => u.Serie == serie);

    public bool ExisteSerie(NumeroDeSerie serie) => ctx.Unidades.Any(u => u.Serie == serie);

    public UnidadInventario? ObtenerUnidadDisponible(ProductoId productoId, SucursalId sucursal) =>
        ctx.Unidades.Where(u => u.ProductoId == productoId && u.SucursalActual == sucursal && u.Estado == EstadoDeUnidad.Disponible)
            .OrderBy(u => u.Serie).ThenBy(u => u.Id).FirstOrDefault();

    public UnidadInventario? ObtenerUnidadParaVenta(ProductoId productoId, SucursalId sucursal,
        NumeroDeSerie? serieCapturada, IReadOnlyCollection<UnidadId>? excluidas = null)
    {
        var ids = (excluidas ?? []).ToList();
        var consulta = ctx.Unidades.Where(u => u.ProductoId == productoId && !ids.Contains(u.Id));
        return serieCapturada is not null
            ? consulta.FirstOrDefault(u => u.Serie == serieCapturada)
            : consulta.Where(u => u.SucursalActual == sucursal && u.Estado == EstadoDeUnidad.Disponible)
                .OrderBy(u => u.Serie).ThenBy(u => u.Id).FirstOrDefault();
    }

    public void Agregar(UnidadInventario unidad) => ctx.Unidades.Add(unidad);

    public void Guardar(UnidadInventario unidad)
    {
        if (ctx.Entry(unidad).State == EntityState.Detached) ctx.Unidades.Update(unidad);
    }
}

public sealed class RepositorioDeVentasEfCore(ContextoDeDatos ctx) : IRepositorioDeVentas
{
    public Venta? ObtenerPorFolio(Folio folio) => ctx.Ventas.Find(folio);
    public void Agregar(Venta venta) => ctx.Ventas.Add(venta);
}

public sealed class RepositorioDeDevolucionesEfCore(ContextoDeDatos ctx) : IRepositorioDeDevoluciones
{
    public Devolucion? Obtener(Folio folio) => ctx.Devoluciones.Find(folio);
    public void Agregar(Devolucion devolucion) => ctx.Devoluciones.Add(devolucion);

    public void Guardar(Devolucion devolucion)
    {
        if (ctx.Entry(devolucion).State == EntityState.Detached) ctx.Devoluciones.Update(devolucion);
    }
}

public sealed class RepositorioDePoliticasEfCore(ContextoDeDatos ctx) : IRepositorioDePoliticas
{
    public PoliticaComercial ObtenerPoliticaVigente() => ctx.Politicas.OrderBy(p => p.Id).First();
}

public sealed class RepositorioDeProductosEfCore(ContextoDeDatos ctx) : IRepositorioDeProductos
{
    public Producto? Obtener(ProductoId productoId) => ctx.Productos.Find(productoId);
}
