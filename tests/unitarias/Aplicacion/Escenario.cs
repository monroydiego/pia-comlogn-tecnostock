using TecnoStock.Aplicacion.Comun;
using TecnoStock.Aplicacion.Eventos;
using TecnoStock.Aplicacion.Servicios;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Servicios;

namespace TecnoStock.Pruebas.Unitarias.Aplicacion;

/// <summary>Arma los servicios con los dobles en memoria y siembra un catálogo mínimo.</summary>
public sealed class Escenario
{
    public static readonly SucursalId Suc1 = new("SUC-01");
    public static readonly SucursalId Suc2 = new("SUC-02");
    public static readonly ClienteId Cliente = new("TEC-C001");

    public AlmacenEnMemoria Almacen { get; } = new();
    public RelojFijo Reloj { get; } = new();
    public ContextoDeUsuarioFijo Contexto { get; } = new();
    public PublicadorDeEventosDePrueba Publicador { get; } = new();
    public ServicioDeApartados Apartados { get; }
    public ServicioDeVentas Ventas { get; }
    public ServicioDeDevoluciones Devoluciones { get; }
    public ServicioDeInventario Inventario { get; }
    public ConsultasDeInventario ConsultasInventario { get; }
    public ConsultasDeDevoluciones ConsultasDevoluciones { get; }

    public Producto Laptop { get; } = Producto.Crear(new ProductoId("TEC-P001"), "TEC-P001", "Laptop 01",
        CategoriaDeProducto.Computadoras, true, true, new Dinero(1000m));
    public Producto Teclado { get; } = Producto.Crear(new ProductoId("TEC-P004"), "TEC-P004", "Teclado 04",
        CategoriaDeProducto.Accesorios, false, false, new Dinero(500m));

    public Escenario()
    {
        Almacen.Productos[Laptop.Id] = Laptop;
        Almacen.Productos[Teclado.Id] = Teclado;

        var uow = new UnidadDeTrabajoEnMemoria(Almacen, Publicador);
        var apartados = new RepositorioDeApartadosEnMemoria(Almacen);
        var unidades = new RepositorioDeUnidadesEnMemoria(Almacen);
        var productos = new RepositorioDeProductosEnMemoria(Almacen);
        var politicas = new RepositorioDePoliticasEnMemoria(Almacen);
        var ventas = new RepositorioDeVentasEnMemoria(Almacen);
        var devoluciones = new RepositorioDeDevolucionesEnMemoria(Almacen);
        var folios = new GeneradorDeFoliosSecuencial();
        var traductor = new TraductorDeRechazos();

        Apartados = new ServicioDeApartados(apartados, unidades, productos, politicas, ventas, new ConversionDeApartado(),
            uow, Reloj, folios, Contexto, traductor);
        Ventas = new ServicioDeVentas(ventas, unidades, productos, politicas, uow, Reloj, folios, Contexto, traductor);
        Devoluciones = new ServicioDeDevoluciones(ventas, devoluciones, productos, politicas, uow, Reloj, folios, Contexto, traductor);
        Inventario = new ServicioDeInventario(unidades, productos, uow, traductor);
        ConsultasInventario = new ConsultasDeInventario(new LectorDeInventarioEnMemoria(Almacen));
        ConsultasDevoluciones = new ConsultasDeDevoluciones(new LectorDeDevolucionesEnMemoria(Almacen));

        Publicador.Comprobantes = new ManejadorDeComprobantes(new EmisorDeComprobantesEspia(Almacen), folios, Contexto, ventas);
        Publicador.Reingreso = new ManejadorDeReingresoDeUnidades(unidades, uow);
        Publicador.Avisos = new ManejadorDeAvisos(new NotificadorEspia(Almacen));
    }

    public UnidadInventario AgregarUnidad(Producto producto, string? serie, SucursalId? sucursal = null)
    {
        var unidad = UnidadInventario.Registrar(UnidadId.Nuevo(), producto.Id, sucursal ?? Suc1,
            serie is null ? null : new NumeroDeSerie(serie));
        Almacen.Unidades.Add(unidad);
        return unidad;
    }

    public static PagoRecibido Pago(decimal monto, MetodoDePago metodo = MetodoDePago.Efectivo) => new(metodo, new Dinero(monto));

    public void ComoRol(Rol rol) => Contexto.Operador = new Operador($"{rol}1".ToLowerInvariant(), rol);
}
