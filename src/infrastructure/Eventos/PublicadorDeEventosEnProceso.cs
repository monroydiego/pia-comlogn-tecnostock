using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecnoStock.Aplicacion.Eventos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Eventos;

namespace TecnoStock.Infraestructura.Eventos;

/// <summary>
/// Despacha cada evento a sus manejadores dentro del proceso, después de confirmar (ADR-003).
/// Los manejadores se resuelven al publicar para evitar dependencias circulares con la Unidad de Trabajo.
/// Un fallo en un manejador no deshace la operación ya confirmada: se registra en el log.
/// </summary>
public sealed class PublicadorDeEventosEnProceso(IServiceProvider proveedor, ILogger<PublicadorDeEventosEnProceso> log) : IPublicadorDeEventos
{
    public void Publicar(IEnumerable<EventoDeDominio> eventos)
    {
        foreach (var evento in eventos)
        {
            switch (evento)
            {
                case ApartadoCreado e:
                    Despachar<ManejadorDeComprobantes>(evento, m => m.AlCrearseApartado(e));
                    break;
                case VentaCerrada e:
                    Despachar<ManejadorDeComprobantes>(evento, m => m.AlCerrarseVenta(e));
                    break;
                case DevolucionAceptada e:
                    Despachar<ManejadorDeComprobantes>(evento, m => m.AlAceptarseDevolucion(e));
                    Despachar<ManejadorDeReingresoDeUnidades>(evento, m => m.AlAceptarseDevolucion(e));
                    break;
                case DevolucionDerivadaASupervisor e:
                    Despachar<ManejadorDeAvisos>(evento, m => m.AlDerivarseDevolucion(e));
                    break;
                case ApartadoVencido e:
                    Despachar<ManejadorDeAvisos>(evento, m => m.AlVencerseApartado(e));
                    break;
            }
        }
    }

    private void Despachar<TManejador>(EventoDeDominio evento, Action<TManejador> accion) where TManejador : notnull
    {
        try
        {
            accion(proveedor.GetRequiredService<TManejador>());
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Falló {Manejador} al atender {Evento}", typeof(TManejador).Name, evento.GetType().Name);
        }
    }
}
