using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Infraestructura.Persistencia;

// Value converters (G-51): los Value Objects del dominio se guardan como columnas simples.
public sealed class DineroConversor() : ValueConverter<Dinero, decimal>(d => d.Monto, v => new Dinero(v, "MXN"));
public sealed class NumeroDeSerieConversor() : ValueConverter<NumeroDeSerie, string>(s => s.Valor, v => new NumeroDeSerie(v));
public sealed class FolioConversor() : ValueConverter<Folio, string>(f => f.Valor, v => new Folio(v));
public sealed class ProductoIdConversor() : ValueConverter<ProductoId, string>(i => i.Valor, v => new ProductoId(v));
public sealed class SucursalIdConversor() : ValueConverter<SucursalId, string>(i => i.Valor, v => new SucursalId(v));
public sealed class ClienteIdConversor() : ValueConverter<ClienteId, string>(i => i.Valor, v => new ClienteId(v));
public sealed class UnidadIdConversor() : ValueConverter<UnidadId, Guid>(i => i.Valor, v => new UnidadId(v));
