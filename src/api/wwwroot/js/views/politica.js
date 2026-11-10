import { h, icono, vacio, errorAviso, chip } from "../ui.js";
import { api } from "../api.js";

export function vistaPolitica() {
  const dev = h("div", { class: "tarjeta-cuerpo" }, vacio("Cargando política vigente…"));
  api("GET", "/politica/devolucion").then((p) => {
    dev.replaceChildren(h("div", {}, h("b", {}, "Una devolución se acepta de inmediato cuando cumple todo esto:"), h("ul", { style: "margin:8px 0 0;padding-left:20px;display:flex;flex-direction:column;gap:4px" }, p.condiciones.map((c) => h("li", {}, c)))),
      h("div", { class: "tabla-caja" }, h("table", {}, h("thead", {}, h("tr", {}, h("th", {}, "Categoría"), h("th", { class: "der" }, "Plazo para devolver"))), h("tbody", {}, Object.entries(p.plazoPorCategoria).map(([c, d]) => h("tr", {}, h("td", {}, c), h("td", { class: "der num" }, `${d} días`)))))),
      h("div", { class: "aviso-caja info" }, icono("info"), "Si no cumple el criterio (fuera de plazo o empaque abierto) queda En revisión y solo un Supervisor puede autorizarla o rechazarla."));
  }).catch((e) => { dev.replaceChildren(vacio("No se pudo cargar la política")); errorAviso(e); });
  const li = (...t) => h("li", {}, ...t);
  return h("div", { class: "dos" },
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Criterio de devolución vigente"), h("p", {}, "Consultado en vivo al servidor"))), dev),
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("h2", {}, "Reglas de apartado y venta")),
      h("div", { class: "tarjeta-cuerpo" }, h("ul", { style: "margin:0;padding-left:20px;display:flex;flex-direction:column;gap:10px" },
        li(h("b", {}, "Productos apartables: "), "laptops, monitores y SSD. Los teclados solo se venden."),
        li(h("b", {}, "Anticipo mínimo: "), "30 % del precio de lista. El apartado vence a los 7 días."),
        li(h("b", {}, "Unidad reservada: "), "una unidad apartada no puede venderse a otro cliente hasta que el apartado se convierta, se cancele o venza."),
        li(h("b", {}, "Cancelar un apartado: "), "solo un Supervisor."),
        li(h("b", {}, "Productos con serie: "), "cada unidad se vende y se devuelve con su número de serie."),
        li(h("b", {}, "Cobro: "), "se puede pagar con varios métodos; el total debe quedar cubierto.")))));
}
