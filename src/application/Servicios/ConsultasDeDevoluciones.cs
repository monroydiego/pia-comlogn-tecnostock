using TecnoStock.Aplicacion.Consultas;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Aplicacion.Servicios;

public sealed class ConsultasDeDevoluciones(ILectorDeDevoluciones lector)
{
    public EstadoDeDevolucionDto ConsultarEstadoDeDevolucion(ConsultarEstadoDeDevolucionQuery query) =>
        lector.EstadoDeDevolucion(query.FolioDeDevolucion)
        ?? throw new ReglaDeNegocioViolada(MotivoDeRechazo.NoEncontrado, $"La devolución {query.FolioDeDevolucion} no existe.");

    public CriterioDeDevolucionDto ConsultarCriterioDeDevolucionVigente(ConsultarCriterioDeDevolucionVigenteQuery query) =>
        lector.CriterioVigente();
}
