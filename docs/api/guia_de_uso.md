# Guía de uso de la API de TecnoStock

Esta guía dice **qué enviar en cada endpoint, cómo se llaman los campos y cómo se forman los folios**. La especificación formal está en [`openapi.json`](openapi.json) y la interactiva en `/swagger`.

- **Dirección base:** `http://localhost:8080` (Docker Compose) o `http://localhost:5080` (`dotnet run`). Swagger UI en `/swagger`.
- **Formato:** JSON. Los nombres de los campos van en *camelCase* (`productoId`, no `ProductoId`) y las listas de valores (métodos de pago, condiciones, etc.) se escriben **como texto**, con la mayúscula indicada.
- **Importes:** pesos mexicanos (MXN), con punto decimal (`101.50`).
- **Encabezados del operador** (obligatorios en todas las operaciones de escritura, salvo `POST /apartados/vencer`):

| Encabezado | Valores | Ejemplo |
|---|---|---|
| `X-Usuario` | cualquier texto no vacío | `cajero1` |
| `X-Rol` | `Vendedor`, `Cajero`, `Supervisor` o `Almacen` | `Cajero` |

En Swagger UI aparecen como dos campos en cada operación. Solo el rol `Supervisor` se valida de verdad (cancelar un apartado y resolver una devolución, C-26); los demás roles se aceptan en cualquier operación.

## 1. Nomenclatura de identificadores y folios

| Identificador | Formato | Ejemplo | Dónde se usa |
|---|---|---|---|
| `productoId` | `TEC-P` + 3 dígitos (001 a 020) | `TEC-P001` | apartados, ventas, devoluciones, unidades, disponibilidad |
| `sucursalId` | `SUC-` + 2 dígitos (01 a 05) | `SUC-01` | apartados, ventas, unidades, disponibilidad |
| `clienteId` | `TEC-C` + 3 dígitos (001 a 040) | `TEC-C001` | apartados |
| número de serie | `SN-<productoId>-<n>` con n de 3 dígitos | `SN-TEC-P001-001` | ventas, conversión, devoluciones, alta de unidades. Se guarda en mayúsculas; da igual cómo se escriba |
| **folio de apartado** | `APT-` + 6 dígitos | `APT-000001` | lo **entrega** `POST /apartados`; se **recibe** en la ruta de `convertir` y `cancelar` |
| **folio de venta** | `VTA-` + 6 dígitos | `VTA-000001` | lo **entrega** `POST /ventas` y `convertir`; se **recibe** como `folioDeVenta` al devolver (es el comprobante de compra) |
| **folio de devolución** | `DEV-` + 6 dígitos | `DEV-000001` | lo **entrega** `POST /devoluciones`; se **recibe** en la ruta de `resolucion` y de `GET /devoluciones/{folio}` |
| folio de comprobante | `CMP-` + 6 dígitos | `CMP-000001` | lo emite el sistema solo (tabla `Comprobantes`); no tiene endpoint |
| `unidadId` | GUID | `3f2c…` | solo aparece en la respuesta de `POST /unidades`; **no** se envía en ninguna petición |

Los folios son consecutivos **por tipo** pero pueden tener huecos: una operación rechazada también consume su número (por eso el primer apartado exitoso puede ser `APT-000002`, C-29).

## 2. Endpoints por caso de uso

