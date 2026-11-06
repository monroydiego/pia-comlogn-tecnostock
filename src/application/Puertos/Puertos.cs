using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Aplicacion.Puertos;

public enum TipoDeOperacion { Apartado, Venta, Devolucion, Comprobante }

/// <summary>Comprobante con folio único (G-06, US12).</summary>
public sealed record Comprobante(Folio FolioDeComprobante, TipoDeOperacion Tipo, Folio FolioDeOperacion,
    DateTime Fecha, string Usuario, IReadOnlyList<string> Detalle);

/// <summary>Unit of Work (G-63): agrupa los cambios de un caso de uso en una transacción.</summary>
public interface IUnidadDeTrabajo
{
    void Iniciar();
    /// <summary>Confirma la transacción y publica los eventos de dominio. Lanza ConflictoDeConcurrencia si otra operación modificó el mismo aggregate.</summary>
    void Confirmar();
    void Descartar();
}

public interface IReloj { DateTime Ahora(); }

public interface IGeneradorDeFolios { Folio Siguiente(TipoDeOperacion tipoDeOperacion); }

public interface IContextoDeUsuario { Operador OperadorActual(); }

public interface IPublicadorDeEventos { void Publicar(IEnumerable<EventoDeDominio> eventos); }

public interface IEmisorDeComprobantes { void Emitir(Comprobante comprobante); }

public interface INotificador { void Avisar(string destinatario, string mensaje); }
