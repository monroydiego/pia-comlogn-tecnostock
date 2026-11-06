using TecnoStock.Aplicacion.Dtos;
using TecnoStock.Aplicacion.Eventos;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.Eventos;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

/// <summary>Almacén en memoria que simula la base de datos; los cambios se aplican solo al Confirmar.</summary>
public sealed class AlmacenEnMemoria
{
    public Dictionary<string, Apartado> Apartados { get; } = new();
    public List<UnidadInventario> Unidades { get; } = new();
    public Dictionary<string, Venta> Ventas { get; } = new();
    public Dictionary<string, Devolucion> Devoluciones { get; } = new();
    public Dictionary<ProductoId, Producto> Productos { get; } = new();
    public PoliticaComercial Politica { get; set; } = PoliticaComercial.PorDefecto();
    public List<Action> Pendientes { get; } = new();
    public List<Comprobante> Comprobantes { get; } = new();
    public List<(string Destinatario, string Mensaje)> Avisos { get; } = new();
    public int Confirmaciones { get; set; }
    public int Descartes { get; set; }
}

public sealed class RepositorioDeApartadosEnMemoria(AlmacenEnMemoria a) : IRepositorioDeApartados
{
    public Apartado? Obtener(Folio folio) => a.Apartados.GetValueOrDefault(folio.Valor);
    public IReadOnlyList<Apartado> ObtenerVigentesVencidos(DateTime ahora) =>
        a.Apartados.Values.Where(x => x.Estado == EstadoDeApartado.Vigente && x.Vigencia.HaVencido(ahora)).ToList();
    public void Agregar(Apartado x) => a.Pendientes.Add(() => a.Apartados[x.Folio.Valor] = x);
    public void Guardar(Apartado x) => a.Pendientes.Add(() => a.Apartados[x.Folio.Valor] = x);
}

public sealed class RepositorioDeUnidadesEnMemoria(AlmacenEnMemoria a) : IRepositorioDeUnidades
{
    public UnidadInventario? Obtener(UnidadId id) => a.Unidades.FirstOrDefault(u => u.Id == id);
    public UnidadInventario? ObtenerPorSerie(NumeroDeSerie s) => a.Unidades.FirstOrDefault(u => u.Serie == s);
    public bool ExisteSerie(NumeroDeSerie s) => ObtenerPorSerie(s) is not null;
    public UnidadInventario? ObtenerUnidadDisponible(ProductoId p, SucursalId s) =>
        a.Unidades.FirstOrDefault(u => u.ProductoId == p && u.SucursalActual == s && u.Estado == EstadoDeUnidad.Disponible);
    public UnidadInventario? ObtenerUnidadParaVenta(ProductoId p, SucursalId s, NumeroDeSerie? serie, IReadOnlyCollection<UnidadId>? excluidas = null) =>
        serie is not null
            ? a.Unidades.FirstOrDefault(u => u.ProductoId == p && u.Serie == serie && excluidas?.Contains(u.Id) != true)
            : a.Unidades.FirstOrDefault(u => u.ProductoId == p && u.SucursalActual == s && u.Estado == EstadoDeUnidad.Disponible
                                              && excluidas?.Contains(u.Id) != true);
    public void Agregar(UnidadInventario u) => a.Pendientes.Add(() => { if (!a.Unidades.Contains(u)) a.Unidades.Add(u); });
    public void Guardar(UnidadInventario u) => Agregar(u);
}

public sealed class RepositorioDeVentasEnMemoria(AlmacenEnMemoria a) : IRepositorioDeVentas
{
    public Venta? ObtenerPorFolio(Folio f) => a.Ventas.GetValueOrDefault(f.Valor);
    public void Agregar(Venta v) => a.Pendientes.Add(() => a.Ventas[v.Folio.Valor] = v);
}

public sealed class RepositorioDeDevolucionesEnMemoria(AlmacenEnMemoria a) : IRepositorioDeDevoluciones
{
    public Devolucion? Obtener(Folio f) => a.Devoluciones.GetValueOrDefault(f.Valor);
    public void Agregar(Devolucion d) => a.Pendientes.Add(() => a.Devoluciones[d.Folio.Valor] = d);
    public void Guardar(Devolucion d) => Agregar(d);
}

public sealed class RepositorioDePoliticasEnMemoria(AlmacenEnMemoria a) : IRepositorioDePoliticas
{
    public PoliticaComercial ObtenerPoliticaVigente() => a.Politica;
}

public sealed class RepositorioDeProductosEnMemoria(AlmacenEnMemoria a) : IRepositorioDeProductos
{
    public Producto? Obtener(ProductoId id) => a.Productos.GetValueOrDefault(id);
}

/// <summary>Aplica los cambios al confirmar y publica los eventos de todos los aggregates modificados.</summary>
public sealed class UnidadDeTrabajoEnMemoria(AlmacenEnMemoria a, IPublicadorDeEventos publicador) : IUnidadDeTrabajo
{
    public void Iniciar() => a.Pendientes.Clear();

