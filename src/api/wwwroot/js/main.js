import { h, icono, selector, campo, entrada, toast } from "./ui.js";
import { sesion, guardarSesion, cerrarSesion } from "./store.js";
import { ROLES, SUCURSALES } from "./data.js";
import { api } from "./api.js";
import { vistaPanel } from "./views/panel.js";
import { vistaVenta } from "./views/venta.js";
import { vistaApartados } from "./views/apartados.js";
import { vistaDevoluciones } from "./views/devoluciones.js";
import { vistaInventario } from "./views/inventario.js";
import { vistaPolitica } from "./views/politica.js";
import { vistaHistorial } from "./views/historial.js";

const TODOS = ["Vendedor", "Cajero", "Supervisor", "Almacen"], MOSTRADOR = ["Vendedor", "Cajero", "Supervisor"];
const RUTAS = [
  { id: "panel", titulo: "Panel", icono: "inicio", roles: TODOS, vista: vistaPanel },
  { id: "venta", titulo: "Punto de venta", icono: "venta", roles: MOSTRADOR, vista: vistaVenta },
  { id: "apartados", titulo: "Apartados", icono: "apartado", roles: MOSTRADOR, vista: vistaApartados },
  { id: "devoluciones", titulo: "Devoluciones", icono: "devolucion", roles: MOSTRADOR, vista: vistaDevoluciones },
  { id: "inventario", titulo: "Inventario", icono: "inventario", roles: TODOS, vista: vistaInventario },
  { id: "politica", titulo: "Política de devolución", icono: "politica", roles: TODOS, vista: vistaPolitica },
  { id: "historial", titulo: "Historial", icono: "historial", roles: TODOS, vista: vistaHistorial },
];
const raiz = document.getElementById("raiz");
let temporizador;

function acceso() {
  let rol = "Cajero";
  const usuario = entrada({ placeholder: "Ej. cajero1", required: true, maxlength: 40, "aria-label": "Usuario" });
  const suc = selector(SUCURSALES.map((s) => [s, `Sucursal ${s.slice(-2)} (${s})`]), SUCURSALES[0]);
  const rolesEl = h("div", { class: "roles", role: "group", "aria-label": "Rol" });
  const pintarRoles = () => { rolesEl.replaceChildren(...ROLES.map((r) => h("button", { type: "button", class: "rol", "aria-pressed": String(r.id === rol), onclick: () => { rol = r.id; pintarRoles(); } }, h("b", {}, r.label), h("span", {}, r.desc)))); };
  pintarRoles();
  const form = h("form", { onsubmit: (e) => { e.preventDefault(); const u = usuario.value.trim(); if (!u) return usuario.focus(); guardarSesion({ usuario: u, rol, sucursal: suc.value }); location.hash = "#/panel"; arrancar(); } },
    h("h2", {}, "Iniciar turno"), h("p", { class: "nota" }, "Identifícate para registrar las operaciones a tu nombre."),
    campo("Usuario", usuario), h("div", { class: "campo" }, h("span", { class: "campo-et" }, "Rol"), rolesEl), campo("Sucursal", suc),
    h("button", { class: "btn primario grande", type: "submit" }, "Entrar"),
    h("p", { class: "nota" }, "El sistema identifica al operador por su nombre y rol, sin contraseña. Solo el rol Supervisor se valida en el servidor."));
  raiz.replaceChildren(h("main", { class: "acceso" },
    h("section", { class: "acceso-marca" }, h("div", { class: "logo" }, h("i", {}, "T"), "TecnoStock"),
      h("div", {}, h("h1", {}, "Ventas, apartados y devoluciones en un solo lugar."), h("p", {}, "Cobra, aparta unidades con número de serie, gestiona devoluciones y consulta existencias en las cinco sucursales."))),
    h("section", { class: "acceso-form" }, form)));
  usuario.focus();
}

function arrancar() {
  const s = sesion(); if (!s) return acceso();
  clearInterval(temporizador);
  const permitidas = RUTAS.filter((r) => r.roles.includes(s.rol));
  const nav = h("nav", { class: "nav", "aria-label": "Principal" });
  const titulo = h("h1", {}); const contenido = h("div", { class: "pagina" });
  const estado = h("div", { class: "estado", title: "Conexión con el servidor" }, h("i"), h("span", {}, "Conectando…"));
  const rolEt = ROLES.find((r) => r.id === s.rol)?.label || s.rol;
  const selSuc = selector(SUCURSALES.map((x) => [x, x]), s.sucursal, { "aria-label": "Sucursal de trabajo", onchange: (e) => { guardarSesion({ ...sesion(), sucursal: e.target.value }); dibujar(); } });
  const salir = h("button", { onclick: () => { if (confirm("¿Cerrar el turno?")) { cerrarSesion(); clearInterval(temporizador); acceso(); } } }, icono("salir", 16), "Cerrar turno");
  raiz.replaceChildren(h("div", { class: "app" },
    h("aside", { class: "lateral" }, h("div", { class: "logo" }, h("i", {}, "T"), "TecnoStock"), nav,
      h("div", { class: "operador" }, h("div", {}, h("b", {}, s.usuario), h("span", {}, `${rolEt} · ${s.sucursal}`)), salir)),
    h("div", { class: "principal" }, h("header", { class: "barra" }, titulo, h("div", { class: "barra-der" }, h("label", { class: "sel-suc" }, "Sucursal", selSuc), estado)), contenido)));

  function dibujar() {
    const id = location.hash.replace(/^#\/?/, "") || "panel";
    const ruta = permitidas.find((r) => r.id === id) || permitidas[0];
    if (ruta.id !== id) { history.replaceState(null, "", `#/${ruta.id}`); }
    titulo.textContent = ruta.titulo; document.title = `${ruta.titulo} · TecnoStock`;
    nav.replaceChildren(...permitidas.map((r) => h("a", { href: `#/${r.id}`, "aria-current": r.id === ruta.id ? "page" : null }, icono(r.icono), h("span", {}, r.titulo))));
    contenido.replaceChildren(ruta.vista({ sesion: sesion(), redibujar: dibujar })); window.scrollTo(0, 0);
  }
  window.onhashchange = dibujar; dibujar();

  const ping = async () => {
    try { await api("GET", "/politica/devolucion"); estado.className = "estado ok"; estado.lastChild.textContent = "Servidor conectado"; }
    catch { estado.className = "estado mal"; estado.lastChild.textContent = "Sin conexión"; }
  };
  ping(); temporizador = setInterval(ping, 30000);
}

if (!sesion()) acceso(); else arrancar();
window.addEventListener("unhandledrejection", (e) => { console.error(e.reason); toast("Ocurrió un error inesperado.", "error"); });
