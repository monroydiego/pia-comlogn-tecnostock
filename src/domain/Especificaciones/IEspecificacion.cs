using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Dominio.Especificaciones;

/// <summary>Specification (G-65, ADR-007): regla reutilizable que explica por qué falla y se combina con Y, O y No.</summary>
public interface IEspecificacion<T>
{
    ResultadoDeRegla EsSatisfechaPor(T candidato);

    IEspecificacion<T> Y(IEspecificacion<T> otra) => new EspecificacionY<T>(this, otra);
    IEspecificacion<T> O(IEspecificacion<T> otra) => new EspecificacionO<T>(this, otra);
    IEspecificacion<T> No() => new EspecificacionNo<T>(this);
}

public sealed class EspecificacionY<T>(IEspecificacion<T> izquierda, IEspecificacion<T> derecha) : IEspecificacion<T>
{
    /// <summary>Cumple si ambas cumplen; si no, reúne las razones de las que fallan.</summary>
    public ResultadoDeRegla EsSatisfechaPor(T candidato)
    {
        var a = izquierda.EsSatisfechaPor(candidato);
        var b = derecha.EsSatisfechaPor(candidato);
        if (a.Cumple && b.Cumple) return ResultadoDeRegla.Satisfecha();
        var razones = new[] { a, b }.Where(r => !r.Cumple).Select(r => r.Razon);
        return ResultadoDeRegla.Incumplida(string.Join("; ", razones));
    }
}

public sealed class EspecificacionO<T>(IEspecificacion<T> izquierda, IEspecificacion<T> derecha) : IEspecificacion<T>
{
    /// <summary>Cumple si al menos una cumple; si ninguna, reúne ambas razones.</summary>
    public ResultadoDeRegla EsSatisfechaPor(T candidato)
    {
        var a = izquierda.EsSatisfechaPor(candidato);
        if (a.Cumple) return a;
        var b = derecha.EsSatisfechaPor(candidato);
        if (b.Cumple) return b;
        return ResultadoDeRegla.Incumplida($"{a.Razon}; {b.Razon}");
    }
}

public sealed class EspecificacionNo<T>(IEspecificacion<T> original) : IEspecificacion<T>
{
    public ResultadoDeRegla EsSatisfechaPor(T candidato)
    {
        var r = original.EsSatisfechaPor(candidato);
        return r.Cumple
            ? ResultadoDeRegla.Incumplida("Se cumple una condición que debía no cumplirse.")
            : ResultadoDeRegla.Satisfecha();
    }
}
