# TecnoStock — PIA Componentes de Lógica de Negocio

Sistema de ventas, apartados, devoluciones e inventario de **TecnoStock**, cadena de retail de electrónica con 5 sucursales (Caso 05).
Implementado con **.NET 10, C#, SQL Server (EF Core)**, DDD, Specification y máquina de estados explícita.

- **Equipo:** Eduardo Alejandro Acevedo Liuskos (2142139), Emma Patricia Brown Sánchez (2135479), Diego Monroy García (2135198).
- **Profesor:** Miguel Ángel Gómez Marroquín.
- **Repositorio:** _(URL por completar al publicar)_ · **Sistema desplegado:** _(URL por completar tras el despliegue en Render)_

## Qué hace

Implementa los casos de uso CU-01 a CU-08 y cuatro consultas:

| Caso de uso | Endpoint | Historias |
|---|---|---|
| CU-01 ApartarProducto | `POST /apartados` | US01, US03, US11 |
| CU-02 RegistrarVenta | `POST /ventas` | US06, US09 |
| CU-03 ConvertirApartadoEnVenta | `POST /apartados/{folio}/convertir` | US07, US09 |
| CU-04 SolicitarDevolucion | `POST /devoluciones` | US08, US10 |
| CU-05 AutorizarDevolucionExcepcional | `POST /devoluciones/{folio}/resolucion` | US13 |
| CU-06 CancelarApartado | `POST /apartados/{folio}/cancelar` | US14 |
| CU-07 RegistrarUnidad | `POST /unidades` | US17 |
| CU-08 VencerApartadosVencidos | `POST /apartados/vencer` | US20 |
| Disponibilidad en sucursal | `GET /disponibilidad/{productoId}?sucursalId=` | US04 |
| Disponibilidad en otras sucursales | `GET /disponibilidad/{productoId}/otras-sucursales?sucursalActual=` | US05 |
| Estado de devolución | `GET /devoluciones/{folio}` | US02 |
| Criterio de devolución vigente | `GET /politica/devolucion` | US16 |

US12 (comprobante con folio único) se cumple con manejadores de eventos. **No se implementan** (solo documentadas): US15 conciliación, US18 y US19 transferencias.

## Ejecución rápida con Docker

Requisitos: Docker con Compose v2.

```bash
cp .env.example .env        # y cambie MSSQL_SA_PASSWORD por una contraseña propia
docker compose up --build
```

Levanta tres servicios: `db` (SQL Server 2022), `app` (la API) y `tests` (corre las pruebas unitarias y de integración con cobertura y termina).

- API y Swagger UI: <http://localhost:8080/swagger> (puerto configurable con `APP_PORT`).
- La base se crea sola en el primer arranque y se llena con los datos semilla.
- Si el puerto 1433 de su máquina está ocupado, defina `DB_HOST_PORT` (por ejemplo `DB_HOST_PORT=1434 docker compose up --build`).
- Para borrar todo, incluida la base: `docker compose down -v`.

## Ejecución local (sin contenedor para la API)

Requisitos: SDK de .NET 10 y un SQL Server accesible (por ejemplo `docker compose up -d db`).

```bash
cp .env.example .env        # DB_HOST=localhost
dotnet run --project src/api --launch-profile http
```

La API abre <http://localhost:5080/swagger>. Si existe un archivo `.env`, la API lo carga al arrancar; las variables de entorno reales tienen prioridad.

### Variables de entorno (12-Factor)

| Variable | Uso | Valor por defecto |
|---|---|---|
| `MSSQL_SA_PASSWORD` | Contraseña de `sa`. **Obligatoria.** | — |
| `DB_HOST`, `DB_PORT` | Servidor de SQL Server | `localhost`, `1433` (Compose fija `db`) |
| `DB_NAME` | Base de datos | `TecnoStock` |
| `DB_USER` | Usuario | `sa` |
| `ConnectionStrings__Default` | Cadena completa (reemplaza a las anteriores) | — |
| `ACCEPT_EULA` | Licencia de SQL Server (solo Compose) | `Y` |
| `DB_HOST_PORT`, `APP_PORT` | Puertos publicados por Compose | `1433`, `8080` |

No hay secretos en el repositorio: `.env` está en `.gitignore` y `.env.example` solo lleva valores de ejemplo.

## Uso de la API

La guía completa con los nombres de los campos, la nomenclatura de los folios, los datos de prueba y recorridos paso a paso está en [`docs/api/guia_de_uso.md`](docs/api/guia_de_uso.md).

