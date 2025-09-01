using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    // Diccionarios base
    static Dictionary<string, List<string>> enEs = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> esEn = new(StringComparer.OrdinalIgnoreCase);

    // Índices “normalizados” (sin acentos) para tolerar entradas sin tildes
    static Dictionary<string, List<string>> enEsNorm = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> esEnNorm = new(StringComparer.OrdinalIgnoreCase);

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8; // Para tildes/ñ

        InicializarDiccionariosBase();

        while (true)
        {
            Console.WriteLine("\n==================== MENÚ ====================");
            Console.WriteLine("1. Traducir una frase");
            Console.WriteLine("2. Agregar palabras al diccionario");
            Console.WriteLine("0. Salir");
            Console.Write("\nSeleccione una opción: ");
            var op = Console.ReadLine()?.Trim();

            switch (op)
            {
                case "1":
                    TraducirFraseInteractivo();
                    break;
                case "2":
                    AgregarPalabrasInteractivo();
                    break;
                case "0":
                    Console.WriteLine("¡Hasta luego!");
                    return;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    // === Inicialización con la lista base sugerida (y algunos sinónimos) ===
    static void InicializarDiccionariosBase()
    {
        enEs["time"] = new() { "tiempo" };
        enEs["person"] = new() { "persona" };
        enEs["year"] = new() { "año" };
        enEs["way"] = new() { "camino", "forma" };
        enEs["day"] = new() { "día" };
        enEs["thing"] = new() { "cosa" };
        enEs["man"] = new() { "hombre" };
        enEs["world"] = new() { "mundo" };
        enEs["life"] = new() { "vida" };
        enEs["hand"] = new() { "mano" };
        enEs["part"] = new() { "parte" };
        enEs["child"] = new() { "niño", "niña", "niño/a" };
        enEs["eye"] = new() { "ojo" };
        enEs["woman"] = new() { "mujer" };
        enEs["place"] = new() { "lugar" };
        enEs["work"] = new() { "trabajo" };
        enEs["week"] = new() { "semana" };
        enEs["case"] = new() { "caso" };
        enEs["point"] = new() { "punto", "tema" };
        enEs["government"] = new() { "gobierno" };
        enEs["company"] = new() { "empresa", "compañía" };

        // Construir ES→EN desde EN→ES
        RebuildReverseAndNormalizedIndexes();
    }

    static void RebuildReverseAndNormalizedIndexes()
    {
        esEn.Clear();
        enEsNorm.Clear();
        esEnNorm.Clear();

        foreach (var (en, esList) in enEs)
        {
            // Índice normalizado para EN→ES
            var enKeyNorm = Normalize(en);
            if (!enEsNorm.ContainsKey(enKeyNorm)) enEsNorm[enKeyNorm] = new List<string>();
            foreach (var es in esList.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                // ES→EN directo
                if (!esEn.ContainsKey(es)) esEn[es] = en;

                // Índice normalizado para ES→EN
                var esNorm = Normalize(es);
                if (!esEnNorm.ContainsKey(esNorm)) esEnNorm[esNorm] = en;

                // Completar EN→ES normalizado
                if (!enEsNorm[enKeyNorm].Contains(es, StringComparer.OrdinalIgnoreCase))
                    enEsNorm[enKeyNorm].Add(es);
            }
        }
    }

    // === Menú: Traducir frase ===
    static void TraducirFraseInteractivo()
    {
        Console.WriteLine("\nDirección de traducción:");
        Console.WriteLine("1) Español → Inglés");
        Console.WriteLine("2) Inglés → Español");
        Console.Write("Elija 1 o 2: ");
        var dir = Console.ReadLine()?.Trim();

        Console.Write("\nIngrese la frase: ");
        var frase = Console.ReadLine() ?? string.Empty;

        bool esAen = dir == "1";
        var traducida = TraducirFrase(frase, esAen);
        Console.WriteLine("\nTraducción:");
        Console.WriteLine(traducida);
    }

    // === Menú: Agregar palabras ===
    static void AgregarPalabrasInteractivo()
    {
        Console.WriteLine("\nAgregar palabra(s) al diccionario:");
        Console.WriteLine("1) Español → Inglés");
        Console.WriteLine("2) Inglés → Español");
        Console.Write("Elija 1 o 2: ");
        var dir = Console.ReadLine()?.Trim();

        if (dir == "1")
        {
            Console.Write("Palabra en español: ");
            var es = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(es)) { Console.WriteLine("Entrada vacía."); return; }

            Console.Write("Traducción en inglés (puede ser una sola): ");
            var en = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(en)) { Console.WriteLine("Entrada vacía."); return; }

            AgregarEsEn(es, en);
            Console.WriteLine($"Añadido: {es} → {en}");
        }
        else if (dir == "2")
        {
            Console.Write("Palabra en inglés: ");
            var en = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(en)) { Console.WriteLine("Entrada vacía."); return; }

            Console.Write("Traducciones en español (separadas por coma si desea varias): ");
            var esLista = (Console.ReadLine() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                    .Select(s => s).ToList();
            if (esLista.Count == 0) { Console.WriteLine("Entrada vacía."); return; }

            AgregarEnEs(en, esLista);
            Console.WriteLine($"Añadido: {en} → {string.Join(", ", esLista)}");
        }
        else
        {
            Console.WriteLine("Opción no válida.");
        }
    }

    // === Traducción de frases preservando signos/capitalización y traduciendo solo palabras registradas ===
    static string TraducirFrase(string input, bool esToEn)
    {
        // Particiona en: palabras, números, separadores/espacios/puntuación
        var tokens = Regex.Matches(input, @"(\p{L}+|\p{N}+|[^\p{L}\p{N}\s]+|\s+)", RegexOptions.Multiline);
        var sb = new StringBuilder();

        foreach (Match m in tokens)
        {
            var t = m.Value;
            if (Regex.IsMatch(t, @"^\p{L}+$")) // solo letras (posible palabra)
            {
                var traducida = TraducirToken(t, esToEn);
                sb.Append(traducida);
            }
            else
            {
                sb.Append(t); // mantener números, espacios, puntuación tal cual
            }
        }
        return sb.ToString();
    }

    static string TraducirToken(string original, bool esToEn)
    {
        string? traducida = null;

        if (esToEn)
        {
            // ES → EN
            if (esEn.TryGetValue(original, out var en1)) traducida = en1;
            else
            {
                var norm = Normalize(original);
                if (esEnNorm.TryGetValue(norm, out var en2)) traducida = en2;
            }
        }
        else
        {
            // EN → ES (si la palabra inglesa tiene varias traducciones, tomamos la primera)
            if (enEs.TryGetValue(original, out var esList) && esList.Count > 0) traducida = esList[0];
            else
            {
                var norm = Normalize(original);
                if (enEsNorm.TryGetValue(norm, out var esList2) && esList2.Count > 0) traducida = esList2[0];
            }
        }

        if (traducida is null)
            return original; // no está en diccionario → se conserva

        // Ajustar capitalización según la palabra original
        return AplicarCapitalizacion(original, traducida);
    }

    // === Altas al diccionario (mantienen índices inversos/normalizados) ===
    static void AgregarEsEn(string es, string en)
    {
        // enEs: agregar español dentro de la lista del inglés
        if (!enEs.ContainsKey(en)) enEs[en] = new List<string>();
        if (!enEs[en].Contains(es, StringComparer.OrdinalIgnoreCase)) enEs[en].Add(es);

        // esEn directo
        if (!esEn.ContainsKey(es)) esEn[es] = en;

        // Normalizados
        var enNorm = Normalize(en);
        var esNorm = Normalize(es);

        if (!enEsNorm.ContainsKey(enNorm)) enEsNorm[enNorm] = new List<string>();
        if (!enEsNorm[enNorm].Contains(es, StringComparer.OrdinalIgnoreCase)) enEsNorm[enNorm].Add(es);

        if (!esEnNorm.ContainsKey(esNorm)) esEnNorm[esNorm] = en;
    }

    static void AgregarEnEs(string en, List<string> esLista)
    {
        if (!enEs.ContainsKey(en)) enEs[en] = new List<string>();

        foreach (var es in esLista)
        {
            if (!enEs[en].Contains(es, StringComparer.OrdinalIgnoreCase))
                enEs[en].Add(es);

            if (!esEn.ContainsKey(es))
                esEn[es] = en;

            var esNorm = Normalize(es);
            if (!esEnNorm.ContainsKey(esNorm))
                esEnNorm[esNorm] = en;
        }

        var enNorm = Normalize(en);
        if (!enEsNorm.ContainsKey(enNorm)) enEsNorm[enNorm] = new List<string>();
        foreach (var es in esLista)
        {
            if (!enEsNorm[enNorm].Contains(es, StringComparer.OrdinalIgnoreCase))
                enEsNorm[enNorm].Add(es);
        }
    }

    // === Utilidades ===
    static string Normalize(string s)
    {
        // Sin acentos, minúscula invariable para comparaciones laxas
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: s.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    static string AplicarCapitalizacion(string original, string traduccion)
    {
        // TODO: soportar ALLCAPS y Capitalización inicial
        bool allCaps = original.ToUpperInvariant() == original && original.Any(char.IsLetter);
        bool initCap = char.IsLetter(original[0]) && char.IsUpper(original[0]) &&
                       original.Skip(1).All(c => !char.IsLetter(c) || char.IsLower(c));

        if (allCaps) return traduccion.ToUpperInvariant();
        if (initCap) return CapitalizeFirst(traduccion);
        return traduccion.ToLowerInvariant(); // por consistencia
    }

    static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}
