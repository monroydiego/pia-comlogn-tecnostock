using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Usuario que ejecuta una operación, con su rol (G-27).</summary>
public sealed record Operador
{
    public string UsuarioId { get; }
    public Rol Rol { get; }

    public Operador(string usuarioId, Rol rol)
    {
        UsuarioId = Limites.Texto(usuarioId, Limites.Usuario, "El usuario");
        Rol = rol;
    }

    public bool EsSupervisor() => Rol == Rol.Supervisor;
}
