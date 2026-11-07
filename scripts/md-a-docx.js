#!/usr/bin/env node
// Convierte un documento Markdown del proyecto a Word (.docx) con el formato del curso (regla 7 de CLAUDE.md):
// Times New Roman, tamaño carta, solo texto negro, encabezados de tabla en gris, número de página en el pie.
// Las tablas con 5 o más columnas se colocan en una sección horizontal para que se lean bien.
//
// Uso:  npm install -g docx
//       NODE_PATH="$(npm root -g)" node scripts/md-a-docx.js entrada.md salida.docx
//
// Admite: títulos (#, ##, ###), tablas, listas con viñetas y numeradas, bloques de código, **negrita**, *cursiva*,
// `código` y <br>. Las imágenes PNG sueltas (![leyenda](ruta)) se incrustan escaladas a la página con su leyenda.
const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun, ImageRun, Table, TableRow, TableCell, Footer, AlignmentType, PageOrientation,
  LevelFormat, BorderStyle, WidthType, ShadingType, PageNumber, HeadingLevel, TabStopType,
} = require("docx");

const [, , entrada, salida] = process.argv;
if (!entrada || !salida) { console.error("Uso: node md-a-docx.js entrada.md salida.docx"); process.exit(1); }

const FUENTE = "Times New Roman";
const ANCHO = { vertical: 9360, horizontal: 12960 }; // ancho útil en DXA con márgenes de 1 pulgada
const path = require("path");
const carpeta = path.dirname(path.resolve(entrada));
const lineas = fs.readFileSync(entrada, "utf8").replace(/\r/g, "").split("\n");

// ---------- Inline ----------
function corridas(texto, base = {}) {
  const runs = [];
  const re = /(\*\*[^*]+\*\*|`[^`]+`|\*[^*\s][^*]*\*)/g;
  let ultimo = 0, m;
  const agregar = (t, extra = {}) => { if (t) runs.push(new TextRun({ text: t, font: FUENTE, ...base, ...extra })); };
  texto = texto.replace(/!\[([^\]]*)\]\([^)]*\)/g, "$1").replace(/\[([^\]]+)\]\([^)]*\)/g, "$1");
  while ((m = re.exec(texto))) {
    agregar(texto.slice(ultimo, m.index));
    const t = m[0];
    if (t.startsWith("**")) agregar(t.slice(2, -2), { bold: true });
    else if (t.startsWith("`")) agregar(t.slice(1, -1), { font: "Courier New", size: Math.max(16, (base.size || 22) - 2) });
    else agregar(t.slice(1, -1), { italics: true });
    ultimo = m.index + t.length;
  }
  agregar(texto.slice(ultimo));
  return runs;
}

const parrafo = (texto, opciones = {}, base = {}) =>
  new Paragraph({ spacing: { after: 120 }, ...opciones, children: corridas(texto, base) });

// ---------- Tablas ----------
const borde = { style: BorderStyle.SINGLE, size: 4, color: "000000" };
const bordes = { top: borde, bottom: borde, left: borde, right: borde };

