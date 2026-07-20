namespace BarrieraMoving.Mobile.Services;

// Una sola lectura de GPS con timeout corto. La ubicación NUNCA bloquea nada:
// permiso denegado, GPS apagado o timeout → null y la acción sigue adelante.
public static class LocationHelper
{
    public static async Task<Location?> TryGetLocationAsync()
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
                if (status != PermissionStatus.Granted) return null;

                return await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            });
        }
        catch (Exception)
        {
            return null;
        }
    }
}
