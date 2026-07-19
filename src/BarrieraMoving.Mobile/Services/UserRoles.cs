using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

public static class UserRoles
{
    // El gating de fichaje aplica SOLO a conductores puros; Admin/Oficina nunca se bloquean
    public static bool IsDriverOnly(UserSummaryDto? user) =>
        user is not null &&
        user.Roles.Contains(RoleNames.Driver) &&
        !user.Roles.Contains(RoleNames.Admin) &&
        !user.Roles.Contains(RoleNames.Office);

    // Personal que puede fichar (los clientes no)
    public static bool IsStaff(UserSummaryDto? user) =>
        user is not null &&
        (user.Roles.Contains(RoleNames.Admin) ||
         user.Roles.Contains(RoleNames.Office) ||
         user.Roles.Contains(RoleNames.Driver));
}
