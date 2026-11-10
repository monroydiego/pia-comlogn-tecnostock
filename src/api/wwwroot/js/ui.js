// Utilidades de interfaz: creación de nodos, iconos, avisos, ventanas modales y formatos.

// replaceChildren convierte null en el texto "null": se ignoran los nulos y falsos para poder usar condicionales en línea.
const reemplazar = Element.prototype.replaceChildren;
Element.prototype.replaceChildren = function (...nodos) { return reemplazar.apply(this, nodos.filter((n) => n != null && n !== false)); };
export function h(tag, attrs, ...hijos) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v == null || v === false) continue;
    if (k.startsWith("on")) el.addEventListener(k.slice(2).toLowerCase(), v);
    else if (k === "class") el.className = v;
    else if (k === "value") el.value = v;
    else if (v === true) el.setAttribute(k, "");
    else el.setAttribute(k, v);
  }
  const poner = (x) => { if (Array.isArray(x)) x.forEach(poner); else if (x != null && x !== false) el.append(x instanceof Node ? x : document.createTextNode(String(x))); };
  hijos.forEach(poner);
  return el;
}

const P = {
  inicio: "M3 11l9-8 9 8M5 10v10h5v-6h4v6h5V10", venta: "M3 4h2l2.2 11h10.6L20 7H6M9 20a1 1 0 100-2 1 1 0 000 2zm8 0a1 1 0 100-2 1 1 0 000 2z",
  apartado: "M6 3h12v18l-6-4-6 4V3z", devolucion: "M9 14L4 9l5-5M4 9h10a6 6 0 010 12h-3", inventario: "M3 7l9-4 9 4-9 4-9-4zM3 7v10l9 4 9-4V7M12 11v10",
  politica: "M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3zM9 12l2 2 4-4", historial: "M12 7v5l3 2M3 12a9 9 0 109-9 9 9 0 00-7 3.5M3 4v3h3",
  mas: "M12 5v14M5 12h14", x: "M6 6l12 12M18 6L6 18", ok: "M5 12l5 5 9-10", imprimir: "M7 9V3h10v6M7 17H4v-6h16v6h-3M7 14h10v7H7v-7z",
  buscar: "M11 18a7 7 0 100-14 7 7 0 000 14zM21 21l-5-5", refrescar: "M20 11a8 8 0 00-14-4M4 4v4h4M4 13a8 8 0 0014 4M20 20v-4h-4",
  salir: "M10 4H5v16h5M14 8l4 4-4 4M18 12H9", alerta: "M12 4l9 16H3L12 4zM12 10v4M12 17.5v.01", info: "M12 21a9 9 0 100-18 9 9 0 000 18zM12 11v5M12 8v.01",
  papelera: "M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13",
};
export function icono(n, tam = 18) {
  const s = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  s.setAttribute("viewBox", "0 0 24 24"); s.setAttribute("width", tam); s.setAttribute("height", tam);
  s.setAttribute("fill", "none"); s.setAttribute("stroke", "currentColor"); s.setAttribute("stroke-width", "1.8");
  s.setAttribute("stroke-linecap", "round"); s.setAttribute("stroke-linejoin", "round"); s.setAttribute("aria-hidden", "true");
  const p = document.createElementNS("http://www.w3.org/2000/svg", "path"); p.setAttribute("d", P[n] || P.info); s.append(p);
  return s;
}

export const MXN = new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" });
export const dinero = (n) => (n == null || Number.isNaN(+n) ? "—" : MXN.format(+n));
export const redondear = (n) => Math.round((+n + Number.EPSILON) * 100) / 100;
export const fecha = (iso, conHora = true) => {
  if (!iso) return "—"; const d = new Date(iso); if (Number.isNaN(+d)) return "—";
  return d.toLocaleString("es-MX", conHora ? { dateStyle: "medium", timeStyle: "short" } : { dateStyle: "medium" });
};
export const mismoDia = (iso) => new Date(iso).toDateString() === new Date().toDateString();
export const normalizar = (s) => (s || "").trim().toUpperCase();

export function toast(mensaje, tipo = "ok") {
  const raiz = document.getElementById("avisos");
  const t = h("div", { class: `aviso aviso-${tipo}`, role: tipo === "error" ? "alert" : "status" }, icono(tipo === "error" ? "alerta" : tipo === "ok" ? "ok" : "info"), h("span", {}, mensaje));
  raiz.append(t); setTimeout(() => t.classList.add("sale"), 4200); setTimeout(() => t.remove(), 4600);
}
export function errorAviso(e) { toast(e?.titulo && e.titulo !== e.message ? `${e.titulo}: ${e.message}` : (e?.message || "Ocurrió un error."), "error"); }

export function modal(titulo, contenido, { ancho = "560px", acciones = [], alCerrar } = {}) {
  const raiz = document.getElementById("modales");
  const cerrar = () => { fondo.remove(); document.removeEventListener("keydown", tecla); alCerrar?.(); };
  const tecla = (e) => { if (e.key === "Escape") cerrar(); };
  const caja = h("div", { class: "modal", role: "dialog", "aria-modal": "true", "aria-label": titulo, style: `max-width:${ancho}` },
    h("header", { class: "modal-cab" }, h("h2", {}, titulo), h("button", { class: "icon-btn", "aria-label": "Cerrar", onclick: cerrar }, icono("x"))),
    h("div", { class: "modal-cuerpo" }, contenido),
    acciones.length ? h("footer", { class: "modal-pie" }, acciones) : null);
  const fondo = h("div", { class: "fondo-modal", onmousedown: (e) => { if (e.target === fondo) cerrar(); } }, caja);
  raiz.append(fondo); document.addEventListener("keydown", tecla);
  setTimeout(() => caja.querySelector("input,select,textarea,button.primario")?.focus(), 30);
  return { cerrar, caja };
}

// Ejecuta una acción asíncrona deshabilitando el botón mientras dura.
export async function ocupado(btn, fn) {
  if (btn.disabled) return; const txt = btn.innerHTML; btn.disabled = true; btn.classList.add("cargando");
  try { return await fn(); } finally { btn.disabled = false; btn.classList.remove("cargando"); btn.innerHTML = txt; }
}

export const campo = (etiqueta, control, ayuda) => h("label", { class: "campo" }, h("span", { class: "campo-et" }, etiqueta), control, ayuda ? h("small", { class: "ayuda" }, ayuda) : null);
export const entrada = (a = {}) => h("input", { class: "entrada", autocomplete: "off", ...a });
export const selector = (opciones, valor, a = {}) => {
  const s = h("select", { class: "entrada", ...a }, opciones.map((o) => { const [v, t, dis] = Array.isArray(o) ? o : [o, o]; return h("option", { value: v, disabled: dis }, t); }));
  if (valor != null) s.value = valor; return s;
};
export const chip = (texto, tono = "gris") => h("span", { class: `chip chip-${tono}` }, texto);
export const tonoEstado = (e) => ({ Vigente: "azul", Convertido: "verde", Cancelado: "rojo", Vencido: "ambar", Aceptada: "verde", EnRevision: "ambar", Rechazada: "rojo", Disponible: "verde" }[e] || "gris");
export const etEstado = (e) => ({ EnRevision: "En revisión" }[e] || e);
export const vacio = (texto, sub) => h("div", { class: "vacio" }, h("strong", {}, texto), sub ? h("span", {}, sub) : null);
export const SERIE_EJEMPLO = "SN-TEC-P001-001";
