using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Estado de fichaje cacheado en memoria: lo consulta el gating de conductores
// sin pegarle a la API en cada navegación. La pantalla de fichaje lo actualiza.
public class ClockState
{
    private bool _loaded;

    public TimeEntryDto? OpenEntry { get; private set; }

    public void Set(TimeEntryDto? entry)
    {
        OpenEntry = entry;
        _loaded = true;
    }

    public void Reset()
    {
        OpenEntry = null;
        _loaded = false;
    }

    public async Task EnsureLoadedAsync(ApiClient api)
    {
        if (_loaded) return;
        OpenEntry = await api.GetCurrentTimeEntryAsync();
        _loaded = true;
    }
}
