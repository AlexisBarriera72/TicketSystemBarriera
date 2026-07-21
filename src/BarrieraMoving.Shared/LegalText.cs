namespace BarrieraMoving.Shared;

// Texto de los términos de servicio. PLACEHOLDER: el texto legal real lo aporta
// el cliente (como las etiquetas de papeleo o el proveedor de firma). Está aquí,
// en Shared, para que web y móvil muestren EXACTAMENTE lo mismo con una sola edición.
public static class LegalText
{
    public const string TermsVersion = "borrador-2026-07";

    public const string TermsOfService = """
        TÉRMINOS DE SERVICIO — BARRIERA MOVING (BORRADOR)

        Este texto es un marcador de posición. Sustitúyelo por los términos reales
        revisados por el cliente antes de usar la app en producción.

        1. Objeto
        Barriera Moving presta servicios de mudanza y transporte de bienes. Al usar
        esta aplicación, el cliente acepta los presentes términos.

        2. Reservas y presupuestos
        Los presupuestos son estimaciones. El precio final puede variar según el
        volumen real, la distancia y servicios adicionales acordados en la orden.

        3. Responsabilidad sobre los bienes
        El estado de los bienes se documenta en el inventario y en las fotos de la
        orden. El cliente firma la conformidad al finalizar el servicio; cualquier
        incidencia debe reflejarse ANTES de firmar, en el chat de la orden.

        4. Datos personales
        Se tratan los datos necesarios para prestar el servicio (contacto, direcciones,
        firma, ubicación de los fichajes). No se ceden a terceros salvo obligación legal.

        5. Reclamaciones
        Las reclamaciones se gestionan desde la sección de Atención al Cliente de la
        aplicación y se responden en un plazo razonable.

        6. Modificaciones
        Barriera Moving puede actualizar estos términos; la versión vigente se muestra
        siempre en la aplicación.
        """;
}
