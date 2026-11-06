using TecnoStock.Aplicacion.Comandos;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>CU-07 RegistrarUnidad (US17, RN-INV-002).</summary>
public sealed class ServicioDeInventario(
    IRepositorioDeUnidades unidades, IRepositorioDeProductos productos, IUnidadDeTrabajo unidadDeTrabajo,
    TraductorDeRechazos traductor) : ServicioDeAplicacion(unidadDeTrabajo, traductor)
{
    public Resultado RegistrarUnidad(RegistrarUnidadCommand cmd) => EjecutarEnUnidadDeTrabajo(() =>
    {
        var producto = productos.Obtener(cmd.ProductoId)
            ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"El producto {cmd.ProductoId} no existe.");

        if (producto.RequiereSerie && cmd.Serie is null)
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieRequerida, $"El producto {producto.Nombre} requiere número de serie.");
        if (cmd.Serie is not null && unidades.ExisteSerie(cmd.Serie))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SerieDuplicada, $"La serie {cmd.Serie} ya existe en el inventario.");

        var unidad = UnidadInventario.Registrar(UnidadId.Nuevo(), cmd.ProductoId, cmd.SucursalId, cmd.Serie);
        unidades.Agregar(unidad);
        return Resultado.Exito(new UnidadRegistradaDto(unidad.Id.Valor, unidad.ProductoId.Valor,
            unidad.SucursalActual.Valor, unidad.Serie?.Valor, unidad.Estado.ToString()));
    });
}
