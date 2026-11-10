// Datos de referencia sembrados por el backend (data/catalogo.csv y data/actores.csv).
// El API no expone listados de productos ni de clientes; por eso viven aquí.
export const SUCURSALES = ["SUC-01", "SUC-02", "SUC-03", "SUC-04", "SUC-05"];
export const CATEGORIAS = ["Computadoras", "Accesorios", "Componentes"];
export const ROLES = [
  { id: "Vendedor", label: "Vendedor", desc: "Atiende al cliente antes del cobro" },
  { id: "Cajero", label: "Cajero", desc: "Cobra, cierra ventas y apartados" },
  { id: "Supervisor", label: "Supervisor", desc: "Autoriza devoluciones y cancelaciones" },
  { id: "Almacen", label: "Almacén", desc: "Da de alta unidades y controla existencias" },
];
export const PRODUCTOS = [
 {
  "id": "TEC-P001",
  "desc": "Laptop 01",
  "precio": 336.0,
  "categoria": "Computadoras",
  "tipo": "Laptop",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P002",
  "desc": "Monitor 02",
  "precio": 407.0,
  "categoria": "Accesorios",
  "tipo": "Monitor",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P003",
  "desc": "SSD 03",
  "precio": 478.0,
  "categoria": "Componentes",
  "tipo": "SSD",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P004",
  "desc": "Teclado 04",
  "precio": 549.0,
  "categoria": "Accesorios",
  "tipo": "Teclado",
  "serie": false,
  "apartable": false,
  "habilitado": true
 },
 {
  "id": "TEC-P005",
  "desc": "Laptop 05",
  "precio": 620.0,
  "categoria": "Computadoras",
  "tipo": "Laptop",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P006",
  "desc": "Monitor 06",
  "precio": 691.0,
  "categoria": "Accesorios",
  "tipo": "Monitor",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P007",
  "desc": "SSD 07",
  "precio": 762.0,
  "categoria": "Componentes",
  "tipo": "SSD",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P008",
  "desc": "Teclado 08",
  "precio": 833.0,
  "categoria": "Accesorios",
  "tipo": "Teclado",
  "serie": false,
  "apartable": false,
  "habilitado": true
 },
 {
  "id": "TEC-P009",
  "desc": "Laptop 09",
  "precio": 904.0,
  "categoria": "Computadoras",
  "tipo": "Laptop",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P010",
  "desc": "Monitor 10",
  "precio": 975.0,
  "categoria": "Accesorios",
  "tipo": "Monitor",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P011",
  "desc": "SSD 11",
  "precio": 1046.0,
  "categoria": "Componentes",
  "tipo": "SSD",
  "serie": true,
  "apartable": true,
  "habilitado": false
 },
 {
  "id": "TEC-P012",
  "desc": "Teclado 12",
  "precio": 1117.0,
  "categoria": "Accesorios",
  "tipo": "Teclado",
  "serie": false,
  "apartable": false,
  "habilitado": true
 },
 {
  "id": "TEC-P013",
  "desc": "Laptop 13",
  "precio": 1188.0,
  "categoria": "Computadoras",
  "tipo": "Laptop",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P014",
  "desc": "Monitor 14",
  "precio": 1259.0,
  "categoria": "Accesorios",
  "tipo": "Monitor",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P015",
  "desc": "SSD 15",
  "precio": 1330.0,
  "categoria": "Componentes",
  "tipo": "SSD",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P016",
  "desc": "Teclado 16",
  "precio": 1401.0,
  "categoria": "Accesorios",
  "tipo": "Teclado",
  "serie": false,
  "apartable": false,
  "habilitado": true
 },
 {
  "id": "TEC-P017",
  "desc": "Laptop 17",
  "precio": 1472.0,
  "categoria": "Computadoras",
  "tipo": "Laptop",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P018",
  "desc": "Monitor 18",
  "precio": 1543.0,
  "categoria": "Accesorios",
  "tipo": "Monitor",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P019",
  "desc": "SSD 19",
  "precio": 1614.0,
  "categoria": "Componentes",
  "tipo": "SSD",
  "serie": true,
  "apartable": true,
  "habilitado": true
 },
 {
  "id": "TEC-P020",
  "desc": "Teclado 20",
  "precio": 1685.0,
  "categoria": "Accesorios",
  "tipo": "Teclado",
  "serie": false,
  "apartable": false,
  "habilitado": true
 }
];
export const CLIENTES = [{"id": "TEC-C001", "nombre": "Comprador 01", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C002", "nombre": "Comprador 02", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C003", "nombre": "Comprador 03", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C004", "nombre": "Comprador 04", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C005", "nombre": "Comprador 05", "preferente": true, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C006", "nombre": "Comprador 06", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C007", "nombre": "Comprador 07", "preferente": false, "activo": false, "ciudad": "Monterrey"}, {"id": "TEC-C008", "nombre": "Comprador 08", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C009", "nombre": "Comprador 09", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C010", "nombre": "Comprador 10", "preferente": true, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C011", "nombre": "Comprador 11", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C012", "nombre": "Comprador 12", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C013", "nombre": "Comprador 13", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C014", "nombre": "Comprador 14", "preferente": false, "activo": false, "ciudad": "Apodaca"}, {"id": "TEC-C015", "nombre": "Comprador 15", "preferente": true, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C016", "nombre": "Comprador 16", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C017", "nombre": "Comprador 17", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C018", "nombre": "Comprador 18", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C019", "nombre": "Comprador 19", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C020", "nombre": "Comprador 20", "preferente": true, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C021", "nombre": "Comprador 21", "preferente": false, "activo": false, "ciudad": "Monterrey"}, {"id": "TEC-C022", "nombre": "Comprador 22", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C023", "nombre": "Comprador 23", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C024", "nombre": "Comprador 24", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C025", "nombre": "Comprador 25", "preferente": true, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C026", "nombre": "Comprador 26", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C027", "nombre": "Comprador 27", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C028", "nombre": "Comprador 28", "preferente": false, "activo": false, "ciudad": "Apodaca"}, {"id": "TEC-C029", "nombre": "Comprador 29", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C030", "nombre": "Comprador 30", "preferente": true, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C031", "nombre": "Comprador 31", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C032", "nombre": "Comprador 32", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C033", "nombre": "Comprador 33", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C034", "nombre": "Comprador 34", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C035", "nombre": "Comprador 35", "preferente": true, "activo": false, "ciudad": "Monterrey"}, {"id": "TEC-C036", "nombre": "Comprador 36", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C037", "nombre": "Comprador 37", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C038", "nombre": "Comprador 38", "preferente": false, "activo": true, "ciudad": "Apodaca"}, {"id": "TEC-C039", "nombre": "Comprador 39", "preferente": false, "activo": true, "ciudad": "Monterrey"}, {"id": "TEC-C040", "nombre": "Comprador 40", "preferente": true, "activo": true, "ciudad": "Apodaca"}];
export const producto = (id) => PRODUCTOS.find((p) => p.id === id);
export const cliente = (id) => CLIENTES.find((c) => c.id === id);
