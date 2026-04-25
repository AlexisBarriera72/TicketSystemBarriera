# Ticket System Barriera

El sistema se desarrolla en **Visual Studio 2026**, **.NET 10** y **C# 14**, incorporando autenticación con **Individual Accounts** y persistencia de datos mediante **SQL Server** con **Entity Framework Core**.

---

## 📌 Project Overview

**Support Ticket System** es una plataforma Web interna para la gestión de tickets de soporte técnico, orientada a entornos organizacionales donde múltiples departamentos requieren reportar incidentes tecnológicos y recibir atención estructurada por parte del equipo de soporte.

La plataforma permite a los **empleados** registrar incidentes asignados a categorías, prioridades y estados definidos; a los **técnicos** gestionar y resolver tickets; y a los **administradores** supervisar el flujo completo con visibilidad sobre todos los usuarios y recursos del sistema.

---

## 🧰 Technology Stack

- Visual Studio 2026
- .NET 10 (Long Term Support)
- C# 14
- Blazor Web App (Unified Template)
- Interactive Server Render Mode
- ASP.NET Core Identity (Individual Accounts)
- Entity Framework Core
- SQL Server (LocalDB para desarrollo)
- Git
- GitHub
- Azure (Deployment en etapa final / producción)

---

## 🏗️ Project Architecture

Este proyecto utiliza la estructura de la plantilla unificada **Blazor Web App (Interactive Server)**, sin una arquitectura por capas.

- Estructura de **un solo proyecto**
- Ejecución **server-side** via SignalR (WebSocket)
- Sistema de identidad integrado (ASP.NET Core Identity)
- Persistencia con **EF Core** (Code-First)

### Estructura de Archivos Clave

| Archivo / Carpeta | Descripción |
|---|---|
| `Program.cs` | Registro de servicios (DI) y configuración del pipeline HTTP |
| `App.razor` | Componente raíz — contiene los tags `<html>`, `<head>` y `<body>` |
| `Routes.razor` | Despachador de rutas — decide qué página mostrar según la URL |
| `/Components/Layout` | Contiene `MainLayout.razor` y `NavMenu.razor` |
| `/Components/Pages` | Páginas de la aplicación (tickets, home, etc.) |
| `/Components/Account` | Lógica de seguridad pre-construida (Login, Register, Forgot Password) |
| `/Data` | `ApplicationDbContext.cs` y `ApplicationUser.cs` |
| `/Models` | Entidades de negocio: `Ticket`, `Category`, `TicketComment` |
| `/Enums` | `TicketStatus.cs` y `TicketPriority.cs` |

---

## 🗄️ Database

**Provider:** SQL Server (LocalDB para desarrollo)

**Connection String (desarrollo)** — ubicada en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TicketSystemBarrieraDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

> **`MultipleActiveResultSets=true`** es importante en Blazor para manejar múltiples consultas asíncronas en una misma página.

### Tablas de Identidad (ASP.NET Core Identity)

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `AspNetUserPasskeys`

### Tablas de la Aplicación

| Tabla | Descripción |
|---|---|
| `Tickets` | Entidad principal del sistema; almacena cada incidente |
| `Categories` | Catálogo de categorías para clasificar tickets |
| `TicketComments` | Comentarios asociados a un ticket |

---

## 🧩 Domain Model

### Enums — `/Enums`

**`TicketStatus.cs`**
```csharp
public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}
```

**`TicketPriority.cs`**
```csharp
public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}
```

---

### Entidades — `/Models`

