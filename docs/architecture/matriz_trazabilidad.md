# Matriz de trazabilidad (versión final, PIA)

Relaciona cada necesidad, regla e historia con su caso de uso, componentes, ADR y **pruebas automáticas reales**. Todos los nombres entre acentos graves son clases o métodos de prueba que existen en `tests/`: la prueba `TrazabilidadPruebas` falla si alguno desaparece o si un escenario Gherkin implementado se queda sin prueba.

**Resumen:** los 44 escenarios Gherkin están en la sección 3. Los 41 que pertenecen a historias implementadas tienen al menos una prueba unitaria, y 40 de ellos también una de integración contra la API real (el escenario «Operación rechazada» de US12 solo tiene prueba unitaria). Los 3 restantes (US15, US18 y US19) están documentados y no se implementan. En total hay 236 casos de prueba, de 200 métodos: 175 unitarios y 61 de integración, contando los casos de las pruebas parametrizadas. La cobertura de líneas es 98.7% (ramas 91.1%).

## 1. Necesidades de negocio

| Necesidad | Reglas | Historias | Caso de uso | Componentes | ADR | Pruebas |
| --- | --- | --- | --- | --- | --- | --- |
| BR-001 Disponibilidad multi-sucursal | — | US04, US05 | Consultas de inventario | ConsultasDeInventario, ILectorDeInventario | ADR-004 | `ServicioDeVentasEInventarioPruebas`, `InventarioApiPruebas` |
| BR-002 Pago autorizado | RN-VTA-002 | US11, US12 | CU-01, CU-02, CU-03 | PoliticaComercial, Venta, EspecificacionDeMetodoDePagoAutorizado | ADR-005, ADR-007 | `EspecificacionesPruebas`, `ServicioDeApartadosPruebas`, `VentaYConversionPruebas`, `PoliticaConfigurableApiPruebas` |
| BR-003 Trazabilidad de serie | RN-VTA-001, RN-INV-002, RN-INV-004 | US09, US17, US19 | CU-02, CU-03, CU-04, CU-07 | UnidadInventario, LineaDeVenta, NumeroDeSerie | ADR-002, ADR-009, ADR-010 | `UnidadInventarioPruebas`, `VentaYConversionPruebas`, `ServicioDeVentasEInventarioPruebas`, `ApartadosYVentasApiPruebas`, `PersistenciaPruebas` |
| BR-004 Apartados | RN-APT-001, 002, 003, 004 | US01, 03, 06, 07, 11, 14, 20 | CU-01, CU-03, CU-06, CU-08 | Apartado, ConversionDeApartado, EspecificacionDeProductoApartable, MaquinaDeEstados | ADR-002, 003, 006, 007, 008 | `ApartadoPruebas`, `UnidadInventarioPruebas`, `ServicioDeApartadosPruebas`, `ApartadosYVentasApiPruebas`, `VencimientoApiPruebas`, `PersistenciaPruebas` |
| BR-005 Devoluciones | RN-DEV-001, 002, 003, 004 | US02, 08, 10, 16 | CU-04 | Devolucion, CriterioDeDevolucion, ReglaDeCondicionDelProducto | ADR-001, 005, 006, 007 | `DevolucionPruebas`, `EspecificacionesPruebas`, `ServicioDeDevolucionesPruebas`, `DevolucionesApiPruebas`, `DevolucionesFueraDePlazoApiPruebas` |
| BR-006 Excepciones | RN-SUP-001, RN-SUP-002 | US13, US14 | CU-05, CU-06 | DecisionDeSupervisor | ADR-006, ADR-008 | `ObjetosDeValorPruebas`, `ApartadoPruebas`, `ServicioDeDevolucionesPruebas`, `DevolucionesApiPruebas` |
| BR-007 Transferencias | RN-INV-003 | US18, US19 | Documentado, sin implementar | EstadoDeUnidad.EnTransito | ADR-002 | — |
| BR-008 Conciliación | RN-INV-001 | US15 | Documentado, sin implementar | Extensión pendiente | — | — |

Casos de uso: CU-01 ApartarProducto, CU-02 RegistrarVenta, CU-03 ConvertirApartadoEnVenta, CU-04 SolicitarDevolucion, CU-05 AutorizarDevolucionExcepcional, CU-06 CancelarApartado, CU-07 RegistrarUnidad, CU-08 VencerApartadosVencidos.

