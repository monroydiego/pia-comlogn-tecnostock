using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Resolución del Supervisor sobre una devolución en revisión o una cancelación (G-41).</summary>
public sealed record DecisionDeSupervisor
{
    public Operador Supervisor { get; }
    public bool Autoriza { get; }
    public string Motivo { get; }
    public DateTime Fecha { get; }

    private DecisionDeSupervisor(Operador supervisor, bool autoriza, string motivo, DateTime fecha)
    {
        Supervisor = supervisor;
        Autoriza = autoriza;
        Motivo = motivo;
        Fecha = fecha;
    }

    /// <summary>Solo un Supervisor puede registrar la decisión (RN-SUP-001, RN-SUP-002).</summary>
    public static DecisionDeSupervisor Registrar(Operador operador, bool autoriza, string motivo, DateTime ahora)
    {
        if (!operador.EsSupervisor())
            throw new ReglaDeNegocioViolada(MotivoDeRechazo.SoloSupervisorAutoriza,
                "Solo un Supervisor puede autorizar esta operación.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la decisión es obligatorio.", nameof(motivo));
        return new DecisionDeSupervisor(operador, autoriza, motivo.Trim(), ahora);
    }
}
