using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia.Configuraciones;

internal static class Comunes
{
    public static void Raiz<T>(this EntityTypeBuilder<T> b) where T : RaizDeAgregado =>
        b.Property(x => x.Version).IsConcurrencyToken();

    public static void Operador<T>(this EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T, Operador?>> navegacion, string prefijo)
        where T : class
    {
        b.OwnsOne(navegacion, o =>
        {
            o.Property(p => p.UsuarioId).HasColumnName(prefijo + "UsuarioId").HasMaxLength(60);
            o.Property(p => p.Rol).HasColumnName(prefijo + "Rol");
        });
        b.Navigation(navegacion).IsRequired();
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
        b.Property(x => x.Cancelacion).HasConversion(
            d => d == null ? null : FormatosDePersistencia.Serializar(d),
            s => s == null ? null : FormatosDePersistencia.DeserializarDecision(s));
        // RN-APT-003: una unidad solo puede tener un apartado vigente.
        b.HasIndex(x => x.UnidadId).IsUnique().HasFilter("[Estado] = 'Vigente'").HasDatabaseName("IX_Apartados_UnidadVigente");
        b.HasIndex(x => x.Estado);
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
        // RN-INV-002: cada serie corresponde a una sola unidad.
        b.HasIndex(x => x.Serie).IsUnique().HasDatabaseName("IX_Unidades_Serie");
        b.HasIndex(x => new { x.ProductoId, x.SucursalActual, x.Estado });
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
        b.Operador(x => x.Cajero, "Cajero");
        b.OwnsMany(x => x.Lineas, l =>
        {
            l.ToTable("LineasDeVenta");
            l.WithOwner().HasForeignKey("FolioDeVenta");
            l.HasKey(x => x.Id);
            l.Property(x => x.Id).ValueGeneratedNever();
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
        b.OwnsOne(x => x.Motivo, o =>
        {
            o.Property(p => p.Causal).HasColumnName("MotivoCausal").HasMaxLength(100);
            o.Property(p => p.Detalle).HasColumnName("MotivoDetalle").HasMaxLength(500);
        });
        b.Navigation(x => x.Motivo).IsRequired();
        b.Operador(x => x.Cajero, "Cajero");
        b.Property(x => x.Dictamen).HasConversion(
            d => FormatosDePersistencia.Serializar(d), s => FormatosDePersistencia.DeserializarDictamen(s));
        b.Property(x => x.Decision).HasConversion(
            d => d == null ? null : FormatosDePersistencia.Serializar(d),
            s => s == null ? null : FormatosDePersistencia.DeserializarDecision(s));
        b.HasIndex(x => x.FolioDeVenta);
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
        b.Property(x => x.MetodosAutorizados)
            .HasConversion(v => FormatosDePersistencia.Serializar(v), s => FormatosDePersistencia.DeserializarMetodos(s),
                FormatosDePersistencia.ComparadorDeMetodos);
        b.Property(x => x.PlazoDeDevolucionEnDiasPorCategoria)
            .HasConversion(v => FormatosDePersistencia.Serializar(v), s => FormatosDePersistencia.DeserializarPlazos(s),
                FormatosDePersistencia.ComparadorDePlazos);
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

internal sealed class ComprobanteConfiguracion : IEntityTypeConfiguration<ComprobanteEmitido>
{
    public void Configure(EntityTypeBuilder<ComprobanteEmitido> b)
    {
        b.ToTable("Comprobantes");
        b.HasKey(x => x.FolioDeComprobante);
        b.Property(x => x.FolioDeComprobante).HasMaxLength(30);
        b.Property(x => x.Tipo).HasMaxLength(30);
        b.Property(x => x.FolioDeOperacion).HasMaxLength(30);
        b.Property(x => x.Usuario).HasMaxLength(60);
    }
}

internal sealed class ClienteConfiguracion : IEntityTypeConfiguration<ClienteSemilla>
{
    public void Configure(EntityTypeBuilder<ClienteSemilla> b)
    {
        b.ToTable("Clientes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(30);
        b.Property(x => x.Nombre).HasMaxLength(120);
        b.Property(x => x.Ciudad).HasMaxLength(60);
    }
}