| Caso de uso | Método y ruta | Rol sugerido | Recibe | Entrega | Éxito |
|---|---|---|---|---|---|
| CU-01 ApartarProducto | `POST /apartados` | Cajero | cuerpo (3.1) | folio de apartado, fecha límite, saldo | 201 |
| CU-03 ConvertirApartadoEnVenta | `POST /apartados/{folio}/convertir` | Cajero | folio de apartado + cuerpo (3.2) | venta con folio `VTA-` | 200 |
| CU-06 CancelarApartado | `POST /apartados/{folio}/cancelar` | **Supervisor** | folio de apartado + cuerpo (3.3) | folio y estado | 200 |
| CU-08 VencerApartadosVencidos | `POST /apartados/vencer` | Sistema (sin encabezados) | nada | cantidad de apartados vencidos | 200 |
| CU-02 RegistrarVenta | `POST /ventas` | Cajero | cuerpo (3.4) | venta con folio `VTA-` | 201 |
| CU-04 SolicitarDevolucion | `POST /devoluciones` | Cajero | cuerpo (3.5) | folio `DEV-` y estado | 201 |
| CU-05 AutorizarDevolucionExcepcional | `POST /devoluciones/{folio}/resolucion` | **Supervisor** | folio de devolución + cuerpo (3.6) | estado final | 200 |
| CU-07 RegistrarUnidad | `POST /unidades` | Almacen | cuerpo (3.7) | unidad registrada | 201 |
| Estado de devolución | `GET /devoluciones/{folio}` | — (sin encabezados) | folio de devolución | estado, motivo, compensación | 200 |
| Disponibilidad en sucursal | `GET /disponibilidad/{productoId}?sucursalId=SUC-01` | — | producto y sucursal | cantidad disponible | 200 |
| Disponibilidad en otras sucursales | `GET /disponibilidad/{productoId}/otras-sucursales?sucursalActual=SUC-01` | — | producto y sucursal actual | lista (vacía si no hay) | 200 |
| Criterio de devolución vigente | `GET /politica/devolucion` | — | nada | condiciones y plazo por categoría | 200 |

## 3. Cuerpos de petición (los nombres exactos de los campos)

Un **pago** siempre tiene esta forma: `{ "metodo": "Efectivo", "monto": 101.00 }` con `metodo` = `Efectivo`, `Tarjeta` o `Transferencia`.

### 3.1 `POST /apartados`
| Campo | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `productoId` | texto | sí | producto apartable (Laptop, Monitor o SSD) |
| `sucursalId` | texto | sí | sucursal donde se aparta |
| `clienteId` | texto | sí | cliente de `actores.csv` |
| `anticipo` | pago | sí | mínimo 30% del precio de lista |

```json
{ "productoId": "TEC-P001", "sucursalId": "SUC-01", "clienteId": "TEC-C001",
  "anticipo": { "metodo": "Efectivo", "monto": 101.00 } }
```

### 3.2 `POST /apartados/{folio}/convertir`
| Campo | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `pagoDelSaldo` | pago | sí | debe cubrir el saldo pendiente (precio − anticipo) |
| `serieCapturada` | texto | sí para productos con serie | la serie de la unidad apartada |

```json
{ "pagoDelSaldo": { "metodo": "Tarjeta", "monto": 235.00 }, "serieCapturada": "SN-TEC-P001-001" }
```

### 3.3 `POST /apartados/{folio}/cancelar`
| Campo | Tipo | Obligatorio |
|---|---|---|
| `motivo` | texto | sí |

```json
{ "motivo": "El cliente lo solicitó" }
```

### 3.4 `POST /ventas`
| Campo | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `sucursalId` | texto | sí | sucursal donde se vende |
| `lineas` | lista | sí | una por unidad vendida |
| `lineas[].productoId` | texto | sí | |
| `lineas[].serie` | texto | sí si el producto lleva serie | si es `null`, el sistema elige una unidad disponible (solo productos sin serie) |
| `pagos` | lista de pagos | sí | la suma debe cubrir el total; permite varios métodos (cobro multimedio) |

```json
{ "sucursalId": "SUC-01",
  "lineas": [ { "productoId": "TEC-P001", "serie": "SN-TEC-P001-006" } ],
  "pagos":  [ { "metodo": "Tarjeta", "monto": 200.00 }, { "metodo": "Efectivo", "monto": 136.00 } ] }
```

