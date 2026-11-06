using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Api.Contratos;
using TecnoStock.Api.Infraestructura;

namespace TecnoStock.Api.Controllers;

[Route("ventas")]
public sealed class VentasController(ServicioDeVentas servicio, TraductorDeRechazosHttp http) : ControladorBase(http)
{
    /// <summary>CU-02. Cierra una venta: valida serie, unidad disponible y métodos de pago autorizados (US06, US09).</summary>
    [HttpPost, RequiereOperador]
    [ProducesResponseType<VentaRegistradaDto>(StatusCodes.Status201Created)]
    public IActionResult Registrar(RegistrarVentaSolicitud solicitud) => Responder(servicio.RegistrarVenta(solicitud.ComoComando()), StatusCodes.Status201Created);
}
