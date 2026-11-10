import { sesion } from "./store.js";
import { PRODUCTOS } from "./data.js";

const BASE = window.TECNOSTOCK_API ?? ""; // vacío = mismo servidor que sirve esta página

const TITULOS = {
  NoEncontrado: "No se encontró", UnidadApartada: "Unidad apartada", ConflictoDeConcurrencia: "Operación simultánea",
  TransicionDeEstadoInvalida: "Acción no permitida en este estado", SinExistenciaDisponible: "Sin existencia",
  ProductoNoApartable: "Producto no apartable", AnticipoInsuficiente: "Anticipo insuficiente",
  MetodoDePagoNoAutorizado: "Método de pago no autorizado", SerieRequerida: "Falta el número de serie",
  SerieNoCoincide: "La serie no coincide", SerieDuplicada: "Serie duplicada", SaldoInsuficiente: "Pago insuficiente",
  ApartadoVencido: "Apartado vencido", ComprobanteNoValido: "Comprobante no válido",
  LineaNoPerteneceALaVenta: "El producto no pertenece a la venta", SoloSupervisorAutoriza: "Requiere un Supervisor",
  UnidadNoDisponible: "Unidad no disponible", DatosInvalidos: "Datos inválidos",
};

export class ApiError extends Error {
  constructor(status, datos) {
    super(datos?.detail || datos?.title || `Error ${status}`);
    this.status = status; this.motivo = datos?.motivo || datos?.title;
    this.titulo = TITULOS[this.motivo] || (status >= 500 ? "Error del servidor" : "No se pudo completar");
    this.alternativa = datos?.alternativaSugerida && !/ning|none/i.test(datos.alternativaSugerida) ? datos.alternativaSugerida : null;
  }
}

export async function api(metodo, ruta, { cuerpo, operador = false } = {}) {
  const h = { Accept: "application/json" };
  if (cuerpo !== undefined) h["Content-Type"] = "application/json";
  if (operador) { const s = sesion(); h["X-Usuario"] = s.usuario; h["X-Rol"] = s.rol; }
  let r;
  try { r = await fetch(BASE + ruta, { method: metodo, headers: h, body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined }); }
  catch { throw new ApiError(0, { title: "SinConexion", detail: "No hay conexión con el servidor. Revisa tu red e inténtalo de nuevo." }); }
  let datos = null; try { datos = await r.json(); } catch { /* respuesta sin cuerpo */ }
  if (!r.ok) throw new ApiError(r.status, datos);
  return datos;
}

// Disponibilidad por sucursal (una consulta por producto, en paralelo) con caché corta.
const cache = {};
export async function existencias(sucursal, forzar = false) {
  if (!forzar && cache[sucursal] && Date.now() - cache[sucursal].t < 60000) return cache[sucursal].d;
  const d = {};
  await Promise.all(PRODUCTOS.map(async (p) => {
    try { d[p.id] = (await api("GET", `/disponibilidad/${p.id}?sucursalId=${sucursal}`)).cantidadDisponible; }
    catch { d[p.id] = null; }
  }));
  cache[sucursal] = { t: Date.now(), d };
  return d;
}
export const invalidarExistencias = () => { for (const k in cache) delete cache[k]; };
