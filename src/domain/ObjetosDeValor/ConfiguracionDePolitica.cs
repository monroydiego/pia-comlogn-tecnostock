using TecnoStock.Dominio.Enumeraciones;

namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Un método de pago autorizado por la política (una fila por método, 1FN).</summary>
public sealed record MetodoDePagoAutorizado(MetodoDePago Metodo);

/// <summary>Plazo de devolución en días naturales de una categoría (una fila por categoría, 1FN).</summary>
public sealed record PlazoDeDevolucion(CategoriaDeProducto Categoria, int Dias);