function tabla(filas, ancho) {
  const columnas = filas[0].length;
  const tam = columnas >= 5 ? 18 : columnas === 4 ? 19 : 20; // medios puntos: 9, 9.5 y 10 pt
  const pesos = Array.from({ length: columnas }, (_, c) => {
    const lens = filas.map(f => (f[c] || "").replace(/<br>/g, " ").length);
    const prom = lens.reduce((a, b) => a + b, 0) / lens.length;
    return Math.min(70, prom) + 10;
  });
  // Ancho mínimo por columna: que quepa la palabra más larga sin partirse (≈ 100 DXA por carácter a 9 pt).
  const minimos = Array.from({ length: columnas }, (_, c) => {
    const larga = Math.max(...filas.map(f => Math.max(0, ...(f[c] || "").replace(/<br>/g, " ").replace(/[`*]/g, "").split(/\s+/).map(w => w.length))));
    return Math.min(2400, larga * (tam / 18) * 100 + 220);
  });
  let anchos = pesos.map(p => (p / pesos.reduce((a, b) => a + b, 0)) * ancho);
  for (let ronda = 0; ronda < 5; ronda++) { // sube las columnas angostas y se lo quita a las holgadas
    const faltante = anchos.reduce((acc, a, c) => acc + Math.max(0, minimos[c] - a), 0);
    if (faltante < 1) break;
    const holgura = anchos.reduce((acc, a, c) => acc + Math.max(0, a - minimos[c]), 0);
    anchos = anchos.map((a, c) => (a < minimos[c] ? minimos[c] : a - faltante * ((a - minimos[c]) / holgura)));
  }
  anchos = anchos.map(a => Math.round(a));
  anchos[anchos.length - 1] += ancho - anchos.reduce((a, b) => a + b, 0); // la suma debe ser exacta
  const encabezadoVacio = filas[0].every(c => c.trim() === "");
  return new Table({
    width: { size: ancho, type: WidthType.DXA },
    columnWidths: anchos,
    rows: filas.map((fila, r) => new TableRow({
      tableHeader: r === 0 && !encabezadoVacio,
      cantSplit: true,
      children: fila.map((celda, c) => new TableCell({
        borders: bordes,
        width: { size: anchos[c], type: WidthType.DXA },
        shading: r === 0 && !encabezadoVacio ? { fill: "D9D9D9", type: ShadingType.CLEAR } : undefined,
        margins: { top: 60, bottom: 60, left: 100, right: 100 },
        children: celda.split(/<br>/).map(parte => new Paragraph({
          spacing: { after: 40 }, children: corridas(parte.trim(), { size: tam, bold: r === 0 && !encabezadoVacio }),
        })),
      })),
    })),
  });
}

const dividirFila = l => l.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map(c => c.trim());
const esSeparador = l => /^\s*\|?\s*:?-{3,}/.test(l);

// ---------- Bloques ----------
const bloques = []; // { tipo, ...}
let i = 0;
while (i < lineas.length) {
  const l = lineas[i];
  if (l.trim() === "") { i++; continue; }
  if (l.startsWith("```")) {
    const codigo = []; i++;
    while (i < lineas.length && !lineas[i].startsWith("```")) codigo.push(lineas[i++]);
    i++; bloques.push({ tipo: "codigo", lineas: codigo }); continue;
  }
  const im = l.trim().match(/^!\[([^\]]*)\]\(([^)]+)\)$/);
  if (im) {
    const ruta = path.resolve(carpeta, im[2]);
    const cab = fs.readFileSync(ruta).subarray(0, 32);
    bloques.push({ tipo: "imagen", leyenda: im[1], ruta, ancho: cab.readUInt32BE(16), alto: cab.readUInt32BE(20) });
    i++; continue;
  }
  const h = l.match(/^(#{1,3}) (.+)$/);
  if (h) { bloques.push({ tipo: "titulo", nivel: h[1].length, texto: h[2] }); i++; continue; }
  if (l.trim().startsWith("|")) {
    const filas = [];
    while (i < lineas.length && lineas[i].trim().startsWith("|")) { if (!esSeparador(lineas[i])) filas.push(dividirFila(lineas[i])); i++; }
    bloques.push({ tipo: "tabla", filas }); continue;
  }
  if (/^\s*[-*] /.test(l)) {
    const items = [];
    while (i < lineas.length && /^\s*[-*] /.test(lineas[i])) items.push(lineas[i++].replace(/^\s*[-*] /, ""));
    bloques.push({ tipo: "vinetas", items }); continue;
  }
  if (/^\s*\d+\. /.test(l)) {
    const items = [];
    while (i < lineas.length && /^\s*\d+\. /.test(lineas[i])) items.push(lineas[i++].replace(/^\s*\d+\. /, ""));
    bloques.push({ tipo: "numerada", items }); continue;
  }
  const texto = [l.trim()]; i++;
  while (i < lineas.length && lineas[i].trim() !== "" && !/^(#{1,3} |\||```|!\[|\s*[-*] |\s*\d+\. )/.test(lineas[i])) texto.push(lineas[i++].trim());
  bloques.push({ tipo: "parrafo", texto: texto.join(" ") });
}

// ---------- Orientación: una tabla ancha lleva consigo su título y sus párrafos previos ----------
// Horizontal si tiene 5 o más columnas o palabras muy largas (por ejemplo nombres de pruebas), que no se pueden partir.
const palabraLarga = b => b.filas.some(f => f.some(c => c.replace(/<br>/g, " ").replace(/[`*]/g, "").split(/\s+/).some(w => w.length > 38)));
const ancha = b => (b.tipo === "tabla" && (b.filas[0].length >= 5 || palabraLarga(b))) || (b.tipo === "imagen" && b.ancho / b.alto >= 1.6);
bloques.forEach(b => (b.horizontal = false));
bloques.forEach((b, idx) => {
  if (!ancha(b)) return;
  b.horizontal = true;
  for (let j = idx - 1; j >= 0 && bloques[j].tipo !== "tabla" && bloques[j].tipo !== "imagen"; j--) {
    bloques[j].horizontal = true;
    if (bloques[j].tipo === "titulo" && bloques[j].nivel <= 2) break;
  }
  // Los párrafos y listas que siguen a la tabla, hasta el próximo título, se quedan en la misma sección horizontal.
  for (let j = idx + 1; j < bloques.length && bloques[j].tipo !== "titulo" && bloques[j].tipo !== "tabla" && bloques[j].tipo !== "imagen"; j++) bloques[j].horizontal = true;
});

// ---------- Construcción ----------
const configuracionesNumeradas = [];
const estilosTitulo = [1, 2, 3].map(n => ({
  id: `Heading${n}`, name: `Heading ${n}`, basedOn: "Normal", next: "Normal", quickFormat: true,
  run: { font: FUENTE, size: [28, 26, 24][n - 1], bold: true, color: "000000" },
  paragraph: { spacing: { before: 280 - n * 40, after: 120 }, outlineLevel: n - 1, keepNext: true },
}));

let primerTitulo = true;
function convertir(b, ancho) {
  switch (b.tipo) {
    case "titulo": {
      if (b.nivel === 1 && primerTitulo) {
        primerTitulo = false;
        return [new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 480, after: 360 },
          children: [new TextRun({ text: b.texto, font: FUENTE, size: 40, bold: true })] })];
      }
      return [new Paragraph({ heading: [HeadingLevel.HEADING_1, HeadingLevel.HEADING_1, HeadingLevel.HEADING_2, HeadingLevel.HEADING_3][b.nivel],
        children: corridas(b.texto, { bold: true, size: [28, 28, 26, 24][b.nivel] }) })];
    }
    case "parrafo": return [parrafo(b.texto)];
    case "vinetas": return b.items.map(t => new Paragraph({ numbering: { reference: "vinetas", level: 0 }, spacing: { after: 60 }, children: corridas(t) }));
    case "numerada": {
      const referencia = `numerada${configuracionesNumeradas.length}`;
      configuracionesNumeradas.push({ reference: referencia, levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] });
      return b.items.map(t => new Paragraph({ numbering: { reference: referencia, level: 0 }, spacing: { after: 60 }, children: corridas(t) }));
    }
    case "codigo": return b.lineas.map((t, k) => new Paragraph({ spacing: { after: k === b.lineas.length - 1 ? 160 : 0 }, indent: { left: 360 },
      children: [new TextRun({ text: t === "" ? " " : t, font: "Courier New", size: 17 })] }));
    case "imagen": {
      const maxAncho = ancho / 15 - 6;                      // píxeles a 96 ppp
      const maxAlto = (b.horizontal ? 8600 : 12300) / 15 - 70; // deja lugar a la leyenda
      const factor = Math.min(maxAncho / b.ancho, maxAlto / b.alto, 1);
      const w = Math.round(b.ancho * factor), h = Math.round(b.alto * factor);
      return [
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 }, keepNext: true, children: [new ImageRun({
          type: "png", data: fs.readFileSync(b.ruta), transformation: { width: w, height: h },
          altText: { title: b.leyenda, description: b.leyenda, name: path.basename(b.ruta) } })] }),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [new TextRun({ text: b.leyenda, font: FUENTE, size: 20, italics: true })] }),
      ];
    }
    case "tabla": return [tabla(b.filas, ancho), new Paragraph({ spacing: { after: 160 }, children: [] })];
  }
  return [];
}

const pie = new Footer({ children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [
  new TextRun({ text: "Página ", font: FUENTE, size: 20 }), new TextRun({ children: [PageNumber.CURRENT], font: FUENTE, size: 20 })] })] });

const secciones = [];
for (const b of bloques) {
  const ultima = secciones[secciones.length - 1];
  if (ultima && ultima.horizontal === b.horizontal) ultima.bloques.push(b);
  else secciones.push({ horizontal: b.horizontal, bloques: [b] });
}

// Se convierte primero para que las listas numeradas queden registradas antes de fijar la configuración del documento.
const seccionesConvertidas = secciones.map(s => {
  const ancho = s.horizontal ? ANCHO.horizontal : ANCHO.vertical;
  return { horizontal: s.horizontal, hijos: s.bloques.flatMap(b => convertir(b, ancho)) };
});

const doc = new Document({
  creator: "Equipo TecnoStock", title: bloques.find(b => b.tipo === "titulo")?.texto || "TecnoStock",
  styles: { default: { document: { run: { font: FUENTE, size: 22 } } }, paragraphStyles: estilosTitulo },
  numbering: { config: [
    { reference: "vinetas", levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
      style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ...configuracionesNumeradas,
  ] },
  sections: seccionesConvertidas.map(s => ({
    properties: { page: {
      size: { width: 12240, height: 15840, orientation: s.horizontal ? PageOrientation.LANDSCAPE : PageOrientation.PORTRAIT },
      margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } } },
    footers: { default: pie },
    children: s.hijos,
  })),
});

Packer.toBuffer(doc).then(buf => { fs.writeFileSync(salida, buf); console.log(`${salida} (${(buf.length / 1024).toFixed(0)} KB, ${secciones.length} secciones)`); });