### 3.5 `POST /devoluciones`
| Campo | Tipo | Obligatorio | Valores |
|---|---|---|---|
| `folioDeVenta` | texto | sí | folio `VTA-` de la compra |
| `productoId` | texto | sí | |
| `serieCapturada` | texto | sí si el producto lleva serie | |
| `motivo.causal` | texto | sí | por ejemplo `Falla` |
| `motivo.detalle` | texto | no | |
| `condicionDelProducto` | texto | sí | `EmpaqueCerrado`, `EmpaqueAbierto` o `Defectuoso` |
| `formaDeCompensacion` | texto | sí | `Reembolso` o `NotaDeCredito` |

```json
{ "folioDeVenta": "VTA-000001", "productoId": "TEC-P001", "serieCapturada": "SN-TEC-P001-006",
  "motivo": { "causal": "Falla", "detalle": "No enciende" },
  "condicionDelProducto": "EmpaqueCerrado", "formaDeCompensacion": "Reembolso" }
```
Resultado: `Aceptada` si cumple el criterio estándar (comprobante válido, dentro del plazo y empaque cerrado o producto defectuoso); `EnRevision` si no (fuera de plazo o empaque abierto), y entonces el Supervisor la resuelve.

### 3.6 `POST /devoluciones/{folio}/resolucion`
| Campo | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `autoriza` | booleano | sí | `true` acepta, `false` rechaza |
| `motivo` | texto | sí | justificación de la decisión |

```json
{ "autoriza": true, "motivo": "Cliente frecuente" }
```

### 3.7 `POST /unidades`
| Campo | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `productoId` | texto | sí | |
| `sucursalId` | texto | sí | debe existir (SUC-01 a SUC-05) |
| `serie` | texto | sí si el producto lleva serie | no puede repetirse en todo el inventario |

```json
{ "productoId": "TEC-P001", "sucursalId": "SUC-05", "serie": "SN-NUEVA-0001" }
```

## 4. Forma de las respuestas

| Operación | Campos de la respuesta |
|---|---|
| Apartar | `folio`, `fechaLimite`, `saldoPendiente` |
| Convertir / Vender | `folio` (venta), `total`, `lineas[]` (`productoId`, `serie`, `precioUnitario`), `folioDeApartadoOrigen` (null en venta directa) |
| Cancelar | `folio`, `estado` (`Cancelado`) |
| Vencer | `apartadosVencidos` |
| Devolución (solicitar, resolver y consultar) | `folio`, `estado` (`EnRevision`, `Aceptada` o `Rechazada`), `motivo`, `formaDeCompensacion` (solo cuando es `Aceptada`) |
| Alta de unidad | `unidadId`, `productoId`, `sucursal`, `serie`, `estado` (`Disponible`) |
| Disponibilidad en sucursal | `producto`, `sucursal`, `cantidadDisponible` |
| Disponibilidad en otras sucursales | lista de `sucursal`, `cantidadDisponible` |
| Criterio vigente | `condiciones[]`, `plazoPorCategoria` |

**Errores** (Problem Details): `status`, `title` y `detail` más `motivo` y `alternativaSugerida`.

| Código | Motivos |
|---|---|
| 400 | datos inválidos: monto negativo, texto vacío, falta un campo o un encabezado, rol o valor de lista desconocido |
| 404 | `NoEncontrado`: producto, folio, apartado, sucursal o cliente inexistente |
| 409 | `UnidadApartada`, `ConflictoDeConcurrencia`, `TransicionDeEstadoInvalida` (por ejemplo cancelar un apartado ya cancelado) |
| 422 | `SinExistenciaDisponible`, `ProductoNoApartable`, `AnticipoInsuficiente`, `MetodoDePagoNoAutorizado`, `SerieRequerida`, `SerieNoCoincide`, `SerieDuplicada`, `SaldoInsuficiente`, `ApartadoVencido`, `ComprobanteNoValido`, `LineaNoPerteneceALaVenta`, `SoloSupervisorAutoriza`, `UnidadNoDisponible`, `UnidadEnOtraSucursal` |

## 5. Datos de prueba (catálogo sembrado)

