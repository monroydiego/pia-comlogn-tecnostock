using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TecnoStock.Dominio.Enumeraciones;
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

/// <summary>Formato de persistencia (solo infraestructura) de los Value Objects que no se consultan por columna.</summary>
public static class FormatosDePersistencia
{
    private sealed record DecisionGuardada(string Usuario, Rol Rol, bool Autoriza, string Motivo, DateTime Fecha);
    private sealed record ResultadoGuardado(bool Cumple, string Razon);

    public static string Serializar(DecisionDeSupervisor d) => JsonSerializer.Serialize(
        new DecisionGuardada(d.Supervisor.UsuarioId, d.Supervisor.Rol, d.Autoriza, d.Motivo, d.Fecha));

    public static DecisionDeSupervisor DeserializarDecision(string json)
    {
        var g = JsonSerializer.Deserialize<DecisionGuardada>(json)!;
        return DecisionDeSupervisor.Registrar(new Operador(g.Usuario, g.Rol), g.Autoriza, g.Motivo, g.Fecha);
    }

    public static string Serializar(DictamenDeDevolucion d) => JsonSerializer.Serialize(
        d.Resultados.Select(r => new ResultadoGuardado(r.Cumple, r.Razon)));

    public static DictamenDeDevolucion DeserializarDictamen(string json) => new(
        JsonSerializer.Deserialize<List<ResultadoGuardado>>(json)!.Select(r => new ResultadoDeRegla(r.Cumple, r.Razon)));

    public static string Serializar(IReadOnlyDictionary<CategoriaDeProducto, int> plazos) =>
        JsonSerializer.Serialize(plazos.ToDictionary(p => p.Key.ToString(), p => p.Value));

    public static IReadOnlyDictionary<CategoriaDeProducto, int> DeserializarPlazos(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, int>>(json)!
            .ToDictionary(p => Enum.Parse<CategoriaDeProducto>(p.Key), p => p.Value);

    public static string Serializar(IReadOnlyList<MetodoDePago> metodos) => string.Join(',', metodos);

    public static IReadOnlyList<MetodoDePago> DeserializarMetodos(string texto) =>
        texto.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<MetodoDePago>).ToList();

    public static readonly ValueComparer<IReadOnlyList<MetodoDePago>> ComparadorDeMetodos = new(
        (a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, m) => HashCode.Combine(h, m)), v => v.ToList());

    public static readonly ValueComparer<IReadOnlyDictionary<CategoriaDeProducto, int>> ComparadorDePlazos = new(
        (a, b) => a!.Count == b!.Count && !a.Except(b).Any(), v => v.Count, v => new Dictionary<CategoriaDeProducto, int>(v));
}
