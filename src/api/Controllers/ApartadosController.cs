using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Api.Contratos;
using TecnoStock.Api.Infraestructura;

namespace TecnoStock.Api.Controllers;

[Route("apartados")]
public sealed class ApartadosController(ServicioDeApartados servicio, TraductorDeRechazosHttp http) : ControladorBase(http)
{
    /// <summary>CU-01. Aparta una unidad de un producto apartable con un anticipo (US01, US03, US11).</summary>
    [HttpPost, RequiereOperador]
    [ProducesResponseType<ConfirmacionDeApartadoDto>(StatusCodes.Status201Created)]
    public IActionResult Apartar(ApartarProductoSolicitud solicitud) => Responder(servicio.ApartarProducto(solicitud.ComoComando()), StatusCodes.Status201Created);

    /// <summary>CU-03. Convierte un apartado vigente en venta cobrando el saldo (US07, US09).</summary>
    [HttpPost("{folio}/convertir"), RequiereOperador]
    [ProducesResponseType<VentaRegistradaDto>(StatusCodes.Status200OK)]
    public IActionResult Convertir(string folio, ConvertirApartadoSolicitud solicitud) => Responder(servicio.ConvertirApartadoEnVenta(solicitud.ComoComando(folio)));

    /// <summary>CU-06. Cancela un apartado vigente; solo el Supervisor (US14).</summary>
    [HttpPost("{folio}/cancelar"), RequiereOperador]
    [ProducesResponseType<CancelacionDeApartadoDto>(StatusCodes.Status200OK)]
    public IActionResult Cancelar(string folio, CancelarApartadoSolicitud solicitud) => Responder(servicio.CancelarApartado(solicitud.ComoComando(folio)));

    /// <summary>CU-08. Vence los apartados cuya fecha límite ya pasó y libera sus unidades (US20). Lo ejecuta el actor Sistema.</summary>
    [HttpPost("vencer")]
    [ProducesResponseType<ResumenDeVencimientoDto>(StatusCodes.Status200OK)]
    public IActionResult Vencer() => Responder(servicio.VencerApartadosVencidos());
}
