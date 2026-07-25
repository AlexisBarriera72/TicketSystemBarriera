namespace BarrieraMoving.Server.Components.Public;

// Datos de contacto REALES de la empresa (una sola fuente para todo el sitio público).
public static class SiteInfo
{
    public const string Name = "PR Transporte Caribe";
    public const string ShortName = "Transporte Caribe";
    public const string PhoneDisplay = "(787) 598-9433";
    public const string TelHref = "tel:+17875989433";
    // WhatsApp con mensaje pre-rellenado (en PR suele convertir mejor que la llamada)
    public const string WaHref = "https://wa.me/17875989433?text=" +
        "Hola%2C%20me%20gustar%C3%ADa%20una%20cotizaci%C3%B3n%20para%20una%20mudanza.";
    public const string Email = "prtransportecaribe@gmail.com";
    public const string MailHref = "mailto:prtransportecaribe@gmail.com" +
        "?subject=Solicitud%20de%20cotizaci%C3%B3n";
    public const string Instagram = "https://www.instagram.com/mudanzasprtransportecaribe";
    public const string Facebook = "https://www.facebook.com/MUDANZASPRTRANSPORTECARIBE";
    public const string GoogleReviews = "https://share.google/1x3SfJh3Czcs5jsp4";
    public const string Locality = "Guaynabo, Puerto Rico";
    public const double RatingValue = 5.0;
    public const int ReviewCount = 6;
}
