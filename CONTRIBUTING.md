# Contributing to Get To Know Your Device

## Building

- Visual Studio 2022 (17.x) with **.NET Desktop Development** and
  **Windows App SDK / WinUI** components, .NET 9 SDK, Windows 10
  1809+ or Windows 11, x64.
- `dotnet restore GetToKnowYourDevice.sln`, then build in VS
  (platform **x64**) or `dotnet build` from the CLI.
- Run the tests: Core and Export are platform-neutral; Scanner tests
  run as x64 (see the README for the exact commands). All 71 tests
  must pass before you push.

## Architecture

Layered, and the layers mean something:

- **Core** — canonical report model, scan contracts
  (`IDeviceScanner<T>`, `ScanContext`, `ScanSectionResult<T>`),
  health rules engine, comparison engine, privacy masking. No Windows
  API dependency.
- **Infrastructure** — Windows scanners (WMI/CIM, Registry, managed
  APIs; PowerShell only as a controlled fallback), orchestrator,
  SQLite persistence, settings, logging.
- **Export** — JSON / CSV / PDF exporters.
- **App** — WinUI 3 views and view models (MVVM via
  CommunityToolkit.Mvvm).

Don't blur the layers. If Core needs `System.Management`, the design
is wrong.

## Adding a scanner

1. Implement `IDeviceScanner<T>` in Infrastructure.
2. Follow the data-source priority: managed Windows APIs → CIM/WMI →
   Registry → PowerShell fallback → `Unavailable`. Never substitute
   `0` or fake data for a failed query — report it as unavailable
   with the reason.
3. Respect the privacy rules: no passwords, tokens, browser data,
   Wi-Fi passwords, BitLocker keys, or private keys — ever. New
   identifiers must go through the masking pipeline before export.
4. Scanner failures must surface in Scan Diagnostics, never fail the
   whole report.

## Rules

- **Local-first, always.** No telemetry, no analytics, no cloud sync,
  no external requests by default. If your change needs the network,
  it needs explicit opt-in in Settings and a damn good reason.
- **Read-only.** The app never writes device configuration and never
  elevates. Keep it that way.
- One change per PR. Small PRs get reviewed fast.
- Commit messages read like a human wrote them: `fix: ...`,
  `feat: ...`.

## Reporting issues

Include: Windows version, app version, scan mode, and what happened
vs. what you expected. For wrong data, include the section and the
reported value — and what the correct value should be.
