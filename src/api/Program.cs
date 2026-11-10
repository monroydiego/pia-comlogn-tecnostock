using System.Text.Json.Serialization;
using TecnoStock.Aplicacion;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Api.Infraestructura;
using TecnoStock.Infraestructura;
using TecnoStock.Infraestructura.Persistencia;

ArchivoEnv.CargarSiExiste();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddAplicacion().AddInfraestructura(builder.Configuration);
builder.Services.AddScoped<IContextoDeUsuario, ContextoDeUsuarioPorEncabezados>();
builder.Services.AddSingleton<TraductorDeRechazosHttp>();
builder.Services.AddExceptionHandler<ManejadorDeExcepciones>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "TecnoStock API", Version = "v1", Description = "Ventas, apartados, devoluciones e inventario de TecnoStock (PIA, Componentes de lógica de negocio)." });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TecnoStock.Api.xml"));
    o.OperationFilter<EncabezadosDeOperadorFiltro>();
});

var app = builder.Build();

await Sembrador.InicializarAsync(app.Services, Path.Combine(AppContext.BaseDirectory, "datos"));

app.UseExceptionHandler();
app.UseDefaultFiles();   // sirve wwwroot/index.html en "/" (frontend de TecnoStock)
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "TecnoStock API v1"));
app.MapControllers();

app.Run();

public partial class Program;
