using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Consultas;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Api.Infraestructura;

namespace TecnoStock.Api.Controllers;

[Route("politica")]
public sealed class PoliticaController(ConsultasDeDevoluciones consultas, TraductorDeRechazosHttp http) : ControladorBase(http)
{
    /// <summary>Criterio de devolución vigente: condiciones y plazo por categoría (US16).</summary>
    [HttpGet("devolucion")]
    [ProducesResponseType<CriterioDeDevolucionDto>(StatusCodes.Status200OK)]
    public IActionResult CriterioDeDevolucion() => Ok(consultas.ConsultarCriterioDeDevolucionVigente(new ConsultarCriterioDeDevolucionVigenteQuery()));
}
