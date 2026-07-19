# CLAUDE.md — Barriera Moving App

## What this project is
A field-service app for a moving company, built by evolving the existing
support-ticket system in this repo (and the university module PDFs) into a real
multi-client app used day to day by office staff, drivers, and clients.
The client's priority is **functional and reliable in the field**, not polished UI.

The three most important features are, in order: **chat**, **document signing +
submission**, and **clock-in/clock-out**. Sequence work so these land early and solid.

## Stack & target architecture
- Current: .NET 10, C# 14, Blazor Web App (Interactive Server render mode),
  EF Core (Code-First), SQL Server, ASP.NET Core Identity.
- Target:
  - **ASP.NET Core Web API** backend  ← holds EF Core, Identity, business logic
  - **.NET MAUI Blazor Hybrid** app   ← drivers/clients on phones (Android first)
  - Blazor web dashboard              ← office/boss (can reuse existing Razor UI)
- Refactor the current Blazor monolith so all data access lives behind the API.

## Hard rules (do NOT break these)
- The phone / MAUI client NEVER connects directly to SQL Server. It only calls the
  Web API over HTTPS. (The current app uses IDbContextFactory straight to SQL — that
  stays server-side, behind the API.)
- No secrets in git. Use .NET user secrets / environment variables for all keys
  (e-signature, Stripe, maps/geocoding, email, push). Confirm .gitignore covers them.
- Reuse existing patterns instead of rewriting from scratch:
  - `TicketComment`               → chat / messages (already threaded, timeline-ordered)
  - `Roles.cs` + `UserManagement` → role switching (relabel to Office / Driver / Client)
  - `ClosedXML` export + KPI cards → boss reports
  - status-gating in `TicketService` → "can't mark Completed until signed & office-approved"
- Keep C# 14 style: file-scoped namespaces, primary constructors, async/await.

## Domain mapping (ticket system → moving app)
- `Ticket`        → Order / Job (a move)
- `TicketComment` → Message (chat)
- Roles           → Admin (Boss), Office, Driver, + Client (external user)
- New concepts    → TimeEntry (clock in/out), SignatureRequest, DirectMessage
- Status flow     → Requested → Assigned → EnRoute → InProgress → PendingSignature → Completed

## Roadmap (build in this order)
1. ✅ Refactor into a Web API; move `TicketService` + EF Core behind it. (Done:
   co-hosted `/api/v1` + JWT in BarrieraMoving.Server; dashboard keeps cookies.)
2. MAUI Blazor Hybrid shell + login against the API.
3. Clock-in / clock-out — small first vertical slice to prove the whole stack.
4. Chat — port `TicketComment`; add client + office as participants.
5. Photo + GPS + timestamp updates posted into the chat.
6. Embedded e-signature + completion gate + email copy to client.
7. Live location / ETA — hardest; scope to active jobs only, not always-on.
8. Boss dashboard, direct messages, Excel exports.

## Reference material
- `/docs/*.pdf`            → university module PDFs = reference implementation + spec
- `/docs/project-plan.md`  → full feasibility analysis, risks, and rationale
- Domain model lives in `src/BarrieraMoving.Server` (`/Models`, `/Services`, `/Data`, `/Api`)
  plus `src/BarrieraMoving.Shared` (`/Dtos`, `/Enums`). Rename Ticket→Order is done.

## Build / run
- Layout (Phase 1 done): `src/BarrieraMoving.Server` = Blazor dashboard + `/api/v1`
  (JWT) + EF Core; `src/BarrieraMoving.Shared` = DTOs/enums for future MAUI client.
- Restore:      `dotnet restore`
- Build:        `dotnet build`
- Run:          `dotnet run --project src/BarrieraMoving.Server` (http: :5070)
- Migrations:   `dotnet ef migrations add <Name>` / `dotnet ef database update`
  (run from `src/BarrieraMoving.Server`; DB is localdb `BarrieraMovingDB`)
- Dev secrets in user-secrets (NOT in git): `Seed:AdminEmail`, `Seed:AdminPassword`,
  `Jwt:SigningKey`. Missing → app warns, skips admin seed / disables the API.
- API smoke tests: `src/BarrieraMoving.Server/BarrieraMoving.Server.http`
- Platform note: iOS builds require macOS + Xcode. Android + the Web API build on Windows.

## Environment gotchas
- No official Claude Code plugin for classic Visual Studio 2026; this repo is edited
  from VS Code / terminal. Visual Studio can still be used to build/debug in parallel.
- VS Code's C# Dev Kit build host can lock project folders on Windows (renames fail
  with "Permission denied"); deleting the project's bin/ and obj/ releases the lock.
- Verify UI/flows by running the app yourself — the agent builds and reads errors but
  does not click through the running app.

## When correcting me
If you fix a mistake I make, add a short rule to this file so it doesn't repeat.
Keep this file short (~100 lines) so it doesn't eat the context budget every session.
