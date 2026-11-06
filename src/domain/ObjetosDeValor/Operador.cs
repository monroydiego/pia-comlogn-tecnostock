using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Usuario que ejecuta una operación, con su rol (G-27).</summary>
public sealed record Operador
{
    public string UsuarioId { get; }
    public Rol Rol { get; }

    public Operador(string usuarioId, Rol rol)
    {
        if (string.IsNullOrWhiteSpace(usuarioId)) throw new ArgumentException("El usuario es obligatorio.", nameof(usuarioId));
        UsuarioId = usuarioId.Trim();
        Rol = rol;
    }

    public bool EsSupervisor() => Rol == Rol.Supervisor;
}
