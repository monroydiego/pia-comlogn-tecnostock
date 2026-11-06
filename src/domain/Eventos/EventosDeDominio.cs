using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Eventos;

public sealed record ApartadoCreado(Folio Folio, DateTime FechaLimite, DateTime OcurridoEn) : EventoDeDominio;

public sealed record ApartadoVencido(Folio Folio, UnidadId UnidadId, DateTime OcurridoEn) : EventoDeDominio;

public sealed record VentaCerrada(Folio Folio, Dinero Total, DateTime OcurridoEn) : EventoDeDominio;

public sealed record DevolucionDerivadaASupervisor(Folio Folio, IReadOnlyList<string> Incumplimientos, DateTime OcurridoEn) : EventoDeDominio;

public sealed record DevolucionAceptada(Folio Folio, UnidadId UnidadId, Folio FolioDeVenta, CondicionDelProducto Condicion, DateTime OcurridoEn) : EventoDeDominio;

public sealed record DevolucionRechazada(Folio Folio, string Motivo, DateTime OcurridoEn) : EventoDeDominio;
