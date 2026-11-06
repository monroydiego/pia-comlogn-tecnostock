namespace TecnoStock.Dominio.ObjetosDeValor;

public sealed record VigenciaDeApartado
{
    public DateTime Desde { get; }
    public DateTime Hasta { get; }

    public VigenciaDeApartado(DateTime desde, DateTime hasta)
    {
        if (hasta < desde) throw new ArgumentException("La vigencia termina antes de empezar.", nameof(hasta));
        Desde = desde;
        Hasta = hasta;
    }

    /// <summary>El apartado ha vencido cuando se alcanza o supera la fecha límite.</summary>
    public bool HaVencido(DateTime ahora) => ahora >= Hasta;
}
