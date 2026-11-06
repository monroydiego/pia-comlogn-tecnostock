# Reporte — Registro de cambios a los documentos de U2 y U3

| Nombre | Matrícula |
|---|---|
| Eduardo Alejandro Acevedo Liuskos | 2142139 |
| Emma Patricia Brown Sánchez | 2135479 |
| Diego Monroy García | 2135198 |

| Datos del curso | |
|---|---|
| Nombre del curso | Componentes de lógica de negocio |
| Nombre del profesor | Miguel Ángel Gómez Marroquín |
| Módulo Netacad | PIA |
| Actividad | PIA — Registro de cambios y decisiones de alineación |
| Fecha | 06/11/2026 |

## Bibliografía (formato APA)

- Documento de caso asignado enviado por el docente. (2026). Caso 05: TecnoStock, retail de electrónica.
- Gómez Marroquín, M. Á. (2026). Producto Integrador de Aprendizaje: Implementación de una solución de software basada en lógica de negocio [Documento del curso Componentes de lógica de negocio].
- BRD, User Stories, BPMN y Glosario de dominio de TecnoStock, versión de la Unidad 2 (07/09/2026) y diagramas y ADR de la Unidad 3.

## Objetivo

Dejar constancia de cada cambio hecho a los documentos de las Unidades 2 y 3 para alinearlos entre sí y con los requisitos del PIA: qué cambió, por qué fue la mejor decisión y qué documentos afecta.

## Procedimiento

Se contrastaron el BRD, las historias, el BPMN, el glosario, los diagramas, los ADR y el PIA. Donde había discrepancia se aplicó el criterio acordado con el equipo: respetar los documentos originales, corregir lo que se contradice y marcar como [Supuesto] lo que el caso no define. Los cambios de lógica de negocio quedan pendientes de validar con el stakeholder.

## 1. Decisiones de alcance y criterio

- Los roles Vendedor, Cajero, Supervisor, Almacén y Cliente los asignó el profesor y no se modifican; se delimita su frontera (C-01).
- Los archivos CSV y el LEEME del curso son datos de ejemplo genéricos; no sustituyen los requerimientos. Se usan solo como datos semilla (productos y clientes).
- Se implementan las necesidades BR-001 a BR-006. BR-007 y BR-008 quedan documentadas.
- Stack: .NET 10, C#, SQL Server (decisión del equipo).

## 2. Cambios

