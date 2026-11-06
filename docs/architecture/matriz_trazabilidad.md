# Matriz de trazabilidad (resumen; versión completa en el Anexo B del informe de U3)

| Necesidad | Reglas | Historias | Caso de uso | Componentes | ADR |
|---|---|---|---|---|---|
| BR-001 Disponibilidad multi-sucursal | — | US04, US05 | Queries | ConsultasDeInventario | ADR-004 |
| BR-002 Pago autorizado | RN-VTA-002 | US11, US12 | CU-01, 02, 03 | PoliticaComercial, Venta | ADR-005 |
| BR-003 Trazabilidad de serie | RN-VTA-001, RN-INV-002 | US09, US17, US19 | CU-02, 03, 04 | UnidadInventario, LineaDeVenta | ADR-002 |
| BR-004 Apartados | RN-APT-001/002/003 | US01, 03, 06, 07, 11, 14, 20 | CU-01, 03 | Apartado, ConversionDeApartado | ADR-002, 003, 006 |
| BR-005 Devoluciones | RN-DEV-001/002 | US02, 08, 10, 16 | CU-04 | Devolucion, CriterioDeDevolucion | ADR-001, 005, 006 |
| BR-006 Excepciones | RN-SUP-001/002 | US13, US14 | CU-05 | DecisionDeSupervisor | ADR-006 |
| BR-007 Transferencias | RN-INV-003 | US18, US19 | Fuera del detalle | EstadoDeUnidad.EN_TRANSITO | ADR-002 |
| BR-008 Conciliación | RN-INV-001 | US15 | Fuera del detalle | Extensión pendiente | — |
