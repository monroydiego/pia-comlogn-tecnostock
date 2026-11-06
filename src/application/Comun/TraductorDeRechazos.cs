using TecnoStock.Dominio.Comun;

namespace TecnoStock.Aplicacion.Comun;

/// <summary>Convierte una violación de regla en un Rechazo con alternativa sugerida (ADR-006).</summary>
public sealed class TraductorDeRechazos
{
    public Rechazo Traducir(ReglaDeNegocioViolada violacion) => Traducir(violacion.Motivo, violacion.MensajeDeNegocio);

    public Rechazo Traducir(MotivoDeRechazo motivo, string mensajeDeNegocio) =>
        new(motivo, mensajeDeNegocio, AlternativaPara(motivo));

    private static AlternativaSugerida AlternativaPara(MotivoDeRechazo motivo) => motivo switch
    {
        MotivoDeRechazo.SinExistenciaDisponible => AlternativaSugerida.ConsultarOtrasSucursales,
        MotivoDeRechazo.UnidadApartada or MotivoDeRechazo.UnidadNoDisponible or MotivoDeRechazo.UnidadEnOtraSucursal
            => AlternativaSugerida.ElegirOtraUnidad,
        MotivoDeRechazo.SerieRequerida or MotivoDeRechazo.SerieNoCoincide => AlternativaSugerida.CapturarSerie,
        MotivoDeRechazo.ApartadoVencido => AlternativaSugerida.IniciarVentaNueva,
        _ => AlternativaSugerida.Ninguna
    };
}
