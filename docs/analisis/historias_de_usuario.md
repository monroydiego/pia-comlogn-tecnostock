# Reporte — Historias de usuario y criterios de aceptación de TecnoStock (versión corregida)

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
| Actividad | PIA — Historias de usuario con Gherkin (versión corregida de U2) |
| Fecha | 06/11/2026 |

## Bibliografía (formato APA)

- Documento de caso asignado enviado por el docente. (2026). Caso 05: TecnoStock, retail de electrónica.
- Gómez Marroquín, M. Á. (2026). Producto Integrador de Aprendizaje: Implementación de una solución de software basada en lógica de negocio [Documento del curso Componentes de lógica de negocio].
- BRD, User Stories, BPMN y Glosario de dominio de TecnoStock, versión de la Unidad 2 (07/09/2026) y diagramas y ADR de la Unidad 3.

## Objetivo

Documentar las 20 historias de usuario de TecnoStock con criterios de aceptación en Gherkin que cubran el flujo normal y los rechazos, alineadas con el BRD, el BPMN, el glosario y los diagramas, para servir de base de la implementación y de las pruebas.

## Procedimiento

Se conservaron los 20 identificadores de U2 (mínimo exigido: 15). Se corrigieron US02, US07 y US09 para alinearlas con el reparto de roles Vendedor y Cajero y con la compensación de devoluciones, y se agregaron escenarios de rechazo a las historias que solo tenían flujo normal. Cada historia indica las necesidades y reglas que cubre y si se implementa en el PIA. Los cambios están en el Registro de cambios (C-14).

## 1. Resumen de historias

| ID | Rol | Historia | Prior. | Necesidad y reglas | Alcance en el PIA |
|---|---|---|---|---|---|
| US01 | Cliente | Como Cliente, quiero apartar un producto pagando un anticipo, para asegurar que nadie más lo compre mientras reúno el pago completo. | Alta | BR-004; RN-APT-001, RN-APT-003, RN-APT-004 | Implementada (CU-01) |
| US02 | Cliente | Como Cliente, quiero conocer el estado de mi devolución, para saber si procede el reembolso o la nota de crédito, o si fue rechazada. | Media | BR-005; RN-DEV-004 | Implementada (consulta) |
| US03 | Cliente | Como Cliente, quiero recibir confirmación de mi apartado con su fecha límite, para saber cuánto tiempo tengo para completar el pago. | Media | BR-004; RN-APT-002 | Implementada (CU-01) |
| US04 | Vendedor | Como Vendedor, quiero consultar la disponibilidad de un producto en mi sucursal en tiempo real, para informar correctamente al cliente. | Alta | BR-001 | Implementada (consulta) |
| US05 | Vendedor | Como Vendedor, quiero consultar la disponibilidad de un producto en otras sucursales, para ofrecer alternativas cuando no hay stock local. | Alta | BR-001 | Implementada (consulta) |
| US06 | Vendedor | Como Vendedor, quiero que el sistema impida vender un producto que está apartado y vigente, para evitar ventas accidentales sobre stock comprometido. | Alta | BR-004; RN-APT-003 | Implementada (CU-02) |
| US07 | Vendedor | Como Vendedor, quiero iniciar la conversión de un apartado vigente en venta cuando el cliente va a liquidar el saldo, para que el Cajero complete la operación sin perder el anticipo. | Alta | BR-004; RN-APT-002, RN-VTA-001, RN-VTA-002 | Implementada (CU-03) |
| US08 | Vendedor | Como Vendedor, quiero registrar el motivo de una devolución, para que el negocio pueda dar seguimiento a problemas recurrentes de producto. | Media | BR-005 | Implementada (CU-04) |
| US09 | Cajero | Como Cajero, quiero confirmar el número de serie exacto que el Vendedor registró al armar la orden, para dejar en cada comprobante la unidad física que salió. | Alta | BR-003; RN-VTA-001 | Implementada (CU-02, CU-03) |
| US10 | Cajero | Como Cajero, quiero validar el comprobante, el plazo y la condición del producto antes de procesar una devolución, para asegurar que corresponde a una venta real y vigente. | Alta | BR-005; RN-DEV-001, RN-DEV-002, RN-DEV-003 | Implementada (CU-04) |
| US11 | Cajero | Como Cajero, quiero recibir el pago del anticipo al crear un apartado, para formalizar la reserva del producto. | Alta | BR-002, BR-004; RN-VTA-002, RN-APT-001 | Implementada (CU-01) |
| US12 | Cajero | Como Cajero, quiero que cada operación genere un comprobante con folio único, para dejar respaldo verificable de la transacción. | Alta | BR-002; CE-07 | Implementada (manejador de eventos) |
| US13 | Supervisor | Como Supervisor, quiero autorizar o denegar una devolución que no cumple las condiciones estándar, para resolver casos excepcionales sin violar la política general. | Alta | BR-006; RN-SUP-002 | Implementada (CU-05) |
| US14 | Supervisor | Como Supervisor, quiero cancelar manualmente un apartado antes de su vencimiento, para atender solicitudes especiales del cliente. | Media | BR-004, BR-006; RN-SUP-001 | Implementada (CU-06) |
| US15 | Supervisor | Como Supervisor, quiero generar un reporte de descuadres entre el inventario del sistema y el físico, para identificar y corregir diferencias. | Alta | BR-008; RN-INV-001 | Documentada (fuera de la implementación) |
| US16 | Supervisor | Como Supervisor, quiero definir y consultar el criterio vigente de devoluciones, para que se aplique de manera uniforme entre sucursales. | Alta | BR-005; RN-DEV-002 | Parcial: consulta implementada; la definición se hace por configuración sembrada |
| US17 | Almacén | Como Almacén, quiero registrar el número de serie al dar de alta una unidad recibida, para rastrearla durante todo su ciclo de vida. | Alta | BR-003; RN-INV-002 | Implementada (CU-07) |
| US18 | Almacén | Como Almacén, quiero solicitar la transferencia de un producto desde otra sucursal, para cubrir una venta o apartado sin existencia local. | Alta | BR-007; RN-INV-003 | Documentada (fuera de la implementación) |
| US19 | Almacén | Como Almacén, quiero confirmar la recepción de una unidad transferida desde otra sucursal, para actualizar su ubicación en el inventario. | Alta | BR-007; RN-INV-003 | Documentada (fuera de la implementación) |
| US20 | Sistema | Como Sistema, quiero liberar automáticamente un apartado que vence sin completarse el pago, para que el producto vuelva a estar disponible. | Alta | BR-004; RN-APT-002; CE-06 | Implementada (CU-08) |

