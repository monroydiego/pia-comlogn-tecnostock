using TecnoStock.Aplicacion.Comun;
using TecnoStock.Dominio.Comun;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

internal static class Ayudas
{
    public static Rechazo DebeRechazar(this Resultado r, MotivoDeRechazo motivo)
    {
        Assert.False(r.Exitoso, "Se esperaba un rechazo.");
        Assert.Equal(motivo, r.Rechazo!.Motivo);
        return r.Rechazo;
    }

    public static T DebeSerExitoso<T>(this Resultado r)
    {
        Assert.True(r.Exitoso, r.Rechazo?.MensajeDeNegocio);
        return r.ValorComo<T>();
    }
}
