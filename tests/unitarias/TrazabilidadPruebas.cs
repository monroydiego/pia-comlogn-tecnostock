using System.Text.RegularExpressions;

namespace TecnoStock.Pruebas.Unitarias;

/// <summary>
/// Mantiene viva la matriz de trazabilidad: cada nombre de prueba que cita existe y cada escenario Gherkin
/// de las historias implementadas tiene al menos una prueba. Lee docs/ y tests/ desde la raíz del repositorio.
/// </summary>
public class TrazabilidadPruebas
{
    private static readonly string[] HistoriasDocumentadas = ["US15", "US18", "US19"];
    private static readonly string Raiz = EncontrarRaiz();

    private static string EncontrarRaiz()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "TecnoStock.sln"))) carpeta = carpeta.Parent;
        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró TecnoStock.sln subiendo desde el directorio de las pruebas.");
    }

    private static string Leer(params string[] ruta) => File.ReadAllText(Path.Combine([Raiz, .. ruta]), System.Text.Encoding.UTF8);

    /// <summary>Clases y métodos de prueba ([Fact] o [Theory]) que existen en tests/.</summary>
    private static HashSet<string> NombresDePrueba()
    {
        var nombres = new HashSet<string>();
        var separador = Path.DirectorySeparatorChar;
        foreach (var archivo in Directory.EnumerateFiles(Path.Combine(Raiz, "tests"), "*.cs", SearchOption.AllDirectories))
        {
            if (archivo.Contains($"{separador}obj{separador}") || archivo.Contains($"{separador}bin{separador}")) continue;
            var codigo = File.ReadAllText(archivo);
            foreach (Match m in Regex.Matches(codigo, @"\bclass\s+(\w+)")) nombres.Add(m.Groups[1].Value);
            foreach (Match m in Regex.Matches(codigo, @"\[(?:Fact|Theory)\][\s\S]*?public\s+(?:async\s+Task|void)\s+(\w+)\s*\(")) nombres.Add(m.Groups[1].Value);
        }
        return nombres;
    }

    private static List<(string Historia, string Escenario)> EscenariosDeLasHistorias()
    {
        var texto = Leer("docs", "analisis", "historias_de_usuario.md");
        var seccion = texto[texto.IndexOf("## 2. Criterios de aceptación", StringComparison.Ordinal)..];
        var resultado = new List<(string, string)>();
        var historia = "";
        foreach (var linea in seccion.Split('\n').Select(l => l.TrimEnd('\r')))
        {
            var h = Regex.Match(linea, @"^### (US\d+)");
            if (h.Success) historia = h.Groups[1].Value;
            var e = Regex.Match(linea, @"^Escenario: (.+)$");
            if (e.Success) resultado.Add((historia, e.Groups[1].Value.Trim()));
        }
        return resultado;
    }

    /// <summary>Filas de la sección 3 de la matriz: historia, escenario y las celdas de pruebas.</summary>
    private static Dictionary<(string, string), string[]> FilasDeEscenarios()
    {
        var matriz = Leer("docs", "architecture", "matriz_trazabilidad.md");
        var seccion = matriz[matriz.IndexOf("## 3. Escenarios Gherkin", StringComparison.Ordinal)..];
        seccion = seccion[..seccion.IndexOf("## 4.", StringComparison.Ordinal)];
        var filas = new Dictionary<(string, string), string[]>();
        foreach (var linea in seccion.Split('\n').Where(l => Regex.IsMatch(l, @"^\| US\d+ \|")))
        {
            var celdas = linea.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            filas[(celdas[0], celdas[1])] = [celdas[2], celdas[3]];
        }
        return filas;
    }

    [Fact]
    public void Todo_nombre_de_prueba_citado_en_la_matriz_existe()
    {
        var matriz = Leer("docs", "architecture", "matriz_trazabilidad.md");
        var citados = Regex.Matches(matriz, @"`([A-Za-z0-9_]+)`").Select(m => m.Groups[1].Value).ToHashSet();
        var existentes = NombresDePrueba();

        Assert.True(citados.Count > 100, "La matriz debería citar las pruebas reales.");
        var inexistentes = citados.Where(c => !existentes.Contains(c)).ToList();
        Assert.True(inexistentes.Count == 0, $"La matriz cita pruebas que no existen: {string.Join(", ", inexistentes)}");
    }

    [Fact]
    public void Cada_escenario_Gherkin_de_las_historias_aparece_en_la_matriz()
    {
        var escenarios = EscenariosDeLasHistorias();
        var filas = FilasDeEscenarios();

        Assert.Equal(44, escenarios.Count);
        var sinFila = escenarios.Where(e => !filas.ContainsKey(e)).ToList();
        Assert.True(sinFila.Count == 0, $"Escenarios sin fila en la matriz: {string.Join("; ", sinFila)}");
        var sobrantes = filas.Keys.Where(k => !escenarios.Contains(k)).ToList();
        Assert.True(sobrantes.Count == 0, $"La matriz tiene escenarios que no están en las historias: {string.Join("; ", sobrantes)}");
    }

    [Fact]
    public void Cada_escenario_de_una_historia_implementada_tiene_al_menos_una_prueba()
    {
        var filas = FilasDeEscenarios();

        var sinPruebas = filas.Where(f => !f.Value.Any(celda => Regex.IsMatch(celda, @"`[A-Za-z0-9_]+`")))
            .Select(f => f.Key.Item1).Distinct().OrderBy(h => h).ToArray();

        Assert.Equal(HistoriasDocumentadas, sinPruebas);
        // Y toda historia implementada tiene al menos una prueba unitaria.
        var sinUnitaria = filas.Where(f => !HistoriasDocumentadas.Contains(f.Key.Item1) && !Regex.IsMatch(f.Value[0], @"`[A-Za-z0-9_]+`")).ToList();
        Assert.True(sinUnitaria.Count == 0, $"Escenarios implementados sin prueba unitaria: {string.Join("; ", sinUnitaria.Select(f => f.Key))}");
    }
}