## 2. Criterios de aceptación (Gherkin)

Cada escenario usa Given, When y Then para describir de forma verificable cuándo una historia se considera cumplida. Los motivos de rechazo (por ejemplo AnticipoInsuficiente) son los valores de MotivoDeRechazo del dominio.

### US01 — Cliente

```gherkin
Escenario: Creación de un apartado
    Given que el producto es apartable y tiene una unidad disponible en la sucursal
    When el cliente solicita apartarla y el Cajero registra un anticipo igual o mayor al mínimo de la política
    Then el sistema reserva esa unidad y la excluye del inventario disponible para venta
```

```gherkin
Escenario: Anticipo insuficiente
    Given que el producto es apartable y tiene una unidad disponible
    When el anticipo es menor al porcentaje mínimo de la política
    Then el sistema rechaza el apartado con el motivo AnticipoInsuficiente y la unidad sigue disponible
```

```gherkin
Escenario: Producto no apartable
    Given que el producto no está marcado como apartable
    When el cliente solicita apartarlo
    Then el sistema rechaza la solicitud con el motivo ProductoNoApartable
```

```gherkin
Escenario: Sin existencia
    Given que el producto no tiene unidades disponibles en la sucursal
    When el cliente solicita apartarlo
    Then el sistema rechaza con el motivo SinExistenciaDisponible y sugiere consultar otras sucursales
```

### US02 — Cliente

```gherkin
Escenario: Consulta de estado de devolución
    Given que el cliente registró una solicitud de devolución
    When consulta el folio de la devolución
    Then el sistema muestra el estado actual (En revisión, Aceptada o Rechazada) y, si aplica, el motivo y la forma de compensación
```

```gherkin
Escenario: Folio inexistente
    Given que el folio no corresponde a ninguna devolución
    When el cliente lo consulta
    Then el sistema responde con el motivo NoEncontrado
```

### US03 — Cliente

```gherkin
Escenario: Confirmación de apartado creado
    Given que se registró un apartado con anticipo
    When se completa el registro del apartado
    Then el sistema muestra al cliente el folio, la fecha límite y el saldo pendiente
```

### US04 — Vendedor

```gherkin
Escenario: Producto con existencia
    Given que el producto tiene unidades disponibles en la sucursal actual
    When el Vendedor lo consulta por su código o nombre
    Then el sistema muestra la cantidad disponible de esa sucursal
```

