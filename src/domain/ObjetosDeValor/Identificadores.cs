namespace TecnoStock.Dominio.ObjetosDeValor;

public readonly record struct ProductoId(string Valor)
{
    public override string ToString() => Valor;
}

public readonly record struct SucursalId(string Valor)
{
    public override string ToString() => Valor;
}

public readonly record struct ClienteId(string Valor)
{
    public override string ToString() => Valor;
}

public readonly record struct UnidadId(Guid Valor)
{
    public static UnidadId Nuevo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}
