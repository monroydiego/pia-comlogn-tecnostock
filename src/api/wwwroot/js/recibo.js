import { h, modal, icono, dinero, fecha } from "./ui.js";
import { producto } from "./data.js";

// Ticket de venta imprimible. `op` es el registro guardado en la bitácora.
export function mostrarTicket(op) {
  const f = (a, b) => h("div", { class: "fl" }, h("span", {}, a), h("span", { class: "num" }, b));
  const cuerpo = h("div", { class: "ticket" },
    h("div", { class: "centro" }, h("b", {}, "TECNOSTOCK"), h("div", {}, `Sucursal ${op.sucursal}`)),
    h("hr"), f("Folio", op.folio), f("Fecha", fecha(op.fecha)), f("Atendió", op.usuario),
    op.origen ? f("Apartado origen", op.origen) : null, h("hr"),
    ...op.lineas.map((l) => h("div", {}, f(producto(l.productoId)?.desc || l.productoId, dinero(l.precioUnitario)), l.serie ? h("div", {}, `  Serie ${l.serie}`) : null)),
    h("hr"), h("div", { class: "fl" }, h("b", {}, "TOTAL"), h("b", { class: "num" }, dinero(op.total))),
    ...(op.pagos || []).map((p) => f(p.metodo, dinero(p.monto))), op.cambio > 0 ? f("Cambio", dinero(op.cambio)) : null,
    h("hr"), h("div", { class: "centro" }, "Conserve este comprobante para devoluciones."));
  return modal("Venta registrada", cuerpo, {
    ancho: "400px",
    acciones: [h("button", { class: "btn", onclick: () => window.print() }, icono("imprimir", 16), "Imprimir"), h("button", { class: "btn primario", onclick: (e) => e.target.closest(".fondo-modal").querySelector(".modal-cab button").click() }, "Listo")],
  });
}