```gherkin
Escenario: Producto inexistente
    Given que el código no existe en el catálogo
    When el Vendedor lo consulta
    Then el sistema responde con el motivo NoEncontrado
```

### US05 — Vendedor

```gherkin
Escenario: Sin existencia local y con existencia en otra sucursal
    Given que el producto no tiene unidades disponibles en la sucursal actual
    When el Vendedor consulta las otras sucursales
    Then el sistema muestra qué sucursales tienen unidades disponibles y la cantidad
```

```gherkin
Escenario: Sin existencia en ninguna sucursal
    Given que ninguna sucursal tiene unidades disponibles
    When el Vendedor consulta las otras sucursales
    Then el sistema devuelve una lista vacía
```

### US06 — Vendedor

```gherkin
Escenario: Intento de venta de unidad apartada
    Given que una unidad está reservada por un apartado vigente
    When otro Vendedor intenta venderla a un cliente distinto
    Then el sistema bloquea la operación con el motivo UnidadApartada
```

### US07 — Vendedor

```gherkin
Escenario: Conversión de apartado a venta
    Given que el cliente tiene un apartado vigente con saldo pendiente
    When el Cajero cobra el saldo restante con un método autorizado y confirma la serie
    Then el sistema genera la venta aplicando el anticipo como parte del pago
    And la unidad reservada pasa a Vendida
```

```gherkin
Escenario: Apartado vencido
    Given que el apartado ya superó su fecha límite
    When se intenta convertirlo en venta
    Then el sistema marca el apartado como Vencido, libera la unidad y rechaza con el motivo ApartadoVencido
```

```gherkin
Escenario: Saldo insuficiente
    Given que el pago del saldo es menor al saldo pendiente
    When el Cajero intenta cerrar la conversión
    Then el sistema rechaza con el motivo SaldoInsuficiente y el apartado sigue vigente
```

```gherkin
Escenario: Método de pago no autorizado
    Given que el método de pago no está en la política
    When el Cajero registra el pago del saldo
    Then el sistema rechaza con el motivo MetodoDePagoNoAutorizado
```

### US08 — Vendedor

```gherkin
Escenario: Registro de motivo
    Given que se está solicitando una devolución
    When el Vendedor captura la causal y el detalle
    Then el sistema almacena el motivo asociado al folio de la devolución para su consulta posterior
```

### US09 — Cajero

```gherkin
Escenario: Venta de producto serializable
    Given que el producto requiere número de serie y el Vendedor lo registró en la orden
    When el Cajero finaliza el cobro y confirma la serie
    Then el sistema cierra la venta
    And el comprobante incluye ese número de serie
```

```gherkin
Escenario: Serie no registrada
    Given que el producto requiere número de serie
    When el Cajero intenta cerrar la venta sin serie
    Then el sistema bloquea el cierre con el motivo SerieRequerida
```

```gherkin
Escenario: Serie que no corresponde
    Given que la serie capturada no pertenece a la unidad seleccionada
    When el Cajero intenta cerrar la venta
    Then el sistema rechaza con el motivo SerieNoCoincide
```

### US10 — Cajero

```gherkin
Escenario: Devolución estándar aceptada
    Given que el cliente presenta un comprobante válido, dentro del plazo de la categoría y con el producto en empaque cerrado
    When el Cajero registra la solicitud
    Then el sistema la acepta sin intervención del Supervisor
```

```gherkin
Escenario: Devolución fuera de plazo
    Given que el comprobante excede el plazo de la categoría
    When el Cajero registra la solicitud
    Then el sistema deja la devolución En revisión y la deriva al Supervisor
```

```gherkin
Escenario: Empaque abierto
    Given que el producto se devuelve con empaque abierto
    When el Cajero registra la condición del producto
    Then el sistema deja la devolución En revisión y la deriva al Supervisor
```

```gherkin
Escenario: Comprobante no válido
    Given que el folio de venta no existe
    When el Cajero registra la solicitud
    Then el sistema rechaza de forma definitiva con el motivo ComprobanteNoValido y no crea la devolución
```

### US11 — Cajero

```gherkin
Escenario: Cobro de anticipo
    Given que el cliente desea apartar un producto
    When el Cajero registra el pago del anticipo con un método autorizado
    Then el sistema crea el apartado únicamente después de aceptar el pago
```

```gherkin
Escenario: Método de pago no autorizado
    Given que el método de pago no está en la política vigente
    When el Cajero registra el anticipo
    Then el sistema rechaza con el motivo MetodoDePagoNoAutorizado y no crea el apartado
```

### US12 — Cajero

