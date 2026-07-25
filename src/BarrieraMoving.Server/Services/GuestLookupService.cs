using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;

namespace BarrieraMoving.Server.Services;

public sealed class GuestLookupService(IDbContextFactory<ApplicationDbContext> dbFactory)
    : IGuestLookupService
{
    // Sin 0/O ni 1/I/L: el cliente va a leer este código por teléfono o copiarlo
    // de una pantalla, y esas parejas se confunden constantemente.
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int CodeLength = 6;
    public const string Prefix = "TC-";

    public async Task<string> GenerateReferenceCodeAsync()
    {
        using var db = dbFactory.CreateDbContext();

        // 31^6 ≈ 887 millones de combinaciones; el reintento es por si acaso.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var chars = new char[CodeLength];
            for (var i = 0; i < CodeLength; i++)
            {
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
            var code = Prefix + new string(chars);

            if (!await db.QuoteRequests.AnyAsync(q => q.ReferenceCode == code))
            {
                return code;
            }
        }
        // Improbable; mejor un código más largo que devolver uno repetido
        return Prefix + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }

    public async Task<GuestOrderStatus?> LookupAsync(string referenceCode, string phone)
    {
        var code = Normalize(referenceCode);
        var digits = IGuestLookupService.NormalizePhone(phone);
        if (code.Length < 6 || digits.Length < 7) return null;

        using var db = dbFactory.CreateDbContext();
        var quote = await db.QuoteRequests
            .Include(q => q.ConvertedOrder!).ThenInclude(o => o.AssignedDriver)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.ReferenceCode == code);

        if (quote is null) return null;

        // El teléfono se compara por sus últimos 10 dígitos: así "787-555-1234",
        // "(787) 555 1234" y "+1 787 555 1234" son el mismo número.
        if (!TailMatches(IGuestLookupService.NormalizePhone(quote.Phone), digits)) return null;

        var order = quote.ConvertedOrder;
        var driver = order?.AssignedDriver?.DisplayName ?? order?.AssignedDriver?.Email;
        var firstName = string.IsNullOrWhiteSpace(driver) ? null : driver.Split(' ', '@')[0];

        return new GuestOrderStatus(
            quote.ReferenceCode!, quote.Name, quote.CreatedAtUtc,
            quote.ServiceType, quote.OriginZone, quote.DestinationZone,
            Converted: order is not null,
            OrderId: order?.Id,
            Status: order?.Status,
            DriverFirstName: firstName);
    }

    private static bool TailMatches(string a, string b)
    {
        if (a.Length < 7 || b.Length < 7) return false;
        var n = Math.Min(10, Math.Min(a.Length, b.Length));
        return a[^n..] == b[^n..];
    }

    // Acepta "tc-4f7k2m", "4F7K2M" o con espacios: siempre se guarda en mayúsculas.
    private static string Normalize(string? raw)
    {
        var s = new string((raw ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (s.StartsWith("TC", StringComparison.Ordinal)) s = s[2..];
        return Prefix + s;
    }
}