**`Category.cs`** (uso de `required` — C# 14)
```csharp
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
```

**`Ticket.cs`** — El centro del sistema
```csharp
using TicketSystemBarriera.Data;
using TicketSystemBarriera.Enums;

public class Ticket
{
    public int Id { get; set; }

    public required string Title { get; set; }
    public required string Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    // Relación con Categoría
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Relación con Usuarios (Identity)
    public required string AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    public string? TechnicianId { get; set; }
    public ApplicationUser? Technician { get; set; }

    // Colección de comentarios
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}
```

**`TicketComment.cs`**
```csharp
public class TicketComment
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public required string UserId { get; set; }
    public Data.ApplicationUser? User { get; set; }
}
```

---

### `ApplicationDbContext.cs` — Integración con EF Core

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketSystemBarriera.Models;

namespace TicketSystemBarriera.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Fluent API — necesaria por múltiples relaciones FK hacia la misma tabla
        builder.Entity<Ticket>()
            .HasOne(t => t.Author)
            .WithMany()
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.Technician)
            .WithMany()
            .HasForeignKey(t => t.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

> **¿Por qué Fluent API?**  
> La tabla `Tickets` apunta dos veces a `AspNetUsers` (Author y Technician). Sin la Fluent API, EF Core no puede resolver la ambigüedad y `Update-Database` fallaría con un error de *"cycles or multiple cascade paths"*. `DeleteBehavior.Restrict` protege la integridad referencial evitando que al borrar un usuario se eliminen sus tickets accidentalmente.

---

## 👥 Roles (a implementar en Módulo 5)

- **Admin** Acceso total: usuarios, roles, todas las categorías y tickets.
- **Technician** Gestionar tickets asignados, cambiar estado, agregar comentarios.
- **Employee** Crear y consultar sus propios tickets; agregar comentarios.

---

## 🗺️ Hoja de Ruta del Desarrollo (Development Roadmap)

| Módulo | Descripción | Estado |
|---|---|---|
| 1 | Creación del Proyecto SystemLastName | ✅ Completado |
| 2 | Fundamentos de EF Core en Blazor Web App | ✅ Completado |
| 3 | Modelado del Dominio del Sistema de Tickets | ✅ Completado |
| 4 | CRUD Básico de Tickets (Creación) | ✅ Completado |
| 5 | Sistema de Roles y Seguridad (RBAC) | ✅ Completado |
| 6 | Flujo Real del Sistema de Soporte | ✅ Completado |
| 7 | Dashboard Visual y Gestión de Usuarios | ✅ Completado |
| 8 | Refactorización y Estructura del Proyecto | ✅ Completado |



---

## 📦 Getting Started

### Requisitos

- Visual Studio 2026
- .NET 10 SDK
- SQL Server LocalDB (incluido con Visual Studio)
- `dotnet-ef` (opcional, para migraciones por CLI)

### Setup

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/AlexisBarriera72/TicketSystemBarriera.git
   cd TicketSystemBarriera
   ```

2. Restaurar dependencias:
   ```bash
   dotnet restore
   ```

3. Verificar/editar la conexión a base de datos en `appsettings.json` (key: `DefaultConnection`).

4. Aplicar migraciones:

   **Package Manager Console (Visual Studio):**
   ```
   Update-Database
   ```

   **CLI:**
   ```bash
   dotnet ef database update
   ```

   > Si `dotnet ef` no está instalado:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

### Ejecutar el Proyecto

```bash
dotnet run
```

O desde Visual Studio: **Ctrl + F5**

---

## ✅ Características Implementadas

### Módulo 1 Creación del Proyecto

**Checkpoint:**
- ✅ Proyecto Blazor Web App creado con .NET 10 y C# 14
- ✅ Interactive Server Render Mode habilitado
- ✅ ASP.NET Core Identity configurado (Individual Accounts)
- ✅ Uso de `App.razor`, `MainLayout.razor` y `Home.razor`
- ✅ Personalización básica de la UI completada

---

### Módulo 2 Fundamentos de EF Core en Blazor Web App

EF Core actúa como el ORM (Mapeador Objeto-Relacional) para interactuar con la base de datos usando objetos C# en lugar de SQL. Puntos clave:

- **DbContext** Centro del acceso a datos; registrado en `Program.cs` con `AddDbContext`.
- **Modelos (Entidades)** Clases C# que mapean a tablas (enfoque Code-First).
- **Ciclo de Vida** Para Blazor Server se recomienda `IDbContextFactory` para evitar problemas de concurrencia entre componentes.
- **`MultipleActiveResultSets=true`** Necesario para consultas asíncronas paralelas en Blazor.

---

### Módulo 3 Modelado del Dominio del Sistema de Tickets

**Objetivos:**
- Diseñar las entidades que representan la lógica de los tickets y sus relaciones con los usuarios de Identity.
- Definir la lógica de datos del negocio usando C# 14.
- Aplicar la migración a SQL Server.

**Pasos realizados:**
1. Creación de la carpeta `/Enums` con `TicketStatus` y `TicketPriority`.
2. Creación de la carpeta `/Models` con `Category`, `Ticket` y `TicketComment`.
3. Integración de los `DbSet` en `ApplicationDbContext` y configuración de la Fluent API.
4. Migración aplicada:
   ```
   Add-Migration AddTicketSystemTables
   Update-Database
   ```

**Resultado:** La base de datos contiene las tablas `dbo.Tickets`, `dbo.Categories` y `dbo.TicketComments` con sus respectivas llaves foráneas apuntando a `AspNetUsers`.

---

### Módulo 4 CRUD Básico de Tickets (Creación)
Objetivo: Implementar el formulario de creación de tickets utilizando componentes de Blazor y validación de datos.
Implementado:

- Carpeta /Services + TicketService.cs (Primary Constructor C# 14)
- Métodos: GetCategoriesAsync() y CreateTicketAsync(Ticket ticket)
- Registro del servicio en Program.cs
- Seed Data de categorías (Hardware, Software, Network)
- Página CreateTicket.razor con <EditForm>, validación y InputSelect

---

### Módulo 5 Sistema de Roles y Seguridad (Extensión)
Objetivo: Implementar Control de Acceso Basado en Roles (RBAC).
Implementado:

- Archivo /Data/Roles.cs con constantes
- Seeding automático de roles + usuario admin@system.com
- Actualización completa de NavMenu.razor con <AuthorizeView>
- Filtrado seguro en TicketService.cs (GetTicketsForUserAsync)
- Logout seguro con antiforgery token

Limpieza profunda recomendada (una sola vez):

1. Borrar carpeta Migrations
2. Drop-Database
3. Add-Migration InitialMigration
4. Update-Database

---

### Módulo 6 Flujo Real del Sistema de Soporte
Objetivo: Implementar la interactividad completa del ciclo de vida del ticket (creación → asignación → cambio de estado → resolución).
Implementado:
1. Consolidación de TicketService.cs (Módulo 6)

- GetAllTicketsAsync() Lista todos los tickets (para Admin)
- UpdateTicketAsync(Ticket)
- GetTicketByIdAsync(int)
- GetTechniciansAsync() Lista solo usuarios con rol Technician
- UpdateTicketStatusAsync(int ticketId, TicketStatus, string? technicianId) Asignación + cambio de estado en una sola operación
- GetUsersByRoleAsync(string roleName)

2. Dashboard Administrativo

- Nueva carpeta /Components/Pages/Admin
- Dashboard.razor (/admin/dashboard)
- Tabla completa de todos los tickets con botón Manage

3. Gestión Individual de Tickets

- ManageTicket.razor (/tickets/manage/{ticketId:int})
- Permisos: Admin + Technician ([Authorize(Roles = "Admin,Technician")])
- Sección Assign Technician (solo Admin)
- Sección Update Status (Start Work / Mark Resolved)

4. Acceso para Técnicos y Empleados

- Actualización de NavMenu.razor:
- Enlace My Tickets visible para todos los usuarios autenticados (/tickets/my-list)
- Admin Panel sigue restringido solo a Admin

- Nueva página MyTickets.razor:
 - Usa GetTicketsForUserAsync() para filtrado inteligente según rol
 - Botón Work on it solo visible para Technician/Admin


Flujo completo del sistema:

1. Empleado → Crea ticket (/tickets/create)
2. Admin → Ve todos los tickets en Dashboard → Asigna técnico
3. Técnico → Ve sus tickets en My Tickets → Gestiona estado
4. Empleado → Puede dar seguimiento en My Tickets

### Módulo 7: Dashboard Visual y Gestión de Usuarios
Objetivo: Crear un centro de control profesional con KPIs y gestión de usuarios.
Implementado:

TicketService.cs (actualizado):
 * GetTicketStatsAsync() → Devuelve estadísticas agregadas (Total, Open, InProgress, Resolved, Urgent)
 * GetAllUsersAsync() → Lista todos los usuarios
 * Métodos existentes de módulos anteriores consolidados

Admin Dashboard (/admin/dashboard):

 * Tarjetas visuales (KPI Cards) con colores semánticos:
  * Total Tickets
  * Open / In Progress
  * Resolved
  * Urgent Cases

 * Tabla de todos los tickets con acciones
 * Uso de Bootstrap cards y iconos

User Management (/admin/users):

 * Nueva página para que el Admin gestione roles
 * Lista de todos los usuarios registrados
 * Botones para asignar rol Technician o Employee con un clic
 * Integración con UserManager<ApplicationUser>

Mejoras visuales:

 * Uso intensivo de Bootstrap 5
 * Iconos de Bootstrap Icons
 * Diseño responsivo y profesional

---

### Módulo 8: Refactorización y Estructura del Proyecto
Objetivo: Organizar el código, corregir namespaces y mejorar mantenibilidad.
Implementado:

 * Estructura clara de carpetas:
  * /Components/Pages/Admin
  * /Components/Pages/Tickets
  * /Data, /Models, /Enums, /Services

 * Centralización de namespaces en Components/_Imports.razor:
  * Todos los @using comunes aplicados automáticamente a los componentes

 * Limpieza de código:
  * Eliminación de páginas por defecto (Counter, Weather)
  * Remoción de using repetidos en archivos .razor
  * Uso recomendado de File-Scoped Namespaces (C# 14)

 * Mejora general en organización y legibilidad del proyecto

Resultado final: Código más limpio, mantenible y profesional.

---

## 📦 NuGet Packages

| Paquete | Versión | Uso |
|---|---|---|
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | 10.0.3 | Middleware de error de migración en desarrollo |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.2 | Integración Identity + EF Core |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.3 | Proveedor SQL Server para EF Core |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.3 | Herramientas CLI (`dotnet ef migrations add`, etc.) |

---

## 📄 Licencia

Desarrollo por Alexis Y. Barriera Pacheco en el curso de CCOM4019-H30.
