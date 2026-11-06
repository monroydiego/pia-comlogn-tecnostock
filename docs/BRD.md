# Reporte — BRD refinado de TecnoStock

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
| Actividad | PIA — BRD refinado (versión corregida de U2) |
| Fecha | 06/11/2026 |

## Bibliografía (formato APA)

- Documento de caso asignado enviado por el docente. (2026). Caso 05: TecnoStock, retail de electrónica.
- Gómez Marroquín, M. Á. (2026). Producto Integrador de Aprendizaje: Implementación de una solución de software basada en lógica de negocio [Documento del curso Componentes de lógica de negocio].
- BRD, User Stories, BPMN y Glosario de dominio de TecnoStock, versión de la Unidad 2 (07/09/2026) y diagramas y ADR de la Unidad 3.

## Objetivo

Documentar los requerimientos de negocio de TecnoStock en un BRD completo, coherente con las historias de usuario, el BPMN, el glosario y los diagramas, que sirva como base obligatoria de la arquitectura y de la implementación del PIA.

## Procedimiento

Se partió del BRD de la Unidad 2 (07/09/2026). Se contrastó con las 20 historias, el BPMN, el glosario y los diagramas de la Unidad 3. Cada discrepancia se corrigió y se registró en el Registro de cambios (C-01 a C-21). Lo que el caso no define se marca como **[Supuesto]** y queda pendiente de validar con el stakeholder; no se inventó ninguna política sin marcarla.

## 1. Contexto y objetivos de negocio

TecnoStock es una cadena de cinco sucursales de retail de electrónica (computadoras, celulares, accesorios y componentes). Vende en sucursal y permite apartados de ciertos productos. El estado actual (AS-IS) presenta descuadres entre inventario físico y de sistema, venta accidental de productos apartados, pérdida de trazabilidad de números de serie y criterios de devolución inconsistentes. El estado futuro (TO-BE) busca una operación controlada, trazable y auditable en las cinco sucursales.

### 1.1 Objetivos estratégicos

- OE-1. Eliminar la discrepancia entre inventario físico y de sistema.
- OE-2. Impedir la venta de productos con apartado vigente mediante el bloqueo íntegro de la unidad reservada.
- OE-3. Garantizar la trazabilidad completa de las unidades serializadas.
- OE-4. Estandarizar los criterios objetivos de devolución y la autorización del Supervisor.

## 2. Problema y oportunidad

| Problema detectado | Causa raíz probable | Oportunidad de mejora | Necesidad |
|---|---|---|---|
| El inventario del sistema no coincide con el físico. | Falta de conciliación periódica y ajustes sin control. | Reglas de conciliación (RN-INV) y autorización de ajustes (RN-SUP). | BR-008 |
| Venta accidental de productos apartados. | Ausencia de bloqueo íntegro de la unidad reservada. | Bloqueo de la unidad apartada (RN-APT-003). | BR-004 |
| Dificultad para rastrear números de serie. | La captura de serie no es obligatoria ni validada. | Validación obligatoria de la serie al cerrar la venta (RN-VTA-001) y unicidad (RN-INV-002). | BR-003 |
| Devoluciones aplicadas de forma inconsistente. | No existe una política objetiva de devolución. | Catálogo formal de condiciones de devolución (RN-DEV). | BR-005 |

## 3. Stakeholders, roles y gobernanza

Los cinco roles los asignó el profesor y no se modifican. Para eliminar la ambigüedad entre Vendedor y Cajero, esta versión define una frontera explícita (cambio C-01): el **Vendedor atiende antes del cobro** y el **Cajero maneja el dinero y el cierre**.

| Rol | Responsabilidad principal | Autoridad clave |
|---|---|---|
| Cliente | Selecciona productos, solicita apartados y devoluciones. | Ninguna autoridad de sistema. |
| Vendedor | Consulta disponibilidad (local y otras sucursales), arma la orden de venta o la solicitud de apartado, escanea la serie al armar la orden, captura el motivo de devolución e inicia la conversión de un apartado en venta. | No cobra, no autoriza excepciones ni descuentos. |
| Cajero | Cobra anticipos y saldos, valida el método de pago, confirma la serie al cerrar la venta, valida el comprobante y la condición física del producto en una devolución y emite comprobantes y reembolsos o notas de crédito. | No modifica precios ni políticas. |
| Supervisor | Resuelve escalaciones y controla excepciones. | Único autorizado para descuentos extraordinarios, cancelaciones, ajustes de inventario y devoluciones fuera de política. |
| Almacén | Resguarda, despacha, recibe y transfiere mercancía; da de alta unidades con su serie; solicita transferencias entre sucursales. | Reporta discrepancias; no ajusta inventario sin autorización. |
| Sistema | Ejecuta de forma automática la evaluación de la política de devolución y el vencimiento de apartados. | Solo aplica reglas ya definidas. |