## 2. Reglas de negocio

| Regla | Qué exige | Pruebas |
| --- | --- | --- |
| RN-VTA-001 | Número de serie antes de cerrar la venta | `US09_Serie_no_registrada_bloquea_el_cierre_con_SerieRequerida`<br>`US09_Venta_directa_exige_serie_y_cierra_con_ella` |
| RN-VTA-002 | Solo métodos de pago autorizados | `MetodoDePagoAutorizado_rechaza_metodos_fuera_de_la_lista`<br>`RN_VTA_002_Metodo_de_pago_no_autorizado_en_la_venta`<br>`Politica_es_configurable`<br>`US11_RN_VTA_002_Un_metodo_retirado_de_la_politica_responde_422_en_apartados_y_ventas` |
| RN-APT-001 | Anticipo mínimo | `US01_Anticipo_insuficiente_y_la_unidad_sigue_disponible`<br>`US01_Anticipo_exactamente_en_el_minimo_se_acepta`<br>`US01_Anticipo_insuficiente_responde_422_y_la_unidad_sigue_disponible` |
| RN-APT-002 | Vigencia y vencimiento | `US20_Vencimiento_libera_la_unidad_y_registra_el_evento`<br>`US07_Apartado_vencido_se_marca_Vencido_libera_la_unidad_y_rechaza_con_ApartadoVencido`<br>`US20_Vencer_libera_las_unidades_de_los_apartados_vencidos_y_no_toca_los_vigentes` |
| RN-APT-003 | Unidad apartada bloqueada | `US06_Intento_de_venta_de_unidad_apartada_se_bloquea_con_UnidadApartada`<br>`US06_Vender_una_unidad_apartada_responde_409_UnidadApartada`<br>`RN_APT_003_Dos_apartados_simultaneos_de_la_misma_unidad_solo_dejan_uno_vigente` |
| RN-APT-004 | Solo productos apartables | `ProductoApartable_cumple_con_Laptop_y_no_con_Teclado`<br>`US01_Producto_no_apartable`<br>`US01_Producto_no_apartable_responde_422` |
| RN-INV-001 | Conciliación periódica (documentada, no implementada) | — |
| RN-INV-002 | Serie única | `US17_CU07_Serie_duplicada`<br>`RN_INV_002_El_indice_unico_impide_la_serie_duplicada_aunque_las_dos_pasen_la_validacion`<br>`US17_Serie_duplicada_responde_422` |
| RN-INV-003 | Unidad en tránsito (documentada; solo el estado EnTransito de la máquina de estados) | `Unidad_transiciones` |
| RN-INV-004 | Destino de la unidad devuelta | `RN_INV_004_Devolucion_con_empaque_cerrado_regresa_a_Disponible`<br>`RN_INV_004_Devolucion_con_empaque_abierto_o_defecto_queda_Defectuosa`<br>`RN_INV_004_CU04_Devolucion_defectuosa_aceptada_deja_la_unidad_Defectuosa`<br>`RN_INV_004_Devolucion_de_producto_defectuoso_deja_la_unidad_Defectuosa` |
| RN-DEV-001 | Comprobante válido | `ComprobanteValido_exige_venta_y_linea`<br>`US10_CU04_Comprobante_no_valido_rechaza_de_forma_definitiva_y_no_crea_la_devolucion`<br>`US10_Comprobante_inexistente_responde_422_y_no_crea_la_devolucion` |
| RN-DEV-002 | Plazo por categoría | `DentroDePlazo_Computadoras_15_dias`<br>`DentroDePlazo_Accesorios_30_dias_y_casos_sin_datos`<br>`US10_El_plazo_depende_de_la_categoria_dentro_se_acepta_y_fuera_queda_EnRevision` |
| RN-DEV-003 | Condición del producto | `CondicionAceptable_empaque_abierto_es_excepcion`<br>`US10_Producto_defectuoso_dentro_de_plazo_se_acepta`<br>`US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza` |
| RN-DEV-004 | Reembolso o nota de crédito | `US02_Consulta_del_estado_de_devolucion_con_motivo_y_compensacion`<br>`US10_US02_Devolucion_estandar_se_acepta_consulta_el_estado_y_la_unidad_regresa_a_Disponible` |
| RN-SUP-001 | Cancelaciones del Supervisor | `US14_CU06_Operador_sin_rol_de_Supervisor_no_cancela`<br>`US14_Cancelar_apartado_solo_lo_hace_el_Supervisor_y_libera_la_unidad` |
| RN-SUP-002 | Devoluciones fuera de política solo con el Supervisor | `US13_CU05_Operador_sin_rol_de_Supervisor`<br>`US13_Resolver_una_devolucion_ya_resuelta_lanza_TransicionDeEstadoInvalida`<br>`Decision_solo_la_registra_un_Supervisor` |

