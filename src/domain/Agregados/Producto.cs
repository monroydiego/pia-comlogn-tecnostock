using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Agregados;

/// <summary>Artículo del catálogo (G-28).</summary>
public sealed class Producto : RaizDeAgregado
{
    public ProductoId Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public CategoriaDeProducto Categoria { get; private set; }
    public bool RequiereSerie { get; private set; }
    public bool PermiteApartado { get; private set; }
    public Dinero PrecioDeLista { get; private set; }

    private Producto() { }

    public static Producto Crear(ProductoId id, string sku, string nombre, CategoriaDeProducto categoria,
        bool requiereSerie, bool permiteApartado, Dinero precioDeLista)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        return new Producto
        {
            Id = id, Sku = sku.Trim(), Nombre = nombre.Trim(), Categoria = categoria,
            RequiereSerie = requiereSerie, PermiteApartado = permiteApartado, PrecioDeLista = precioDeLista
        };
    }
}
