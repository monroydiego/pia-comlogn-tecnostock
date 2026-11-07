using TecnoStock.Dominio.Comun;

namespace TecnoStock.Dominio.ObjetosDeValor;

public readonly record struct ProductoId
{
    public string Valor { get; }
    public ProductoId(string valor) => Valor = Limites.Texto(valor, Limites.Codigo, "El identificador del producto");
    public override string ToString() => Valor ?? string.Empty;
}

public readonly record struct SucursalId
{
    public string Valor { get; }
    public SucursalId(string valor) => Valor = Limites.Texto(valor, Limites.Codigo, "El identificador de la sucursal");
    public override string ToString() => Valor ?? string.Empty;
}

public readonly record struct ClienteId
{
    public string Valor { get; }
    public ClienteId(string valor) => Valor = Limites.Texto(valor, Limites.Codigo, "El identificador del cliente");
    public override string ToString() => Valor ?? string.Empty;
}

public readonly record struct UnidadId(Guid Valor)
{
    public static UnidadId Nuevo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}
