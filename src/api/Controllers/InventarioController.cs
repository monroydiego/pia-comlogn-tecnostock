using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TecnoStock.Aplicacion.Consultas;
using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Api.Contratos;
using TecnoStock.Api.Infraestructura;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Api.Controllers;

public sealed class InventarioController(ServicioDeInventario servicio, ConsultasDeInventario consultas, TraductorDeRechazosHttp http) : ControladorBase(http)
{
    /// <summary>CU-07. Da de alta una unidad con su número de serie (US17).</summary>
    [HttpPost("unidades"), RequiereOperador]
    [ProducesResponseType<UnidadRegistradaDto>(StatusCodes.Status201Created)]
    public IActionResult RegistrarUnidad(RegistrarUnidadSolicitud solicitud) => Responder(servicio.RegistrarUnidad(solicitud.ComoComando()), StatusCodes.Status201Created);

    /// <summary>Disponibilidad de un producto en una sucursal (US04).</summary>
    [HttpGet("disponibilidad/{productoId}")]
    [ProducesResponseType<DisponibilidadDeProductoDto>(StatusCodes.Status200OK)]
    public IActionResult DisponibilidadEnSucursal(string productoId, [FromQuery, Required] string sucursalId) =>
        Ok(consultas.ConsultarDisponibilidadEnSucursal(new ConsultarDisponibilidadEnSucursalQuery(new ProductoId(productoId), new SucursalId(sucursalId))));

    /// <summary>Disponibilidad de un producto en las otras sucursales (US05). Lista vacía si ninguna tiene existencia.</summary>
    [HttpGet("disponibilidad/{productoId}/otras-sucursales")]
    [ProducesResponseType<IReadOnlyList<DisponibilidadPorSucursalDto>>(StatusCodes.Status200OK)]
    public IActionResult DisponibilidadEnOtrasSucursales(string productoId, [FromQuery, Required] string sucursalActual) =>
        Ok(consultas.ConsultarDisponibilidadEnOtrasSucursales(new ConsultarDisponibilidadEnOtrasSucursalesQuery(new ProductoId(productoId), new SucursalId(sucursalActual))));
}
