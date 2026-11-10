// Sesión del operador y bitácora local de operaciones. El API no tiene listados,
// así que los folios que este equipo genera se guardan en el navegador.
const KS = "tecnostock.sesion.v1", KH = "tecnostock.bitacora.v1";
const leer = (k, d) => { try { return JSON.parse(localStorage.getItem(k)) ?? d; } catch { return d; } };
const guardar = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch { /* almacenamiento lleno o bloqueado */ } };

export const sesion = () => leer(KS, null);
export const guardarSesion = (s) => guardar(KS, s);
export const cerrarSesion = () => { try { localStorage.removeItem(KS); } catch { /* nada */ } };

const bitacora = () => leer(KH, []);
export const operaciones = (tipo) => bitacora().filter((o) => !tipo || o.tipo === tipo);
export const registrar = (op) => { const b = bitacora(); b.unshift({ fecha: new Date().toISOString(), ...op }); guardar(KH, b.slice(0, 500)); };
export const actualizar = (tipo, folio, cambios) => {
  const b = bitacora(); const o = b.find((x) => x.tipo === tipo && x.folio === folio);
  if (o) { Object.assign(o, cambios); guardar(KH, b); }
  return !!o;
};
export const buscar = (tipo, folio) => bitacora().find((o) => o.tipo === tipo && o.folio === folio?.trim().toUpperCase());
export const limpiarBitacora = () => guardar(KH, []);
