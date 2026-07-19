namespace BarrieraMoving.Server.Api;

// Nombres de las policies de autorización de la API (esquema JWT Bearer)
public static class ApiAuth
{
    public const string Policy = "ApiJwt";
    public const string StaffPolicy = "ApiJwtStaff"; // Admin u Oficina
}
