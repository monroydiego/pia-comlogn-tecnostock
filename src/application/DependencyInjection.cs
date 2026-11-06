using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Eventos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Dominio.Servicios;

namespace TecnoStock.Aplicacion;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton<TraductorDeRechazos>();
        servicios.AddSingleton<ConversionDeApartado>();
        servicios.AddScoped<ServicioDeApartados>();
        servicios.AddScoped<ServicioDeVentas>();
        servicios.AddScoped<ServicioDeDevoluciones>();
        servicios.AddScoped<ServicioDeInventario>();
        servicios.AddScoped<ConsultasDeInventario>();
        servicios.AddScoped<ConsultasDeDevoluciones>();
        servicios.AddScoped<ManejadorDeComprobantes>();
        servicios.AddScoped<ManejadorDeReingresoDeUnidades>();
        servicios.AddScoped<ManejadorDeAvisos>();
        return servicios;
    }
}
