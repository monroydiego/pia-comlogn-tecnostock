namespace TecnoStock.Dominio.Comun;

/// <summary>Hecho del negocio que ya ocurrió (G-54).</summary>
public interface EventoDeDominio
{
    DateTime OcurridoEn { get; }
}
