namespace TecnoStock.Dominio.ObjetosDeValor;

/// <summary>Resultado de evaluar una regla: si cumple y, si no, la razón en términos del negocio.</summary>
public sealed record ResultadoDeRegla(bool Cumple, string Razon)
{
    public static ResultadoDeRegla Satisfecha() => new(true, string.Empty);
    public static ResultadoDeRegla Incumplida(string razon) => new(false, razon);
}
