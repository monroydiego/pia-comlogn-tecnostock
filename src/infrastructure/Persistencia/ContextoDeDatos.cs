using Microsoft.EntityFrameworkCore;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia;

/// <summary>EF Core / SQL Server (G-69, ADR-009). Vive solo en infraestructura.</summary>
public sealed class ContextoDeDatos(DbContextOptions<ContextoDeDatos> opciones) : DbContext(opciones)
{
    public const string SecuenciaDeApartados = "SecuenciaDeApartados";
    public const string SecuenciaDeVentas = "SecuenciaDeVentas";
    public const string SecuenciaDeDevoluciones = "SecuenciaDeDevoluciones";
    public const string SecuenciaDeComprobantes = "SecuenciaDeComprobantes";

    public DbSet<Apartado> Apartados => Set<Apartado>();
    public DbSet<UnidadInventario> Unidades => Set<UnidadInventario>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<PoliticaComercial> Politicas => Set<PoliticaComercial>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ComprobanteEmitido> Comprobantes => Set<ComprobanteEmitido>();
    public DbSet<ClienteSemilla> Clientes => Set<ClienteSemilla>();
    public DbSet<SucursalSemilla> Sucursales => Set<SucursalSemilla>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<Dinero>().HaveConversion<DineroConversor>().HaveColumnType("decimal(18,2)");
        c.Properties<NumeroDeSerie>().HaveConversion<NumeroDeSerieConversor>().HaveMaxLength(Limites.Serie).AreUnicode(false);
        c.Properties<Folio>().HaveConversion<FolioConversor>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<ProductoId>().HaveConversion<ProductoIdConversor>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<SucursalId>().HaveConversion<SucursalIdConversor>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<ClienteId>().HaveConversion<ClienteIdConversor>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<UnidadId>().HaveConversion<UnidadIdConversor>();
        c.Properties<EstadoDeApartado>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<EstadoDeUnidad>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<EstadoDeDevolucion>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<CondicionDelProducto>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<FormaDeCompensacion>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<MetodoDePago>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<Rol>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
        c.Properties<CategoriaDeProducto>().HaveConversion<string>().HaveMaxLength(Limites.Codigo).AreUnicode(false);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.ApplyConfigurationsFromAssembly(typeof(ContextoDeDatos).Assembly);
        foreach (var s in new[] { SecuenciaDeApartados, SecuenciaDeVentas, SecuenciaDeDevoluciones, SecuenciaDeComprobantes })
            m.HasSequence<int>(s);
    }

    /// <summary>Concurrencia optimista (G-68): cada aggregate modificado incrementa su versión; EF la compara al guardar.</summary>
    private void IncrementarVersiones()
    {
        ChangeTracker.DetectChanges();
        foreach (var entrada in ChangeTracker.Entries<RaizDeAgregado>().Where(e => e.State == EntityState.Modified))
        {
            var version = entrada.Property(nameof(RaizDeAgregado.Version));
            version.CurrentValue = (int)version.OriginalValue! + 1;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IncrementarVersiones();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
}
