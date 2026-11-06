using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Aplicacion.Eventos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Repositorios;
using TecnoStock.Infraestructura.Eventos;
using TecnoStock.Infraestructura.Lectores;
using TecnoStock.Infraestructura.Persistencia;
using TecnoStock.Infraestructura.Repositorios;
using TecnoStock.Infraestructura.Servicios;

namespace TecnoStock.Infraestructura;

public static class DependencyInjection
{
    /// <summary>
    /// Cadena de conexión 12-Factor: ConnectionStrings__Default si existe; si no, se arma con DB_HOST, DB_PORT,
    /// DB_NAME, DB_USER (por defecto sa) y MSSQL_SA_PASSWORD. Nunca se guardan secretos en el repositorio.
    /// </summary>
    public static string ConstruirCadenaDeConexion(IConfiguration cfg)
    {
        var directa = cfg.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(directa)) return directa;

        var contrasena = cfg["MSSQL_SA_PASSWORD"]
            ?? throw new InvalidOperationException(
                "Falta la variable de entorno MSSQL_SA_PASSWORD (o ConnectionStrings__Default). Defínala o cree un archivo .env a partir de .env.example.");
        return new SqlConnectionStringBuilder
        {
            DataSource = $"{cfg["DB_HOST"] ?? "localhost"},{cfg["DB_PORT"] ?? "1433"}",
            InitialCatalog = cfg["DB_NAME"] ?? "TecnoStock",
            UserID = cfg["DB_USER"] ?? "sa",
            Password = contrasena,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        }.ConnectionString;
    }

    public static IServiceCollection AddInfraestructura(this IServiceCollection servicios, IConfiguration cfg)
    {
        servicios.AddDbContext<ContextoDeDatos>(o => o.UseSqlServer(ConstruirCadenaDeConexion(cfg)));

        servicios.AddScoped<IRepositorioDeApartados, RepositorioDeApartadosEfCore>();
        servicios.AddScoped<IRepositorioDeUnidades, RepositorioDeUnidadesEfCore>();
        servicios.AddScoped<IRepositorioDeVentas, RepositorioDeVentasEfCore>();
        servicios.AddScoped<IRepositorioDeDevoluciones, RepositorioDeDevolucionesEfCore>();
        servicios.AddScoped<IRepositorioDePoliticas, RepositorioDePoliticasEfCore>();
        servicios.AddScoped<IRepositorioDeProductos, RepositorioDeProductosEfCore>();

        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajoEfCore>();
        servicios.AddScoped<IPublicadorDeEventos, PublicadorDeEventosEnProceso>();
        servicios.AddScoped<ILectorDeInventario, LectorDeInventarioSql>();
        servicios.AddScoped<ILectorDeDevoluciones, LectorDeDevolucionesSql>();
        servicios.AddScoped<IGeneradorDeFolios, GeneradorDeFoliosSql>();
        servicios.AddScoped<IEmisorDeComprobantes, EmisorDeComprobantesBasico>();
        servicios.AddScoped<INotificador, NotificadorDeAvisos>();
        servicios.AddSingleton<IReloj, RelojDelSistema>();
        return servicios;
    }
}
