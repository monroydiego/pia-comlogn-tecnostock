using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Consultas;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Api.Contratos;
using TecnoStock.Api.Infraestructura;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Api.Controllers;

[Route("devoluciones")]
public sealed class DevolucionesController(ServicioDeDevoluciones servicio, ConsultasDeDevoluciones consultas, TraductorDeRechazosHttp http) : ControladorBase(http)
{
    /// <summary>CU-04. Solicita una devolución: queda Aceptada, o EnRevision si incumple el criterio estándar (US08, US10).</summary>
    [HttpPost, RequiereOperador]
    [ProducesResponseType<EstadoDeDevolucionDto>(StatusCodes.Status201Created)]
    public IActionResult Solicitar(SolicitarDevolucionSolicitud solicitud) => Responder(servicio.SolicitarDevolucion(solicitud.ComoComando()), StatusCodes.Status201Created);

    /// <summary>CU-05. El Supervisor autoriza o deniega una devolución en revisión (US13).</summary>
    [HttpPost("{folio}/resolucion"), RequiereOperador]
    [ProducesResponseType<EstadoDeDevolucionDto>(StatusCodes.Status200OK)]
    public IActionResult Resolver(string folio, ResolverDevolucionSolicitud solicitud) => Responder(servicio.AutorizarDevolucionExcepcional(solicitud.ComoComando(folio)));

    /// <summary>Consulta el estado de una devolución (US02).</summary>
    [HttpGet("{folio}")]
    [ProducesResponseType<EstadoDeDevolucionDto>(StatusCodes.Status200OK)]
    public IActionResult Consultar(string folio) => Ok(consultas.ConsultarEstadoDeDevolucion(new ConsultarEstadoDeDevolucionQuery(new Folio(folio))));
}
