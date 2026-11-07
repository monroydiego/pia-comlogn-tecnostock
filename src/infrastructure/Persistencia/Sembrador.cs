using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia;

/// <summary>
/// Datos semilla: catálogo (catalogo.csv) y clientes (actores.csv) del curso, la política comercial por defecto,
/// sucursales SUC-01 a SUC-05 y series sintéticas. [Supuesto]: los CSV son solo datos de ejemplo.
/// </summary>
public static class Sembrador
{
    public static readonly string[] Sucursales = ["SUC-01", "SUC-02", "SUC-03", "SUC-04", "SUC-05"];

    /// <summary>[Supuesto] El catálogo solo trae el tipo en la descripción; el mapeo a categoría fija el plazo de devolución.</summary>
    private static readonly Dictionary<string, (CategoriaDeProducto Categoria, bool Serie)> Tipos = new()
    {
        ["Laptop"] = (CategoriaDeProducto.Computadoras, true),
        ["Monitor"] = (CategoriaDeProducto.Accesorios, true),
        ["SSD"] = (CategoriaDeProducto.Componentes, true),
        ["Teclado"] = (CategoriaDeProducto.Accesorios, false)
    };

    public static async Task InicializarAsync(IServiceProvider proveedor, string carpetaDeDatos, CancellationToken ct = default)
    {
        using var alcance = proveedor.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        var log = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Sembrador");

        await CrearBaseDeDatosAsync(ctx, log, ct);
        if (await ctx.Productos.AnyAsync(ct)) return;

        ctx.Sucursales.AddRange(Sucursales.Select((id, i) => new SucursalSemilla { Id = new SucursalId(id), Nombre = $"Sucursal {i + 1:00}" }));
        ctx.Politicas.Add(PoliticaComercial.PorDefecto());
        var productos = LeerCatalogo(Path.Combine(carpetaDeDatos, "catalogo.csv"));
        ctx.Productos.AddRange(productos.Select(p => p.Producto));
        foreach (var (producto, stock) in productos)
            ctx.Unidades.AddRange(GenerarUnidades(producto, stock));
        ctx.Clientes.AddRange(LeerClientes(Path.Combine(carpetaDeDatos, "actores.csv")));
        await ctx.SaveChangesAsync(ct);
        log.LogInformation("Datos semilla cargados: {Productos} productos", productos.Count);
    }

    private static async Task CrearBaseDeDatosAsync(ContextoDeDatos ctx, ILogger log, CancellationToken ct)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                await ctx.Database.EnsureCreatedAsync(ct);
                return;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (intento < 30)
            {
                log.LogWarning("SQL Server aún no responde (intento {Intento} de 30): {Mensaje}", intento, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    private static List<(Producto Producto, int Stock)> LeerCatalogo(string ruta)
    {
        var resultado = new List<(Producto, int)>();
        foreach (var linea in File.ReadAllLines(ruta).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var c = linea.Split(',');
            var tipo = c[1].Split(' ')[0];
            var (categoria, conSerie) = Tipos[tipo];
            var stock = int.Parse(c[3], CultureInfo.InvariantCulture);
            var habilitado = c[4].Trim() == "1";
            var precio = new Dinero(decimal.Parse(c[2], CultureInfo.InvariantCulture));
            var producto = Producto.Crear(new ProductoId(c[0]), c[0], c[1], categoria, conSerie, conSerie, precio);
            resultado.Add((producto, habilitado ? stock : 0));
        }
        return resultado;
    }

    private static IEnumerable<UnidadInventario> GenerarUnidades(Producto producto, int cantidad)
    {
        for (var i = 0; i < cantidad; i++)
        {
            var serie = producto.RequiereSerie ? new NumeroDeSerie($"SN-{producto.Sku}-{i + 1:000}") : null;
            yield return UnidadInventario.Registrar(UnidadId.Nuevo(), producto.Id, new SucursalId(Sucursales[i % Sucursales.Length]), serie);
        }
    }

    private static IEnumerable<ClienteSemilla> LeerClientes(string ruta) =>
        File.ReadAllLines(ruta).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l =>
        {
            var c = l.Split(',');
            return new ClienteSemilla { Id = new ClienteId(c[0]), Nombre = c[1], EsPreferente = c[2].Trim() == "1", Activo = c[3].Trim() == "1", Ciudad = c[4].Trim() };
        });
}
