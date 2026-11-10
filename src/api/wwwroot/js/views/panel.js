import { h, icono, chip, vacio, dinero, fecha, mismoDia, tonoEstado, etEstado } from "../ui.js";
import { PRODUCTOS, producto } from "../data.js";
import { existencias } from "../api.js";
import { operaciones } from "../store.js";

export function vistaPanel({ sesion }) {
  const ventas = operaciones("venta").filter((v) => mismoDia(v.fecha)), apts = operaciones("apartado").filter((a) => a.estado === "Vigente"), revision = operaciones("devolucion").filter((d) => d.estado === "EnRevision");
  const hora = new Date().getHours(), saludo = hora < 12 ? "Buenos días" : hora < 19 ? "Buenas tardes" : "Buenas noches";
  const m = (et, val, sub) => h("div", { class: "tarjeta metrica" }, h("span", {}, et), h("b", { class: "num" }, val), sub ? h("span", {}, sub) : null);
  const bajos = h("div", { class: "tarjeta-cuerpo" }, vacio("Consultando existencias…"));
  existencias(sesion.sucursal).then((d) => {
    const l = PRODUCTOS.filter((p) => p.habilitado && d[p.id] != null && d[p.id] <= 2).sort((a, b) => d[a.id] - d[b.id]);
    bajos.replaceChildren(l.length ? h("div", { class: "tabla-caja" }, h("table", {}, h("tbody", {}, l.map((p) => h("tr", {}, h("td", {}, p.desc), h("td", { class: "der" }, chip(d[p.id] === 0 ? "Sin existencia" : `${d[p.id]} disp.`, d[p.id] === 0 ? "rojo" : "ambar")))))) ) : vacio("Todo en orden", "Ningún producto con 2 o menos unidades."));
  }).catch(() => bajos.replaceChildren(vacio("No se pudo consultar el inventario")));
  const rec = operaciones().slice(0, 6);
  const atajos = [["venta", "venta", "Nueva venta", "Cobra productos con uno o varios métodos de pago", ["Vendedor", "Cajero", "Supervisor"]], ["apartados", "apartado", "Apartar un producto", "Reserva una unidad con anticipo del 30 %", ["Vendedor", "Cajero", "Supervisor"]],
    ["devoluciones", "devolucion", "Devolución", "Solicita o resuelve una devolución", ["Vendedor", "Cajero", "Supervisor"]], ["inventario", "inventario", "Inventario", "Consulta existencias y registra unidades", ["Vendedor", "Cajero", "Supervisor", "Almacen"]]]
    .filter((a) => a[4].includes(sesion.rol));
  return h("div", { class: "pagina", style: "padding:0;max-width:none" },
    h("div", {}, h("h2", { style: "font-size:26px;letter-spacing:-.02em" }, `${saludo}, ${sesion.usuario}`), h("p", { class: "nota" }, `Turno en ${sesion.sucursal}`)),
    h("div", { class: "metricas" }, m("Ventas de hoy", ventas.length, "en este equipo"), m("Total vendido hoy", dinero(ventas.reduce((a, v) => a + v.total, 0))), m("Apartados vigentes", apts.length, "en este equipo"), m("Devoluciones en revisión", revision.length, revision.length ? "esperan a un Supervisor" : "ninguna pendiente")),
    h("div", { class: "atajos" }, atajos.map(([r, ic, t, d]) => h("a", { class: "atajo", href: `#/${r}` }, h("div", { class: "ic" }, icono(ic, 22)), h("b", {}, t), h("span", {}, d)))),
    h("div", { class: "dos" },
      h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Existencias bajas"), h("p", {}, sesion.sucursal))), bajos),
      h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("h2", {}, "Últimas operaciones")), rec.length ? h("div", { class: "tabla-caja" }, h("table", {}, h("tbody", {}, rec.map((o) => h("tr", {}, h("td", {}, { venta: "Venta", apartado: "Apartado", devolucion: "Devolución", unidad: "Alta de unidad" }[o.tipo]), h("td", { class: "mono" }, o.folio), h("td", { class: "nota" }, fecha(o.fecha)), h("td", {}, o.estado ? chip(etEstado(o.estado), tonoEstado(o.estado)) : null)))))) : h("div", { class: "tarjeta-cuerpo" }, vacio("Sin operaciones todavía", "Empieza con una venta o un apartado.")))));
}
