# FusionPortal Client API Contract (proposed)

This is a **proposed** contract for the endpoints `FusionPortalApiClient`
calls. No FusionPortal backend code exists in this repository, so nothing
here is confirmed against a real server — reconcile with whatever the
FusionPortal API team actually builds/owns, and update
`Services/FusionPortalApiClient.cs` to match the real shapes.

All requests are HTTPS, JSON, authenticated with `Authorization: Bearer
<device-token>` obtained from enrollment. Base URL and site code come from
`appsettings.json`.

## Enrollment

### `POST /api/v1/devices/enroll`

Request:
```json
{
  "siteCode": "SCHOOL-CODE",
  "enrollmentCode": "one-time-code-issued-by-org-admin",
  "hostname": "PLAYER-PC-01"
}
```

Response:
```json
{
  "deviceId": "dev_01H...",
  "deviceToken": "opaque-bearer-token",
  "orgName": "Example High School Esports"
}
```

## Current match for this device

### `GET /api/v1/devices/{deviceId}/current-match`

Returns the match this device/site is scheduled for "now", or `null`.

Response:
```json
{
  "matchId": "match_01H...",
  "title": "Varsity vs. Lincoln HS - Round 3",
  "game": "Overwatch 2",
  "scheduledStart": "2026-08-07T18:00:00Z",
  "checkInOpensAt": "2026-08-07T17:30:00Z",
  "checkInClosesAt": "2026-08-07T18:05:00Z",
  "status": "check_in_open"
}
```

## Roster for a match

### `GET /api/v1/matches/{matchId}/roster`

Response:
```json
{
  "players": [
    {
      "playerId": "plr_01H...",
      "displayName": "J. Kell",
      "team": "Home",
      "role": "DPS",
      "checkedIn": false
    }
  ]
}
```

## Public IP echo

### `GET /api/v1/whoami`

A trivial endpoint FusionPortal exposes so the client can learn its own
public IP by asking the server it already talks to, instead of calling a
third-party "what's my IP" service with player data.

Response:
```json
{ "publicIp": "203.0.113.45" }
```

## Check-in

### `POST /api/v1/matches/{matchId}/checkin`

Request:
```json
{
  "playerId": "plr_01H...",
  "deviceId": "dev_01H...",
  "telemetry": { "$ref": "TelemetrySnapshot, see below" }
}
```

Response:
```json
{ "success": true, "checkedInAt": "2026-08-07T17:31:02Z" }
```

## Telemetry snapshot (sent with check-in and periodically while a match session is open)

### `POST /api/v1/devices/{deviceId}/telemetry`

```json
{
  "capturedAtUtc": "2026-08-07T17:31:00Z",
  "hostname": "PLAYER-PC-01",
  "domainJoined": true,
  "domainName": "SCHOOLDISTRICT.LOCAL",
  "localIpAddresses": ["10.0.4.22"],
  "publicIpAddress": "203.0.113.45",
  "vpnDetected": false,
  "vpnAdapterNames": [],
  "activeGame": "Overwatch 2",
  "runningPrograms": [
    { "processName": "Overwatch.exe", "windowTitle": "Overwatch 2" },
    { "processName": "discord.exe", "windowTitle": "Discord" }
  ]
}
```

Notes:
- `publicIpAddress` is intentionally the only geolocation-relevant field the
  client sends. Location resolution (city/region/ASN) should happen
  **server-side** in FusionPortal, not via a third-party geolocation API
  called from the client — see README "Why this design".
- `runningPrograms` should stay limited to processes with a visible main
  window (see `SystemInfoService`), not a dump of every OS process, to avoid
  over-collecting and to keep payload size sane.
