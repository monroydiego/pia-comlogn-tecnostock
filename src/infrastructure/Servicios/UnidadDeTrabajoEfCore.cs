using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Infraestructura.Persistencia;

namespace TecnoStock.Infraestructura.Servicios;

/// <summary>
/// Unit of Work (G-63, ADR-003/009): abre la transacción, la confirma o la descarta y publica los eventos
/// de dominio solo después de confirmar. Traduce los conflictos de la base de datos a motivos de rechazo.
/// </summary>
public sealed class UnidadDeTrabajoEfCore(ContextoDeDatos ctx, IPublicadorDeEventos publicador) : IUnidadDeTrabajo
{
    private IDbContextTransaction? _transaccion;

    public void Iniciar()
    {
        if (_transaccion is not null) throw new InvalidOperationException("Ya hay una unidad de trabajo en curso.");
        _transaccion = ctx.Database.BeginTransaction();
    }

    public void Confirmar()
    {
        if (_transaccion is null) throw new InvalidOperationException("No hay una unidad de trabajo iniciada.");
        try
        {
            ctx.SaveChanges();
            _transaccion.Commit();
        }
        catch (DbUpdateConcurrencyException)
        {
            Descartar();
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.ConflictoDeConcurrencia,
                "Otra operación modificó el mismo registro. Vuelva a intentarlo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
        {
            Descartar();
            throw TraducirViolacionDeUnicidad(sql);
        }

        _transaccion.Dispose();
        _transaccion = null;

        var eventos = ctx.ChangeTracker.Entries<RaizDeAgregado>().SelectMany(e => e.Entity.ExtraerEventos()).ToList();
        publicador.Publicar(eventos);
    }

    public void Descartar()
    {
        if (_transaccion is not null)
        {
            _transaccion.Rollback();
            _transaccion.Dispose();
            _transaccion = null;
        }
        ctx.ChangeTracker.Clear();
    }

    private static ReglaDeNegocioViolada TraducirViolacionDeUnicidad(SqlException sql) => sql.Message switch
    {
        var m when m.Contains("IX_Unidades_Serie") => new(MotivoDeRechazo.SerieDuplicada, "La serie ya existe en el inventario."),
        var m when m.Contains("IX_Apartados_UnidadVigente") => new(MotivoDeRechazo.UnidadApartada, "La unidad ya tiene un apartado vigente."),
        _ => new(MotivoDeRechazo.ConflictoDeConcurrencia, "Otra operación modificó el mismo registro. Vuelva a intentarlo.")
    };
}
