# Reporte — Glosario de dominio de TecnoStock (versión corregida)

| Nombre | Matrícula |
|---|---|
| Eduardo Alejandro Acevedo Liuskos | 2142139 |
| Emma Patricia Brown Sánchez | 2135479 |
| Diego Monroy García | 2135198 |

| Datos del curso | |
|---|---|
| Nombre del curso | Componentes de lógica de negocio |
| Nombre del profesor | Miguel Ángel Gómez Marroquín |
| Módulo Netacad | PIA (refina el Módulo 2) |
| Actividad | PIA — Glosario de dominio (versión corregida de U2) |
| Fecha | 06/11/2026 |

## Bibliografía (formato APA)

- Documento de caso asignado enviado por el docente. (2026). Caso 05: TecnoStock, retail de electrónica.
- Gómez Marroquín, M. Á. (2026). Producto Integrador de Aprendizaje: Implementación de una solución de software basada en lógica de negocio [Documento del curso Componentes de lógica de negocio].
- BRD, User Stories, BPMN y Glosario de dominio de TecnoStock, versión de la Unidad 2 (07/09/2026) y diagramas y ADR de la Unidad 3.

## Objetivo

Mantener un lenguaje ubicuo único entre el BRD, las historias, el BPMN, los diagramas, el código y las pruebas, incluyendo todo término técnico o concepto usado durante el desarrollo.

## Procedimiento

Se conservaron los 20 términos de la Unidad 2 con sus identificadores (G-01 a G-20), se corrigieron los que contradecían al BRD, las historias o los diagramas y se agregaron los términos de negocio y técnicos que aparecen en el modelo y en el código (G-21 en adelante). La columna Estado indica si el término es Original, Corregido o Nuevo; el motivo está en el Registro de cambios. Los nombres del código usan los términos de este glosario, en español.

## 1. Términos de negocio de la Unidad 2

| ID | Término | Tipo | Definición | Estado |
|---|---|---|---|---|
| G-01 | Apartado | Negocio | Esquema comercial en el que el cliente reserva una unidad de un producto apartable pagando un anticipo. Mientras esté vigente, la unidad queda bloqueada para venta a terceros. Estados: Vigente, Convertido, Vencido y Cancelado. | Corregido |
| G-02 | Anticipo | Negocio | Pago inicial mínimo obligatorio, calculado como porcentaje del precio acordado según la política comercial (30% [Supuesto], dentro del rango 20%–30% del BRD), requerido para formalizar un apartado. | Corregido |
| G-03 | Stock Reservado | Negocio | Estado lógico de una unidad específica (Apartada) que impide venderla a otro cliente. | Original |
| G-04 | Vencimiento de Apartado | Negocio | Límite temporal (7 días naturales [Supuesto]) tras el cual, si el cliente no liquida el saldo, el apartado pasa a Vencido y la unidad vuelve a Disponible. | Corregido |
| G-05 | Número de Serie | Negocio | Código alfanumérico único grabado por el fabricante en dispositivos serializables (laptops, monitores, SSD) para identificar una unidad física. Una serie corresponde a una sola unidad activa (RN-INV-002). | Corregido |
| G-06 | Comprobante | Negocio | Documento con folio único emitido por el sistema al concretar una venta, un apartado o una devolución (ticket de caja o factura electrónica). Reemplaza al término «Comprobante de Venta» de U2, que no cubría apartados ni devoluciones. | Corregido |
| G-07 | Transferencia Inter-Sucursal | Negocio | Proceso logístico, solicitado por Almacén, de movimiento de una unidad desde una sucursal con existencia hacia la sucursal donde se requiere. La unidad queda En Tránsito hasta que el destino confirma su recepción (RN-INV-003). | Corregido |
| G-08 | Inconsistencia de Inventario | Negocio | Discrepancia no deseada entre las unidades registradas en el sistema (stock lógico) y las unidades físicas presentes. También se llama descuadre. | Original |
| G-09 | Devolución Estándar | Negocio | Solicitud de devolución con comprobante válido, dentro del plazo de su categoría y con el producto en empaque cerrado o defectuoso. El sistema la acepta sin intervención del Supervisor. | Corregido |
| G-10 | Devolución por Excepción | Negocio | Solicitud con comprobante válido que incumple algún criterio estándar (fuera de plazo o empaque abierto). Queda En Revisión y solo procede con la decisión documentada del Supervisor (RN-SUP-002). Un comprobante inválido no es excepción: se rechaza de forma definitiva. | Corregido |
| G-11 | Nota de Crédito | Negocio | Forma de compensación de una devolución aceptada, alternativa al reembolso, utilizable como saldo a favor en compras dentro de TecnoStock. | Corregido |
| G-12 | Producto Defectuoso | Negocio | Unidad en estado Defectuosa: se retira de la venta directa y se canaliza a garantía con el proveedor. Una unidad devuelta con empaque abierto o con falla pasa a este estado. | Corregido |
| G-13 | Reingreso a Inventario | Negocio | Acción de devolver a Disponible una unidad con devolución aceptada y empaque cerrado, para que vuelva a venderse. | Corregido |
| G-14 | Consulta Multi-Sucursal | Negocio | Función del punto de venta que muestra en tiempo real la disponibilidad de un producto en las 5 sucursales. | Original |
| G-15 | Orden de Venta | Negocio | Registro previo que arma el Vendedor con los productos elegidos por el cliente antes del cobro. En el sistema se materializa como la solicitud de venta con sus líneas; no es un agregado propio. | Corregido |
| G-16 | Período de Garantía | Negocio | Plazo durante el cual TecnoStock o el fabricante responden por fallas o defectos. Se reconoce en el negocio pero queda fuera de la implementación del PIA. | Corregido |
| G-17 | Punto de Venta | Negocio | Sistema de software y hardware operado por Vendedores y Cajeros para consultas, ventas, apartados y devoluciones. | Original |
| G-18 | Cobro Multimedio | Negocio | Capacidad de liquidar una venta con más de un método de pago en la misma transacción (por ejemplo tarjeta y efectivo). Se modela como una lista de pagos recibidos. | Corregido |
| G-19 | Escaneo Obligatorio | Negocio | Validación que bloquea el cierre de la venta si la serie de un producto serializable no está registrada. El Vendedor la escanea al armar la orden y el Cajero la confirma al cerrar la venta (RN-VTA-001). | Corregido |
| G-20 | Límite de Bloqueo | Negocio | Regla que impide seleccionar para venta o apartado una unidad cuyo estado no sea Disponible (Apartada, En Tránsito, Vendida o Defectuosa). Excepción: el apartado propietario puede convertir su propia unidad en venta. | Corregido |

