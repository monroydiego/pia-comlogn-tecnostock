using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Aplicacion.Servicios;

/// <summary>
/// Application Service (G-56): solo coordina. Una Unidad de Trabajo por caso de uso (ADR-003):
/// si la acción regresa (incluso con un rechazo con efecto) se confirma; si lanza una violación de regla se descarta
/// y se traduce a Rechazo; cualquier otra excepción descarta y se propaga.
/// </summary>
public abstract class ServicioDeAplicacion(IUnidadDeTrabajo unidadDeTrabajo, TraductorDeRechazos traductor)
{
    protected Resultado EjecutarEnUnidadDeTrabajo(Func<Resultado> accion)
    {
        unidadDeTrabajo.Iniciar();
        try
        {
            var resultado = accion();
            unidadDeTrabajo.Confirmar();
            return resultado;
        }
        catch (ReglaDeNegocioViolada violacion)
        {
            unidadDeTrabajo.Descartar();
            return Resultado.Rechazar(traductor.Traducir(violacion));
        }
        catch
        {
            unidadDeTrabajo.Descartar();
            throw;
        }
    }

    protected Resultado Rechazo(MotivoDeRechazo motivo, string mensaje) =>
        Resultado.Rechazar(traductor.Traducir(motivo, mensaje));
}