## 4. Alcance

### 4.1 Dentro del alcance

- Venta en sucursal, apartados, devoluciones y transferencias entre las 5 sucursales.
- Trazabilidad de número de serie y conciliación de inventario físico contra sistema.
- Niveles de autorización del rol Supervisor sobre las operaciones anteriores.

### 4.2 Fuera del alcance

- Comercio electrónico (web o app), nómina y finanzas corporativas avanzadas.
- Compras a proveedores, marketing y fidelización, apertura de nuevas sucursales.

### 4.3 Alcance de la implementación del PIA

Las necesidades BR-001 a BR-006 se implementan en el sistema. BR-007 (transferencias) y BR-008 (conciliación) forman parte del alcance del negocio y están documentadas con sus historias (US15, US18, US19), pero **no se implementan en el PIA**; el diseño las contempla mediante el estado EnTransito de la unidad. Las historias US01 a US14, US16 (consulta), US17 y US20 sí se implementan.

## 5. Necesidades y requerimientos de negocio

| ID | Título | Descripción | Prioridad | Historias |
|---|---|---|---|---|
| BR-001 | Disponibilidad multi-sucursal | Consultar la existencia de un producto en las 5 sucursales en tiempo de atención. | Alta | US04, US05 |
| BR-002 | Venta con pago autorizado | Restringir el cobro a métodos de pago formalmente autorizados. | Alta | US11, US12 |
| BR-003 | Trazabilidad de número de serie | Registrar la unidad física exacta entregada en venta, transferencia o devolución. | Alta | US09, US17, US19 |
| BR-004 | Gestión de apartados | Reservar una unidad de un producto apartable mediante anticipo, con vigencia y bloqueo. | Alta | US01, US03, US06, US07, US11, US14, US20 |
| BR-005 | Gestión de devoluciones | Validar comprobante, plazo, condición del producto y causal antes de aceptar una devolución. | Alta | US02, US08, US10, US16 |
| BR-006 | Autorización de excepciones | Requerir validación del Supervisor en operaciones sensibles. | Alta | US13, US14 |
| BR-007 | Transferencias entre sucursales | Trasladar mercancía con visibilidad de estado (origen, tránsito, destino). | Media | US18, US19 |
| BR-008 | Conciliación de inventario | Comparar periódicamente la existencia física contra el sistema. | Alta | US15 |

## 6. Catálogo de reglas de negocio

Las reglas con **[Supuesto]** no estaban definidas en el caso; se derivan de una necesidad, del glosario o del BPMN y quedan pendientes de validar con el stakeholder.

| ID | Tipo | Declaración formal | Disparador | Origen |
|---|---|---|---|---|
| RN-VTA-001 | Restricción | Todo producto serializable requiere número de serie registrado antes de cerrar la venta. | Cierre de venta. | U2 |
| RN-VTA-002 | Restricción | Solo se aceptan métodos de pago autorizados por la política vigente. | Captura del pago. | U2 |
| RN-APT-001 | Restricción | Todo apartado exige un anticipo mínimo según la política comercial. | Registro de apartado. | U2 [Supuesto] |
| RN-APT-002 | Restricción | El apartado tiene un plazo máximo de vigencia; vencido sin liquidar cambia a «Vencido» y libera la unidad. | Cumplimiento del plazo. | U2 [Supuesto] |
| RN-APT-003 | Restricción | La unidad apartada queda bloqueada para venta a terceros mientras el apartado esté vigente. | Registro exitoso del apartado. | U2 |
| RN-APT-004 | Restricción | Solo los productos marcados como apartables pueden apartarse. | Registro de apartado. | Nueva (C-08) [Supuesto]. Caso: «algunos artículos pueden apartarse». |
| RN-INV-001 | Disparador | Debe realizarse una conciliación periódica de inventario físico contra sistema. | Cumplimiento del periodo. | U2 |
| RN-INV-002 | Restricción | Cada número de serie corresponde a una única unidad activa; se prohíbe la duplicidad. | Registro o recepción de unidad. | U2 |
| RN-INV-003 | Disparador | La mercancía transferida queda «en tránsito» hasta la confirmación en destino. | Envío de transferencia. | U2 |
| RN-INV-004 | Restricción | Una unidad con devolución aceptada vuelve a «Disponible» solo si se devolvió con empaque cerrado; en otro caso pasa a «Defectuosa» y se retira de la venta. | Devolución aceptada. | Nueva (C-11) [Supuesto]. Glosario G-12 y G-13; BPMN «Reingresar a stock / merma». |
| RN-DEV-001 | Restricción | Toda devolución requiere un comprobante de compra válido. Sin comprobante válido la solicitud se rechaza de forma definitiva. | Solicitud de devolución. | U2 |
| RN-DEV-002 | Restricción | La devolución procede dentro de un plazo máximo desde la compra, según la categoría del producto. | Solicitud de devolución. | U2 |
| RN-DEV-003 | Restricción | El Cajero registra la condición del producto (empaque cerrado, empaque abierto o defectuoso). La devolución estándar acepta empaque cerrado o defectuoso; empaque abierto es excepción. | Solicitud de devolución. | Nueva (C-09) [Supuesto]. BR-005; glosario G-09 y G-10. |
| RN-DEV-004 | Restricción | Toda devolución aceptada se compensa con reembolso o con nota de crédito. | Devolución aceptada. | Nueva (C-10) [Supuesto]. Glosario G-11; BPMN «Emitir reembolso / nota de crédito». |
| RN-SUP-001 | Autorización | Descuentos fuera de política, cancelaciones y ajustes de inventario requieren autorización del Supervisor. | Operación fuera de política estándar. | U2 |
| RN-SUP-002 | Autorización | Las devoluciones fuera de política solo proceden con autorización documentada del Supervisor. | Incumplimiento de criterio estándar. | U2 |

