import { h, icono, campo, entrada, selector, chip, vacio, dinero, fecha, normalizar, modal, toast, errorAviso, ocupado, tonoEstado, etEstado } from "../ui.js";
import { PRODUCTOS, producto } from "../data.js";
import { api, invalidarExistencias } from "../api.js";
import { registrar, actualizar, operaciones, buscar } from "../store.js";

const CAUSALES = ["Falla", "Producto equivocado", "El cliente se arrepintió", "Daño en el traslado", "Otra"];
const CONDICIONES = [["EmpaqueCerrado", "Empaque cerrado", "Sin abrir"], ["EmpaqueAbierto", "Empaque abierto", "Requiere autorización"], ["Defectuoso", "Defectuoso", "Falla de fábrica"]];
const COMPENSACIONES = [["Reembolso", "Reembolso", "Al método original"], ["NotaDeCredito", "Nota de crédito", "Saldo a favor"]];

const radios = (nombre, op, val) => h("div", { class: "opciones" }, op.map(([v, t, s]) => h("label", { class: "opcion" }, h("input", { type: "radio", name: nombre, value: v, checked: v === val }), h("div", {}, h("b", {}, t), h("span", {}, s)))));
const leer = (el, n) => el.querySelector(`input[name=${n}]:checked`)?.value;

