namespace BarrieraMoving.Mobile.Services;

// Android 16 fuerza edge-to-edge (target API 35+): el WebView se dibuja debajo de la
// barra de estado/cámara y los toques en esa franja se los queda el sistema.
// En vez de tocar la jerarquía nativa de MAUI (rompe sus fragments), leemos el inset
// una vez y el layout Blazor lo aplica como padding-top del header.
public static class SafeInsets
{
    // Altura de la barra de estado + cámara en píxeles CSS (dp). 0 si no aplica.
    public static double TopCssPx()
    {
#if ANDROID
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        var rootInsets = decor?.RootWindowInsets;
        if (decor is null || rootInsets is null) return 0;

        var compat = AndroidX.Core.View.WindowInsetsCompat.ToWindowInsetsCompat(rootInsets, decor);
        var bars = compat.GetInsets(
            AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars() |
            AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout());

        var density = DeviceDisplay.MainDisplayInfo.Density;
        return density > 0 ? bars.Top / density : bars.Top;
#else
        return 0; // iOS: env(safe-area-inset-top) del CSS ya lo cubre
#endif
    }

    // Barra de gestos inferior, misma idea
    public static double BottomCssPx()
    {
#if ANDROID
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        var rootInsets = decor?.RootWindowInsets;
        if (decor is null || rootInsets is null) return 0;

        var compat = AndroidX.Core.View.WindowInsetsCompat.ToWindowInsetsCompat(rootInsets, decor);
        var bars = compat.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());

        var density = DeviceDisplay.MainDisplayInfo.Density;
        return density > 0 ? bars.Bottom / density : bars.Bottom;
#else
        return 0;
#endif
    }
}
