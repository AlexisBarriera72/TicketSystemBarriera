# Barriera Moving App — Project Plan & Feasibility Analysis

Reference implementation: the support-ticket system in this repo
(`TicketSystemBarriera`) and the university module PDFs in `/docs`.
This document captures the architecture decision, feature feasibility, risks,
and the phased plan. `CLAUDE.md` is the short version the agent reads each session.

---

## 1. The decision that comes before the feature list: platform

The current app is a **Blazor Web App in Interactive Server render mode** — every
interaction travels over a live SignalR/WebSocket connection to the server. That is
fine for office staff on stable Wi-Fi, but a real problem for movers in the field
(driving, stairwells, elevators, basements): when the connection drops, the app
freezes on a "reconnecting" overlay. The three most-used features (clock-in, chat,
signing) all happen in the field, so this is the wrong foundation as-is.

Options considered:
- **Responsive web app** (current, phone-friendly CSS) — keeps the connectivity
  fragility, no camera/GPS/push/app icon. Ruled out for field use.
- **PWA / Blazor WebAssembly** — installable, better offline, but iOS heavily
  restricts background location and push for PWAs. Hits a wall given the live-location
  requirement on iPhones.
- **.NET MAUI Blazor Hybrid** — recommended. Runs natively on iOS/Android, real access
  to camera, GPS/background location, push, local storage, AND reuses existing Razor
  components + C# knowledge.

**Chosen architecture:** ASP.NET Core **Web API** backend (EF Core, Identity, business
logic, SQL Server) + thin clients that call it over HTTPS — a **MAUI Blazor Hybrid**
app for phones, plus a Blazor web dashboard for office/boss. A phone must never talk
directly to SQL Server; the API in the middle is what makes "the boss sees everything
in real time" work, since every client hits the same source of truth.

The biggest structural change from the current code is splitting the monolith into
API + client. Budget for that before building most features.

---

## 2. What the existing template already gives you (~40%)

- `Ticket` → **Order/Job**. Same shape: author, assignee, status, timestamps.
- `TicketComment` → **the chat.** Threaded per-order conversation with sender identity
  and timeline ordering already built. This is priority #1 and mostly done.
- `TicketStatus` enum → job lifecycle. Status transitions are already gated in code —
  the same mechanism blocks "Completed" until a signed doc exists.
- `Roles.cs` + `UserManagement.razor` → **boss changes an employee's role.** One-click
  role assignment via `UserManager` already exists. Relabel to Office / Driver / Client.
- `GenerateExcelReportAsync` (ClosedXML) + KPI dashboard → **boss's export.** Already
  exports to `.xlsx` and shows metric cards. Extend the query with clock-in/delivery times.
- Admin "visión total" → **boss sees all chats/orders.** Admin already reaches every
  ticket's manage page.

---

## 3. Feasibility, feature by feature

### Employees
- Chat with client/office — **already built**; add client as participant, office read access.
- Photo with location + timestamp posted to chat — **straightforward** (MAUI camera + GPS);
  attach lat/long + capture time. "Alert client when near" is the harder half (see risks).
- Signed docs required before Completed — **straightforward gate** using existing status logic.
- App locked until clock-in — **easy.** A `TimeEntry` (ClockIn/ClockOut) + a UI guard.
  Great first vertical slice.
- In-app map/navigation — **easy if you deep-link** to Google Maps / Waze / Apple Maps by
  URL scheme. Do NOT build turn-by-turn.
- Docs sent in-app AND to client email — **straightforward** (SMTP or SendGrid/Resend + storage).

### Clients
- See employee's chat — **already built.**
- See employee location / ETA — **real work; the hardest item** (see risks).
- Add items to an existing order — **straightforward.** Give orders line-items; let client
  append. Decide the business rule for whether that changes the quote.
- Sign from a tablet or the employee's phone — **straightforward with an embedded-signing
  provider** (see §4). Flow: employee uploads doc → client signs on-device → office reviews
  before Completed. Same webhook pattern already used for Stripe in the donations module.
- Terms of service page — **trivial** (static page).
- Customer service / complaint section — **easy**; model as another order type or a support
  thread reusing the chat.
- Show helping employee's name in chat — **easy**; add a display name to `ApplicationUser`.

### Boss / CEO
- Change employee role — **already built.**
- Download everything to Excel + display — **already built**; extend dataset.
- Role badge in chat (Boss/Driver/Office) — **easy**; render from sender's role.
- See every chat/active order in real time — **mostly built**; cleaner with an API + SignalR
  hub the dashboard subscribes to.
- Private 1:1 messages to any employee/client — **new subsystem.** Current chat is per-order;
  direct messaging is a separate conversation concept. Doable, but genuinely new — don't
  assume the existing comment table covers it.

---

## 4. The four things to flag honestly before quoting a price

1. **Live location + ETA is the hard one.** Background GPS drains battery; iOS/Android make
   you justify background-location permission (Apple scrutinizes this in review); you need a
   sane update strategy (push location only while a job is EnRoute, not 24/7). There's a real
   privacy/labor dimension to tracking employees — raise it with the client deliberately.
   Consider scoping v1 to "share location only during an active job."

2. **E-signature legal weight.** For a moving company, signed inventory/condition/waiver docs
   are liability protection against damage claims. A hand-rolled canvas signature is trivial
   but weak if disputed. Use a provider with a real audit trail (timestamped, tamper-evident,
   who signed what and from where). This is the one place NOT to DIY.
   Embedded-signing providers (sign inside the app, no redirect) with legally-binding audit
   trails include Dropbox Sign, SignNow, Signeasy, and SignWell; BoldSign also offers embedded
   signing with custom branding. SignWell has a small free API tier plus unlimited test usage,
   good for development. Verify current pricing before committing.

3. **Push notifications need real setup** — Firebase Cloud Messaging (Android) and APNs (Apple).
   Not hard, but a genuine integration; it powers "employee is near" and "document ready to sign".

4. **App store reality.** Apple Developer ~$99/yr, Google Play ~$25 one-time; Apple review can
   bounce you on background-location and privacy-policy grounds. iOS builds require a Mac.

Also: data privacy for client PII, addresses, and signatures is a real obligation — encrypt
in transit and at rest, and have a privacy policy.

---

## 5. Where to start (phased MVP)

Follow the client's own priority (chat, signing, clock-in), but lay the plumbing first:

1. Restructure into a Web API — move `TicketService` + EF Core behind it; rename the domain
   (Order, TimeEntry, roles).
2. Stand up the MAUI Blazor Hybrid shell; log in against the API.
3. Clock-in/out — small, self-contained; proves the whole stack end to end.
4. Chat — ported from `TicketComment`, with client + office participants.
5. Photo + location updates into the chat.
6. Embedded e-signature + "can't complete until signed & office-reviewed" gate + email copy.
7. Live location / ETA (the hard one, once basics are solid).
8. Boss dashboard, direct messages, exports.

---

## 6. Business guardrails (paid client project)

- Get the scope in writing with the phased list above. "Add items to an order" and "boss sees
  everything in real time" are the phrases that balloon later.
- Decide up front who pays the **recurring third-party costs**: e-sign provider, maps/geocoding,
  email, push, Apple/Google developer accounts, and API hosting. Those are the client's operating
  costs, not yours to absorb — name them now.
- Clarify code ownership and handoff.