    public void Confirmar()
    {
        var pendientes = a.Pendientes.ToList();
        a.Pendientes.Clear();
        foreach (var cambio in pendientes) cambio();
        a.Confirmaciones++;

        var eventos = new List<EventoDeDominio>();
        eventos.AddRange(a.Apartados.Values.SelectMany(x => x.ExtraerEventos()));
        eventos.AddRange(a.Unidades.SelectMany(x => x.ExtraerEventos()));
        eventos.AddRange(a.Ventas.Values.SelectMany(x => x.ExtraerEventos()));
        eventos.AddRange(a.Devoluciones.Values.SelectMany(x => x.ExtraerEventos()));
        publicador.Publicar(eventos);
    }

    public void Descartar()
    {
        a.Pendientes.Clear();
        a.Descartes++;
    }
}

/// <summary>Despacha cada evento a su manejador, como lo hará PublicadorDeEventosEnProceso.</summary>
public sealed class PublicadorDeEventosDePrueba : IPublicadorDeEventos
{
    public ManejadorDeComprobantes? Comprobantes { get; set; }
    public ManejadorDeReingresoDeUnidades? Reingreso { get; set; }
    public ManejadorDeAvisos? Avisos { get; set; }
    public List<EventoDeDominio> Publicados { get; } = new();

    public void Publicar(IEnumerable<EventoDeDominio> eventos)
    {
        foreach (var e in eventos.ToList())
        {
            Publicados.Add(e);
            switch (e)
            {
                case ApartadoCreado x: Comprobantes!.AlCrearseApartado(x); break;
                case VentaCerrada x: Comprobantes!.AlCerrarseVenta(x); break;
                case DevolucionAceptada x: Comprobantes!.AlAceptarseDevolucion(x); Reingreso!.AlAceptarseDevolucion(x); break;
                case DevolucionDerivadaASupervisor x: Avisos!.AlDerivarseDevolucion(x); break;
                case ApartadoVencido x: Avisos!.AlVencerseApartado(x); break;
            }
        }
    }
}

public sealed class RelojFijo : IReloj
{
    public DateTime Actual { get; set; } = new(2026, 11, 6, 10, 0, 0);
    public DateTime Ahora() => Actual;
}

public sealed class GeneradorDeFoliosSecuencial : IGeneradorDeFolios
{
    private readonly Dictionary<TipoDeOperacion, int> _contadores = new();
    public Folio Siguiente(TipoDeOperacion tipo)
    {
        _contadores[tipo] = _contadores.GetValueOrDefault(tipo) + 1;
        var prefijo = tipo switch { TipoDeOperacion.Apartado => "APT", TipoDeOperacion.Venta => "VTA", TipoDeOperacion.Devolucion => "DEV", _ => "CMP" };
        return new Folio($"{prefijo}-{_contadores[tipo]:000000}");
    }
}

public sealed class ContextoDeUsuarioFijo : IContextoDeUsuario
{
    public Operador Operador { get; set; } = new("cajero1", Rol.Cajero);
    public Operador OperadorActual() => Operador;
}

public sealed class EmisorDeComprobantesEspia(AlmacenEnMemoria a) : IEmisorDeComprobantes
{
    public void Emitir(Comprobante c) => a.Comprobantes.Add(c);
}

public sealed class NotificadorEspia(AlmacenEnMemoria a) : INotificador
{
    public void Avisar(string destinatario, string mensaje) => a.Avisos.Add((destinatario, mensaje));
}

public sealed class LectorDeInventarioEnMemoria(AlmacenEnMemoria a) : ILectorDeInventario
{
    private static readonly string[] Sucursales = ["SUC-01", "SUC-02", "SUC-03", "SUC-04", "SUC-05"];

    private int Cantidad(ProductoId p, string sucursal) =>
        a.Unidades.Count(u => u.ProductoId == p && u.SucursalActual.Valor == sucursal && u.Estado == EstadoDeUnidad.Disponible);

    public DisponibilidadDeProductoDto? DisponibilidadEnSucursal(ProductoId p, SucursalId s) =>
        a.Productos.ContainsKey(p) ? new(p.Valor, s.Valor, Cantidad(p, s.Valor)) : null;

    public IReadOnlyList<DisponibilidadPorSucursalDto>? DisponibilidadEnOtrasSucursales(ProductoId p, SucursalId actual) =>
        !a.Productos.ContainsKey(p)
            ? null
            : Sucursales.Where(s => s != actual.Valor).Select(s => new DisponibilidadPorSucursalDto(s, Cantidad(p, s)))
                .Where(d => d.CantidadDisponible > 0).ToList();
}

public sealed class LectorDeDevolucionesEnMemoria(AlmacenEnMemoria a) : ILectorDeDevoluciones
{
    public EstadoDeDevolucionDto? EstadoDeDevolucion(Folio folio) =>
        a.Devoluciones.TryGetValue(folio.Valor, out var d) ? EstadoDeDevolucionDto.Desde(d) : null;
    public CriterioDeDevolucionDto CriterioVigente() => CriterioDeDevolucionDto.Desde(a.Politica);
}