| ID | Documento | Qué cambió | Por qué (conflicto o vacío) | Por qué fue la mejor decisión | Afecta |
|---|---|---|---|---|---|
| C-01 | BRD, glosario, historias, BPMN | Se define la frontera entre Vendedor (atiende antes del cobro) y Cajero (dinero y cierre). US07 y US09 se reformulan. | Los roles no estaban delimitados y el BPMN, US07 y US09 se contradecían. | Respeta los roles del profesor y deja una sola lectura en todos los documentos. | BRD 3; glosario G-22 y G-23; US07, US09; BPMN |
| C-02 | BPMN | La serie la escanea el Vendedor y la confirma el Cajero. | BPMN: Vendedor; US09: Cajero. | RN-VTA-001 dispara en el cierre de venta, que es del Cajero; el Vendedor ya tiene el producto en la mano. | BPMN; glosario G-19 |
| C-03 | BPMN | La transferencia la solicita Almacén. | BPMN: Vendedor; US18: Almacén. | Las historias y el BRD asignan a Almacén el movimiento de mercancía. | BPMN |
| C-04 | BPMN | El Sistema evalúa la política de devolución; el Supervisor solo ve excepciones. Comprobante inválido es rechazo definitivo. | BPMN ponía la decisión en el Supervisor para toda devolución. | US10, US13, el BRD y los diagramas de U3 lo definen así; evita saturar al Supervisor. | BPMN; glosario G-09 y G-10 |
| C-05 | BPMN | Se agregan el cobro de anticipo por el Cajero, el carril Sistema, el vencimiento (US20), la cancelación (US14) y las notas de alcance de BR-007 y BR-008. | El BPMN omitía flujos que las historias sí describen. | El BPMN debe cubrir todas las historias implementadas. | BPMN |
| C-06 | BRD | Se agregan 7 criterios de éxito medibles (CE-01 a CE-07). | El BRD no tenía criterios de éxito, que la guía de U3 exige como insumo. | Permite verificar los objetivos estratégicos. Las metas son [Supuesto]. | BRD 8 |
| C-07 | BRD | Se cierra la tabla de parámetros: anticipo 30%, vigencia 7 días, devolución 15 y 30 días por categoría, métodos de pago Efectivo, Tarjeta y Transferencia. | El BRD solo tenía rangos y el catálogo de pagos pendiente. | No se puede implementar ni probar sin valores concretos; quedan configurables y marcados [Supuesto]. | BRD 7.1 |
| C-08 | BRD, historias, dominio | Nueva regla RN-APT-004: solo los productos apartables pueden apartarse. Producto lleva permiteApartado. | El caso dice «algunos artículos pueden apartarse» y el modelo no lo representaba. | Sin la regla cualquier producto podría apartarse. [Supuesto] | BRD 6; US01; diagramas 01 y 02 |
| C-09 | BRD, glosario, historias, dominio | Nueva regla RN-DEV-003: condición del producto (empaque cerrado, abierto o defectuoso). Se agrega CondicionDelProducto y ReglaDeCondicionDelProducto. | BR-005 pide validar el estado del producto, pero RN-DEV solo tenía comprobante y plazo; el glosario hablaba de empaque cerrado. | Cierra la brecha entre la necesidad, el glosario y las reglas. [Supuesto] | BRD 5 y 6; G-09, G-10, G-43; US10; diagramas 02 y 05 |
| C-10 | BRD, glosario, historias, dominio | Nueva regla RN-DEV-004: toda devolución aceptada se compensa con reembolso o nota de crédito. US02 deja de mencionar «cambio». | US02 hablaba de reembolso o cambio, y el modelo no tenía compensación. | Alinea US02, el glosario G-11 y el BPMN; el cambio se resuelve con nota de crédito. [Supuesto] | BRD 6; US02; G-44; diagramas 02, 03 y 05 |
| C-11 | BRD, glosario, dominio, ADR | Nueva regla RN-INV-004. EstadoDeUnidad reemplaza Devuelta por Defectuosa: la unidad vuelve a Disponible solo con empaque cerrado. | U3 dejaba el destino de la unidad devuelta como supuesto (H-06) y el BPMN habla de reingreso o merma. | Una unidad devuelta con empaque abierto no debe volver a venderse como nueva. [Supuesto] | BRD 6; G-12, G-13; diagramas 01, 02 y 05; ADR-008 |
| C-12 | Glosario | Se corrigen G-01, G-02, G-04 a G-07, G-09 a G-13, G-15, G-16, G-18 a G-20. G-06 pasa a «Comprobante». | Definiciones que contradecían al BRD, las historias o los diagramas. | Un solo significado por término en todos los documentos y en el código. | Glosario |
| C-13 | Glosario | Se agregan 58 términos (G-21 a G-78): negocio y técnicos del desarrollo. | El equipo pidió registrar todo término técnico usado en el desarrollo. | El glosario es la fuente del lenguaje ubicuo. | Glosario |
| C-14 | Historias | Se agregan escenarios de rechazo (44 escenarios en total), columnas de trazabilidad y de alcance. US02, US07 y US09 se reformulan. | Varias historias solo tenían flujo normal. | Sirven como base de las pruebas y evidencian las reglas de rechazo. | Historias |
| C-15 | Diagrama 02 (dominio) | Se agregan Specification, MaquinaDeEstados, permiteApartado, CondicionDelProducto, FormaDeCompensacion, EstadoDeUnidad.Defectuosa, motivos ProductoNoApartable y SerieDuplicada, y métodos de repositorio. | El PIA exige Specification y State Machine; U3 las tenía implícitas. | Hace explícitos los componentes obligatorios y evita diagramas desalineados del código. | Diagrama 02; ADR-007 y ADR-008 |
| C-16 | Diagrama 03 (aplicación) | Infraestructura con EF Core y SQL Server en lugar de memoria, capa API, ServicioDeInventario y casos de uso CU-06 a CU-08. | U3 decía «sin base de datos por ahora»; el PIA exige base de datos y API. | El diagrama debe corresponder al código. | Diagrama 03; ADR-009 |
| C-17 | Diagramas 01 y 05 | El mapa incorpora los nuevos elementos. La secuencia 05 agrega condición y compensación y las Specifications del criterio. | Consistencia con el diagrama 02. | Los diagramas deben representar la misma arquitectura. | Diagramas 01 y 05 |
| C-18 | Diagrama 04 | Sin cambios. | Ya era consistente con el reparto de roles (el Cajero cobra el saldo). | — | — |
| C-19 | ADR y matriz | Se agregan ADR-007 (Specification), ADR-008 (máquina de estados) y ADR-009 (EF Core y SQL Server). La matriz agrega CU-06 a CU-08, las nuevas reglas y la columna de pruebas. | Son decisiones nuevas con consecuencias de diseño. | La guía exige registrar cada decisión con alternativas y consecuencias. | docs/architecture |
| C-20 | ADR-005 | Sin cambio de texto; ahora la lista de reglas de devolución incluye ReglaDeCondicionDelProducto y se apoya en Specifications (ADR-007). | Evitar contradicciones entre ADR. | — | ADR-005, ADR-007 |
| C-21 | CLAUDE.md y CONTEXTO.md | Se actualizan con las decisiones vigentes y las rutas de los documentos corregidos. | Claude Code debe trabajar con la versión corregida y no con los PDF originales. | Evita que el código nazca desalineado. | CLAUDE.md; docs/contexto/CONTEXTO.md |
| C-22 | Diagrama 02 (dominio) | `Devolucion.Resolver(decision, ahora)` recibe la hora; `PoliticaComercial.ValidarMetodoDePago(metodo)` pasa a `ValidarPago(pago)`; se agregan `Apartado.ValidarLiquidacion` y `UnidadInventario.ValidarVentaDesdeApartado`; `Apartado.Crear` aparta la unidad por dentro; `PoliticaComercial` guarda los plazos por categoría y arma el `CriterioDeDevolucion` al pedirlo. | Los eventos necesitan la hora; la Specification evalúa un `PagoRecibido`; y sin validar antes de modificar, un rechazo dejaba cambios parciales en memoria. | Las invariantes quedan en el dominio, un rechazo no modifica nada y la política se puede guardar en SQL sin serializar objetos. | Diagrama 02; ADR-007 |
| C-23 | Diagrama 02 (repositorios) | Las interfaces de repositorio devuelven `null` si no existe el aggregate y la capa de aplicación lo traduce a `NoEncontrado`. `ObtenerUnidadParaVenta` recibe además las unidades ya elegidas (`excluidas`). | Una venta con dos líneas del mismo producto sin serie elegiría dos veces la misma unidad. | Evita vender dos veces una unidad dentro de la misma orden sin agregar reglas al servicio. | Diagrama 02 |
| C-24 | Diagrama 03 (aplicación) | `ManejadorDeComprobantes` usa además `IGeneradorDeFolios`, `IContextoDeUsuario` y `IRepositorioDeVentas`; se define el registro `Comprobante` y el enum `TipoDeOperacion`. | US09 exige que el comprobante incluya la serie y US12 un folio único con usuario y fecha; los eventos de dominio solo traen folio y total. | El comprobante sale completo sin ampliar los eventos del dominio. | Diagrama 03; US09, US12 |
| C-25 | Diagrama 03 (aplicación) | Los lectores devuelven `null` y las consultas lanzan `ReglaDeNegocioViolada(NoEncontrado)`. Los servicios de escritura heredan de `ServicioDeAplicacion`: si la acción regresa se confirma (incluido el rechazo con efecto), si lanza una violación se descarta y se traduce a `Rechazo`, y cualquier otra excepción descarta y se propaga. | Los diagramas no definían cómo se informa un folio o producto inexistente en una consulta ni dónde vive el control de la transacción. | Un solo lugar implementa los tres tipos de rechazo de ADR-006 y evita repetir el try/catch en cada caso de uso. | Diagrama 03; ADR-003, ADR-006 |
| C-26 | Servicios de aplicación | [Supuesto] Solo se exige rol para las acciones del Supervisor (CU-05 y CU-06). No se valida que el cliente ni la sucursal existan en un catálogo. | Los documentos solo definen la autorización del Supervisor y no hay catálogo de sucursales ni repositorio de clientes. | Evita inventar permisos y entidades que el negocio no definió; los clientes y las sucursales llegan como datos semilla. | CU-01 a CU-08 |

## 3. Pendientes de validar con el stakeholder

- Todos los valores de la tabla de parámetros del BRD 7.1.
- RN-APT-004, RN-DEV-003, RN-DEV-004 y RN-INV-004.
- Metas numéricas de los criterios de éxito.
- Canal de notificación de vencimientos y devoluciones en revisión.
