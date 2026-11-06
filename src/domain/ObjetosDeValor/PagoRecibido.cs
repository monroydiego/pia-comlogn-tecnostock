using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Cobro de una parte del importe (G-35). El anticipo es un pago recibido.</summary>
public sealed record PagoRecibido(MetodoDePago Metodo, Dinero Monto);
