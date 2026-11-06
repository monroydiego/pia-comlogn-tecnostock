using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Api.Infraestructura;

namespace TecnoStock.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ControladorBase(TraductorDeRechazosHttp http) : ControllerBase
{
    /// <summary>Éxito con 200 o 201; rechazo traducido a 404, 409 o 422.</summary>
    protected IActionResult Responder(Resultado resultado, int exito = StatusCodes.Status200OK) =>
        resultado.Exitoso ? StatusCode(exito, resultado.Valor) : http.Traducir(resultado.Rechazo!);
}
