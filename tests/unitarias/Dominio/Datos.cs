using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Pruebas.Unitarias.Dominio;

/// <summary>Constructores de datos de prueba.</summary>
internal static class Datos
{
    public static readonly DateTime Ahora = new(2026, 11, 6, 10, 0, 0);
    public static readonly SucursalId Suc1 = new("SUC-01");
    public static readonly SucursalId Suc2 = new("SUC-02");
    public static readonly ClienteId Cliente = new("TEC-C001");
    public static readonly Operador Cajero = new("cajero1", Rol.Cajero);
    public static readonly Operador Vendedor = new("vendedor1", Rol.Vendedor);
    public static readonly Operador Supervisor = new("super1", Rol.Supervisor);

    public static PoliticaComercial Politica() => PoliticaComercial.PorDefecto();

    public static Producto Laptop(decimal precio = 1000m) => Producto.Crear(new ProductoId("TEC-P001"), "TEC-P001", "Laptop 01",
        CategoriaDeProducto.Computadoras, true, true, new Dinero(precio));

    public static Producto Teclado() => Producto.Crear(new ProductoId("TEC-P004"), "TEC-P004", "Teclado 04",
        CategoriaDeProducto.Accesorios, false, false, new Dinero(500m));

    public static UnidadInventario Unidad(Producto producto, string? serie = "SN-0001", SucursalId? sucursal = null) =>
        UnidadInventario.Registrar(UnidadId.Nuevo(), producto.Id, sucursal ?? Suc1, serie is null ? null : new NumeroDeSerie(serie));

    public static PagoRecibido Pago(decimal monto, MetodoDePago metodo = MetodoDePago.Efectivo) => new(metodo, new Dinero(monto));

    public static Apartado Apartado(Producto producto, UnidadInventario unidad, decimal precio = 1000m, decimal anticipo = 300m) =>
        TecnoStock.Dominio.Agregados.Apartado.Crear(new Folio("APT-1"), Cliente, unidad, producto, new Dinero(precio), Pago(anticipo), Politica(), Ahora);

    public static Venta Venta(Producto producto, UnidadInventario unidad, string folio = "VTA-1", DateTime? fecha = null)
    {
        var linea = LineaDeVenta.Crear(producto, unidad, unidad.Serie, producto.PrecioDeLista);
        unidad.Vender(new Folio(folio), unidad.SucursalActual);
        return TecnoStock.Dominio.Agregados.Venta.Cerrar(new Folio(folio), Suc1, Cajero, [linea],
            [Pago(producto.PrecioDeLista.Monto)], Politica(), fecha ?? Ahora);
    }
}
