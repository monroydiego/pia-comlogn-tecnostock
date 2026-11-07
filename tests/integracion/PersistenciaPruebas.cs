using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TecnoStock.Aplicacion.Puertos;
using TecnoStock.Dominio.Agregados;
using TecnoStock.Dominio.Comun;
using TecnoStock.Dominio.Enumeraciones;
using TecnoStock.Dominio.ObjetosDeValor;
using TecnoStock.Dominio.Repositorios;
using TecnoStock.Infraestructura.Persistencia;
using static TecnoStock.Pruebas.Integracion.AyudasHttp;

namespace TecnoStock.Pruebas.Integracion;

/// <summary>ADR-009: concurrencia por versión, índices únicos, mapeo de aggregates y datos semilla sobre SQL Server real.</summary>
public sealed class PersistenciaPruebas : IClassFixture<FabricaDeApi>
{
    private static readonly SucursalId Suc1 = new("SUC-01");
    private readonly FabricaDeApi _fabrica;

    public PersistenciaPruebas(FabricaDeApi fabrica) => _fabrica = fabrica;

    private sealed class Contexto(IServiceScope alcance) : IDisposable
    {
        public T Obtener<T>() where T : notnull => alcance.ServiceProvider.GetRequiredService<T>();
        public void Dispose() => alcance.Dispose();
    }

    private Contexto Nuevo() => new(_fabrica.NuevoAlcance());

    private void Sembrar(UnidadInventario unidad)
    {
        using var c = Nuevo();
        var uow = c.Obtener<IUnidadDeTrabajo>();
        uow.Iniciar();
        c.Obtener<IRepositorioDeUnidades>().Agregar(unidad);
        uow.Confirmar();
    }

    [Fact]
    public void Concurrencia_optimista_la_segunda_operacion_sobre_la_misma_unidad_recibe_ConflictoDeConcurrencia()
    {
        var id = UnidadId.Nuevo();
        Sembrar(UnidadInventario.Registrar(id, new ProductoId("TEC-P018"), Suc1, new NumeroDeSerie("SN-CONCURRENCIA-1")));

        using var a = Nuevo();
        using var b = Nuevo();
        var uowA = a.Obtener<IUnidadDeTrabajo>();
        var uowB = b.Obtener<IUnidadDeTrabajo>();
        uowA.Iniciar();
        uowB.Iniciar();
        var unidadA = a.Obtener<IRepositorioDeUnidades>().Obtener(id)!;
        var unidadB = b.Obtener<IRepositorioDeUnidades>().Obtener(id)!; // ambas leen la misma versión

        unidadA.Vender(new Folio("VTA-CONC-A"), Suc1);
        uowA.Confirmar();

        unidadB.Apartar(new Folio("APT-CONC-B"), Suc1); // su copia está desactualizada
        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => uowB.Confirmar());
        Assert.Equal(MotivoDeRechazo.ConflictoDeConcurrencia, ex.Motivo);

