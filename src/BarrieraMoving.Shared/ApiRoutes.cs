namespace BarrieraMoving.Shared;

// Rutas de la API compartidas entre el servidor y los clientes (web, MAUI)
public static class ApiRoutes
{
    public const string Base = "/api/v1";
    public const string Auth = $"{Base}/auth";
    public const string Orders = $"{Base}/orders";
    public const string Categories = $"{Base}/categories";
    public const string Users = $"{Base}/users";
    public const string Reports = $"{Base}/reports";
    public const string Time = $"{Base}/time";
    public const string Photos = $"{Base}/photos";
    public const string Documents = $"{Base}/documents";
    public const string Paperwork = $"{Base}/paperwork";
    public const string DirectMessages = $"{Base}/dm";
    public const string Complaints = $"{Base}/complaints";
    public const string Push = $"{Base}/push";
    public const string EsignWebhook = $"{Base}/webhooks/esign";
}
