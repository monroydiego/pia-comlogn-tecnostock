using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Repositorios;

// Repository (G-62): interfaces expresadas en términos del negocio y sin SQL. Devuelven null cuando no existe;
// la capa de aplicación traduce la ausencia a NoEncontrado.

public interface IRepositorioDeApartados
{
    Apartado? Obtener(Folio folio);
    /// <summary>Apartados Vigentes cuya fecha límite ya se alcanzó (CU-08).</summary>
    IReadOnlyList<Apartado> ObtenerVigentesVencidos(DateTime ahora);
    void Agregar(Apartado apartado);
    void Guardar(Apartado apartado);
}

public interface IRepositorioDeUnidades
{
    UnidadInventario? Obtener(UnidadId unidadId);
    UnidadInventario? ObtenerPorSerie(NumeroDeSerie serie);
    bool ExisteSerie(NumeroDeSerie serie);
    /// <summary>Una unidad en estado Disponible del producto en la sucursal, o null si no hay.</summary>
    UnidadInventario? ObtenerUnidadDisponible(ProductoId productoId, SucursalId sucursal);
    /// <summary>
    /// Con serie: la unidad con esa serie sin importar su estado (para que el dominio rechace una apartada).
    /// Sin serie: una unidad Disponible en la sucursal que no esté en <paramref name="excluidas"/>.
    /// </summary>
    UnidadInventario? ObtenerUnidadParaVenta(ProductoId productoId, SucursalId sucursal,
        NumeroDeSerie? serieCapturada, IReadOnlyCollection<UnidadId>? excluidas = null);
    void Agregar(UnidadInventario unidad);
    void Guardar(UnidadInventario unidad);
}

public interface IRepositorioDeVentas
{
    Venta? ObtenerPorFolio(Folio folio);
    void Agregar(Venta venta);
}

public interface IRepositorioDeDevoluciones
{
    Devolucion? Obtener(Folio folio);
    void Agregar(Devolucion devolucion);
    void Guardar(Devolucion devolucion);
}

public interface IRepositorioDePoliticas
{
    PoliticaComercial ObtenerPoliticaVigente();
}

public interface IRepositorioDeProductos
{
    Producto? Obtener(ProductoId productoId);
}