Las operaciones de escritura identifican al operador con encabezados (puerto `IContextoDeUsuario`):

```
X-Usuario: cajero1
X-Rol: Cajero          # Vendedor | Cajero | Supervisor | Almacen
```

`POST /apartados/vencer` lo ejecuta el actor Sistema y no los requiere. Swagger UI muestra los dos encabezados en cada operación.

Ejemplo (apartar una laptop con 30% de anticipo):

```bash
curl -X POST http://localhost:8080/apartados \
  -H "Content-Type: application/json" -H "X-Usuario: cajero1" -H "X-Rol: Cajero" \
  -d '{"productoId":"TEC-P001","sucursalId":"SUC-01","clienteId":"TEC-C001","anticipo":{"metodo":"Efectivo","monto":101}}'
```

| Código | Cuándo |
|---|---|
| 200 / 201 | Operación exitosa |
| 400 | Datos inválidos, encabezados faltantes o rol desconocido |
| 404 | `NoEncontrado`: producto, folio o apartado inexistente |
| 409 | `UnidadApartada`, `ConflictoDeConcurrencia`, `TransicionDeEstadoInvalida` |
| 422 | Regla de negocio violada (por ejemplo `AnticipoInsuficiente`, `ProductoNoApartable`, `SerieRequerida`, `SoloSupervisorAutoriza`) |

Las respuestas de error usan Problem Details e incluyen `motivo` y `alternativaSugerida`. La especificación está en [`docs/api/openapi.json`](docs/api/openapi.json) y se sirve en `/swagger/v1/swagger.json`.

### Datos semilla

Al arrancar con la base vacía se cargan: catálogo (`data/catalogo.csv`, 20 productos), clientes (`data/actores.csv`, 40), la política comercial por defecto, las sucursales SUC-01 a SUC-05 y series sintéticas `SN-<sku>-nnn`. Laptop, Monitor y SSD llevan serie y son apartables; Teclado no. Los valores de política y el mapeo de categorías son **[Supuesto]** (ver `docs/BRD.md` 7.1 y `docs/cambios/registro_de_cambios.md`, C-30).

## Arquitectura

```
src/domain/          TecnoStock.Dominio        entidades, value objects, aggregates, eventos, Specification, máquina de estados
src/application/     TecnoStock.Aplicacion     servicios de aplicación, commands/queries, puertos, manejadores de eventos
src/infrastructure/  TecnoStock.Infraestructura  EF Core + SQL Server, repositorios, Unidad de Trabajo, publicador de eventos
src/api/             TecnoStock.Api            controllers REST, traducción a HTTP, Swagger
tests/unitarias/     dominio y casos de uso con repositorios en memoria
tests/integracion/   la API completa contra SQL Server real
```

Dependencias: `api → application → domain`; `infrastructure → application y domain`. La API también referencia a Infraestructura solo para ensamblar las dependencias (C-28). El dominio no depende de nada.

- **Domain Model** para los comandos y **Transaction Script** para las lecturas (ADR-001); CQRS básico sin bus (ADR-004).
- **Aggregates:** Apartado, UnidadInventario, Venta (con LineaDeVenta), Devolucion, PoliticaComercial y Producto, que se refieren entre sí por identidad (ADR-002).
- **Specification** (ADR-007): `IEspecificacion<T>` con `Y`, `O` y `No`, y siete especificaciones: producto apartable, unidad disponible para venta, apartado vigente, método de pago autorizado, comprobante válido, devolución dentro de plazo y condición aceptable.
- **Máquina de estados** (ADR-008): `MaquinaDeEstados<T>` con las tablas de transiciones de Apartado, UnidadInventario y Devolucion.
- **Tres tipos de rechazo** (ADR-006): violación de regla (rollback), derivación a Supervisor (`EnRevision`) y rechazo con efecto (el vencimiento de un apartado se guarda aunque la conversión se rechace).
- **Una Unidad de Trabajo por caso de uso**; los eventos se publican después de confirmar y atienden comprobantes, reingreso de unidades y avisos (ADR-003).
- **EF Core / SQL Server** (ADR-009, ADR-010): claves de negocio de texto, 18 claves foráneas, esquema en primera forma normal, índice único de serie (RN-INV-002), un solo apartado vigente por unidad (RN-APT-003) y concurrencia optimista por versión.

## Pruebas y cobertura

