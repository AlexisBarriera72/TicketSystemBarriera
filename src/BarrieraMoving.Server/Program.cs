using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
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
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

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
}
else
{
    app.Logger.LogWarning(
        "API deshabilitada: falta Jwt:SigningKey. Configúrala con 'dotnet user-secrets set \"Jwt:SigningKey\" ...'.");
}

// Datos iniciales: roles, admin (desde user-secrets) y tipos de mudanza
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

app.Run();
