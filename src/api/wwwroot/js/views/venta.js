import { h, icono, campo, entrada, selector, chip, vacio, dinero, redondear, normalizar, toast, errorAviso, ocupado } from "../ui.js";
import { PRODUCTOS, CATEGORIAS, producto } from "../data.js";
import { api, existencias, invalidarExistencias } from "../api.js";
import { registrar } from "../store.js";
import { mostrarTicket } from "../recibo.js";

const METODOS = [["Efectivo", "Efectivo"], ["Tarjeta", "Tarjeta"], ["Transferencia", "Transferencia"]];

export function vistaVenta({ sesion }) {
  const suc = sesion.sucursal;
  let stock = {}, cat = "Todos", texto = "", cart = [], pagos = [{ metodo: "Efectivo", monto: "" }];
  const catalogo = h("div", { class: "catalogo" }), carritoEl = h("div", { class: "tarjeta-cuerpo" });
  const total = () => redondear(cart.reduce((a, l) => a + producto(l.id).precio, 0));
  const enCarrito = (id) => cart.filter((l) => l.id === id).length;

  function dibujarCatalogo() {
    const q = texto.trim().toLowerCase();
    const lista = PRODUCTOS.filter((p) => (cat === "Todos" || p.categoria === cat) && (!q || p.desc.toLowerCase().includes(q) || p.id.toLowerCase().includes(q)));
    catalogo.replaceChildren(...(lista.length ? lista.map((p) => {
      const n = stock[p.id], libre = n == null ? null : n - enCarrito(p.id), agotado = !p.habilitado || (n != null && libre <= 0);
      return h("button", { class: "prod", disabled: agotado, onclick: () => { cart.push({ id: p.id, serie: "" }); dibujarTodo(); } },
        h("div", {}, h("b", {}, p.desc), h("div", { class: "id" }, p.id)),
        h("div", { class: "pie" }, p.serie ? chip("Con serie", "azul") : chip("Sin serie"), n == null ? chip("…") : agotado ? chip(p.habilitado ? "Agotado" : "No disponible", "rojo") : chip(`${libre} disp.`, libre <= 2 ? "ambar" : "verde")),
        h("div", { class: "precio num" }, dinero(p.precio)));
    }) : [vacio("Sin resultados", "Prueba con otro nombre o categoría.")]));
  }

  function aplicados() { // lo que se manda al API: el efectivo recibido de más se devuelve como cambio
    let ap = pagos.map((p) => ({ metodo: p.metodo, monto: redondear(+p.monto || 0) })).filter((p) => p.monto > 0);
    const t = total(); const recibido = redondear(ap.reduce((a, p) => a + p.monto, 0)); let exceso = redondear(recibido - t);
    const cambio = exceso > 0 ? exceso : 0;
    for (let i = ap.length - 1; i >= 0 && exceso > 0; i--) if (ap[i].metodo === "Efectivo") { const q = Math.min(ap[i].monto, exceso); ap[i].monto = redondear(ap[i].monto - q); exceso = redondear(exceso - q); }
    return { ap: ap.filter((p) => p.monto > 0), recibido, pendiente: Math.max(0, redondear(t - recibido)), cambio, sobra: exceso > 0 };
  }

  function dibujarCarrito() {
    const t = total();
    if (!cart.length) { carritoEl.replaceChildren(vacio("El carrito está vacío", "Elige productos del catálogo para empezar la venta.")); return; }
    const r = aplicados();
    const lineas = cart.map((l, i) => { const p = producto(l.id);
      return h("div", { class: "linea" },
        h("div", {}, h("div", { class: "tit" }, p.desc), h("div", { class: "sub" }, p.serie ? "Captura el número de serie de la unidad" : "El sistema asigna la unidad automáticamente")),
        h("div", { style: "text-align:right" }, h("div", { class: "num", style: "font-weight:700" }, dinero(p.precio)), h("button", { class: "icon-btn", "aria-label": `Quitar ${p.desc}`, onclick: () => { cart.splice(i, 1); dibujarTodo(); } }, icono("papelera", 16))),
        p.serie ? entrada({ class: "entrada mono", placeholder: `SN-${p.id}-001`, value: l.serie, "aria-label": `Serie de ${p.desc}`, oninput: (e) => { l.serie = e.target.value; } }) : null);
    });
    const filasPago = pagos.map((p, i) => h("div", { class: "pago" },
      selector(METODOS, p.metodo, { "aria-label": "Método de pago", onchange: (e) => { p.metodo = e.target.value; dibujarCarrito(); } }),
      entrada({ type: "number", min: "0", step: "0.01", inputmode: "decimal", placeholder: "Monto", value: p.monto, "aria-label": "Monto", oninput: (e) => { p.monto = e.target.value; resumen(); } }),
      pagos.length > 1 ? h("button", { class: "icon-btn", "aria-label": "Quitar pago", onclick: () => { pagos.splice(i, 1); dibujarCarrito(); } }, icono("x", 16)) : h("span")));
    resumenEl = h("div", { class: "totales" });
    carritoEl.replaceChildren(h("div", {}, lineas),
      h("div", { class: "totales" }, h("div", {}, h("span", {}, `${cart.length} ${cart.length === 1 ? "artículo" : "artículos"}`), h("span", { class: "grande num" }, dinero(t)))),
      h("div", { class: "campo" }, h("span", { class: "campo-et" }, "Pago"), ...filasPago,
        h("div", { class: "lista-acc", style: "justify-content:flex-start" },
          h("button", { class: "btn chico", onclick: () => { pagos = [{ metodo: pagos[0].metodo, monto: String(t) }]; dibujarCarrito(); } }, "Cobrar exacto"),
          h("button", { class: "btn chico", onclick: () => { pagos.push({ metodo: "Tarjeta", monto: "" }); dibujarCarrito(); } }, icono("mas", 14), "Otro método"))),
      resumenEl, h("button", { class: "btn primario grande", id: "cobrar", onclick: (e) => ocupado(e.currentTarget, cobrar) }, "Cobrar y registrar venta"));
    resumen();
  }
  let resumenEl;
  function resumen() {
    if (!resumenEl) return; const r = aplicados();
    resumenEl.replaceChildren(h("div", {}, h("span", {}, "Recibido"), h("span", { class: "num" }, dinero(r.recibido))),
      r.pendiente > 0 ? h("div", { style: "color:var(--rojo)" }, h("span", {}, "Falta por cobrar"), h("b", { class: "num" }, dinero(r.pendiente))) : null,
      r.cambio > 0 ? h("div", { style: r.sobra ? "color:var(--rojo)" : "color:var(--verde)" }, h("span", {}, r.sobra ? "Excedente no devolvible (solo efectivo)" : "Cambio a entregar"), h("b", { class: "num" }, dinero(r.cambio))) : null);
  }

  async function cobrar() {
    const faltan = cart.filter((l) => producto(l.id).serie && !l.serie.trim());
    if (faltan.length) return toast("Captura el número de serie de cada unidad con serie.", "error");
    const r = aplicados();
    if (r.pendiente > 0) return toast("Los pagos no cubren el total de la venta.", "error");
    if (r.sobra) return toast("Solo el efectivo puede generar cambio. Ajusta los montos de tarjeta o transferencia.", "error");
    try {
      const v = await api("POST", "/ventas", { operador: true, cuerpo: { sucursalId: suc, lineas: cart.map((l) => ({ productoId: l.id, serie: producto(l.id).serie ? normalizar(l.serie) : null })), pagos: r.ap } });
      const op = { tipo: "venta", folio: v.folio, sucursal: suc, usuario: sesionActual().usuario, total: v.total, lineas: v.lineas, pagos: r.ap, cambio: r.cambio, origen: v.folioDeApartadoOrigen };
      registrar(op); invalidarExistencias(); mostrarTicket({ ...op, fecha: new Date().toISOString() });
      cart = []; pagos = [{ metodo: "Efectivo", monto: "" }]; stock = await existencias(suc, true); dibujarTodo();
    } catch (e) { errorAviso(e); if (e.status === 409 || e.status === 422) { stock = await existencias(suc, true); dibujarCatalogo(); } }
  }
  const sesionActual = () => sesion;
  const dibujarTodo = () => { dibujarCatalogo(); dibujarCarrito(); };

  const chips = h("div", { class: "chips" });
  const pintarChips = () => chips.replaceChildren(...["Todos", ...CATEGORIAS].map((c) => h("button", { "aria-pressed": String(c === cat), onclick: () => { cat = c; pintarChips(); dibujarCatalogo(); } }, c)));
  pintarChips();
  existencias(suc).then((d) => { stock = d; dibujarCatalogo(); }); dibujarTodo();

  return h("div", { class: "venta-grid" },
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Catálogo"), h("p", {}, `Existencias en ${suc}`)),
      h("div", { class: "buscador" }, entrada({ type: "search", placeholder: "Buscar producto o código", "aria-label": "Buscar", oninput: (e) => { texto = e.target.value; dibujarCatalogo(); } }))),
      h("div", { class: "tarjeta-cuerpo" }, chips, catalogo)),
    h("section", { class: "tarjeta carrito" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Venta actual"), h("p", {}, `${suc} · cobro con uno o varios métodos`)),
      h("button", { class: "btn chico", onclick: () => { cart = []; dibujarTodo(); } }, "Vaciar")), carritoEl));
}