## 3. Escenarios Gherkin

Cada fila es un escenario de `docs/analisis/historias_de_usuario.md`. Las pruebas unitarias ejecutan el dominio y los casos de uso con repositorios en memoria; las de integración llaman a la API real contra SQL Server.

| Historia | Escenario | Pruebas unitarias | Pruebas de integración |
| --- | --- | --- | --- |
| US01 | Creación de un apartado | `US01_Creacion_de_un_apartado`<br>`US01_CU01_Creacion_de_un_apartado_reserva_la_unidad` | `US01_US03_Apartar_responde_201_con_folio_fecha_limite_y_saldo_y_bloquea_la_unidad` |
| US01 | Anticipo insuficiente | `US01_Anticipo_insuficiente_y_la_unidad_sigue_disponible`<br>`US01_CU01_Anticipo_insuficiente_rechaza_y_la_unidad_sigue_disponible` | `US01_Anticipo_insuficiente_responde_422_y_la_unidad_sigue_disponible` |
| US01 | Producto no apartable | `US01_Producto_no_apartable`<br>`US01_CU01_Producto_no_apartable` | `US01_Producto_no_apartable_responde_422` |
| US01 | Sin existencia | `US01_CU01_Sin_existencia_sugiere_consultar_otras_sucursales` | `US01_Sin_existencia_responde_422_y_sugiere_consultar_otras_sucursales` |
| US02 | Consulta de estado de devolución | `US02_Consulta_del_estado_de_devolucion_con_motivo_y_compensacion` | `US10_US02_Devolucion_estandar_se_acepta_consulta_el_estado_y_la_unidad_regresa_a_Disponible` |
| US02 | Folio inexistente | `US02_Folio_inexistente_responde_NoEncontrado` | `US02_Folio_de_devolucion_inexistente_responde_404` |
| US03 | Confirmación de apartado creado | `US03_Confirmacion_con_folio_fecha_limite_y_saldo_pendiente`<br>`US03_CU01_Confirmacion_muestra_folio_fecha_limite_y_saldo_pendiente` | `US01_US03_Apartar_responde_201_con_folio_fecha_limite_y_saldo_y_bloquea_la_unidad` |
| US04 | Producto con existencia | `US04_Producto_con_existencia_muestra_la_cantidad_de_la_sucursal` | `Datos_semilla_catalogo_de_20_productos_repartido_en_las_5_sucursales` |
| US04 | Producto inexistente | `US04_Producto_inexistente_responde_NoEncontrado` | `US04_Producto_inexistente_responde_404` |
| US05 | Sin existencia local y con existencia en otra sucursal | `US05_Sin_existencia_local_muestra_las_otras_sucursales_con_unidades` | `Datos_semilla_catalogo_de_20_productos_repartido_en_las_5_sucursales` |
| US05 | Sin existencia en ninguna sucursal | `US05_Sin_existencia_en_ninguna_sucursal_devuelve_lista_vacia` | `US05_Producto_deshabilitado_sin_existencia_devuelve_lista_vacia` |
| US06 | Intento de venta de unidad apartada | `US06_Intento_de_venta_de_unidad_apartada_se_bloquea_con_UnidadApartada`<br>`US06_CU02_Venta_de_una_unidad_apartada_se_bloquea_con_UnidadApartada` | `US06_Vender_una_unidad_apartada_responde_409_UnidadApartada` |
| US07 | Conversión de apartado a venta | `US07_Conversion_de_apartado_a_venta_aplica_el_anticipo_y_la_unidad_pasa_a_Vendida`<br>`US07_CU03_Conversion_de_apartado_a_venta_la_unidad_pasa_a_Vendida` | `US07_US09_US12_Convertir_apartado_en_venta_aplica_el_anticipo_marca_la_unidad_vendida_y_emite_comprobante_con_serie` |
| US07 | Apartado vencido | `US07_Apartado_vencido_se_marca_Vencido_libera_la_unidad_y_rechaza_con_ApartadoVencido`<br>`US07_CU03_Apartado_vencido_se_persiste_libera_la_unidad_y_rechaza` | `US07_Convertir_un_apartado_vencido_responde_422_y_el_vencimiento_queda_guardado` |
| US07 | Saldo insuficiente | `US07_Saldo_insuficiente_rechaza_y_el_apartado_sigue_vigente_sin_cambios`<br>`US07_CU03_Saldo_insuficiente_el_apartado_sigue_vigente` | `US07_Saldo_insuficiente_responde_422_y_el_apartado_sigue_vigente` |
| US07 | Método de pago no autorizado | `US07_Metodo_de_pago_no_autorizado_en_el_saldo`<br>`US07_CU03_Metodo_de_pago_no_autorizado_en_el_saldo` | `US11_RN_VTA_002_Un_metodo_retirado_de_la_politica_responde_422_en_apartados_y_ventas` |
| US08 | Registro de motivo | `US08_CU04_El_motivo_queda_asociado_al_folio_de_la_devolucion` | `US10_US02_Devolucion_estandar_se_acepta_consulta_el_estado_y_la_unidad_regresa_a_Disponible` |
| US09 | Venta de producto serializable | `US09_Venta_de_producto_serializable_cierra_y_conserva_la_serie`<br>`US09_CU02_Venta_de_producto_serializable_cierra_y_el_comprobante_incluye_la_serie` | `US09_Venta_directa_exige_serie_y_cierra_con_ella` |
| US09 | Serie no registrada | `US09_Serie_no_registrada_bloquea_el_cierre_con_SerieRequerida`<br>`US09_CU02_Serie_no_registrada_bloquea_el_cierre` | `US09_Venta_directa_exige_serie_y_cierra_con_ella` |
| US09 | Serie que no corresponde | `US09_Serie_que_no_corresponde_a_la_unidad_se_rechaza_con_SerieNoCoincide`<br>`US09_CU03_Serie_que_no_corresponde_o_faltante` | `US09_Convertir_con_serie_equivocada_o_faltante_responde_422` |
| US10 | Devolución estándar aceptada | `US10_Devolucion_estandar_se_acepta_sin_intervencion_del_Supervisor`<br>`US10_CU04_Devolucion_estandar_se_acepta_y_la_unidad_regresa_a_Disponible` | `US10_US02_Devolucion_estandar_se_acepta_consulta_el_estado_y_la_unidad_regresa_a_Disponible` |
| US10 | Devolución fuera de plazo | `US10_Devolucion_fuera_de_plazo_queda_EnRevision_y_se_deriva`<br>`US10_CU04_Fuera_de_plazo_queda_EnRevision_y_se_avisa_al_Supervisor` | `US10_El_plazo_depende_de_la_categoria_dentro_se_acepta_y_fuera_queda_EnRevision` |
| US10 | Empaque abierto | `US10_Empaque_abierto_queda_EnRevision_y_se_deriva`<br>`US10_CU04_Empaque_abierto_queda_EnRevision` | `US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza` |
| US10 | Comprobante no válido | `US10_Comprobante_no_valido_el_dictamen_incumple`<br>`US10_CU04_Comprobante_no_valido_rechaza_de_forma_definitiva_y_no_crea_la_devolucion` | `US10_Comprobante_inexistente_responde_422_y_no_crea_la_devolucion` |
| US11 | Cobro de anticipo | `US11_CU01_El_apartado_se_crea_solo_despues_de_aceptar_el_pago` | `US01_US03_Apartar_responde_201_con_folio_fecha_limite_y_saldo_y_bloquea_la_unidad` |
| US11 | Método de pago no autorizado | `US11_Metodo_de_pago_no_autorizado_en_el_anticipo_no_crea_el_apartado`<br>`US11_CU01_Metodo_de_pago_no_autorizado_no_crea_el_apartado` | `US11_RN_VTA_002_Un_metodo_retirado_de_la_politica_responde_422_en_apartados_y_ventas` |
| US12 | Emisión de comprobante | `US12_Apartado_creado_emite_comprobante_con_folio_usuario_y_fecha`<br>`US09_CU02_Venta_de_producto_serializable_cierra_y_el_comprobante_incluye_la_serie`<br>`US10_CU04_Devolucion_estandar_se_acepta_y_la_unidad_regresa_a_Disponible` | `US07_US09_US12_Convertir_apartado_en_venta_aplica_el_anticipo_marca_la_unidad_vendida_y_emite_comprobante_con_serie` |
| US12 | Operación rechazada | `US12_Operacion_rechazada_no_emite_comprobante` | — |
| US13 | Autorización de excepción | `US13_Autorizacion_de_excepcion_acepta_y_registra_al_Supervisor`<br>`US13_CU05_Autorizacion_acepta_registra_al_Supervisor_y_deja_la_unidad_Defectuosa` | `US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza` |
| US13 | Denegación de excepción | `US13_Denegacion_de_excepcion_marca_Rechazada`<br>`US13_CU05_Denegacion_marca_Rechazada_y_no_toca_la_unidad` | `US13_Denegar_la_excepcion_marca_Rechazada_y_no_toca_la_unidad` |
| US13 | Operador sin rol de Supervisor | `US13_CU05_Operador_sin_rol_de_Supervisor`<br>`Decision_solo_la_registra_un_Supervisor` | `US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza` |
| US13 | Devolución que no está en revisión | `US13_Resolver_una_devolucion_ya_resuelta_lanza_TransicionDeEstadoInvalida`<br>`US13_CU05_Devolucion_que_ya_fue_resuelta_o_inexistente` | `US10_US13_Empaque_abierto_queda_EnRevision_y_el_Supervisor_la_autoriza` |
| US14 | Cancelación manual de apartado | `US14_Cancelacion_manual_por_Supervisor_libera_la_unidad`<br>`US14_CU06_Cancelacion_manual_por_Supervisor_libera_la_unidad_y_registra_el_motivo` | `US14_Cancelar_apartado_solo_lo_hace_el_Supervisor_y_libera_la_unidad` |
| US14 | Cancelación por un operador sin rol | `US14_Cancelacion_por_operador_sin_rol_de_Supervisor`<br>`US14_CU06_Operador_sin_rol_de_Supervisor_no_cancela` | `US14_Cancelar_apartado_solo_lo_hace_el_Supervisor_y_libera_la_unidad` |
| US14 | Apartado que no está vigente | `US14_Cancelar_un_apartado_que_no_esta_vigente`<br>`US14_CU06_Apartado_que_no_esta_vigente` | `US14_Cancelar_apartado_solo_lo_hace_el_Supervisor_y_libera_la_unidad` |
| US15 | Generación de reporte de descuadre | Documentado, sin implementar | Documentado, sin implementar |
| US16 | Consulta del criterio vigente | `US16_Criterio_vigente_expone_las_tres_reglas_y_los_plazos`<br>`US16_Consulta_del_criterio_vigente_muestra_condiciones_y_plazos_por_categoria` | `US16_Criterio_vigente_incluye_condiciones_y_plazo_por_categoria` |
| US17 | Alta de unidad con serie | `US17_Alta_de_unidad_con_serie_queda_Disponible`<br>`US17_CU07_Alta_de_unidad_con_serie_queda_Disponible` | `US17_Alta_de_unidad_con_serie_responde_201_y_suma_a_la_disponibilidad` |
| US17 | Serie duplicada | `US17_CU07_Serie_duplicada` | `US17_Serie_duplicada_responde_422`<br>`RN_INV_002_El_indice_unico_impide_la_serie_duplicada_aunque_las_dos_pasen_la_validacion` |
| US17 | Serie faltante | `US17_CU07_Serie_faltante` | `US17_Serie_faltante_responde_422_pero_un_producto_sin_serie_no_la_exige` |
| US18 | Solicitud de transferencia | Documentado, sin implementar | Documentado, sin implementar |
| US19 | Recepción de transferencia | Documentado, sin implementar | Documentado, sin implementar |
| US20 | Vencimiento de apartado sin pago completo | `US20_Vencimiento_libera_la_unidad_y_registra_el_evento`<br>`US20_CU08_Vence_los_apartados_cuya_fecha_limite_paso_y_libera_la_unidad` | `US20_Vencer_libera_las_unidades_de_los_apartados_vencidos_y_no_toca_los_vigentes` |
| US20 | Apartado vigente | `US20_Apartado_vigente_no_se_puede_vencer_antes_de_su_fecha`<br>`US20_CU08_Apartado_vigente_no_se_modifica` | `US20_Vencer_libera_las_unidades_de_los_apartados_vencidos_y_no_toca_los_vigentes` |