## 2. Términos de negocio nuevos

| ID | Término | Tipo | Definición | Estado |
|---|---|---|---|---|
| G-21 | Cliente | Negocio | Persona que compra, aparta o devuelve productos. En los datos de ejemplo del curso se llama «comprador». | Nuevo |
| G-22 | Vendedor | Negocio | Rol que atiende antes del cobro: consulta existencias, arma la orden de venta o la solicitud de apartado, escanea la serie, captura el motivo de devolución e inicia la conversión de un apartado. No maneja dinero. | Nuevo |
| G-23 | Cajero | Negocio | Rol que maneja el dinero y el cierre: cobra anticipos y saldos, valida el método de pago, confirma la serie, valida el comprobante y la condición del producto en devoluciones, y emite comprobantes y compensaciones. | Nuevo |
| G-24 | Supervisor | Negocio | Único rol autorizado para descuentos extraordinarios, cancelaciones, ajustes de inventario y devoluciones fuera de política. | Nuevo |
| G-25 | Almacén | Negocio | Rol que resguarda, recibe, da de alta con serie, despacha y transfiere mercancía y solicita transferencias entre sucursales. | Nuevo |
| G-26 | Sistema | Negocio | Actor automático que evalúa la política de devolución y vence los apartados cuyo plazo terminó. | Nuevo |
| G-27 | Operador | Negocio | Usuario que ejecuta una operación, identificado por su usuario y su rol. | Nuevo |
| G-28 | Producto | Negocio | Artículo del catálogo con SKU, nombre, categoría, precio de lista, indicador de si requiere serie e indicador de si se puede apartar. | Nuevo |
| G-29 | Unidad de Inventario | Negocio | Pieza física individual de un producto, con identidad propia, sucursal actual, serie y estado (Disponible, Apartada, Vendida, En Tránsito o Defectuosa). | Nuevo |
| G-30 | Sucursal | Negocio | Cada una de las 5 tiendas físicas de TecnoStock. | Nuevo |
| G-31 | Categoría de Producto | Negocio | Clasificación del producto: Computadoras, Celulares, Accesorios o Componentes. Determina el plazo de devolución. | Nuevo |
| G-32 | Folio | Negocio | Identificador único de un comprobante, apartado, venta o devolución. | Nuevo |
| G-33 | Venta | Negocio | Operación en la que se cobra y entrega al menos una unidad. Puede originarse directamente o desde un apartado. | Nuevo |
| G-34 | Línea de Venta | Negocio | Renglón de una venta con el producto, la unidad exacta, la serie capturada y el precio unitario. | Nuevo |
| G-35 | Pago Recibido | Negocio | Cobro de una parte del importe, con método de pago y monto. El anticipo es un pago recibido. | Nuevo |
| G-36 | Saldo Pendiente | Negocio | Parte del precio acordado de un apartado que falta pagar tras el anticipo. | Nuevo |
| G-37 | Conversión de Apartado | Negocio | Proceso en que un apartado vigente se convierte en venta cuando el Cajero cobra el saldo, aplicando el anticipo como parte del pago. | Nuevo |
| G-38 | Política Comercial | Negocio | Conjunto de parámetros vigentes: porcentaje de anticipo, vigencia del apartado, métodos de pago autorizados y criterio de devolución. | Nuevo |
| G-39 | Criterio de Devolución | Negocio | Conjunto de reglas de devolución (comprobante vigente, plazo por categoría, condición del producto) que se evalúa para cada solicitud. | Nuevo |
| G-40 | Dictamen de Devolución | Negocio | Resultado de evaluar el criterio de devolución: indica si cumple y lista los incumplimientos. | Nuevo |
| G-41 | Decisión de Supervisor | Negocio | Registro de la resolución del Supervisor sobre una devolución en revisión: quién decidió, si autoriza, el motivo y la fecha. | Nuevo |
| G-42 | Motivo de Devolución | Negocio | Causal y detalle que el Vendedor captura para dar seguimiento a problemas recurrentes de producto. | Nuevo |
| G-43 | Condición del Producto | Negocio | Estado físico del producto devuelto, verificado por el Cajero: empaque cerrado, empaque abierto o defectuoso. | Nuevo |
| G-44 | Forma de Compensación | Negocio | Manera de compensar una devolución aceptada: reembolso o nota de crédito. | Nuevo |
| G-45 | Rechazo | Negocio | Respuesta del sistema cuando una operación no procede. Incluye el motivo en términos del negocio y una alternativa sugerida. | Nuevo |

