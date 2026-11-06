**CASO 05**

**TecnoStock**

_Unidad 2 · Análisis de Requerimientos de Negocio_

| **Giro**            | Retail de electrónica                       |
| ------------------- | ------------------------------------------- |
| **Proceso central** | Venta, inventario, apartados y devoluciones |
| **Rol del alumno**  | Equipo de análisis de requerimientos        |

# 1\. Contexto y modelo de negocio

TecnoStock es una cadena de cinco tiendas que vende computadoras, celulares, accesorios y componentes. Vende en sucursal y permite apartados de ciertos productos.

# 2\. Operación actual (AS-IS)

El vendedor consulta disponibilidad por sucursal. El cliente selecciona productos y puede pagar con los métodos autorizados. Al finalizar se genera un comprobante y disminuye inventario. Los productos con número de serie requieren identificar la unidad exacta entregada.

Algunos artículos pueden apartarse mediante anticipo y quedan reservados durante un periodo. Las devoluciones requieren revisar comprobante, producto y condiciones aplicables.

# 3\. Actores que participan

- Cliente
- Vendedor
- Cajero
- Supervisor
- Almacén

# 4\. Situaciones especiales y excepciones conocidas

- Apartado que vence sin completar el pago.
- Producto reservado que no debe venderse a otro cliente.
- Devoluciones con restricciones.
- Supervisor requerido para ciertos casos.
- Transferencias de mercancía entre sucursales.

# 5\. Problemas actuales observados

- Existencia de sistema no coincide con inventario físico.
- Venta accidental de productos apartados.
- Dificultad para rastrear números de serie.
- Criterios de devolución aplicados de forma inconsistente.

# 6\. Información que el equipo deberá descubrir

La descripción anterior NO constituye un conjunto completo de requerimientos. Durante el levantamiento, el equipo deberá identificar y validar información faltante, reglas implícitas, límites, permisos, tiempos, excepciones, prioridades, datos, responsables y criterios medibles. No deben inventarse valores o políticas: deberán formularse preguntas al stakeholder.

- ¿Qué eventos inician y terminan el proceso?
- ¿Qué roles pueden realizar, autorizar, cancelar o modificar cada operación?
- ¿Qué límites de tiempo, cantidad, monto, capacidad o frecuencia existen?
- ¿Qué ocurre cuando el flujo normal no puede continuar?
- ¿Qué datos se capturan, consultan, generan o modifican?
- ¿Qué reglas se aplican de forma manual o por experiencia?
- ¿Qué reportes o indicadores necesita el negocio?
- ¿Qué necesidades son prioritarias y cómo se medirá el éxito?

# 7\. Trabajo solicitado

- Levantamiento: preparar una guía de 15–20 preguntas y realizar una entrevista simulada con un compañero como stakeholder.
- BRD: documentar contexto, problema/oportunidad, stakeholders, alcance, necesidades, restricciones, supuestos y criterios de éxito.
- User Stories: elaborar al menos 15 historias con formato Como \[rol\], quiero \[capacidad\], para \[beneficio\].
- Criterios de aceptación: definir criterios verificables y utilizar Gherkin (Given/When/Then) cuando corresponda.
- BPMN: modelar el proceso principal incluyendo participantes/swimlanes, actividades, gateways, eventos, excepciones y data objects pertinentes.
- Modelo de dominio: identificar conceptos relevantes, relaciones y vocabulario del negocio.
- Glosario: definir al menos 20 términos utilizando lenguaje ubicuo.
- Validación y trazabilidad: relacionar necesidades del BRD con User Stories, criterios de aceptación y procesos modelados.

# 8\. Condición del ejercicio

**Importante:** el caso contiene suficiente información para iniciar el análisis, pero deliberadamente deja decisiones sin definir. El objetivo no es adivinar la operación faltante, sino descubrirla mediante técnicas de levantamiento y documentarla de forma verificable.