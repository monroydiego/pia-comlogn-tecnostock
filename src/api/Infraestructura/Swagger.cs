using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TecnoStock.Api.Infraestructura;

/// <summary>Muestra en Swagger UI los encabezados del operador en las operaciones que los exigen.</summary>
public sealed class EncabezadosDeOperadorFiltro : IOperationFilter
{
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        var exige = contexto.MethodInfo.GetCustomAttributes(true).OfType<RequiereOperadorAttribute>().Any()
                    || contexto.MethodInfo.DeclaringType!.GetCustomAttributes(true).OfType<RequiereOperadorAttribute>().Any();
        if (!exige) return;
        operacion.Parameters ??= new List<IOpenApiParameter>();
        operacion.Parameters.Add(new OpenApiParameter
        {
            Name = ContextoDeUsuarioPorEncabezados.EncabezadoUsuario, In = ParameterLocation.Header, Required = true,
            Description = "Identificador del operador que ejecuta la operación.", Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
        operacion.Parameters.Add(new OpenApiParameter
        {
            Name = ContextoDeUsuarioPorEncabezados.EncabezadoRol, In = ParameterLocation.Header, Required = true,
            Description = "Rol del operador: Vendedor, Cajero, Supervisor o Almacen.", Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}