## 7. Supuestos, parámetros y restricciones

### 7.1 Parámetros de la política comercial

El BRD de U2 solo daba rangos. Para poder implementar y probar se fijan los valores siguientes. Todos son configurables en la política comercial y deben validarse con el stakeholder (cambio C-07).

| Parámetro | Valor propuesto | Rango del BRD de U2 | Estado |
|---|---|---|---|
| Anticipo mínimo de apartado | 30% del precio acordado | 20% a 30% | [Supuesto] |
| Vigencia máxima de apartado | 7 días naturales | 7 días naturales | [Supuesto] |
| Plazo de devolución, Computadoras y Celulares | 15 días naturales | 15 a 30 días según categoría | [Supuesto] |
| Plazo de devolución, Accesorios y Componentes | 30 días naturales | 15 a 30 días según categoría | [Supuesto] |
| Métodos de pago autorizados | Efectivo, Tarjeta y Transferencia | Sin catálogo | [Supuesto] |
| Periodicidad de conciliación | Mensual | Mensual | [Supuesto] |
| Productos con número de serie | Laptop, Monitor y SSD | Sin lista | [Supuesto] |
| Productos apartables | Laptop, Monitor y SSD | «Ciertos productos» | [Supuesto] |

### 7.2 Restricciones del negocio

- Operación limitada a las 5 sucursales físicas existentes; sin canal digital al cliente final.
- Toda excepción a la política estándar requiere intervención documentada del Supervisor.
- Este documento define lógica de negocio, no arquitectura tecnológica.
- Los importes se manejan en pesos mexicanos (MXN).

### 7.3 Supuestos pendientes de validar

- El canal de notificación de vencimientos y de devoluciones en revisión (US13 y US20) no está definido.
- Un cambio de producto se resuelve emitiendo una nota de crédito; aplicar la nota de crédito como método de pago queda fuera del alcance.
- Los precios y el catálogo de la implementación provienen de los datos de ejemplo del curso y no representan precios reales.

## 8. Criterios de éxito medibles

Las metas numéricas son **[Supuesto]** y se validarán con el stakeholder (cambio C-06).

| ID | Criterio | Medición | Objetivo | Se verifica con |
|---|---|---|---|---|
| CE-01 | Ninguna venta de una unidad con apartado vigente. | Ventas de unidades en estado Apartada ajenas a su apartado. | 0 | US06; prueba de rechazo UnidadApartada |
| CE-02 | Toda venta de un producto serializable lleva serie registrada y única. | Ventas serializables sin serie o con serie duplicada. | 0 | US09, US17 |
| CE-03 | Los descuadres de inventario se detectan en la conciliación mensual. | Conciliaciones realizadas por mes. | 1 por sucursal | US15 (documentada) |
| CE-04 | Toda devolución fuera de política tiene decisión documentada del Supervisor. | Devoluciones aceptadas fuera de política sin decisión registrada. | 0 | US13 |
| CE-05 | La disponibilidad se consulta en las 5 sucursales durante la atención. | Tiempo de respuesta de la consulta. | 3 segundos o menos | US04, US05 |
| CE-06 | Los apartados vencidos liberan su unidad. | Apartados vencidos con unidad aún bloqueada tras el proceso de vencimiento. | 0 | US20 |
| CE-07 | Toda operación genera un comprobante con folio único. | Operaciones sin comprobante o con folio repetido. | 0 | US12 |

## 9. Trazabilidad

La trazabilidad completa (necesidad, regla, historia, caso de uso, componente, ADR y prueba) se mantiene en docs/architecture/matriz_trazabilidad.md. Las historias de usuario y sus escenarios Gherkin están en docs/analisis/historias_de_usuario.md; el BPMN en docs/analisis/bpmn/; el glosario en docs/analisis/glosario.md.