## 3. Términos técnicos del desarrollo

| ID | Término | Tipo | Definición | Estado |
|---|---|---|---|---|
| G-46 | Lenguaje ubicuo | Técnico | Vocabulario común entre usuarios, analistas y desarrolladores; este glosario es su fuente. | Nuevo |
| G-47 | Domain Model | Técnico | Patrón de lógica de negocio en el que las reglas viven en objetos del dominio con comportamiento (ADR-001). | Nuevo |
| G-48 | Transaction Script | Técnico | Patrón en el que cada operación es un procedimiento que contiene sus reglas. Se usa solo para lecturas (ADR-001). | Nuevo |
| G-49 | Table Module | Técnico | Patrón con una clase por tabla que agrupa la lógica; evaluado y descartado (ADR-001). | Nuevo |
| G-50 | Entidad | Técnico | Objeto con identidad y ciclo de vida, distinguible aunque cambien sus datos (por ejemplo Línea de Venta). | Nuevo |
| G-51 | Value Object | Técnico | Objeto definido por sus valores y reglas, sin identidad propia, inmutable (por ejemplo Dinero, Número de Serie, Folio). | Nuevo |
| G-52 | Aggregate | Técnico | Grupo de objetos con un límite de consistencia y una raíz que controla los cambios. Son Apartado, Unidad de Inventario, Venta, Devolución, Política Comercial y Producto (ADR-002). | Nuevo |
| G-53 | Raíz de Agregado | Técnico | Objeto único por el que se accede y se modifica un aggregate; lleva la versión para concurrencia. | Nuevo |
| G-54 | Domain Event | Técnico | Hecho del negocio que ya ocurrió (por ejemplo VentaCerrada) y que otros componentes pueden atender. | Nuevo |
| G-55 | Servicio de Dominio | Técnico | Comportamiento del dominio que no pertenece a una sola entidad; aquí, la Conversión de Apartado. | Nuevo |
| G-56 | Application Service | Técnico | Clase de la capa de aplicación que expone un caso de uso y solo coordina; no contiene reglas de negocio. | Nuevo |
| G-57 | Service Layer | Técnico | Capa formada por los Application Services (ADR-003). | Nuevo |
| G-58 | Command | Técnico | Solicitud que cambia el estado del sistema (por ejemplo ApartarProducto). | Nuevo |
| G-59 | Query | Técnico | Solicitud que solo consulta información y devuelve un DTO de lectura. | Nuevo |
| G-60 | CQRS | Técnico | Separación entre Commands y Queries con modelos distintos para escribir y leer (ADR-004). | Nuevo |
| G-61 | DTO | Técnico | Objeto de transferencia de datos usado en las respuestas y en las peticiones de la API; no se reutiliza como modelo de escritura. | Nuevo |
| G-62 | Repository | Técnico | Interfaz del dominio para obtener y guardar aggregates, expresada en términos del negocio y sin SQL. | Nuevo |
| G-63 | Unit of Work | Técnico | Objeto que agrupa los cambios de un caso de uso en una transacción: se inicia, se confirma o se descarta. | Nuevo |
| G-64 | Puerto | Técnico | Interfaz de la capa de aplicación que oculta un recurso externo (reloj, generador de folios, notificador). | Nuevo |
| G-65 | Specification | Técnico | Regla de negocio reutilizable que responde si un objeto cumple una condición y se combina con Y, O y No (ADR-007). | Nuevo |
| G-66 | Máquina de Estados | Técnico | Definición explícita de los estados de un objeto y de las transiciones permitidas; una transición no permitida lanza una violación de regla (ADR-008). | Nuevo |
| G-67 | Regla de Negocio Violada | Técnico | Excepción de dominio que lleva el motivo de rechazo y un mensaje en términos del negocio (ADR-006). | Nuevo |
| G-68 | Concurrencia Optimista | Técnico | Control por versión que evita que dos operaciones sobrescriban el mismo aggregate; el conflicto se informa como ConflictoDeConcurrencia. | Nuevo |
| G-69 | ORM / EF Core | Técnico | Biblioteca que mapea los aggregates a tablas de SQL Server; vive solo en infraestructura (ADR-009). | Nuevo |
| G-70 | API REST | Técnico | Interfaz HTTP del sistema; traduce los rechazos a códigos 200/201, 400, 404, 409 y 422. | Nuevo |
| G-71 | OpenAPI / Swagger | Técnico | Especificación y página interactiva que documentan los endpoints de la API. | Nuevo |
| G-72 | ADR | Técnico | Architecture Decision Record: registro de una decisión con contexto, alternativas, decisión, consecuencias y trazabilidad. | Nuevo |
| G-73 | Trazabilidad | Técnico | Relación verificable entre necesidad, regla, historia, caso de uso, componente, ADR y prueba. | Nuevo |
| G-74 | Caso de Uso (CU) | Técnico | Intención completa de un actor que el sistema expone como un método de servicio. | Nuevo |
| G-75 | Contexto de Usuario | Técnico | Puerto que entrega el Operador que ejecuta la operación, tomado de los encabezados de la petición. | Nuevo |
| G-76 | Cobertura | Técnico | Porcentaje del código ejercitado por las pruebas automáticas; el mínimo exigido es 80%. | Nuevo |
| G-77 | Docker Compose | Técnico | Definición de los contenedores del sistema: base de datos, aplicación y pruebas. | Nuevo |
| G-78 | CI/CD | Técnico | Automatización que compila, prueba y valida el proyecto en cada cambio (GitHub Actions). | Nuevo |

## 4. Equivalencias con los datos de ejemplo del curso

| Término de los CSV del curso | Término de TecnoStock | Nota |
|---|---|---|
| comprador (actor) | Cliente | Los CSV se usan solo como datos semilla. |
| recurso (catálogo) | Producto | El campo capacidad_o_stock se usa para generar unidades de inventario. |
| operación | Venta, Apartado o Devolución | Los estados BORRADOR, APROBADA, SURTIDA y ENTREGADA no existen en el negocio de TecnoStock. |
