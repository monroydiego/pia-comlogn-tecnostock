namespace TecnoStock.Dominio.Comun;

/// <summary>Raíz de agregado (G-53): lleva la versión para concurrencia optimista y los eventos pendientes.</summary>
public abstract class RaizDeAgregado
{
    private readonly List<EventoDeDominio> _eventosPendientes = new();

    /// <summary>Versión de concurrencia optimista (G-68); la incrementa la infraestructura al guardar.</summary>
    public int Version { get; protected set; }

    protected void Registrar(EventoDeDominio evento) => _eventosPendientes.Add(evento);

    public IReadOnlyList<EventoDeDominio> ExtraerEventos()
    {
        var eventos = _eventosPendientes.ToList();
        _eventosPendientes.Clear();
        return eventos;
    }
}