```bash
docker compose up -d db                      # SQL Server para las pruebas de integración
set -a; source .env; set +a                  # DB_HOST, DB_PORT y MSSQL_SA_PASSWORD
dotnet test                                  # 175 unitarias + 61 de integración
./scripts/pruebas-con-cobertura.sh           # une la cobertura y falla si es menor a 80%
```

Cada escenario Gherkin tiene su prueba y la matriz de trazabilidad se verifica automáticamente (`TrazabilidadPruebas`). Última ejecución completa: **236 pruebas en verde** (175 unitarias y 61 de integración); **98.7% de cobertura de líneas** y 91.1% de ramas (el umbral exigido es 80%), medidas con `scripts/pruebas-con-cobertura.sh` (98.1% dentro de Docker Compose). Cada prueba lleva en su nombre la historia que cubre (por ejemplo `US07_CU03_Saldo_insuficiente...`). Cada fixture de integración crea su propia base de datos y la elimina al terminar, así que no toca los datos de desarrollo.

## CI/CD

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) corre en cada push a `main` y en cada pull request:

1. **pruebas:** levanta SQL Server con una contraseña efímera, compila, ejecuta las pruebas con cobertura y falla si queda por debajo de 80%.
2. **compose:** ejecuta `docker compose up --build --exit-code-from tests`.

El despliegue en **Render** está definido en [`render.yaml`](render.yaml): SQL Server Express en un servicio privado con disco persistente y la API como servicio web, que se redespliega sola cuando pasan las pruebas. Pasos, costos (los discos y servicios privados son de pago), lo que se verificó y los límites en [`docs/despliegue.md`](docs/despliegue.md). **Sistema desplegado:** _(URL por completar tras crear el Blueprint)_.

## Documentación

| Documento | Ubicación |
|---|---|
| **Reporte final** (BRD, arquitectura, evidencias y conclusiones; también en `.docx` y `.pdf`) | [`docs/reporte_final.md`](docs/reporte_final.md) |
| BRD (necesidades BR-001 a BR-008, reglas RN-*) | [`docs/BRD.md`](docs/BRD.md) |
| Historias de usuario con Gherkin (US01 a US20) | [`docs/analisis/historias_de_usuario.md`](docs/analisis/historias_de_usuario.md) |
| Glosario (lenguaje ubicuo) | [`docs/analisis/glosario.md`](docs/analisis/glosario.md) |
| BPMN | [`docs/analisis/bpmn/BPMN.md`](docs/analisis/bpmn/BPMN.md) |
| Diagramas (dominio, aplicación, secuencias y modelo de datos, en `.mmd` y `.png`) | [`docs/architecture/diagramas/`](docs/architecture/diagramas) |
| ADR-001 a ADR-010 | [`docs/architecture/adr/`](docs/architecture/adr) |
| Matriz de trazabilidad (también en `.docx` y `.pdf`) | [`docs/architecture/matriz_trazabilidad.md`](docs/architecture/matriz_trazabilidad.md) |
| Registro de cambios (C-01 a C-41) | [`docs/cambios/registro_de_cambios.md`](docs/cambios/registro_de_cambios.md) |
| OpenAPI | [`docs/api/openapi.json`](docs/api/openapi.json) |
| Despliegue en Render (pasos, costos y límites) | [`docs/despliegue.md`](docs/despliegue.md) |
| Guía de uso de la API: campos, folios, datos de prueba y recorridos (también en `.docx` y `.pdf`) | [`docs/api/guia_de_uso.md`](docs/api/guia_de_uso.md) |

### Regenerar los documentos Word y PDF

```bash
npm install -g docx
NODE_PATH="$(npm root -g)" node scripts/md-a-docx.js docs/cambios/registro_de_cambios.md docs/cambios/registro_de_cambios.docx
```

El Markdown es la fuente; el PDF se exporta con LibreOffice (`soffice --headless --convert-to pdf`).

## Limitaciones conocidas

- Los valores de política (anticipo 30%, vigencia 7 días, plazos de devolución, métodos de pago) y las reglas RN-APT-004, RN-DEV-003, RN-DEV-004 y RN-INV-004 son **[Supuesto]** pendientes de validar con el stakeholder.
- Solo se exige rol para las acciones del Supervisor; no se valida que el cliente ni la sucursal existan en un catálogo (C-26).
- Si un manejador de eventos falla, el error se registra en el log y no se reintenta (C-32).
- La base se crea con `EnsureCreated`, sin migraciones (C-27): si ya tenía una base creada con una versión anterior del esquema, bórrela (`DROP DATABASE`) y la API la vuelve a crear y sembrar.
- Los folios son únicos pero no consecutivos (C-29).
