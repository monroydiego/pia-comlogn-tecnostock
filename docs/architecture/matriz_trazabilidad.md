# Matriz de trazabilidad (versión corregida, PIA)

La columna Pruebas se completa durante la implementación con los nombres de las pruebas automáticas.

| Necesidad                            | Reglas                             | Historias                    | Caso de uso                  | Componentes                                                                         | ADR                         | Pruebas         |
| ------------------------------------ | ---------------------------------- | ---------------------------- | ---------------------------- | ----------------------------------------------------------------------------------- | --------------------------- | --------------- |
| BR-001 Disponibilidad multi-sucursal | —                                  | US04, US05                   | Consultas de inventario      | ConsultasDeInventario, ILectorDeInventario                                          | ADR-004                     | (por completar) |
| BR-002 Pago autorizado               | RN-VTA-002                         | US11, US12                   | CU-01, CU-02, CU-03          | PoliticaComercial, Venta, EspecificacionDeMetodoDePagoAutorizado                    | ADR-005, ADR-007            | (por completar) |
| BR-003 Trazabilidad de serie         | RN-VTA-001, RN-INV-002, RN-INV-004 | US09, US17, US19             | CU-02, CU-03, CU-04, CU-07   | UnidadInventario, LineaDeVenta, NumeroDeSerie                                       | ADR-002, ADR-009            | (por completar) |
| BR-004 Apartados                     | RN-APT-001, 002, 003, 004          | US01, 03, 06, 07, 11, 14, 20 | CU-01, CU-03, CU-06, CU-08   | Apartado, ConversionDeApartado, EspecificacionDeProductoApartable, MaquinaDeEstados | ADR-002, 003, 006, 007, 008 | (por completar) |
| BR-005 Devoluciones                  | RN-DEV-001, 002, 003, 004          | US02, 08, 10, 16             | CU-04                        | Devolucion, CriterioDeDevolucion, ReglaDeCondicionDelProducto                       | ADR-001, 005, 006, 007      | (por completar) |
| BR-006 Excepciones                   | RN-SUP-001, RN-SUP-002             | US13, US14                   | CU-05, CU-06                 | DecisionDeSupervisor                                                                | ADR-006, ADR-008            | (por completar) |
| BR-007 Transferencias                | RN-INV-003                         | US18, US19                   | Documentado, sin implementar | EstadoDeUnidad.EnTransito                                                           | ADR-002                     | —               |
| BR-008 Conciliación                  | RN-INV-001                         | US15                         | Documentado, sin implementar | Extensión pendiente                                                                 | —                           | —               |

Casos de uso: CU-01 ApartarProducto, CU-02 RegistrarVenta, CU-03 ConvertirApartadoEnVenta, CU-04 SolicitarDevolucion, CU-05 AutorizarDevolucionExcepcional, CU-06 CancelarApartado, CU-07 RegistrarUnidad, CU-08 VencerApartadosVencidos.
