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
        var (secuencia, prefijo) = tipo switch
        {
            TipoDeOperacion.Apartado => (ContextoDeDatos.SecuenciaDeApartados, "APT"),
            TipoDeOperacion.Venta => (ContextoDeDatos.SecuenciaDeVentas, "VTA"),
            TipoDeOperacion.Devolucion => (ContextoDeDatos.SecuenciaDeDevoluciones, "DEV"),
            _ => (ContextoDeDatos.SecuenciaDeComprobantes, "CMP")
        };
        // El nombre de la secuencia es una constante interna, nunca entrada del usuario.
        var numero = ctx.Database.SqlQueryRaw<int>($"SELECT NEXT VALUE FOR dbo.{secuencia} AS [Value]").AsEnumerable().First();
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
            Fecha = c.Fecha, Usuario = c.Usuario, Detalle = string.Join(Environment.NewLine, c.Detalle)
        });
        ctx.SaveChanges();
    }
}

/// <summary>Los avisos se registran en el log: el canal de notificación está por validar con el stakeholder (BRD 7.3).</summary>
public sealed class NotificadorDeAvisos(ILogger<NotificadorDeAvisos> log) : INotificador
{
    public void Avisar(string destinatario, string mensaje) => log.LogInformation("Aviso para {Destinatario}: {Mensaje}", destinatario, mensaje);
}
