using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Dominio.Estados;

/// <summary>Tablas de transiciones de cada aggregate (ADR-008).</summary>
public static class MaquinasDelDominio
{
    public static readonly MaquinaDeEstados<EstadoDeApartado> Apartado = new(
        new Dictionary<EstadoDeApartado, EstadoDeApartado[]>
        {
            [EstadoDeApartado.Vigente] = [EstadoDeApartado.Convertido, EstadoDeApartado.Vencido, EstadoDeApartado.Cancelado],
            [EstadoDeApartado.Convertido] = [],
            [EstadoDeApartado.Vencido] = [],
            [EstadoDeApartado.Cancelado] = []
        });

    public static readonly MaquinaDeEstados<EstadoDeUnidad> Unidad = new(
        new Dictionary<EstadoDeUnidad, EstadoDeUnidad[]>
        {
            [EstadoDeUnidad.Disponible] = [EstadoDeUnidad.Apartada, EstadoDeUnidad.Vendida, EstadoDeUnidad.EnTransito],
            [EstadoDeUnidad.Apartada] = [EstadoDeUnidad.Disponible, EstadoDeUnidad.Vendida],
            [EstadoDeUnidad.EnTransito] = [EstadoDeUnidad.Disponible],
            [EstadoDeUnidad.Vendida] = [EstadoDeUnidad.Disponible, EstadoDeUnidad.Defectuosa],
            [EstadoDeUnidad.Defectuosa] = []
        });

    public static readonly MaquinaDeEstados<EstadoDeDevolucion> Devolucion = new(
        new Dictionary<EstadoDeDevolucion, EstadoDeDevolucion[]>
        {
            [EstadoDeDevolucion.EnRevision] = [EstadoDeDevolucion.Aceptada, EstadoDeDevolucion.Rechazada],
            [EstadoDeDevolucion.Aceptada] = [],
            [EstadoDeDevolucion.Rechazada] = []
        });
}
