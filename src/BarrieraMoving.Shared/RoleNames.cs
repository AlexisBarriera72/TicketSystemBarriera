namespace BarrieraMoving.Shared;

// Nombres de rol tal como viajan en los claims del JWT. El servidor tiene su
// propia clase Roles (Server.Data); estos son para que los clientes (MAUI)
// puedan razonar sobre roles sin referenciar al servidor.
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Office = "Office";
    public const string Driver = "Driver";
    public const string Client = "Client";

    // Etiqueta visible del rol en el chat (petición explícita del cliente:
    // que se sepa siempre con quién se habla)
    public static string DisplayLabel(string? role) => role switch
    {
        Admin => "Jefe",
        Office => "Oficina",
        Driver => "Conductor",
        Client => "Cliente",
        _ => ""
    };
}
