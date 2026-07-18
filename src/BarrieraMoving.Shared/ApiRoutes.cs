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
}
