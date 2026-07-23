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
2. ✅ MAUI Blazor Hybrid shell + login against the API. (Done:
   src/BarrieraMoving.Mobile, Android-only; ver docs/mobile-dev.md.)
3. ✅ Clock-in / clock-out. (Done: /api/v1/time, hora SIEMPRE del servidor,
   una jornada abierta por empleado (índice único filtrado), olvido de salida →
   auto-cierre a Time:MaxShiftHours con flag AutoClosed; gating solo Drivers;
   ubicación opcional que nunca bloquea; dashboard + hoja Excel "Fichajes".
   Cola offline diferida a propósito — se hará junto a la cola de fotos (fase 5).)
4. ✅ Chat. (Done: móvil /orders/{id}/chat + web ManageOrder unificados;
   rol del remitente CONGELADO al enviar (Message.SenderRole) + flag IsSystem
   real; polling delta 5s (afterId) en ambos — SignalR descartado a propósito
   hasta la fase 7; paginado últimos 50 + beforeId; cliente participa desde
   su orden; sin lector de "leído" (llegará con las notificaciones push).)
5. ✅ Photo + GPS + timestamp updates into the chat + cola offline compartida.
   (Done: toda foto se RE-CODIFICA en el servidor — SkiaSharp, EXIF eliminado
   por construcción, 1600px/q75 + miniatura — disco local tras IPhotoStorage,
   servida SOLO por endpoint con ACL de la orden (cookie o JWT). Cola SQLite
   única (mensajes/fotos/fichajes): sobrevive reinicios, backoff 30s→10m,
   idempotencia por GUID + índices únicos filtrados, nada se descarta solo.
   Hora del dispositivo = CapturedAtUtc (metadato); la de nómina SIEMPRE la
   pone el servidor — badge "Diferido" + columnas en Excel cuando difieren.)
6. ✅ E-firma HÍBRIDA + gate de cierre + copia por email. (Done: SignatureDocument
   inmutable (sin endpoints de borrado), PDF SIEMPRE espejado en nuestro disco
   con hash SHA-256; ceremonia OFFLINE (canvas + nombre + GPS, PDF PDFsharp
   marcado PROVISIONAL) que viaja por la cola de la fase 5; gate en
   UpdateOrderStatusAsync: Completed EXIGE doc aprobado por oficina y no se
   salta ni con bypass; webhook HMAC-verificado (los forjados se rechazan);
   revisión de oficina en web con motivo de rechazo accionable.
   PENDIENTE de Alexis: nombrar el proveedor real (adaptador = 1 clase +
   ESign:ApiKey) y credenciales SMTP (Email:Host…) — mientras tanto el correo
   queda en estado VISIBLE "NotConfigured", nunca falla en silencio.)
7. ⏭️ Live location / ETA — OMITIDO por decisión: el "avisar que estamos cerca"
   ya lo cubren las fotos+GPS del chat (fase 5). No construir tracking.
8A. ✅ Papeleo obligatorio + DMs + export Excel completo. (Done en
   phase8a-paperwork-push-dm: papeleo = slots configurables (Paperwork:Slots)
   PLEGADOS en el paquete de firma de la fase 6 — ensamblar-y-luego-firmar,
   nada se añade tras firmar, rechazo de un papel invalida la firma en cascada,
   gate doble en UpdateOrderStatusAsync; DMs = entidad NUEVA (no Message) con
   ACL por pertenencia al conjunto — el cliente NUNCA alcanza un hilo del
   personal; Excel = hojas Órdenes/Fichajes/Empleados/Documentos + estado de
   documentos en el dashboard. Notificaciones push (FCM) APLAZADAS: falta que
   Alexis cree el proyecto Firebase. APNs encajará luego sin reestructurar.)
8B. ✅ Términos + reclamaciones + pulido. (Done en phase8b-terms-complaints-polish:
   /terms público (texto placeholder en Shared/LegalText, web+móvil); Complaint
   = registro del cliente (ve solo las suyas) + respuesta/resolución de oficina
   (ACL verificado por curl); home rol-consciente con accesos directos. Las
   pantallas de cuenta de Identity siguen en inglés a propósito — estándar.)
9. ✅ Notificaciones push (FCM) — CÓDIGO COMPLETO (proyecto Firebase
   "barrieramoving", google-services.json en Platforms/Android ya en git — es
   config de cliente, va en el APK). DeviceToken + IPushSender/FirebasePushSender
   (estado NotConfigured VISIBLE como el email) + INotificationService que
   dispara en 4 eventos (chat de orden, DM, respuesta a reclamación, cambio de
   estado — este desde OrderService, cubre API y dashboard). Móvil: permiso
   Android 13+, token FCM registrado en /api/v1/push (VERIFICADO: fila en
   DeviceTokens tras login en el emulador). Tokens muertos se purgan solos.
   PENDIENTE de Alexis para que EMITA de verdad: cargar la clave de cuenta de
   servicio en user-secrets del server →
   `dotnet user-secrets set "Push:ServiceAccountJson" "$(cat clave.json)"`
   (la clave NUNCA en git; el .gitignore ya bloquea *firebase-adminsdk*.json).
   iOS/APNs aplazado: encaja en IPushSender/IPushRegistrar sin reestructurar.

## Reference material
- `/docs/*.pdf`            → university module PDFs = reference implementation + spec
- `/docs/project-plan.md`  → full feasibility analysis, risks, and rationale
- Domain model lives in `src/BarrieraMoving.Server` (`/Models`, `/Services`, `/Data`, `/Api`)
  plus `src/BarrieraMoving.Shared` (`/Dtos`, `/Enums`). Rename Ticket→Order is done.

## Build / run
- Layout: `src/BarrieraMoving.Server` = Blazor dashboard + `/api/v1` (JWT) + EF Core;
  `src/BarrieraMoving.Shared` = DTOs/enums; `src/BarrieraMoving.Mobile` = MAUI Blazor
  Hybrid (Android; tokens en SecureStorage, ver docs/mobile-dev.md para emulador/LAN).
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
- Source files are UTF-8 WITHOUT a BOM; csc then decodes them as Windows-1252 and
  corrupts accented / ·  / — string literals (mojibake). Fixed once via
  `<CodePage>65001</CodePage>` in every csproj — keep it. New tools that write .cs
  without a BOM rely on it; don't remove it.
- No official Claude Code plugin for classic Visual Studio 2026; this repo is edited
  from VS Code / terminal. Visual Studio can still be used to build/debug in parallel.
- VS Code's C# Dev Kit build host can lock project folders on Windows (renames fail
  with "Permission denied"); deleting the project's bin/ and obj/ releases the lock.
- MAUI Android: tras añadir/quitar archivos en Platforms/Android/Resources haz clean
  rebuild (borra bin/obj del Mobile) — la caché de recursos desfasada crashea al abrir
  con "No view found for id". Android 16 fuerza edge-to-edge: los insets se aplican
  desde Blazor (Services/SafeInsets), NUNCA con un insets-listener nativo (rompe MAUI).
- Verify UI/flows by running the app yourself — the agent builds and reads errors but
  does not click through the running app.

## When correcting me
If you fix a mistake I make, add a short rule to this file so it doesn't repeat.
Keep this file short (~100 lines) so it doesn't eat the context budget every session.
