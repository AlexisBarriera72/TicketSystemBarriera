using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;

namespace BarrieraMoving.Server.Api;

// Sirve las fotos que un visitante anónimo adjuntó a su cotización.
//
// Estas fotos enseñan el INTERIOR de la casa de alguien que todavía no es cliente:
// se guardan fuera de wwwroot y solo salen por aquí, exigiendo sesión de oficina o
// admin en CADA petición. Adivinar la URL no sirve de nada.
//
// A diferencia de las fotos de una orden (PhotoEndpoints), aquí NO hace falta JWT:
// solo las mira el personal desde el panel web, con su cookie de sesión.
public static class QuotePhotoEndpoints
{
    public static IEndpointRouteBuilder MapQuotePhotoApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/quote-photos/{id:int}",
                (int id, IDbContextFactory<ApplicationDbContext> dbf, IPhotoStorage storage) =>
                    ServeAsync(id, thumb: false, dbf, storage))
            .RequireAuthorization(p => p.RequireRole(Roles.Admin, Roles.Office));

        app.MapGet("/quote-photos/{id:int}/thumb",
                (int id, IDbContextFactory<ApplicationDbContext> dbf, IPhotoStorage storage) =>
                    ServeAsync(id, thumb: true, dbf, storage))
            .RequireAuthorization(p => p.RequireRole(Roles.Admin, Roles.Office));

        return app;
    }

    private static async Task<IResult> ServeAsync(int id, bool thumb,
        IDbContextFactory<ApplicationDbContext> dbf, IPhotoStorage storage)
    {
        await using var db = await dbf.CreateDbContextAsync();
        var photo = await db.QuotePhotos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (photo is null) return Results.NotFound();

        var key = thumb ? photo.ThumbPath : photo.FilePath;
        var stream = storage.Open(key);
        return stream is null ? Results.NotFound() : Results.File(stream, "image/jpeg");
    }
}
