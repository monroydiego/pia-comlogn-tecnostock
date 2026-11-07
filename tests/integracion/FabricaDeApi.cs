using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TecnoStock.Aplicacion.Puertos;

namespace TecnoStock.Pruebas.Integracion;

public sealed class RelojDePrueba : IReloj
{
    public DateTime Actual { get; private set; } = DateTime.UtcNow;
    public DateTime Ahora() => Actual;
    public void Avanzar(int dias) => Actual = Actual.AddDays(dias);
}

/// <summary>
/// Levanta la API completa contra SQL Server real, en una base de datos exclusiva por fixture
/// (se crea al iniciar con los datos semilla y se elimina al terminar).
/// Conexión por variables de entorno: DB_HOST, DB_PORT, MSSQL_SA_PASSWORD.
/// </summary>
public sealed class FabricaDeApi : WebApplicationFactory<Program>
{
    public string NombreDeBd { get; } = $"TecnoStock_Pruebas_{Guid.NewGuid():N}";
    public RelojDePrueba Reloj { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Development");
        constructor.UseSetting("DB_NAME", NombreDeBd);
        constructor.ConfigureServices(s =>
        {
            s.RemoveAll<IReloj>();
            s.AddSingleton<IReloj>(Reloj);
        });
    }

    public IServiceScope NuevoAlcance() => Services.CreateScope();

    private static string CadenaAlMaster() => new SqlConnectionStringBuilder
    {
        DataSource = $"{Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"},{Environment.GetEnvironmentVariable("DB_PORT") ?? "1433"}",
        InitialCatalog = "master",
        UserID = "sa",
        Password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD"),
        TrustServerCertificate = true
    }.ConnectionString;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqlConnection.ClearAllPools();
        using var conexion = new SqlConnection(CadenaAlMaster());
        conexion.Open();
        using var comando = conexion.CreateCommand();
        comando.CommandText = $"IF DB_ID('{NombreDeBd}') IS NOT NULL BEGIN ALTER DATABASE [{NombreDeBd}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{NombreDeBd}]; END";
        comando.ExecuteNonQuery();
    }
}

public sealed record Respuesta(HttpStatusCode Codigo, JsonElement Cuerpo)
{
    public string Texto(string propiedad) => Cuerpo.GetProperty(propiedad).GetString()!;
}

public static class AyudasHttp
{
    public static async Task<Respuesta> Enviar(this HttpClient cliente, HttpMethod metodo, string url, object? cuerpo = null,
        string? rol = "Cajero", string usuario = "usuario1")
    {
        var peticion = new HttpRequestMessage(metodo, url);
        if (cuerpo is not null) peticion.Content = JsonContent.Create(cuerpo);
        if (rol is not null)
        {
            peticion.Headers.Add("X-Usuario", usuario);
            peticion.Headers.Add("X-Rol", rol);
        }
        var respuesta = await cliente.SendAsync(peticion);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var json = string.IsNullOrWhiteSpace(texto) ? default : JsonDocument.Parse(texto).RootElement.Clone();
        return new Respuesta(respuesta.StatusCode, json);
    }

    /// <summary>Envía el JSON tal cual (sin volver a serializarlo): sirve para probar valores que un objeto C# no puede expresar.</summary>
    public static async Task<Respuesta> PostJson(this HttpClient cliente, string url, string json, string? rol = "Cajero", string usuario = "usuario1")
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        if (rol is not null)
        {
            peticion.Headers.Add("X-Usuario", usuario);
            peticion.Headers.Add("X-Rol", rol);
        }
        var respuesta = await cliente.SendAsync(peticion);
        var texto = await respuesta.Content.ReadAsStringAsync();
        return new Respuesta(respuesta.StatusCode, string.IsNullOrWhiteSpace(texto) ? default : JsonDocument.Parse(texto).RootElement.Clone());
    }

    public static Task<Respuesta> Post(this HttpClient c, string url, object? cuerpo = null, string? rol = "Cajero") => c.Enviar(HttpMethod.Post, url, cuerpo, rol);
    public static Task<Respuesta> Get(this HttpClient c, string url) => c.Enviar(HttpMethod.Get, url, null, null);

    public static object Pago(string metodo, decimal monto) => new { metodo, monto };

    /// <summary>Series sintéticas del sembrador: SN-{sku}-{n:000}; la n-ésima unidad (1..) del producto va a SUC-0((n-1)%5+1).</summary>
    public static string Serie(string sku, int n) => $"SN-{sku}-{n:000}";

    public static object Apartar(string producto, decimal anticipo, string metodo = "Efectivo", string sucursal = "SUC-01") =>
        new { productoId = producto, sucursalId = sucursal, clienteId = "TEC-C001", anticipo = Pago(metodo, anticipo) };

    /// <summary>Sucursal sembrada de la n-ésima unidad (1..): SUC-01, SUC-02, ... en ciclo.</summary>
    public static string SucursalDe(int n) => $"SUC-0{(n - 1) % 5 + 1}";

    public static object VentaConSerie(string producto, string? serie, decimal pago, string sucursal = "SUC-01") =>
        new { sucursalId = sucursal, lineas = new[] { new { productoId = producto, serie } }, pagos = new[] { Pago("Efectivo", pago) } };

    public static async Task<int> Disponibles(this HttpClient c, string producto, string sucursal = "SUC-01") =>
        (await c.Get($"/disponibilidad/{producto}?sucursalId={sucursal}")).Cuerpo.GetProperty("cantidadDisponible").GetInt32();
}
