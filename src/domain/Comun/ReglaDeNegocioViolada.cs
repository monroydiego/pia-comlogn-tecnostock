namespace TecnoStock.Dominio.Comun;

/// <summary>Excepción de dominio: una regla de negocio no se cumple (G-67, ADR-006).</summary>
public sealed class ReglaDeNegocioViolada : Exception
{
    public MotivoDeRechazo Motivo { get; }
    public string MensajeDeNegocio { get; }

    public ReglaDeNegocioViolada(MotivoDeRechazo motivo, string mensajeDeNegocio)
        : base(mensajeDeNegocio)
    {
        Motivo = motivo;
        MensajeDeNegocio = mensajeDeNegocio;
    }
}
