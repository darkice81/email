# FusionPortal Client (Windows)

A Windows desktop client that runs on a player's/student's machine, reports
device and compliance telemetry to **FusionPortal**, and lets a player check
in to their scheduled match directly from that machine. Conceptually similar
to the client agents used by competitive gaming platforms (FACEIT, ESEA) for
match integrity: visible to the user, opt-in, and scoped to what's needed to
verify a fair, correctly-configured match environment.

> **Status: MVP scaffold.** This was generated without access to a live
> FusionPortal API or a Windows machine to build/run on (this repo's dev
> environment is Linux-only and has no .NET SDK installed). The code is
> written against the real .NET 8 / WPF APIs and should build as-is on a
> Windows machine with the .NET 8 SDK + WPF workload, but it has **not been
> compiled or run**. Treat the API contract in `docs/API_CONTRACT.md` as a
> proposal to reconcile with whatever FusionPortal's backend team actually
> implements.

## Why this design

This client reports sensitive information (location, VPN status, running
programs, active game). To keep that legitimate and defensible:

- **Visible, not covert.** The app runs as a normal window + system tray
  icon. It is never hidden from the task list, and the tray icon always
  shows when telemetry reporting is active.
- **Disclosed scope.** On first run the app shows exactly which data
  categories it collects and why (see `ConsentService` / first-run dialog),
  before any reporting starts.
- **Purpose-limited.** Telemetry collection is tied to an active/upcoming
  match window for the enrolled device, not continuous 24/7 surveillance.
  (`TelemetryReportingService` only runs while a match session is open.)
- **Server does the risky lookups.** The client reports its public IP; it
  does **not** call a third-party geolocation API directly with player data.
  FusionPortal's backend resolves IP → location, so no third-party service
  receives player telemetry client-side.

## Project layout

```
windows-client/
  FusionPortalClient.sln
  src/FusionPortalClient/
    App.xaml(.cs)               Application entry, tray icon, consent gate
    MainWindow.xaml(.cs)        Match / roster / check-in UI
    appsettings.json            FusionPortal base URL, site/org code, polling interval
    Models/                     DTOs shared with the API contract
    Services/
      SystemInfoService         Domain-join status, running programs, network info
      VpnDetectionService       Heuristic VPN/tunnel adapter detection
      GameDetectionService      Matches running processes against KnownGames.json
      FusionPortalApiClient     HTTP client for match/roster/check-in/telemetry endpoints
      TelemetryReportingService Periodic snapshot + POST while a match session is open
      EnrollmentService         First-run device enrollment (org/site code -> device token)
    ViewModels/MainViewModel    Bindable state for the UI
    Data/KnownGames.json        Process-name -> game-title lookup table
  docs/API_CONTRACT.md          Proposed REST contract with FusionPortal
```

## Building (on Windows)

Requires .NET 8 SDK with the "Desktop development with .NET" (WPF) workload.

```
cd windows-client
dotnet build FusionPortalClient.sln
dotnet run --project src/FusionPortalClient
```

Edit `src/FusionPortalClient/appsettings.json` to point at your FusionPortal
environment before running:

```json
{
  "FusionPortal": {
    "BaseUrl": "https://portal.example.org",
    "SiteCode": "SCHOOL-CODE",
    "PollIntervalSeconds": 60
  }
}
```

## What's implemented vs. stubbed

| Feature | Status |
|---|---|
| Domain-join / hostname detection | Implemented (`IPGlobalProperties`, `Environment`) |
| Running-programs list (visible windows) | Implemented (`Process` enumeration) |
| VPN / tunnel-adapter heuristic detection | Implemented, heuristic only — see caveats below |
| Local/public IP reporting | Implemented (public IP via FusionPortal's own `/whoami` echo, not a third party) |
| Video-game identification | Implemented via `KnownGames.json` lookup table (starter list, extend as needed) |
| Geolocation | **Not done client-side on purpose** — server resolves IP to location |
| Match / roster fetch + check-in | Implemented against the proposed contract in `docs/API_CONTRACT.md` |
| Device enrollment / auth | Minimal stub — issues a device token via an enrollment code; replace with your real auth (OAuth device flow, cert-based, etc.) before production use |

### VPN detection caveats

There is no reliable, false-positive-free way to detect "is a VPN running"
from user-mode code. `VpnDetectionService` flags:

- Network interfaces of type `Tunnel`
- Adapters whose description matches known VPN client patterns (OpenVPN TAP,
  WireGuard/Wintun, Cisco AnyConnect, GlobalProtect, FortiClient, etc.)

This will miss some VPNs (e.g. some proxy-based or browser-only VPNs) and can
false-positive on corporate SDP/ZTNA clients that use similar tunnel
adapters. Treat the result as a signal for a human/compliance workflow, not
an infallible block.