```gherkin
Escenario: Emisión de comprobante
    Given que se completa una venta, un apartado o una devolución aceptada
    When el sistema confirma la operación
    Then el sistema genera un comprobante con folio único, fecha, productos y usuario que la realizó
```

```gherkin
Escenario: Operación rechazada
    Given que la operación fue rechazada y no se confirmó
    When el sistema responde al usuario
    Then no se emite ningún comprobante
```

### US13 — Supervisor

```gherkin
Escenario: Autorización de excepción
    Given que una devolución quedó En revisión por incumplir el criterio estándar
    When el Supervisor revisa el caso y decide autorizarla
    Then el sistema la acepta y registra al Supervisor, el motivo y la fecha
```

```gherkin
Escenario: Denegación de excepción
    Given que una devolución está En revisión
    When el Supervisor decide denegarla
    Then el sistema la marca como Rechazada y registra la decisión
```

```gherkin
Escenario: Operador sin rol de Supervisor
    Given que el operador no tiene el rol Supervisor
    When intenta resolver una devolución En revisión
    Then el sistema rechaza con el motivo SoloSupervisorAutoriza
```

```gherkin
Escenario: Devolución que no está en revisión
    Given que la devolución ya fue resuelta
    When se intenta resolverla de nuevo
    Then el sistema rechaza con el motivo TransicionDeEstadoInvalida
```

### US14 — Supervisor

```gherkin
Escenario: Cancelación manual de apartado
    Given que existe un apartado vigente
    When el Supervisor autoriza su cancelación con un motivo
    Then el sistema libera la unidad reservada y registra el motivo de cancelación
```

```gherkin
Escenario: Cancelación por un operador sin rol
    Given que existe un apartado vigente
    When un operador que no es Supervisor intenta cancelarlo
    Then el sistema rechaza con el motivo SoloSupervisorAutoriza
```

```gherkin
Escenario: Apartado que no está vigente
    Given que el apartado ya está Vencido, Convertido o Cancelado
    When el Supervisor intenta cancelarlo
    Then el sistema rechaza con el motivo TransicionDeEstadoInvalida
```

### US15 — Supervisor

```gherkin
Escenario: Generación de reporte de descuadre
    Given que existen conteos físicos registrados para una sucursal
    When el Supervisor solicita el reporte de comparación contra el inventario del sistema
    Then el sistema muestra las diferencias por producto y por número de serie cuando aplique
```

### US16 — Supervisor

```gherkin
Escenario: Consulta del criterio vigente
    Given que existe una política de devolución configurada
    When el Supervisor consulta el criterio vigente
    Then el sistema muestra las condiciones actuales (comprobante, plazo por categoría y condición del producto) aplicables a todas las sucursales
```

### US17 — Almacén

```gherkin
Escenario: Alta de unidad con serie
    Given que llega mercancía nueva a la sucursal
    When Almacén registra la recepción con el número de serie
    Then el sistema crea la unidad como Disponible
```

```gherkin
Escenario: Serie duplicada
    Given que la serie ya existe en el inventario
    When Almacén intenta registrarla
    Then el sistema rechaza con el motivo SerieDuplicada
```

```gherkin
Escenario: Serie faltante
    Given que el producto requiere serie
    When Almacén intenta registrar la unidad sin serie
    Then el sistema rechaza con el motivo SerieRequerida
```

### US18 — Almacén

```gherkin
Escenario: Solicitud de transferencia
    Given que un producto no tiene existencia en la sucursal local pero sí en otra
    When Almacén genera una solicitud de transferencia
    Then el sistema descuenta la unidad del inventario disponible de la sucursal de origen y la marca En tránsito
```

### US19 — Almacén

```gherkin
Escenario: Recepción de transferencia
    Given que una unidad está En tránsito hacia la sucursal
    When Almacén confirma la recepción física
    Then el sistema actualiza la sucursal de la unidad (por número de serie) y la agrega al inventario disponible
```

### US20 — Sistema

```gherkin
Escenario: Vencimiento de apartado sin pago completo
    Given que un apartado alcanzó la fecha límite sin liquidarse
    When se ejecuta el proceso de vencimiento
    Then el sistema marca el apartado como Vencido, libera la unidad y la regresa al inventario disponible
    And se registra el evento ApartadoVencido para avisar al Vendedor o al Cliente (canal por validar)
```

```gherkin
Escenario: Apartado vigente
    Given que un apartado aún no alcanza su fecha límite
    When se ejecuta el proceso de vencimiento
    Then el sistema no modifica el apartado ni la unidad
```
