using TecnoStock.Dominio.Comun;

namespace TecnoStock.Aplicacion.Comun;

public enum AlternativaSugerida
{
    Ninguna,
    ConsultarOtrasSucursales,
    ElegirOtraUnidad,
    CapturarSerie,
    IniciarVentaNueva,
    DerivarASupervisor
}

/// <summary>Respuesta del sistema cuando una operación no procede (G-45).</summary>
public sealed record Rechazo(MotivoDeRechazo Motivo, string MensajeDeNegocio, AlternativaSugerida Alternativa);

/// <summary>Resultado de un caso de uso: éxito con un valor o rechazo (ADR-006).</summary>
public sealed class Resultado
{
    public bool Exitoso { get; }
    public object? Valor { get; }
    public Rechazo? Rechazo { get; }

    private Resultado(bool exitoso, object? valor, Rechazo? rechazo) { Exitoso = exitoso; Valor = valor; Rechazo = rechazo; }

    public static Resultado Exito(object? valor = null) => new(true, valor, null);
    public static Resultado Rechazar(Rechazo rechazo) => new(false, null, rechazo);

    public T ValorComo<T>() => (T)(Valor ?? throw new InvalidOperationException("El resultado no tiene valor."));
}