Las series se asignan en orden a las sucursales: la serie `001` va a SUC-01, la `002` a SUC-02, … la `006` otra vez a SUC-01. La columna *Stock* muestra el total y entre paréntesis las unidades en SUC-01/02/03/04/05. Al apartar o vender sin indicar serie, el sistema usa **la primera serie disponible en orden alfabético** de la sucursal.

| Producto | Descripción | Categoría (plazo de devolución) | Precio MXN | Anticipo mínimo (30%) | Lleva serie | Apartable | Stock (por sucursal) | Primera serie |
|---|---|---|---|---|---|---|---|---|
| TEC-P001 | Laptop 01 | Computadoras (15 d) | 336.00 | 100.80 | Sí | Sí | 13 (3/3/3/2/2) | `SN-TEC-P001-001` |
| TEC-P002 | Monitor 02 | Accesorios (30 d) | 407.00 | 122.10 | Sí | Sí | 16 (4/3/3/3/3) | `SN-TEC-P002-001` |
| TEC-P003 | SSD 03 | Componentes (30 d) | 478.00 | 143.40 | Sí | Sí | 19 (4/4/4/4/3) | `SN-TEC-P003-001` |
| TEC-P004 | Teclado 04 | Accesorios (30 d) | 549.00 | 164.70 | No | No | 22 (5/5/4/4/4) | `—` |
| TEC-P005 | Laptop 05 | Computadoras (15 d) | 620.00 | 186.00 | Sí | Sí | 25 (5/5/5/5/5) | `SN-TEC-P005-001` |
| TEC-P006 | Monitor 06 | Accesorios (30 d) | 691.00 | 207.30 | Sí | Sí | 28 (6/6/6/5/5) | `SN-TEC-P006-001` |
| TEC-P007 | SSD 07 | Componentes (30 d) | 762.00 | 228.60 | Sí | Sí | 31 (7/6/6/6/6) | `SN-TEC-P007-001` |
| TEC-P008 | Teclado 08 | Accesorios (30 d) | 833.00 | 249.90 | No | No | 34 (7/7/7/7/6) | `—` |
| TEC-P009 | Laptop 09 | Computadoras (15 d) | 904.00 | 271.20 | Sí | Sí | 37 (8/8/7/7/7) | `SN-TEC-P009-001` |
| TEC-P010 | Monitor 10 | Accesorios (30 d) | 975.00 | 292.50 | Sí | Sí | 40 (8/8/8/8/8) | `SN-TEC-P010-001` |
| TEC-P011 | SSD 11 | Componentes (30 d) | 1,046.00 | 313.80 | Sí | Sí | 0 (0/0/0/0/0) | `—` |
| TEC-P012 | Teclado 12 | Accesorios (30 d) | 1,117.00 | 335.10 | No | No | 46 (10/9/9/9/9) | `—` |
| TEC-P013 | Laptop 13 | Computadoras (15 d) | 1,188.00 | 356.40 | Sí | Sí | 49 (10/10/10/10/9) | `SN-TEC-P013-001` |
| TEC-P014 | Monitor 14 | Accesorios (30 d) | 1,259.00 | 377.70 | Sí | Sí | 52 (11/11/10/10/10) | `SN-TEC-P014-001` |
| TEC-P015 | SSD 15 | Componentes (30 d) | 1,330.00 | 399.00 | Sí | Sí | 10 (2/2/2/2/2) | `SN-TEC-P015-001` |
| TEC-P016 | Teclado 16 | Accesorios (30 d) | 1,401.00 | 420.30 | No | No | 13 (3/3/3/2/2) | `—` |
| TEC-P017 | Laptop 17 | Computadoras (15 d) | 1,472.00 | 441.60 | Sí | Sí | 16 (4/3/3/3/3) | `SN-TEC-P017-001` |
| TEC-P018 | Monitor 18 | Accesorios (30 d) | 1,543.00 | 462.90 | Sí | Sí | 19 (4/4/4/4/3) | `SN-TEC-P018-001` |
| TEC-P019 | SSD 19 | Componentes (30 d) | 1,614.00 | 484.20 | Sí | Sí | 22 (5/5/4/4/4) | `SN-TEC-P019-001` |
| TEC-P020 | Teclado 20 | Accesorios (30 d) | 1,685.00 | 505.50 | No | No | 25 (5/5/5/5/5) | `—` |