        using var lectura = Nuevo();
        var final = lectura.Obtener<IRepositorioDeUnidades>().Obtener(id)!;
        Assert.Equal(EstadoDeUnidad.Vendida, final.Estado); // gana la primera
        Assert.Equal(1, final.Version);
    }

    [Fact]
    public void RN_INV_002_El_indice_unico_impide_la_serie_duplicada_aunque_las_dos_pasen_la_validacion()
    {
        var serie = new NumeroDeSerie("SN-DUPLICADA-1");
        using var a = Nuevo();
        using var b = Nuevo();
        var uowA = a.Obtener<IUnidadDeTrabajo>();
        var uowB = b.Obtener<IUnidadDeTrabajo>();
        uowA.Iniciar();
        uowB.Iniciar();
        Assert.False(a.Obtener<IRepositorioDeUnidades>().ExisteSerie(serie));
        Assert.False(b.Obtener<IRepositorioDeUnidades>().ExisteSerie(serie));

        a.Obtener<IRepositorioDeUnidades>().Agregar(UnidadInventario.Registrar(UnidadId.Nuevo(), new ProductoId("TEC-P018"), Suc1, serie));
        b.Obtener<IRepositorioDeUnidades>().Agregar(UnidadInventario.Registrar(UnidadId.Nuevo(), new ProductoId("TEC-P018"), Suc1, serie));
        uowA.Confirmar();

        var ex = Assert.Throws<ReglaDeNegocioViolada>(() => uowB.Confirmar());
        Assert.Equal(MotivoDeRechazo.SerieDuplicada, ex.Motivo);
    }

    [Fact]
    public void RN_APT_003_Dos_apartados_simultaneos_de_la_misma_unidad_solo_dejan_uno_vigente()
    {
        var id = UnidadId.Nuevo();
        Sembrar(UnidadInventario.Registrar(id, new ProductoId("TEC-P019"), Suc1, new NumeroDeSerie("SN-CARRERA-1")));
        var ahora = DateTime.UtcNow;

        MotivoDeRechazo? motivoB = null;
        using var a = Nuevo();
        using var b = Nuevo();
        var uowA = a.Obtener<IUnidadDeTrabajo>();
        var uowB = b.Obtener<IUnidadDeTrabajo>();
        uowA.Iniciar();
        uowB.Iniciar();

        Apartado Crear(Contexto c, string folio)
        {
            var unidad = c.Obtener<IRepositorioDeUnidades>().Obtener(id)!;
            var producto = c.Obtener<IRepositorioDeProductos>().Obtener(new ProductoId("TEC-P019"))!;
            var apartado = Apartado.Crear(new Folio(folio), new ClienteId("TEC-C001"), unidad, producto, producto.PrecioDeLista,
                new PagoRecibido(MetodoDePago.Efectivo, new Dinero(1000m)), c.Obtener<IRepositorioDePoliticas>().ObtenerPoliticaVigente(), ahora);
            c.Obtener<IRepositorioDeApartados>().Agregar(apartado);
            return apartado;
        }

        Crear(a, "APT-CARRERA-A");
        Crear(b, "APT-CARRERA-B");
        uowA.Confirmar();
        try { uowB.Confirmar(); }
        catch (ReglaDeNegocioViolada ex) { motivoB = ex.Motivo; }

        Assert.NotNull(motivoB);
        Assert.Contains(motivoB!.Value, new[] { MotivoDeRechazo.ConflictoDeConcurrencia, MotivoDeRechazo.UnidadApartada });
        using var lectura = Nuevo();
        var vigentes = lectura.Obtener<ContextoDeDatos>().Apartados.AsNoTracking().Count(x => x.UnidadId == id && x.Estado == EstadoDeApartado.Vigente);
        Assert.Equal(1, vigentes);
    }

    [Fact]
    public async Task Descartar_deshace_los_cambios_y_no_publica_eventos()
    {
        var id = UnidadId.Nuevo();
        using (var c = Nuevo())
        {
            var uow = c.Obtener<IUnidadDeTrabajo>();
            uow.Iniciar();
            c.Obtener<IRepositorioDeUnidades>().Agregar(UnidadInventario.Registrar(id, new ProductoId("TEC-P018"), Suc1, new NumeroDeSerie("SN-DESCARTADA-1")));
            uow.Descartar();
        }
        using var lectura = Nuevo();
        Assert.False(await lectura.Obtener<ContextoDeDatos>().Unidades.AnyAsync(u => u.Id == id));
    }

    [Fact]
    public async Task La_venta_con_cobro_multimedio_se_reconstruye_desde_SQL()
    {
        var cliente = _fabrica.CreateClient();
        var r = await cliente.Post("/ventas", new
        {
            sucursalId = "SUC-02",
            lineas = new[] { new { productoId = "TEC-P004", serie = (string?)null }, new { productoId = "TEC-P004", serie = (string?)null } },
            pagos = new[] { Pago("Tarjeta", 700m), Pago("Efectivo", 398m) }
        }, rol: "Cajero");
        Assert.Equal(HttpStatusCode.Created, r.Codigo);

        using var c = Nuevo();
        var venta = c.Obtener<IRepositorioDeVentas>().ObtenerPorFolio(new Folio(r.Texto("folio")))!;
        Assert.Equal(2, venta.Lineas.Count);
        Assert.Equal(2, venta.Pagos.Count);
        Assert.Equal(1098m, venta.Total().Monto);
        Assert.Equal(MetodoDePago.Tarjeta, venta.Pagos[0].Metodo);
        Assert.Equal(new Operador("usuario1", Rol.Cajero), venta.Cajero);
        Assert.Equal(new SucursalId("SUC-02"), venta.SucursalId);
        Assert.All(venta.Lineas, l => Assert.Null(l.Serie));
    }

    [Fact]
    public async Task El_apartado_se_reconstruye_con_anticipo_vigencia_y_se_actualiza_con_version()
    {
        var cliente = _fabrica.CreateClient();
        var folio = (await cliente.Post("/apartados", Apartar("TEC-P010", 300m, "Transferencia"))).Texto("folio");

        using var c = Nuevo();
        var apartado = c.Obtener<IRepositorioDeApartados>().Obtener(new Folio(folio))!;
        Assert.Equal(MetodoDePago.Transferencia, apartado.Anticipo.Metodo);
        Assert.Equal(300m, apartado.Anticipo.Monto.Monto);
        Assert.Equal(EstadoDeApartado.Vigente, apartado.Estado);
        Assert.Equal(7, (apartado.Vigencia.Hasta - apartado.Vigencia.Desde).Days);
        Assert.Equal(0, apartado.Version);

        await cliente.Post($"/apartados/{folio}/cancelar", new { motivo = "prueba" }, rol: "Supervisor");
        using var despues = Nuevo();
        var cancelado = despues.Obtener<IRepositorioDeApartados>().Obtener(new Folio(folio))!;
        Assert.Equal(EstadoDeApartado.Cancelado, cancelado.Estado);
        Assert.Equal(1, cancelado.Version);
        Assert.Equal("prueba", cancelado.Cancelacion!.Motivo);
    }

    [Fact]
    public async Task Los_datos_semilla_cargan_catalogo_clientes_politica_y_son_idempotentes()
    {
        var carpeta = Path.Combine(AppContext.BaseDirectory, "datos");
        var esperadas = File.ReadAllLines(Path.Combine(carpeta, "catalogo.csv")).Skip(1).Where(l => l.Trim().Length > 0)
            .Select(l => l.Split(',')).Where(c => c[4].Trim() == "1").Sum(c => int.Parse(c[3], CultureInfo.InvariantCulture));

        async Task<(int, int, int, int)> Contar()
        {
            using var c = Nuevo();
            var ctx = c.Obtener<ContextoDeDatos>();
            // Solo las unidades sembradas: otras pruebas de esta clase agregan unidades con series propias.
            var sembradas = (await ctx.Unidades.AsNoTracking().ToListAsync()).Count(u => u.Serie is null || u.Serie.Valor.StartsWith("SN-TEC-"));
            return (await ctx.Productos.CountAsync(), await ctx.Clientes.CountAsync(), sembradas, await ctx.Politicas.CountAsync());
        }

        var (productos, clientes, unidades, politicas) = await Contar();
        Assert.Equal((20, 40, esperadas, 1), (productos, clientes, unidades, politicas));

        await Sembrador.InicializarAsync(_fabrica.Services, carpeta);
        Assert.Equal((productos, clientes, unidades, politicas), await Contar());

        using var c2 = Nuevo();
        var producto = c2.Obtener<IRepositorioDeProductos>().Obtener(new ProductoId("TEC-P004"))!;
        Assert.False(producto.RequiereSerie);
        Assert.False(producto.PermiteApartado);
        Assert.Equal(CategoriaDeProducto.Accesorios, producto.Categoria);
        var laptop = c2.Obtener<IRepositorioDeProductos>().Obtener(new ProductoId("TEC-P001"))!;
        Assert.True(laptop.RequiereSerie && laptop.PermiteApartado);
        Assert.Equal(336m, laptop.PrecioDeLista.Monto);
        var politica = c2.Obtener<IRepositorioDePoliticas>().ObtenerPoliticaVigente();
        Assert.Equal(30m, politica.PorcentajeDeAnticipoMinimo);
        Assert.Equal(7, politica.PlazoMaximoDeApartadoEnDias);
        Assert.Equal(15, politica.PlazoDeDevolucionEnDiasPorCategoria[CategoriaDeProducto.Computadoras]);
        Assert.Equal(3, politica.MetodosAutorizados.Count);
    }

    [Fact]
    public async Task ADR_010_El_esquema_esta_normalizado_con_claves_foraneas_y_sin_columnas_con_varios_valores()
    {
        using var c = Nuevo();
        var bd = c.Obtener<ContextoDeDatos>().Database;

        var foraneas = (await bd.SqlQuery<string>($@"SELECT OBJECT_NAME(parent_object_id) + '.' + COL_NAME(parent_object_id, parent_column_id)
            + ' -> ' + OBJECT_NAME(referenced_object_id) AS [Value] FROM sys.foreign_key_columns").ToListAsync()).ToHashSet();
        foreach (var esperada in new[]
                 {
                     "UnidadesDeInventario.ProductoId -> Productos", "UnidadesDeInventario.SucursalActual -> Sucursales",
                     "UnidadesDeInventario.FolioDeApartadoVigente -> Apartados", "Apartados.UnidadId -> UnidadesDeInventario",
                     "Apartados.ClienteId -> Clientes", "Apartados.SucursalId -> Sucursales", "Ventas.SucursalId -> Sucursales",
                     "Ventas.FolioDeApartadoOrigen -> Apartados", "LineasDeVenta.ProductoId -> Productos",
                     "LineasDeVenta.UnidadId -> UnidadesDeInventario", "LineasDeVenta.FolioDeVenta -> Ventas",
                     "PagosDeVenta.FolioDeVenta -> Ventas", "Devoluciones.FolioDeVenta -> Ventas",
                     "Devoluciones.UnidadDevuelta -> UnidadesDeInventario", "DevolucionResultadosDeRegla.FolioDeDevolucion -> Devoluciones",
                     "PoliticaMetodosDePago.PoliticaId -> PoliticasComerciales", "PoliticaPlazosDeDevolucion.PoliticaId -> PoliticasComerciales",
                     "ComprobanteLineas.FolioDeComprobante -> Comprobantes"
                 })
            Assert.Contains(esperada, foraneas);

        // Ninguna columna guarda JSON ni listas: no hay nvarchar(max) ni varchar(max).
        var columnasMax = await bd.SqlQuery<int>($@"SELECT COUNT(*) AS [Value] FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id WHERE c.max_length = -1").SingleAsync();
        Assert.Equal(0, columnasMax);

        // La política se guarda como filas, una por método y una por categoría.
        Assert.Equal(3, await bd.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM PoliticaMetodosDePago").SingleAsync());
        Assert.Equal(4, await bd.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM PoliticaPlazosDeDevolucion").SingleAsync());
        Assert.Equal(5, await bd.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Sucursales").SingleAsync());
    }

    [Fact]
    public async Task ADR_010_El_dictamen_y_la_decision_del_Supervisor_se_guardan_en_filas_y_columnas()
    {
        var http = _fabrica.CreateClient();
        var venta = await http.Post("/ventas", VentaConSerie("TEC-P001", Serie("TEC-P001", 1), 336m, "SUC-01"));
        Assert.Equal(HttpStatusCode.Created, venta.Codigo);
        var folio = (await http.Post("/devoluciones", new
        {
            folioDeVenta = venta.Texto("folio"), productoId = "TEC-P001", serieCapturada = Serie("TEC-P001", 1),
            motivo = new { causal = "Falla" }, condicionDelProducto = "EmpaqueAbierto", formaDeCompensacion = "Reembolso"
        })).Texto("folio");
        await http.Post($"/devoluciones/{folio}/resolucion", new { autoriza = true, motivo = "excepción" }, rol: "Supervisor");

        using var c = Nuevo();
        var bd = c.Obtener<ContextoDeDatos>().Database;
        Assert.Equal(3, await bd.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM DevolucionResultadosDeRegla WHERE FolioDeDevolucion = {folio}").SingleAsync());
        Assert.Equal(1, await bd.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM DevolucionResultadosDeRegla WHERE FolioDeDevolucion = {folio} AND Cumple = 0").SingleAsync());
        Assert.Equal("Supervisor", await bd.SqlQuery<string>($"SELECT DecisionSupervisorRol AS [Value] FROM Devoluciones WHERE Folio = {folio}").SingleAsync());
        Assert.Equal("excepción", await bd.SqlQuery<string>($"SELECT DecisionMotivo AS [Value] FROM Devoluciones WHERE Folio = {folio}").SingleAsync());
    }

    [Fact]
    public async Task Las_referencias_inexistentes_se_rechazan_en_la_base_y_el_rollback_deja_la_unidad_disponible()
    {
        var http = _fabrica.CreateClient();
        var antes = await http.Disponibles("TEC-P013");
        var r = await http.Post("/apartados", new
        {
            productoId = "TEC-P013", sucursalId = "SUC-01", clienteId = "TEC-C999",
            anticipo = Pago("Efectivo", 400m)
        });
        Assert.Equal(HttpStatusCode.NotFound, r.Codigo);
        Assert.Equal("NoEncontrado", r.Texto("motivo"));
        Assert.Contains("cliente", r.Texto("detail"));
        Assert.Equal(antes, await http.Disponibles("TEC-P013"));
    }

    [Fact]
    public void Los_folios_son_unicos_y_crecientes_por_tipo_de_operacion()
    {
        using var c = Nuevo();
        var folios = c.Obtener<IGeneradorDeFolios>();
        var a = folios.Siguiente(TipoDeOperacion.Apartado);
        var b = folios.Siguiente(TipoDeOperacion.Apartado);
        Assert.StartsWith("APT-", a.Valor);
        Assert.NotEqual(a, b);
        Assert.True(string.CompareOrdinal(b.Valor, a.Valor) > 0);
        Assert.StartsWith("VTA-", folios.Siguiente(TipoDeOperacion.Venta).Valor);
        Assert.StartsWith("DEV-", folios.Siguiente(TipoDeOperacion.Devolucion).Valor);
        Assert.StartsWith("CMP-", folios.Siguiente(TipoDeOperacion.Comprobante).Valor);
    }
}

/// <summary>RN-VTA-002: la política cambia en la base de datos y el sistema deja de aceptar el método retirado.</summary>
public sealed class PoliticaConfigurableApiPruebas : IClassFixture<FabricaDeApi>
{
    private readonly FabricaDeApi _fabrica;

    public PoliticaConfigurableApiPruebas(FabricaDeApi fabrica) => _fabrica = fabrica;

    [Fact]
    public async Task US11_RN_VTA_002_Un_metodo_retirado_de_la_politica_responde_422_en_apartados_y_ventas()
    {
        var http = _fabrica.CreateClient();
        using (var alcance = _fabrica.NuevoAlcance())
            await alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>().Database
                .ExecuteSqlRawAsync("DELETE FROM PoliticaMetodosDePago WHERE Metodo <> 'Efectivo'");

        var apartado = await http.Post("/apartados", Apartar("TEC-P001", 120m, "Tarjeta"));
        Assert.Equal((HttpStatusCode)422, apartado.Codigo);
        Assert.Equal("MetodoDePagoNoAutorizado", apartado.Texto("motivo"));

        var venta = await http.Post("/ventas", new
        {
            sucursalId = "SUC-01", lineas = new[] { new { productoId = "TEC-P001", serie = Serie("TEC-P001", 1) } },
            pagos = new[] { Pago("Transferencia", 336m) }
        });
        Assert.Equal("MetodoDePagoNoAutorizado", venta.Texto("motivo"));

        var permitido = await http.Post("/apartados", Apartar("TEC-P001", 120m, "Efectivo"));
        Assert.Equal(HttpStatusCode.Created, permitido.Codigo);
    }
}
