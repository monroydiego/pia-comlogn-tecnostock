using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.Estados;

/// <summary>
/// Máquina de estados explícita (G-66, ADR-008): una tabla de transiciones permitidas.
/// Una transición no permitida lanza <see cref="ReglaDeNegocioViolada"/> con TransicionDeEstadoInvalida.
/// </summary>
public sealed class MaquinaDeEstados<TEstado> where TEstado : struct, Enum
{
    private readonly IReadOnlyDictionary<TEstado, IReadOnlySet<TEstado>> _transicionesPermitidas;

    public MaquinaDeEstados(IReadOnlyDictionary<TEstado, TEstado[]> transiciones)
    {
        _transicionesPermitidas = transiciones.ToDictionary(
            t => t.Key,
            t => (IReadOnlySet<TEstado>)new HashSet<TEstado>(t.Value));
    }

    public bool PuedeTransicionar(TEstado desde, TEstado hacia) =>
        _transicionesPermitidas.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

    public TEstado Transicionar(TEstado actual, TEstado destino)
    {
        if (!PuedeTransicionar(actual, destino))
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.TransicionDeEstadoInvalida,
                $"No se puede pasar de {actual} a {destino}.");
        return destino;
    }
}
