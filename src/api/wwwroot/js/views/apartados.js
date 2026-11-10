import { h, icono, campo, entrada, selector, chip, vacio, dinero, redondear, fecha, normalizar, modal, toast, errorAviso, ocupado, tonoEstado } from "../ui.js";
import { PRODUCTOS, CLIENTES, SUCURSALES, producto, cliente } from "../data.js";
import { api, invalidarExistencias } from "../api.js";
import { registrar, actualizar, operaciones, buscar } from "../store.js";
import { mostrarTicket } from "../recibo.js";

const METODOS = [["Efectivo", "Efectivo"], ["Tarjeta", "Tarjeta"], ["Transferencia", "Transferencia"]];
const minimo = (p) => Math.ceil(p.precio * 30) / 100; // 30 % del precio de lista

export function vistaApartados({ sesion, redibujar }) {
  const esSup = sesion.rol === "Supervisor";
  const apartables = PRODUCTOS.filter((p) => p.apartable && p.habilitado);
  const resultado = h("div"), lista = h("div");

  // ---- nuevo apartado ----
  const prod = selector(apartables.map((p) => [p.id, `${p.desc} · ${dinero(p.precio)}`]), apartables[0].id);
  const suc = selector(SUCURSALES, sesion.sucursal);
  const cli = selector(CLIENTES.map((c) => [c.id, `${c.nombre} · ${c.ciudad}${c.preferente ? " · Preferente" : ""}${c.activo ? "" : " (inactivo)"}`, !c.activo]), CLIENTES[0].id);
  const metodo = selector(METODOS, "Efectivo"), monto = entrada({ type: "number", min: "0", step: "0.01", inputmode: "decimal" });
  const ayuda = h("small", { class: "ayuda" });
  const sync = () => { const p = producto(prod.value); ayuda.textContent = `Anticipo mínimo: ${dinero(minimo(p))} (30 % de ${dinero(p.precio)}). El apartado vence a los 7 días.`; };
  prod.onchange = sync; sync();
  const usarMin = () => { monto.value = minimo(producto(prod.value)).toFixed(2); };

  async function apartar() {
    try {
      const r = await api("POST", "/apartados", { operador: true, cuerpo: { productoId: prod.value, sucursalId: suc.value, clienteId: cli.value, anticipo: { metodo: metodo.value, monto: redondear(+monto.value || 0) } } });
      registrar({ tipo: "apartado", folio: r.folio, fechaLimite: r.fechaLimite, saldo: r.saldoPendiente, productoId: prod.value, sucursal: suc.value, clienteId: cli.value, anticipo: redondear(+monto.value), estado: "Vigente", usuario: sesion.usuario });
      invalidarExistencias(); monto.value = "";
      resultado.replaceChildren(h("div", { class: "resultado aviso-caja verde", style: "flex-direction:column" }, h("span", {}, "Apartado registrado"), h("div", { class: "folio" }, r.folio),
        h("div", {}, `Vigente hasta ${fecha(r.fechaLimite)}. Saldo pendiente: `, h("b", {}, dinero(r.saldoPendiente)), "."),
        h("div", { class: "nota" }, "La unidad queda reservada y no puede venderse a otro cliente.")));
      pintarLista();
    } catch (e) { errorAviso(e); }
  }

  // ---- operaciones sobre un apartado ----
  function convertir(folio) {
    const apt = buscar("apartado", folio), p = apt && producto(apt.productoId);
    const m = selector(METODOS, "Efectivo"), mt = entrada({ type: "number", min: "0", step: "0.01", value: apt ? apt.saldo : "" });
    const serie = entrada({ class: "entrada mono", placeholder: "SN-TEC-P001-001" });
    const mod = modal(`Convertir ${folio} en venta`, h("div", { class: "tarjeta-cuerpo", style: "padding:0" },
      apt ? h("div", { class: "aviso-caja info" }, icono("info"), h("div", {}, `${p.desc} · saldo pendiente `, h("b", {}, dinero(apt.saldo)))) : h("div", { class: "aviso-caja ambar" }, icono("alerta"), "Este folio no se creó en este equipo: captura el saldo pendiente tal como figura en el apartado."),
      h("div", { class: "fila c2" }, campo("Método de pago del saldo", m), campo("Monto", mt)),
      campo("Número de serie de la unidad apartada", serie, !apt || p.serie ? "Está en la etiqueta de la unidad que se apartó. Es obligatorio para productos con serie." : "Este producto no lleva serie.")),
      { acciones: [h("button", { class: "btn", onclick: () => mod.cerrar() }, "Cancelar"), h("button", { class: "btn primario", onclick: (e) => ocupado(e.currentTarget, async () => {
        try {
          const v = await api("POST", `/apartados/${encodeURIComponent(folio)}/convertir`, { operador: true, cuerpo: { pagoDelSaldo: { metodo: m.value, monto: redondear(+mt.value || 0) }, serieCapturada: normalizar(serie.value) || null } });
          const cambio = Math.max(0, redondear(+mt.value - (apt?.saldo ?? +mt.value)));
          const op = { tipo: "venta", folio: v.folio, sucursal: apt?.sucursal || sesion.sucursal, usuario: sesion.usuario, total: v.total, lineas: v.lineas, pagos: [{ metodo: m.value, monto: redondear(+mt.value) }], cambio: 0, origen: folio };
          registrar(op); actualizar("apartado", folio, { estado: "Convertido", folioVenta: v.folio }); invalidarExistencias(); mod.cerrar(); mostrarTicket({ ...op, fecha: new Date().toISOString() }); pintarLista();
        } catch (er) { errorAviso(er); }
      }) }, "Cobrar saldo y convertir")] });
  }
  function cancelar(folio) {
    const motivo = h("textarea", { class: "entrada", placeholder: "Ej. El cliente lo solicitó" });
    const mod = modal(`Cancelar ${folio}`, h("div", { class: "tarjeta-cuerpo", style: "padding:0" }, h("div", { class: "aviso-caja ambar" }, icono("alerta"), "La unidad vuelve al inventario. Solo un Supervisor puede cancelar apartados."), campo("Motivo de la cancelación", motivo)),
      { acciones: [h("button", { class: "btn", onclick: () => mod.cerrar() }, "Volver"), h("button", { class: "btn peligro", onclick: (e) => ocupado(e.currentTarget, async () => {
        if (!motivo.value.trim()) return toast("Escribe el motivo de la cancelación.", "error");
        try { await api("POST", `/apartados/${encodeURIComponent(folio)}/cancelar`, { operador: true, cuerpo: { motivo: motivo.value.trim() } }); actualizar("apartado", folio, { estado: "Cancelado" }); invalidarExistencias(); mod.cerrar(); toast(`Apartado ${folio} cancelado.`); pintarLista(); }
        catch (er) { errorAviso(er); }
      }) }, "Cancelar apartado")] });
  }
  async function vencer(btn) {
    try { const r = await api("POST", "/apartados/vencer"); invalidarExistencias(); toast(r.apartadosVencidos ? `${r.apartadosVencidos} apartado(s) vencido(s); sus unidades quedaron libres.` : "No hay apartados vencidos por procesar.", "info"); }
    catch (e) { errorAviso(e); }
  }

  const folioLibre = entrada({ placeholder: "APT-000001", class: "entrada mono", "aria-label": "Folio de apartado" });
  function pintarLista() {
    const apts = operaciones("apartado");
    lista.replaceChildren(apts.length ? h("div", { class: "tabla-caja" }, h("table", {}, h("thead", {}, h("tr", {}, ["Folio", "Producto", "Cliente", "Saldo", "Estado", ""].map((t, i) => h("th", { class: i === 3 ? "der" : "" }, t)))),
      h("tbody", {}, apts.map((a) => { const vig = a.estado === "Vigente", vencida = vig && new Date(a.fechaLimite) < new Date();
        return h("tr", {}, h("td", { class: "mono" }, a.folio, h("div", { class: "nota" }, `vence ${fecha(a.fechaLimite, false)}`)), h("td", {}, producto(a.productoId)?.desc), h("td", {}, cliente(a.clienteId)?.nombre || a.clienteId), h("td", { class: "der num" }, dinero(a.saldo)),
          h("td", {}, chip(vencida ? "Fecha límite pasada" : a.estado, vencida ? "ambar" : tonoEstado(a.estado))),
          h("td", {}, vig ? h("div", { class: "lista-acc" }, h("button", { class: "btn chico primario", onclick: () => convertir(a.folio) }, "Convertir"), h("button", { class: "btn chico", disabled: !esSup, title: esSup ? "" : "Solo un Supervisor puede cancelar", onclick: () => cancelar(a.folio) }, "Cancelar")) : null)); })))) : vacio("Aún no hay apartados", "Los que registres en este equipo aparecerán aquí."));
  }
  pintarLista();

  return h("div", { class: "dos" },
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Nuevo apartado"), h("p", {}, "Laptops, monitores y SSD con anticipo mínimo del 30 %"))),
      h("div", { class: "tarjeta-cuerpo" }, campo("Producto", prod), h("div", { class: "fila c2" }, campo("Sucursal", suc), campo("Cliente", cli)),
        h("div", { class: "fila c2" }, campo("Método del anticipo", metodo), campo("Monto del anticipo", monto)), ayuda,
        h("div", { class: "lista-acc", style: "justify-content:flex-start" }, h("button", { class: "btn", onclick: usarMin }, "Usar anticipo mínimo"), h("button", { class: "btn primario", onclick: (e) => ocupado(e.currentTarget, apartar) }, "Registrar apartado")), resultado)),
    h("div", { style: "display:flex;flex-direction:column;gap:22px;min-width:0" },
      h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Apartados"), h("p", {}, "Registrados en este equipo")),
        h("button", { class: "btn chico", title: "Libera las unidades de apartados con fecha límite vencida", onclick: (e) => ocupado(e.currentTarget, () => vencer()) }, "Vencer apartados caducados")),
        h("div", { class: "tarjeta-cuerpo" }, h("div", { class: "fila", style: "grid-template-columns:1fr auto auto" }, folioLibre,
          h("button", { class: "btn", onclick: () => folioLibre.value.trim() ? convertir(normalizar(folioLibre.value)) : toast("Escribe un folio.", "error") }, "Convertir"),
          h("button", { class: "btn", disabled: !esSup, title: esSup ? "" : "Solo un Supervisor", onclick: () => folioLibre.value.trim() ? cancelar(normalizar(folioLibre.value)) : toast("Escribe un folio.", "error") }, "Cancelar")),
          h("small", { class: "ayuda" }, "¿Otro equipo creó el apartado? Escribe su folio para operarlo."), lista))));
}
