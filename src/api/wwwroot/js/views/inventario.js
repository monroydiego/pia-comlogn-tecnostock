import { h, icono, campo, entrada, selector, chip, vacio, dinero, normalizar, modal, toast, errorAviso, ocupado } from "../ui.js";
import { PRODUCTOS, SUCURSALES, producto } from "../data.js";
import { api, existencias, invalidarExistencias } from "../api.js";
import { registrar } from "../store.js";

export function vistaInventario({ sesion }) {
  const suc = sesion.sucursal, puedeAlta = sesion.rol === "Almacen" || sesion.rol === "Supervisor";
  let stock = {}, texto = "";
  const cuerpo = h("tbody"), resumen = h("p");

  function pintar() {
    const q = texto.trim().toLowerCase();
    const l = PRODUCTOS.filter((p) => !q || p.desc.toLowerCase().includes(q) || p.id.toLowerCase().includes(q));
    cuerpo.replaceChildren(...l.map((p) => { const n = stock[p.id];
      return h("tr", {}, h("td", {}, h("b", {}, p.desc), h("div", { class: "nota mono" }, p.id)), h("td", {}, p.categoria), h("td", { class: "der num" }, dinero(p.precio)),
        h("td", { class: "der" }, n == null ? chip("…") : chip(n === 0 ? "Sin existencia" : `${n} disponibles`, n === 0 ? "rojo" : n <= 2 ? "ambar" : "verde")),
        h("td", { class: "der" }, h("button", { class: "btn chico", onclick: () => otras(p) }, "Otras sucursales"))); }));
    const bajos = Object.values(stock).filter((n) => n != null && n <= 2).length;
    resumen.textContent = Object.keys(stock).length ? `${bajos} producto(s) con existencia baja (2 o menos) en ${suc}.` : "Consultando existencias…";
  }
  async function cargar(forzar) { stock = await existencias(suc, forzar); pintar(); }

  async function otras(p) {
    try {
      const r = await api("GET", `/disponibilidad/${p.id}/otras-sucursales?sucursalActual=${suc}`);
      modal(`${p.desc} en otras sucursales`, r.length ? h("div", { class: "tabla-caja" }, h("table", {}, h("thead", {}, h("tr", {}, h("th", {}, "Sucursal"), h("th", { class: "der" }, "Disponibles"))),
        h("tbody", {}, r.map((x) => h("tr", {}, h("td", {}, x.sucursal), h("td", { class: "der num" }, x.cantidadDisponible)))))) : vacio("Ninguna otra sucursal tiene existencia", "Puedes registrar una unidad nueva desde Almacén."), { ancho: "440px" });
    } catch (e) { errorAviso(e); }
  }

  // ---- alta de unidad ----
  const prod = selector(PRODUCTOS.filter((p) => p.habilitado).map((p) => [p.id, p.desc]), PRODUCTOS[0].id), sucA = selector(SUCURSALES, suc), serie = entrada({ class: "entrada mono" }), res = h("div");
  const syncSerie = () => { const p = producto(prod.value); serie.disabled = !p.serie; serie.placeholder = p.serie ? "SN-NUEVA-0001 (no puede repetirse)" : "Este producto no lleva serie"; if (!p.serie) serie.value = ""; };
  prod.onchange = syncSerie; syncSerie();
  async function alta() {
    const p = producto(prod.value); if (p.serie && !serie.value.trim()) return toast("Captura el número de serie de la unidad.", "error");
    try {
      const r = await api("POST", "/unidades", { operador: true, cuerpo: { productoId: p.id, sucursalId: sucA.value, serie: p.serie ? normalizar(serie.value) : null } });
      registrar({ tipo: "unidad", folio: r.serie || r.unidadId, productoId: r.productoId, sucursal: r.sucursal, estado: r.estado, usuario: sesion.usuario });
      invalidarExistencias(); serie.value = "";
      res.replaceChildren(h("div", { class: "aviso-caja verde" }, icono("ok"), h("div", {}, `${p.desc} registrado en ${r.sucursal}`, r.serie ? [" · serie ", h("b", { class: "mono" }, r.serie)] : "", " · ", r.estado)));
      if (r.sucursal === suc) cargar(true);
    } catch (e) { errorAviso(e); }
  }

  cargar(false); pintar();
  return h("div", { class: puedeAlta ? "venta-grid" : "" },
    h("section", { class: "tarjeta" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, `Existencias en ${suc}`), resumen),
      h("div", { class: "buscador" }, entrada({ type: "search", placeholder: "Buscar producto", "aria-label": "Buscar", oninput: (e) => { texto = e.target.value; pintar(); } }), h("button", { class: "btn", onclick: (e) => ocupado(e.currentTarget, () => cargar(true)) }, icono("refrescar", 16), "Actualizar"))),
      h("div", { class: "tabla-caja" }, h("table", {}, h("thead", {}, h("tr", {}, h("th", {}, "Producto"), h("th", {}, "Categoría"), h("th", { class: "der" }, "Precio"), h("th", { class: "der" }, "Disponibilidad"), h("th"))), cuerpo))),
    puedeAlta ? h("section", { class: "tarjeta carrito" }, h("div", { class: "tarjeta-cab" }, h("div", {}, h("h2", {}, "Alta de unidad"), h("p", {}, "Recepción de mercancía con número de serie"))),
      h("div", { class: "tarjeta-cuerpo" }, campo("Producto", prod), campo("Sucursal", sucA), campo("Número de serie", serie), h("button", { class: "btn primario", onclick: (e) => ocupado(e.currentTarget, alta) }, icono("mas", 16), "Registrar unidad"), res)) : null);
}
