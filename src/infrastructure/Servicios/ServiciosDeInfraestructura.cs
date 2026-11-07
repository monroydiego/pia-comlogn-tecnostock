using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Infraestructura.Persistencia;

namespace TecnoStock.Infraestructura.Servicios;

public sealed class RelojDelSistema : IReloj
{
    public DateTime Ahora() => DateTime.UtcNow;
}

/// <summary>Folios únicos con una secuencia de SQL Server por tipo de operación (ej. APT-000001).</summary>
public sealed class GeneradorDeFoliosSql(ContextoDeDatos ctx) : IGeneradorDeFolios
{
    public Folio Siguiente(TipoDeOperacion tipo)
    {
        // Las consultas son constantes internas (nunca entrada del usuario), por eso no hay riesgo de inyección SQL.
        var (consulta, prefijo) = tipo switch
        {
            TipoDeOperacion.Apartado => ("SELECT NEXT VALUE FOR dbo.SecuenciaDeApartados AS [Value]", "APT"),
            TipoDeOperacion.Venta => ("SELECT NEXT VALUE FOR dbo.SecuenciaDeVentas AS [Value]", "VTA"),
            TipoDeOperacion.Devolucion => ("SELECT NEXT VALUE FOR dbo.SecuenciaDeDevoluciones AS [Value]", "DEV"),
            _ => ("SELECT NEXT VALUE FOR dbo.SecuenciaDeComprobantes AS [Value]", "CMP")
        };
        var numero = ctx.Database.SqlQueryRaw<int>(consulta).AsEnumerable().First();
        return new Folio($"{prefijo}-{numero:000000}");
    }
}

/// <summary>Guarda el comprobante emitido (ticket de caja básico).</summary>
public sealed class EmisorDeComprobantesBasico(ContextoDeDatos ctx) : IEmisorDeComprobantes
{
    public void Emitir(Comprobante c)
    {
        ctx.Comprobantes.Add(new ComprobanteEmitido
        {
            FolioDeComprobante = c.FolioDeComprobante.Valor, Tipo = c.Tipo.ToString(), FolioDeOperacion = c.FolioDeOperacion.Valor,
            Fecha = c.Fecha, Usuario = c.Usuario,
            Lineas = c.Detalle.Select((texto, i) => new ComprobanteLinea { FolioDeComprobante = c.FolioDeComprobante.Valor, Orden = i + 1, Texto = texto }).ToList()
        });
        ctx.SaveChanges();
    }
}

/// <summary>Los avisos se registran en el log: el canal de notificación está por validar con el stakeholder (BRD 7.3).</summary>
public sealed class NotificadorDeAvisos(ILogger<NotificadorDeAvisos> log) : INotificador
{
    public void Avisar(string destinatario, string mensaje) => log.LogInformation("Aviso para {Destinatario}: {Mensaje}", destinatario, mensaje);
}
