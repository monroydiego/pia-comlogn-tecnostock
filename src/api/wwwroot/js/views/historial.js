import { h, chip, vacio, dinero, fecha, tonoEstado, etEstado } from "../ui.js";
import { producto } from "../data.js";
import { operaciones, limpiarBitacora } from "../store.js";

const TIPOS = [["", "Todas"], ["venta", "Ventas"], ["apartado", "Apartados"], ["devolucion", "Devoluciones"], ["unidad", "Altas de unidad"]];
const NOMBRE = { venta: "Venta", apartado: "Apartado", devolucion: "Devolución", unidad: "Alta de unidad" };

export function vistaHistorial({ redibujar }) {
  let tipo = "";
  const cont = h("div"), tabs = h("div", { class: "tabs", role: "tablist" });
  const detalle = (o) => {
    const nombre = producto(o.productoId)?.desc;
    if (o.tipo === "venta") return `${o.lineas.length} artículo(s) · ${dinero(o.total)}`;
    if (o.tipo === "apartado") return `${nombre} · saldo ${dinero(o.saldo)}`;
    if (o.tipo === "devolucion") return `${nombre} · venta ${o.folioVenta}`;
    return `${nombre} · ${o.sucursal}`;
  };
  function pintar() {
    tabs.replaceChildren(...TIPOS.map(([v, t]) => h("button", { class: "tab", role: "tab", "aria-selected": String(v === tipo), onclick: () => { tipo = v; pintar(); } }, t)));
    const l = operaciones(tipo);
    cont.replaceChildren(l.length ? h("div", { class: "tabla-caja" }, h("table", {}, h("thead", {}, h("tr", {}, ["Fecha", "Tipo", "Folio / serie", "Detalle", "Estado", "Operador"].map((t) => h("th", {}, t)))),
      h("tbody", {}, l.map((o) => h("tr", {}, h("td", {}, fecha(o.fecha)), h("td", {}, NOMBRE[o.tipo]), h("td", { class: "mono" }, o.folio), h("td", {}, detalle(o)), h("td", {}, o.estado ? chip(etEstado(o.estado), tonoEstado(o.estado)) : "—"), h("td", {}, o.usuario)))))) : vacio("Sin operaciones", "Aquí verás lo que se registre desde este equipo."));
  }
  pintar();
  return h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Operaciones de este equipo"), h("p", {}, "El servidor no ofrece listados; se guardan en este navegador")),
    h("button", { class: "btn chico peligro", onclick: () => { if (confirm("¿Borrar el historial guardado en este equipo? Los datos del servidor no se tocan.")) { limpiarBitacora(); pintar(); } } }, "Borrar historial local")),
    h("div", { class: "tarjeta-cuerpo" }, tabs, cont));
}
