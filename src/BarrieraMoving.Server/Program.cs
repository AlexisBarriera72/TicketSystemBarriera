using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using BarrieraMoving.Server.Api;
using BarrieraMoving.Server.Components;
using BarrieraMoving.Server.Components.Account;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// PDFsharp en .NET moderno no resuelve fuentes por sí solo: en Windows usamos las
// del sistema. (Si el servidor se despliega en Linux algún día, hará falta un
// IFontResolver con una fuente embebida — anotado en docs.)
if (OperatingSystem.IsWindows())
{
    PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
builder.Services.AddScoped<ISignatureService, SignatureService>();
builder.Services.AddScoped<IPaperworkService, PaperworkService>();
builder.Services.AddScoped<IDirectMessageService, DirectMessageService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<INotificationFeedService, NotificationFeedService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<IGuestLookupService, GuestLookupService>();
builder.Services.AddScoped<TokenService>();

// Correo saliente: SMTP (MailKit) si hay Email:Host en user-secrets; si no, el
// estado queda VISIBLE como NotConfigured — nunca un fallo silencioso
if (!string.IsNullOrEmpty(builder.Configuration["Email:Host"]))
{
    builder.Services.AddSingleton<IAppEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IAppEmailSender, NullEmailSender>();
}

// TODO(proveedor de firma): cuando Alexis nombre el proveedor y ponga
// ESign:ApiKey en user-secrets, registrar aquí el adaptador real en su lugar.
builder.Services.AddSingleton<ISignatureProvider, FakeSignatureProvider>();

// Push (FCM): si hay credencial de Firebase en user-secrets → sender real; si no,
// NullPushSender (estado NotConfigured VISIBLE, nunca un fallo silencioso).
if (!string.IsNullOrEmpty(builder.Configuration["Push:ServiceAccountJson"]) ||
    !string.IsNullOrEmpty(builder.Configuration["Push:ServiceAccountFile"]))
{
    builder.Services.AddSingleton<IPushSender, FirebasePushSender>();
}
else
{
    builder.Services.AddSingleton<IPushSender, NullPushSender>();
}
builder.Services.AddScoped<INotificationService, NotificationService>();

// Cookies para el dashboard web; JWT Bearer para la API (teléfonos MAUI).
// La clave de firma vive en user-secrets, nunca en appsettings.json.
var jwtKey = builder.Configuration["Jwt:SigningKey"];
var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });
authBuilder.AddIdentityCookies();

if (!string.IsNullOrEmpty(jwtKey))
{
    authBuilder.AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
        };
    });
}

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiAuth.Policy, p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser())
    .AddPolicy(ApiAuth.StaffPolicy, p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireRole(Roles.Admin, Roles.Office))
    .AddPolicy(ApiAuth.EmployeePolicy, p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireRole(Roles.Admin, Roles.Office, Roles.Driver))
    .AddPolicy(ApiAuth.PhotoPolicy, p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Azure SQL serverless se AUTO-PAUSA cuando está ocioso y tarda 30-60 s en
// despertar; el timeout por defecto (15 s) expira antes y la primera petición
// del día fallaba. Reintentos + timeouts amplios cubren ese arranque en frío.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 8,
            maxRetryDelay: TimeSpan.FromSeconds(20),
            errorNumbersToAdd: null);
        sql.CommandTimeout(120);
    }));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// La API acepta y devuelve enums como texto ("EnRoute"), no como números
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// Sonda de salud para el hosting (uptime, balanceadores). Comprueba la BD.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("db");

// LÍMITE DE PETICIONES en el formulario público de cotización: es anónimo y
// escribe en la BD, así que sin esto un script puede inundar QuoteRequests.
// 5 envíos por IP cada 10 minutos; el resto recibe 429 con mensaje amable.
builder.Services.AddRateLimiter(options =>
{
    // Limitador GLOBAL que solo actúa sobre el POST del formulario público.
    // (Blazor SSR estático publica sobre la misma URL, así que no hay endpoint
    // aparte al que colgar una policy con nombre.)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        if (!HttpMethods.IsPost(http.Request.Method)) return RateLimitPartition.GetNoLimiter("sin-limite");
        var path = http.Request.Path;
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip";

        // Envío del formulario de cotización
        if (path.StartsWithSegments("/cotizacion"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"cotizacion:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                });
        }

        // Consulta de invitados: sin límite se podrían probar códigos a lo bruto.
        if (path.StartsWithSegments("/estado"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"estado:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                });
        }

        return RateLimitPartition.GetNoLimiter("sin-limite");
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Has enviado varias solicitudes seguidas. Espera unos minutos e inténtalo de nuevo, " +
            "o llámanos directamente al (787) 598-9433.", ct);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.MapOpenApi(); // especificación en /openapi/v1.json
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// La página /not-found es solo para el dashboard web; la API devuelve sus
// códigos de estado tal cual (sin re-ejecutar hacia una página Blazor)
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    web => web.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseRateLimiter();

// Sonda de salud para el hosting. Anónima a propósito y sin detalles internos.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Descargas del dashboard (cookie de Identity, no JWT)
app.MapReportDownloads();

// API REST /api/v1 para los clientes (MAUI en Fase 2)
if (!string.IsNullOrEmpty(jwtKey))
{
    app.MapAuthApi();
    app.MapOrderApi();
    app.MapCatalogApi();
    app.MapTimeApi();
    app.MapPhotoApi();
    app.MapDocumentApi();
    app.MapPaperworkApi();
    app.MapDirectMessageApi();
    app.MapComplaintApi();
    app.MapPushApi();
    app.MapNotificationApi();
}
else
{
    app.Logger.LogWarning(
        "API deshabilitada: falta Jwt:SigningKey. Configúrala con 'dotnet user-secrets set \"Jwt:SigningKey\" ...'.");
}

// Datos iniciales: roles, admin (desde user-secrets) y tipos de mudanza.
// NUNCA debe tumbar el arranque: el sitio público de marketing no necesita base
// de datos, así que un fallo aquí (BD pausada, migración pendiente, red) tiene
// que dejar la web en pie y quedar VISIBLE en el log, no devolver un 500 a quien
// escanea el código QR del camión. /health seguirá reportando el problema.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "Fallo al sembrar datos iniciales. La aplicación arranca igualmente, pero " +
            "revisa la conexión a la base de datos y si las migraciones están aplicadas.");
    }
}

app.Run();
