using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Api.Infraestructura;

/// <summary>Traduce los rechazos del negocio a códigos HTTP: 404, 409 (conflicto) y 422 (regla de negocio violada).</summary>
public sealed class TraductorDeRechazosHttp
{
    public int Codigo(MotivoDeRechazo motivo) => motivo switch
    {
        MotivoDeRechazo.NoEncontrado => StatusCodes.Status404NotFound,
        MotivoDeRechazo.UnidadApartada or MotivoDeRechazo.ConflictoDeConcurrencia or MotivoDeRechazo.TransicionDeEstadoInvalida
            => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity
    };

    public ProblemDetails Problema(Rechazo rechazo)
    {
        var problema = new ProblemDetails
        {
            Status = Codigo(rechazo.Motivo), Title = rechazo.Motivo.ToString(), Detail = rechazo.MensajeDeNegocio
        };
        problema.Extensions["motivo"] = rechazo.Motivo.ToString();
        problema.Extensions["alternativaSugerida"] = rechazo.Alternativa.ToString();
        return problema;
    }

    public IActionResult Traducir(Rechazo rechazo)
    {
        var problema = Problema(rechazo);
        return new ObjectResult(problema) { StatusCode = problema.Status };
    }
}

/// <summary>Convierte excepciones en respuestas: datos inválidos 400, reglas violadas en consultas y errores inesperados 500.</summary>
public sealed class ManejadorDeExcepciones(TraductorDeRechazosHttp http, TraductorDeRechazos traductor, ILogger<ManejadorDeExcepciones> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        ProblemDetails problema;
        switch (excepcion)
        {
            case ReglaDeNegocioViolada violacion:
                problema = http.Problema(traductor.Traducir(violacion));
                break;
            case ArgumentException or FormatException or System.Text.Json.JsonException or BadHttpRequestException:
                var mensaje = excepcion.Message;
                var corte = mensaje.IndexOf(" (Parameter", StringComparison.Ordinal);
                problema = new ProblemDetails { Status = 400, Title = "DatosInvalidos", Detail = corte > 0 ? mensaje[..corte] : mensaje };
                break;
            default:
                log.LogError(excepcion, "Error no controlado en {Ruta}", contexto.Request.Path);
                problema = new ProblemDetails { Status = 500, Title = "ErrorInterno", Detail = "Ocurrió un error inesperado." };
                break;
        }
        contexto.Response.StatusCode = problema.Status!.Value;
        await contexto.Response.WriteAsJsonAsync(problema, ct);
        return true;
    }
}
