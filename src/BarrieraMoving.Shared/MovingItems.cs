namespace BarrieraMoving.Shared;

// Catálogo de artículos de mudanza. FUENTE ÚNICA: lo usan el formulario público
// de cotización y el de nueva orden del portal, así que la oficina siempre lee los
// mismos nombres que marcó el cliente (nada de texto libre que luego no cuadra).
//
// El tamaño es orientativo para el equipo que carga: "Grande" no es solo volumen,
// es "hacen falta dos personas o equipo especial".
public static class MovingItems
{
    public const string Small = "Pequeño";
    public const string Medium = "Mediano";
    public const string Large = "Grande";
    public const string Other = "Otro";

    // Separador con el que se guardan los artículos escogidos en una sola columna.
    // Se elige "|" porque no aparece en ningún nombre del catálogo.
    public const char Separator = '|';

    public static readonly IReadOnlyList<ItemCategory> Categories =
    [
        new(Small, "Lo que carga una persona sin ayuda",
        [
            "Caja pequeña", "Caja mediana", "Maleta", "Bolsas de ropa",
            "Lámpara de mesa", "Microondas", "Abanico / ventilador", "Silla",
            "Mesa de noche", "Televisor pequeño (hasta 32\")", "Computadora / monitor",
            "Impresora", "Espejo pequeño", "Cuadro / pintura", "Bicicleta",
            "Coche de bebé", "Aspiradora", "Utensilios de cocina",
        ]),

        new(Medium, "Entre dos personas, o pesa pero cabe por la puerta",
        [
            "Sofá de 2 plazas", "Butaca / sillón reclinable", "Mesa de comedor",
            "Juego de 4 sillas", "Escritorio", "Cómoda / gavetero",
            "Ropero pequeño", "Televisor grande (43\" a 65\")",
            "Aire acondicionado de ventana", "Estufa", "Lavadora", "Secadora",
            "Lavaplatos", "Colchón individual (twin)", "Colchón matrimonial (full/queen)",
            "Librero / estantería", "Mesa de centro", "Cuna", "Bicicleta estática",
        ]),

        new(Large, "Pesado o voluminoso: puede necesitar equipo o más personal",
        [
            "Nevera", "Congelador (freezer)", "Sofá seccional",
            "Juego de sala completo", "Juego de comedor completo",
            "Cama king con base", "Cama queen con base", "Ropero / armario grande",
            "Piano vertical", "Caja fuerte", "Trotadora / caminadora",
            "Equipo de gimnasio", "Vitrina / gabinete grande", "Escritorio ejecutivo",
            "Juego de patio (mesa y sillas)", "Tanque de agua",
        ]),

        new(Other, "Requiere cuidado especial: dilo también en los detalles",
        [
            "Plantas", "Artículos frágiles (cristalería, vajilla)",
            "Instrumentos musicales", "Herramientas / taller",
            "Cajas de documentos / archivos", "Equipo médico",
            "Acuario / pecera", "Obras de arte", "Motora / scooter",
            "Generador eléctrico", "Materiales de construcción",
            "Mercancía / inventario comercial", "Mascotas (jaulas o transportadoras)",
        ]),
    ];

    // --- Piso ---
    // Para una mudanza el piso NO es un dato menor: un tercer piso sin ascensor
    // cambia el personal, el tiempo y el precio. Por eso se pregunta aparte y no
    // se deja enterrado en "detalles".
    public static readonly IReadOnlyList<string> Floors =
    [
        "Planta baja / nivel de calle",
        "2do piso",
        "3er piso",
        "4to piso",
        "5to piso o más",
        "Varios niveles (casa de dos plantas)",
    ];

    public static readonly IReadOnlyList<string> Elevators =
    [
        "Con ascensor",
        "Sin ascensor",
        "No estoy seguro",
    ];

    // Junta piso y ascensor en una sola línea legible para la oficina.
    public static string PackFloor(string? floor, string? elevator)
    {
        var f = string.IsNullOrWhiteSpace(floor) || !Floors.Contains(floor) ? null : floor;
        var e = string.IsNullOrWhiteSpace(elevator) || !Elevators.Contains(elevator) ? null : elevator;
        if (f is null && e is null) return string.Empty;
        if (f is null) return e!;
        if (e is null) return f;
        return $"{f} · {e}";
    }

    // Todos los nombres válidos, para descartar cualquier cosa que llegue por POST
    // y no esté en el catálogo (el formulario es público y anónimo).
    private static readonly HashSet<string> Valid =
        new(Categories.SelectMany(c => c.Items), StringComparer.Ordinal);

    public static bool IsKnown(string item) => Valid.Contains(item);

    // Une lo escogido en el texto que se guarda en la BD, descartando lo desconocido
    // y lo repetido, y respetando el orden del catálogo (no el de llegada).
    public static string Pack(IEnumerable<string> selected)
    {
        var set = new HashSet<string>(selected.Where(IsKnown), StringComparer.Ordinal);
        if (set.Count == 0) return string.Empty;
        var ordered = Categories.SelectMany(c => c.Items).Where(set.Contains);
        return string.Join(Separator, ordered);
    }

    public static IReadOnlyList<string> Unpack(string? packed) =>
        string.IsNullOrWhiteSpace(packed)
            ? []
            : packed.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Agrupa lo guardado por categoría, para que la oficina lo lea ordenado.
    public static IReadOnlyList<(string Category, IReadOnlyList<string> Items)> GroupPacked(string? packed)
    {
        var chosen = new HashSet<string>(Unpack(packed), StringComparer.Ordinal);
        if (chosen.Count == 0) return [];

        var groups = new List<(string, IReadOnlyList<string>)>();
        foreach (var cat in Categories)
        {
            var hit = cat.Items.Where(chosen.Contains).ToList();
            if (hit.Count > 0) groups.Add((cat.Name, hit));
        }
        return groups;
    }

    public sealed record ItemCategory(string Name, string Hint, IReadOnlyList<string> Items)
    {
        // Id apto para atributos HTML (name/id de los checkbox y del <details>)
        public string Slug => Name switch
        {
            Small  => "peq",
            Medium => "med",
            Large  => "gra",
            _      => "otr",
        };
    }
}
