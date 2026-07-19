namespace BarrieraMoving.Mobile.Services;

// URL base del servidor. NO es un secreto: se guarda en Preferences y se puede
// cambiar desde el campo "Servidor" de la pantalla de login (emulador vs. LAN).
public static class ApiOptions
{
    // 10.0.2.2 = el localhost del PC visto desde el emulador de Android.
    // Para un teléfono físico usa la IP LAN del PC, ej. http://192.168.1.20:5070
    private const string DefaultBaseUrl = "http://10.0.2.2:5070";
    private const string BaseUrlKey = "api_base_url";

    public static string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, DefaultBaseUrl);
        set => Preferences.Default.Set(BaseUrlKey, value.Trim().TrimEnd('/'));
    }
}
