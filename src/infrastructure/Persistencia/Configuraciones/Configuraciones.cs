using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Esquema normalizado (ADR-010): claves de negocio de texto, una clave foránea por cada referencia entre aggregates
/// (sin propiedades de navegación en el dominio, solo en la base de datos) y ninguna columna con varios valores.
/// Las FK usan DeleteBehavior.Restrict: nada se borra en cascada.
/// </summary>
internal static class Comunes
{
    public static void Raiz<T>(this EntityTypeBuilder<T> b) where T : RaizDeAgregado =>
        b.Property(x => x.Version).IsConcurrencyToken();

    public static void Operador<T>(this EntityTypeBuilder<T> b, Expression<Func<T, Operador?>> navegacion, string prefijo) where T : class
    {
        b.OwnsOne(navegacion, o =>
        {
            o.Property(p => p.UsuarioId).HasColumnName(prefijo + "UsuarioId").HasMaxLength(60);
            o.Property(p => p.Rol).HasColumnName(prefijo + "Rol");
        });
        b.Navigation(navegacion).IsRequired();
    }

    /// <summary>Decisión del Supervisor como columnas (no como JSON); es opcional, por eso las columnas son nulas si no hay decisión.</summary>
    public static void Decision<T>(this EntityTypeBuilder<T> b, Expression<Func<T, DecisionDeSupervisor?>> navegacion, string prefijo) where T : class =>
        b.OwnsOne(navegacion, d =>
        {
            d.Property(p => p.Autoriza).HasColumnName(prefijo + "Autoriza");
            d.Property(p => p.Motivo).HasColumnName(prefijo + "Motivo").HasMaxLength(500);
            d.Property(p => p.Fecha).HasColumnName(prefijo + "Fecha");
            d.OwnsOne(p => p.Supervisor, s =>
            {
                s.Property(x => x.UsuarioId).HasColumnName(prefijo + "SupervisorUsuarioId").HasMaxLength(60);
                s.Property(x => x.Rol).HasColumnName(prefijo + "SupervisorRol");
            });
            d.Navigation(p => p.Supervisor).IsRequired();
        });
}

internal sealed class SucursalConfiguracion : IEntityTypeConfiguration<SucursalSemilla>
{
    public void Configure(EntityTypeBuilder<SucursalSemilla> b)
    {
        b.ToTable("Sucursales");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(60);
    }
}

internal sealed class ClienteConfiguracion : IEntityTypeConfiguration<ClienteSemilla>
{
    public void Configure(EntityTypeBuilder<ClienteSemilla> b)
    {
        b.ToTable("Clientes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(120);
        b.Property(x => x.Ciudad).HasMaxLength(60);
    }
}

internal sealed class ProductoConfiguracion : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("Productos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Raiz();
        b.Property(x => x.Sku).HasMaxLength(30);
        b.Property(x => x.Nombre).HasMaxLength(120);
    }
}