`TEC-P011` está deshabilitado en el catálogo: se siembra sin unidades (sirve para probar `SinExistenciaDisponible`). Clientes: `TEC-C001` a `TEC-C040`.

## 6. Recorridos de prueba

**A. Apartar y convertir en venta (y ver que una unidad apartada no se vende)**
1. `GET /disponibilidad/TEC-P001?sucursalId=SUC-01` → 200, `cantidadDisponible` 3.
2. `POST /apartados` (3.1, anticipo 101) → 201 con `folio` (`APT-…`) y `saldoPendiente` 235. Se apartó la serie `SN-TEC-P001-001`.
3. `POST /apartados` con anticipo 10 → 422 `AnticipoInsuficiente`.
4. `POST /ventas` con `serie` `SN-TEC-P001-001` → 409 `UnidadApartada`.
5. `POST /apartados/{folio}/convertir` (3.2, saldo 235, esa serie) → 200 con folio `VTA-…`.

**B. Devolución estándar y por excepción**
1. `POST /ventas` de la serie `SN-TEC-P001-006` pagando 336 → 201, anota el folio `VTA-…`.
2. `POST /devoluciones` con `EmpaqueCerrado` → 201 `Aceptada`; la unidad vuelve a estar disponible.
3. Otra venta y `POST /devoluciones` con `EmpaqueAbierto` → 201 `EnRevision`.
4. `POST /devoluciones/{folio}/resolucion` con rol `Cajero` → 422 `SoloSupervisorAutoriza`; con rol `Supervisor` → 200 `Aceptada`; repetirla → 409 `TransicionDeEstadoInvalida`.

**C. Cancelar y vencer**
1. Apartar y luego `POST /apartados/{folio}/cancelar` con `Vendedor` → 422; con `Supervisor` → 200 y la unidad se libera.
2. Para probar el vencimiento sin esperar 7 días, simule un apartado creado hace 8 días (hay que mover las dos fechas) en la base de datos: `SET QUOTED_IDENTIFIER ON; UPDATE Apartados SET VigenciaDesde = DATEADD(day, -8, SYSUTCDATETIME()), VigenciaHasta = DATEADD(day, -1, SYSUTCDATETIME()) WHERE Folio = 'APT-000001'` y luego `POST /apartados/vencer` → 200 con `apartadosVencidos` 1.
3. Para probar una devolución fuera de plazo: `SET QUOTED_IDENTIFIER ON; UPDATE Ventas SET FechaDeCierre = DATEADD(day, -20, FechaDeCierre) WHERE Folio = 'VTA-000001'` y devolver una laptop (15 días) → `EnRevision`.

**D. Inventario**
1. `POST /unidades` con rol `Almacen` (3.7) → 201.
2. Repetir la misma serie → 422 `SerieDuplicada`; omitirla en una laptop → 422 `SerieRequerida`; usar `SUC-99` → 404.

## 7. Consejos

- **Las instrucciones `UPDATE` de esta guía** llevan `SET QUOTED_IDENTIFIER ON;` porque la tabla de apartados tiene un índice filtrado; DBeaver ya lo tiene activo, pero `sqlcmd` no (o use `sqlcmd -I`).
- **Ver qué serie quedó apartada:** `SELECT Serie FROM UnidadesDeInventario WHERE FolioDeApartadoVigente = 'APT-000001'`.
- **Una unidad vendida no se vuelve a vender** (`UnidadNoDisponible`). Si ya probó con una serie, use la siguiente de la sucursal (001, 006, 011, …).
- **Base limpia:** borre la base de datos (`DROP DATABASE`) o ejecute `docker compose down -v`; la API la vuelve a crear y sembrar al arrancar.
- Estos mismos recorridos están automatizados en las pruebas de integración (`tests/integracion`).