export function vistaDevoluciones({ sesion }) {
  const esSup = sesion.rol === "Supervisor";
  const ventas = operaciones("venta");
  const resultado = h("div"), pendientes = h("div"), consulta = h("div");

  const folio = entrada({ class: "entrada mono", placeholder: "VTA-000001", list: "ventas-lista", "aria-label": "Folio de venta" });
  const dl = h("datalist", { id: "ventas-lista" }, ventas.map((v) => h("option", { value: v.folio }, `${dinero(v.total)} · ${v.lineas.length} art.`)));
  const prod = selector(PRODUCTOS.filter((p) => p.habilitado).map((p) => [p.id, p.desc]), PRODUCTOS[0].id);
  const serie = entrada({ class: "entrada mono", placeholder: "SN-TEC-P001-001" });
  const ayudaVenta = h("small", { class: "ayuda" });
  const causal = selector(CAUSALES, "Falla"), detalle = entrada({ placeholder: "Describe brevemente el problema (opcional)" });
  const cond = radios("cond", CONDICIONES, "EmpaqueCerrado"), comp = radios("comp", COMPENSACIONES, "Reembolso");

  function ajustar() { // si la venta es de este equipo, limita productos y completa la serie
    const v = buscar("venta", folio.value);
    if (v) {
      const ids = [...new Set(v.lineas.map((l) => l.productoId))];
      prod.replaceChildren(...ids.map((id) => h("option", { value: id }, producto(id)?.desc || id)));
      ayudaVenta.textContent = `Venta del ${fecha(v.fecha)} · total ${dinero(v.total)}.`;
    } else {
      prod.replaceChildren(...PRODUCTOS.filter((p) => p.habilitado).map((p) => h("option", { value: p.id }, p.desc)));
      ayudaVenta.textContent = folio.value.trim() ? "Esta venta no se registró en este equipo: elige el producto y captura la serie." : "";
    }
    serieAuto();
  }
  function serieAuto() {
    const p = producto(prod.value), v = buscar("venta", folio.value);
    serie.disabled = !p?.serie; serie.placeholder = p?.serie ? `SN-${p.id}-001` : "Este producto no lleva serie";
    const l = v?.lineas.find((x) => x.productoId === prod.value && x.serie); serie.value = p?.serie && l ? l.serie : (p?.serie ? serie.value : "");
  }
  folio.addEventListener("input", ajustar); prod.addEventListener("change", serieAuto); ajustar();

  async function solicitar() {
    const f = normalizar(folio.value); if (!f) return toast("Captura el folio de la venta.", "error");
    const p = producto(prod.value);
    if (p.serie && !serie.value.trim()) return toast("Captura el número de serie de la unidad.", "error");
    try {
      const r = await api("POST", "/devoluciones", { operador: true, cuerpo: { folioDeVenta: f, productoId: p.id, serieCapturada: p.serie ? normalizar(serie.value) : null, motivo: { causal: causal.value, detalle: detalle.value.trim() || null }, condicionDelProducto: leer(cond, "cond"), formaDeCompensacion: leer(comp, "comp") } });
      registrar({ tipo: "devolucion", folio: r.folio, folioVenta: f, productoId: p.id, serie: p.serie ? normalizar(serie.value) : null, estado: r.estado, motivo: r.motivo, compensacion: r.formaDeCompensacion, usuario: sesion.usuario });
      invalidarExistencias(); mostrar(resultado, r); pintarPendientes();
    } catch (e) { errorAviso(e); }
  }
  function mostrar(donde, r) {
    const ok = r.estado === "Aceptada", rev = r.estado === "EnRevision";
    donde.replaceChildren(h("div", { class: `resultado aviso-caja ${ok ? "verde" : rev ? "ambar" : "rojo"}`, style: "flex-direction:column" },
      h("span", {}, "Devolución"), h("div", { class: "folio" }, r.folio), h("div", {}, chip(etEstado(r.estado), tonoEstado(r.estado)), r.formaDeCompensacion ? ` Compensación: ${r.formaDeCompensacion === "NotaDeCredito" ? "nota de crédito" : "reembolso"}.` : ""),
      h("div", {}, ok ? "La unidad regresa al inventario." : rev ? "No cumple el criterio estándar: un Supervisor debe autorizarla o rechazarla." : "La devolución fue rechazada."), r.motivo ? h("div", { class: "nota" }, `Motivo: ${r.motivo}`) : null));
  }

  function resolver(d, autoriza) {
    const motivo = h("textarea", { class: "entrada", placeholder: "Justifica tu decisión" });
    const mod = modal(`${autoriza ? "Autorizar" : "Rechazar"} ${d.folio}`, h("div", { class: "tarjeta-cuerpo", style: "padding:0" }, campo("Motivo de la decisión", motivo)),
      { acciones: [h("button", { class: "btn", onclick: () => mod.cerrar() }, "Volver"), h("button", { class: `btn ${autoriza ? "primario" : "peligro"}`, onclick: (e) => ocupado(e.currentTarget, async () => {
        if (!motivo.value.trim()) return toast("Escribe el motivo de la decisión.", "error");
        try { const r = await api("POST", `/devoluciones/${encodeURIComponent(d.folio)}/resolucion`, { operador: true, cuerpo: { autoriza, motivo: motivo.value.trim() } });
          actualizar("devolucion", d.folio, { estado: r.estado, motivo: r.motivo, compensacion: r.formaDeCompensacion }); invalidarExistencias(); mod.cerrar(); mostrar(resultado, r); pintarPendientes(); }
        catch (er) { errorAviso(er); }
      }) }, autoriza ? "Autorizar devolución" : "Rechazar devolución")] });
  }
  async function refrescar(d) { try { const r = await api("GET", `/devoluciones/${encodeURIComponent(d.folio)}`); actualizar("devolucion", d.folio, { estado: r.estado, motivo: r.motivo, compensacion: r.formaDeCompensacion }); pintarPendientes(); toast(`${d.folio}: ${etEstado(r.estado)}`, "info"); } catch (e) { errorAviso(e); } }

  function pintarPendientes() {
    const l = operaciones("devolucion");
    const rev = l.filter((d) => d.estado === "EnRevision");
    const acciones = (d) => {
      const dis = !esSup, tit = esSup ? "" : "Solo un Supervisor";
      const botones = d.estado === "EnRevision" ? [
        h("button", { class: "btn chico primario", disabled: dis, title: tit, onclick: () => resolver(d, true) }, "Autorizar"),
        h("button", { class: "btn chico", disabled: dis, title: tit, onclick: () => resolver(d, false) }, "Rechazar")] : [];
      botones.push(h("button", { class: "icon-btn", title: "Actualizar estado", "aria-label": "Actualizar estado", onclick: () => refrescar(d) }, icono("refrescar", 16)));
      return h("div", { class: "lista-acc" }, botones);
    };
    const fila = (d) => h("tr", {},
      h("td", { class: "mono" }, d.folio, h("div", { class: "nota" }, `venta ${d.folioVenta}`)),
      h("td", {}, producto(d.productoId)?.desc),
      h("td", {}, chip(etEstado(d.estado), tonoEstado(d.estado))),
      h("td", {}, acciones(d)));
    const tabla = h("div", { class: "tabla-caja" }, h("table", {},
      h("thead", {}, h("tr", {}, ["Folio", "Producto", "Estado", ""].map((t) => h("th", {}, t)))),
      h("tbody", {}, l.map(fila))));
    pendientes.replaceChildren(
      !esSup && rev.length ? h("div", { class: "aviso-caja info" }, icono("info"), `${rev.length} devolución(es) esperan la decisión de un Supervisor.`) : null,
      l.length ? tabla : vacio("Sin devoluciones", "Las que solicites en este equipo aparecerán aquí."));
  }
  pintarPendientes();

  const folioC = entrada({ class: "entrada mono", placeholder: "DEV-000001" });
  const consultar = async () => { const f = normalizar(folioC.value); if (!f) return toast("Escribe un folio.", "error"); try { mostrar(consulta, await api("GET", `/devoluciones/${encodeURIComponent(f)}`)); } catch (e) { consulta.replaceChildren(); errorAviso(e); } };

  return h("div", { class: "dos" },
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Nueva devolución"), h("p", {}, "Se acepta si hay comprobante, está en plazo y el empaque está cerrado o es defectuoso"))),
      h("div", { class: "tarjeta-cuerpo" }, campo("Folio de la venta (comprobante)", folio), dl, ayudaVenta, h("div", { class: "fila c2" }, campo("Producto", prod), campo("Número de serie", serie)),
        h("div", { class: "fila c2" }, campo("Causal", causal), campo("Detalle", detalle)), h("div", { class: "campo" }, h("span", { class: "campo-et" }, "Condición del producto"), cond), h("div", { class: "campo" }, h("span", { class: "campo-et" }, "Forma de compensación"), comp),
        h("button", { class: "btn primario grande", onclick: (e) => ocupado(e.currentTarget, solicitar) }, "Solicitar devolución"), resultado)),
    h("div", { style: "display:flex;flex-direction:column;gap:22px;min-width:0" },
      h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Seguimiento"), h("p", {}, esSup ? "Resuelve las que están en revisión" : "Devoluciones registradas en este equipo"))), h("div", { class: "tarjeta-cuerpo" }, pendientes)),
      h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("h2", {}, "Consultar por folio")), h("div", { class: "tarjeta-cuerpo" }, h("div", { class: "fila", style: "grid-template-columns:1fr auto" }, folioC, h("button", { class: "btn", onclick: (e) => ocupado(e.currentTarget, consultar) }, "Consultar")), consulta))));
}