internal sealed class PoliticaConfiguracion : IEntityTypeConfiguration<PoliticaComercial>
{
    public void Configure(EntityTypeBuilder<PoliticaComercial> b)
    {
        b.ToTable("PoliticasComerciales");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Raiz();
        b.Property(x => x.PorcentajeDeAnticipoMinimo).HasColumnType("decimal(5,2)");

        b.Ignore(x => x.MetodosAutorizados);
        b.Ignore(x => x.PlazoDeDevolucionEnDiasPorCategoria);

        // Un método por fila; la clave compuesta impide repetirlo.
        b.OwnsMany(x => x.AutorizacionesDePago, o =>
        {
            o.ToTable("PoliticaMetodosDePago");
            o.WithOwner().HasForeignKey("PoliticaId");
            o.HasKey("PoliticaId", nameof(MetodoDePagoAutorizado.Metodo));
        });
        b.Navigation(x => x.AutorizacionesDePago).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Una categoría por fila con su plazo en días.
        b.OwnsMany(x => x.PlazosDeDevolucion, o =>
        {
            o.ToTable("PoliticaPlazosDeDevolucion");
            o.WithOwner().HasForeignKey("PoliticaId");
            o.HasKey("PoliticaId", nameof(PlazoDeDevolucion.Categoria));
        });
        b.Navigation(x => x.PlazosDeDevolucion).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UnidadConfiguracion : IEntityTypeConfiguration<UnidadInventario>
{
    public void Configure(EntityTypeBuilder<UnidadInventario> b)
    {
        b.ToTable("UnidadesDeInventario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Raiz();
        b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SucursalSemilla>().WithMany().HasForeignKey(x => x.SucursalActual).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Apartado>().WithMany().HasForeignKey(x => x.FolioDeApartadoVigente).OnDelete(DeleteBehavior.Restrict);
        // RN-INV-002: cada serie corresponde a una sola unidad.
        b.HasIndex(x => x.Serie).IsUnique().HasDatabaseName("IX_Unidades_Serie");
        b.HasIndex(x => new { x.ProductoId, x.SucursalActual, x.Estado });
    }
}

internal sealed class ApartadoConfiguracion : IEntityTypeConfiguration<Apartado>
{
    public void Configure(EntityTypeBuilder<Apartado> b)
    {
        b.ToTable("Apartados");
        b.HasKey(x => x.Folio);
        b.Property(x => x.Folio).ValueGeneratedNever();
        b.Raiz();
        b.HasOne<ClienteSemilla>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SucursalSemilla>().WithMany().HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnidadInventario>().WithMany().HasForeignKey(x => x.UnidadId).OnDelete(DeleteBehavior.Restrict);
        b.OwnsOne(x => x.Anticipo, o =>
        {
            o.Property(p => p.Metodo).HasColumnName("AnticipoMetodo");
            o.Property(p => p.Monto).HasColumnName("AnticipoMonto");
        });
        b.Navigation(x => x.Anticipo).IsRequired();
        b.OwnsOne(x => x.Vigencia, o =>
        {
            o.Property(p => p.Desde).HasColumnName("VigenciaDesde");
            o.Property(p => p.Hasta).HasColumnName("VigenciaHasta");
        });
        b.Navigation(x => x.Vigencia).IsRequired();
        b.Decision(x => x.Cancelacion, "Cancelacion");
        // RN-APT-003: una unidad solo puede tener un apartado vigente.
        b.HasIndex(x => x.UnidadId).IsUnique().HasFilter("[Estado] = 'Vigente'").HasDatabaseName("IX_Apartados_UnidadVigente");
        b.HasIndex(x => x.Estado);
    }
}

internal sealed class VentaConfiguracion : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.ToTable("Ventas");
        b.HasKey(x => x.Folio);
        b.Property(x => x.Folio).ValueGeneratedNever();
        b.Raiz();
        b.HasOne<SucursalSemilla>().WithMany().HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Apartado>().WithMany().HasForeignKey(x => x.FolioDeApartadoOrigen).OnDelete(DeleteBehavior.Restrict);
        b.Operador(x => x.Cajero, "Cajero");
        b.OwnsMany(x => x.Lineas, l =>
        {
            l.ToTable("LineasDeVenta");
            l.WithOwner().HasForeignKey("FolioDeVenta");
            l.HasKey(x => x.Id);
            l.Property(x => x.Id).ValueGeneratedNever();
            l.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            l.HasOne<UnidadInventario>().WithMany().HasForeignKey(x => x.UnidadId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Navigation(x => x.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.OwnsMany(x => x.Pagos, p =>
        {
            p.ToTable("PagosDeVenta");
            p.WithOwner().HasForeignKey("FolioDeVenta");
            p.Property<int>("Id").ValueGeneratedOnAdd();
            p.HasKey("Id");
        });
        b.Navigation(x => x.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DevolucionConfiguracion : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> b)
    {
        b.ToTable("Devoluciones");
        b.HasKey(x => x.Folio);
        b.Property(x => x.Folio).ValueGeneratedNever();
        b.Raiz();
        b.HasOne<Venta>().WithMany().HasForeignKey(x => x.FolioDeVenta).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnidadInventario>().WithMany().HasForeignKey(x => x.UnidadDevuelta).OnDelete(DeleteBehavior.Restrict);
        b.OwnsOne(x => x.Motivo, o =>
        {
            o.Property(p => p.Causal).HasColumnName("MotivoCausal").HasMaxLength(100);
            o.Property(p => p.Detalle).HasColumnName("MotivoDetalle").HasMaxLength(500);
        });
        b.Navigation(x => x.Motivo).IsRequired();
        b.Operador(x => x.Cajero, "Cajero");
        b.Decision(x => x.Decision, "Decision");

        // El dictamen son varias filas (una por regla evaluada), no un JSON.
        b.Ignore(x => x.Dictamen);
        b.OwnsMany(x => x.ResultadosDelDictamen, o =>
        {
            o.ToTable("DevolucionResultadosDeRegla");
            o.WithOwner().HasForeignKey("FolioDeDevolucion");
            o.Property<int>("Id").ValueGeneratedOnAdd();
            o.HasKey("Id");
            o.Property(p => p.Razon).HasMaxLength(300);
        });
        b.Navigation(x => x.ResultadosDelDictamen).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.FolioDeVenta);
    }
}

internal sealed class ComprobanteConfiguracion : IEntityTypeConfiguration<ComprobanteEmitido>
{
    public void Configure(EntityTypeBuilder<ComprobanteEmitido> b)
    {
        b.ToTable("Comprobantes");
        b.HasKey(x => x.FolioDeComprobante);
        b.Property(x => x.FolioDeComprobante).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Tipo).HasMaxLength(30).IsUnicode(false);
        // Referencia a un apartado, venta o devolución según Tipo: es polimórfica, por eso no lleva FK.
        b.Property(x => x.FolioDeOperacion).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Usuario).HasMaxLength(60);
        b.HasMany(x => x.Lineas).WithOne().HasForeignKey(l => l.FolioDeComprobante).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.FolioDeOperacion);
    }
}

internal sealed class ComprobanteLineaConfiguracion : IEntityTypeConfiguration<ComprobanteLinea>
{
    public void Configure(EntityTypeBuilder<ComprobanteLinea> b)
    {
        b.ToTable("ComprobanteLineas");
        b.HasKey(x => x.Id);
        b.Property(x => x.FolioDeComprobante).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Texto).HasMaxLength(300);
    }
}