## 4. Requisitos obligatorios del PIA

| Requisito | Qué se verifica | Pruebas |
| --- | --- | --- |
| Specification (ADR-007) | Y, O, No y las 7 especificaciones | `Y_exige_ambas_y_reune_las_razones`<br>`O_basta_con_una`<br>`No_invierte_el_resultado`<br>`Composiciones_se_pueden_anidar`<br>`ProductoApartable_cumple_con_Laptop_y_no_con_Teclado`<br>`UnidadDisponibleParaVenta_solo_cumple_en_estado_Disponible`<br>`ApartadoVigente_depende_del_estado_y_de_la_fecha`<br>`MetodoDePagoAutorizado_rechaza_metodos_fuera_de_la_lista`<br>`ComprobanteValido_exige_venta_y_linea`<br>`DentroDePlazo_Computadoras_15_dias`<br>`CondicionAceptable_empaque_abierto_es_excepcion` |
| Máquina de estados (ADR-008) | Transiciones válidas e inválidas de Apartado, Unidad y Devolución | `Apartado_transiciones`<br>`Unidad_transiciones`<br>`Devolucion_transiciones`<br>`Transicionar_devuelve_el_destino_si_es_valida`<br>`Transicionar_invalida_lanza_TransicionDeEstadoInvalida` |
| API REST y OpenAPI | Endpoints publicados, códigos HTTP y datos inválidos | `OpenAPI_expone_todos_los_endpoints_del_diagrama_03`<br>`Datos_invalidos_responden_400`<br>`Producto_inexistente_responde_404`<br>`US06_Vender_una_unidad_apartada_responde_409_UnidadApartada`<br>`US01_Anticipo_insuficiente_responde_422_y_la_unidad_sigue_disponible` |
| Persistencia en SQL Server (ADR-009, ADR-010) | Esquema normalizado, claves foráneas, siembra y reconstrucción de aggregates | `ADR_010_El_esquema_esta_normalizado_con_claves_foraneas_y_sin_columnas_con_varios_valores`<br>`ADR_010_El_dictamen_y_la_decision_del_Supervisor_se_guardan_en_filas_y_columnas`<br>`Las_referencias_inexistentes_se_rechazan_en_la_base_y_el_rollback_deja_la_unidad_disponible`<br>`Los_datos_semilla_cargan_catalogo_clientes_politica_y_son_idempotentes`<br>`La_venta_con_cobro_multimedio_se_reconstruye_desde_SQL`<br>`El_apartado_se_reconstruye_con_anticipo_vigencia_y_se_actualiza_con_version`<br>`Los_folios_son_unicos_y_crecientes_por_tipo_de_operacion` |
| Concurrencia y transacciones (ADR-003, ADR-006) | Versión optimista, índices únicos y rollback | `Concurrencia_optimista_la_segunda_operacion_sobre_la_misma_unidad_recibe_ConflictoDeConcurrencia`<br>`RN_INV_002_El_indice_unico_impide_la_serie_duplicada_aunque_las_dos_pasen_la_validacion`<br>`RN_APT_003_Dos_apartados_simultaneos_de_la_misma_unidad_solo_dejan_uno_vigente`<br>`Descartar_deshace_los_cambios_y_no_publica_eventos`<br>`Una_excepcion_inesperada_descarta_la_transaccion_y_se_propaga`<br>`Si_el_reingreso_falla_la_transaccion_propia_se_descarta` |
| Eventos y manejadores (ADR-003) | Comprobantes, reingreso de unidades y avisos tras confirmar | `US12_Apartado_creado_emite_comprobante_con_folio_usuario_y_fecha`<br>`US12_Operacion_rechazada_no_emite_comprobante`<br>`US10_CU04_Fuera_de_plazo_queda_EnRevision_y_se_avisa_al_Supervisor`<br>`RN_INV_004_CU04_Devolucion_defectuosa_aceptada_deja_la_unidad_Defectuosa` |

## 5. Verificación automática

`TrazabilidadPruebas` (en `tests/unitarias`) lee este archivo y comprueba que: (1) cada nombre entre acentos graves existe como clase o método de prueba, (2) cada escenario de `historias_de_usuario.md` aparece en la sección 3, y (3) solo US15, US18 y US19 quedan sin pruebas.
