namespace TecnoStock.Api.Infraestructura;

/// <summary>
/// Conveniencia para desarrollo local: si existe un archivo .env (que no se sube al repositorio) lo busca desde la carpeta
/// actual hacia arriba y carga sus variables. Las variables de entorno reales siempre tienen prioridad (12-Factor);
/// en Docker no hay .env dentro de la imagen, así que no tiene efecto.
/// </summary>
public static class ArchivoEnv
{
    public static void CargarSiExiste()
    {
        for (var carpeta = new DirectoryInfo(Directory.GetCurrentDirectory()); carpeta is not null; carpeta = carpeta.Parent)
        {
            var ruta = Path.Combine(carpeta.FullName, ".env");
            if (!File.Exists(ruta)) continue;
            foreach (var linea in File.ReadAllLines(ruta))
            {
                var texto = linea.Trim();
                if (texto.Length == 0 || texto.StartsWith('#')) continue;
                var separador = texto.IndexOf('=');
                if (separador <= 0) continue;
                var nombre = texto[..separador].Trim();
                if (Environment.GetEnvironmentVariable(nombre) is null)
                    Environment.SetEnvironmentVariable(nombre, texto[(separador + 1)..].Trim());
            }
            return;
        }
    }
}
