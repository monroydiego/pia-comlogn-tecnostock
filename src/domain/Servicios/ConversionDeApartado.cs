using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Servicios;

/// <summary>Resultado de la conversión: exitosa con la venta, o rechazada con efecto (ADR-006).</summary>
public sealed record ResultadoDeConversion
{
    public Venta? Venta { get; }
    public MotivoDeRechazo? Motivo { get; }

    private ResultadoDeConversion(Venta? venta, MotivoDeRechazo? motivo) { Venta = venta; Motivo = motivo; }

    public static ResultadoDeConversion Exitosa(Venta venta) => new(venta, null);
    public static ResultadoDeConversion Rechazada(MotivoDeRechazo motivo) => new(null, motivo);
    public bool EsExitosa() => Venta is not null;
}

/// <summary>Servicio de dominio (G-55): convierte un apartado vigente en venta (CU-03, diagrama 04).</summary>
public sealed class ConversionDeApartado
{
    public ResultadoDeConversion Convertir(Apartado apartado, UnidadInventario unidad, Producto producto,
        PagoRecibido pagoSaldo, NumeroDeSerie? serieCapturada, Folio folioVenta, Operador cajero,
        PoliticaComercial politica, DateTime ahora)
    {
        if (!apartado.EstaVigente(ahora))
        {
            // Vencer lanza TransicionDeEstadoInvalida si el apartado ya estaba Convertido o Cancelado.
            apartado.Vencer(ahora);
            unidad.LiberarApartado(apartado.Folio);
            return ResultadoDeConversion.Rechazada(MotivoDeRechazo.ApartadoVencido);
        }

        // Se valida todo antes de modificar nada: si algo falla se lanza la excepción sin dejar cambios parciales.
        apartado.ValidarLiquidacion(pagoSaldo, politica);
        unidad.ValidarVentaDesdeApartado(apartado.Folio, serieCapturada);
        var venta = Venta.CerrarDesdeApartado(folioVenta, apartado, unidad, producto, pagoSaldo, cajero, ahora);

        apartado.Liquidar(pagoSaldo, politica, ahora);
        unidad.VenderDesdeApartado(apartado.Folio, serieCapturada);
        return ResultadoDeConversion.Exitosa(venta);
    }
}
