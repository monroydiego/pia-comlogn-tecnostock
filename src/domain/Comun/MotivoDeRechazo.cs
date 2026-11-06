namespace TecnoStock.Dominio.Comun;

/// <summary>Motivos de rechazo del negocio (ADR-006). Coinciden con los de los escenarios Gherkin.</summary>
public enum MotivoDeRechazo
{
    SinExistenciaDisponible,
    UnidadApartada,
    UnidadNoDisponible,
    UnidadEnOtraSucursal,
    ProductoNoApartable,
    AnticipoInsuficiente,
    MetodoDePagoNoAutorizado,
    SerieRequerida,
    SerieNoCoincide,
    SerieDuplicada,
    SaldoInsuficiente,
    ApartadoVencido,
    ComprobanteNoValido,
    LineaNoPerteneceALaVenta,
    SoloSupervisorAutoriza,
    TransicionDeEstadoInvalida,
    ConflictoDeConcurrencia,
    NoEncontrado
}
