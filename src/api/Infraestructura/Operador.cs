using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;

namespace TecnoStock.Api.Infraestructura;

/// <summary>Puerto IContextoDeUsuario (G-75): el operador y su rol llegan por los encabezados X-Usuario y X-Rol.</summary>
public sealed class ContextoDeUsuarioPorEncabezados(IHttpContextAccessor acceso) : IContextoDeUsuario
{
    public const string EncabezadoUsuario = "X-Usuario";
    public const string EncabezadoRol = "X-Rol";

    public static bool TryObtener(HttpRequest peticion, out Operador? operador, out string error)
    {
        operador = null;
        var usuario = peticion.Headers[EncabezadoUsuario].ToString();
        var rol = peticion.Headers[EncabezadoRol].ToString();
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(rol))
        {
            error = $"Faltan los encabezados {EncabezadoUsuario} y {EncabezadoRol}.";
            return false;
        }
        if (!Enum.TryParse<Rol>(rol, true, out var rolValido) || !Enum.IsDefined(rolValido))
        {
            error = $"El rol '{rol}' no es válido. Use Vendedor, Cajero, Supervisor o Almacen.";
            return false;
        }
        operador = new Operador(usuario, rolValido);
        error = string.Empty;
        return true;
    }

    public Operador OperadorActual()
    {
        var peticion = acceso.HttpContext?.Request ?? throw new InvalidOperationException("No hay una petición HTTP en curso.");
        return TryObtener(peticion, out var operador, out var error) ? operador! : throw new ArgumentException(error);
    }
}

/// <summary>Exige los encabezados del operador; responde 400 si faltan o el rol no es válido.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiereOperadorAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext contexto)
    {
        if (!ContextoDeUsuarioPorEncabezados.TryObtener(contexto.HttpContext.Request, out _, out var error))
            contexto.Result = new ObjectResult(new ProblemDetails { Status = 400, Title = "DatosInvalidos", Detail = error }) { StatusCode = 400 };
    }
}
