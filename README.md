# Get To Know Your Device

A local-first Windows desktop application for device inspection, system information, and diagnostic reporting. Scans real Windows hardware, software, security, and network state; stores results as history; compares snapshots; and exports to JSON, CSV, and PDF.

> Built with .NET 9, WinUI 3, and the Windows App SDK. Targets Windows 10 (1809+) and Windows 11 on x64. Some information depends on firmware, driver, and vendor support and may be reported as `Unavailable`.

## Screenshots

<img width="1425" height="760" alt="image" src="https://github.com/user-attachments/assets/062c72b3-fde3-445d-a8f5-b5733887795f" />


## Installation (end users)

No Visual Studio, .NET runtime, or Windows App SDK install required — the build is self-contained.

1. Download `GetToKnowYourDevice-Setup-1.0.0-x64.exe`.
2. Run it and follow the wizard (installs to `Program Files`, creates Start Menu and optional desktop shortcuts).
3. Launch from the Start Menu.

Prefer no install? A portable build is the publish folder (see [Distribution](#distribution-self-contained-exe--installer)) — copy it anywhere and run `GetToKnowYourDevice.exe` directly.

> Requires 64-bit Windows 10 (1809+) or Windows 11. The installer requests administrator rights to write to `Program Files`; the app itself runs as a normal user.

## Feature list

- **Scan modes**: Quick, Full, and Custom (per-category selection).
- **Real-time progress**: running scanner, percentage, elapsed time, warning/error counts, and a Cancel button.
- **Partial results**: one scanner failing never fails the whole report; failures are recorded in Scan Diagnostics.
- **Source & availability transparency**: each section records its data source; unavailable data shows the reason (including when administrator permission is required).
- **Pages**: Summary (dashboard + health score), Hardware, Storage, Battery, Drivers, Security, Network, Raw Report, Scan History, Compare Reports, Settings, About.
- **Device Health Score (0–100)**: explainable rules engine with per-finding reason, source property, severity, and recommendation. Thresholds are configurable.
- **Scan history**: SQLite-backed immutable snapshots with rename, note, pin, delete, and prune (pinned scans are never auto-removed).
- **Compare reports**: structured diff with a volatile-field policy (transient values like uptime and current load are excluded by default).
- **Export**: JSON (canonical), CSV (multiple files zipped), and PDF (structured, paginated).
- **Privacy**: local-first; no telemetry, analytics, or external requests by default. Masking for identifiers is applied before export.

## Technology stack

- C# / .NET 9
- WinUI 3 + Windows App SDK 2.2.0 (stable)
- XAML, MVVM (`CommunityToolkit.Mvvm`)
- Dependency injection (`Microsoft.Extensions.DependencyInjection`)
- `async`/`await` with `CancellationToken` throughout
- SQLite via `Microsoft.Data.Sqlite`
- WMI/CIM via `System.Management`, Windows Registry, and managed Windows APIs; PowerShell only as a controlled fallback
- JSON via `System.Text.Json`; PDF via `QuestPDF`
- Distribution: unpackaged self-contained publish + Inno Setup installer (MSIX packaging also supported)

## Architecture

Layered solution with a single canonical report model used by the UI, history, comparison, and all exports:

- **Core** — canonical models, enums, scan contracts (`IDeviceScanner<T>`, `ScanContext`, `ScanSectionResult<T>`), health rules engine, comparison policy/engine, privacy masking interface + default masker, export contracts, calculations, JSON options. No Windows API dependency.
- **Infrastructure** — Windows scanners (WMI/CIM, Registry, managed APIs, PowerShell fallback), `ScanOrchestrator` (bounded parallelism, per-scanner timeout, cancellation, result merge), SQLite persistence with schema versioning, settings persistence, structured file logging, DI registration.
- **Export** — JSON, CSV (multi-file ZIP), and PDF exporters plus an export-service facade.
- **App** — WinUI 3 views, view models, navigation shell with the global scan progress area, converters, dialogs, and DI bootstrap with global exception handling.

## Project structure

```
GetToKnowYourDevice/
├── GetToKnowYourDevice.sln
├── src/
│   ├── GetToKnowYourDevice.App/             (WinUI 3, MSIX, entry point)
│   ├── GetToKnowYourDevice.Core/            (models, contracts, rules)
│   ├── GetToKnowYourDevice.Infrastructure/  (scanners, SQLite, settings, logging)
│   └── GetToKnowYourDevice.Export/          (JSON/CSV/PDF exporters)
├── tests/
│   ├── GetToKnowYourDevice.Core.Tests/
│   ├── GetToKnowYourDevice.Scanner.Tests/
│   └── GetToKnowYourDevice.Export.Tests/
├── installer/
│   ├── GetToKnowYourDevice.iss               (Inno Setup script)
│   └── output/                               (generated Setup.exe — gitignored)
├── docs/
└── README.md
```

## Requirements (building from source)

> Just want to run the app? See [Installation](#installation-end-users) — no SDK or toolchain needed.

- Visual Studio 2022 (17.x) with the **.NET Desktop Development** and **Windows App SDK / WinUI** components
- .NET 9 SDK
- Windows 10 version 1809 (build 17763) or later, or Windows 11
- x64 architecture

## How to restore

```
dotnet restore GetToKnowYourDevice.sln
```

## How to build

From Visual Studio: open `GetToKnowYourDevice.sln`, set the platform to **x64**, and build.

From the CLI:

```
dotnet build src/GetToKnowYourDevice.App/GetToKnowYourDevice.App.csproj -c Debug -p:Platform=x64
```

## How to run

In Visual Studio, set `GetToKnowYourDevice.App` as the startup project, choose the **(Package)** launch profile, select **x64**, and press **F5**. On first launch the Summary page shows an empty state with a **Run Your First Scan** button.

## How to test

The Core and Export test projects are platform-neutral; the Scanner test project references Infrastructure and runs as x64:

```
dotnet test tests/GetToKnowYourDevice.Core.Tests/GetToKnowYourDevice.Core.Tests.csproj
dotnet test tests/GetToKnowYourDevice.Export.Tests/GetToKnowYourDevice.Export.Tests.csproj
dotnet test tests/GetToKnowYourDevice.Scanner.Tests/GetToKnowYourDevice.Scanner.Tests.csproj -p:Platform=x64 --runtime win-x64
```

Current status: **71 tests passing** (Core 35, Export 19, Scanner 17).

## How to package (MSIX)

The App project keeps `EnableMsixTooling` and `WindowsPackageType`/packaging configured. In Visual Studio use **Project → Package and Publish → Create App Packages** to produce an MSIX for x64. The package identity, display name, version, logos, and minimum/maximum Windows versions are defined in `src/GetToKnowYourDevice.App/Package.appxmanifest`.

## Distribution (self-contained exe + installer)

The app ships as an **unpackaged, self-contained** build — the .NET runtime and the Windows App SDK runtime are bundled, so target machines need nothing pre-installed.

The unpackaged switches live in `GetToKnowYourDevice.App.csproj` (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`). These must be set at compile time: they make the build emit the Windows App SDK bootstrap initializer that starts the runtime without MSIX. Publishing a build compiled without them crashes on launch in `combase.dll` (stowed exception `0x802B000A`) before any app code runs.

### 1. Publish the self-contained build

```
dotnet publish src/GetToKnowYourDevice.App/GetToKnowYourDevice.App.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
```

Output: `src/GetToKnowYourDevice.App/bin/Release/net9.0-windows10.0.19041.0/win-x64/publish/`. This folder is the **portable build** — `GetToKnowYourDevice.exe` runs directly, no install. Confirm `Microsoft.ui.xaml.dll` and `Microsoft.WindowsAppRuntime.dll` are present (proof the runtime is bundled).

### 2. Build the installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php). Compile the script that wraps the publish folder:

```
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\GetToKnowYourDevice.iss
```

Output: `installer/output/GetToKnowYourDevice-Setup-1.0.0-x64.exe` (~80 MB). It installs to `Program Files`, creates Start Menu and optional desktop shortcuts, registers an uninstaller, and uses the app icon.

The app icon (`src/GetToKnowYourDevice.App/Assets/app.ico`) is embedded in the exe via `<ApplicationIcon>` and reused by the installer.

## Privacy behavior

- Local-first: no telemetry, analytics, cloud sync, remote storage, public IP lookup, or external connectivity tests run by default.
- External diagnostics and public IP lookup are **off by default** and require explicit opt-in in Settings.
- The app never reads or stores passwords, authentication tokens, browser data, Wi-Fi passwords, BitLocker recovery keys, or private keys.
- Masking (username, device name, serial numbers, MAC/BSSID, UUID, IP) is applied to a copy of the report **before** export bytes are written; stored snapshots keep their true values.

## Administrator permission behavior

- The app runs as a normal user. It does **not** declare `requireAdministrator`.
- Data that requires elevation (some TPM and BitLocker properties) is reported with status `PermissionRequired` and a clear message rather than failing the scan.
- The app is a read-only diagnostic tool; it never writes device configuration and never elevates for write operations.

## Data sources

Priority order, per scanner: managed Windows APIs → CIM/WMI → Registry → PowerShell fallback → `Unavailable`. Representative sources:

- System/OS: `Win32_ComputerSystem`, `Win32_ComputerSystemProduct`, `Win32_OperatingSystem`, `CurrentVersion` registry
- Motherboard/BIOS: `Win32_BaseBoard`, `Win32_BIOS`, `SecureBoot` registry, `PEFirmwareType`
- CPU/Memory/GPU: `Win32_Processor`, `Win32_PhysicalMemory`, `Win32_PhysicalMemoryArray`, `Win32_VideoController`
- Storage: `MSFT_PhysicalDisk`, `MSFT_Partition`, `Win32_LogicalDisk`
- Battery: `Win32_Battery`, `root\wmi` `BatteryFullChargedCapacity` / `BatteryStaticData` / `BatteryCycleCount`
- Drivers: `Win32_PnPSignedDriver`
- Security: `SecurityCenter2` `AntiVirusProduct`, firewall registry, `Win32_Tpm`, `Win32_DeviceGuard`, `Win32_EncryptableVolume`, UAC registry
- Network: `System.Net.NetworkInformation` + `Win32_NetworkAdapter`

## Known limitations

- **GPU memory**: WMI `AdapterRAM` is a 32-bit field and is unreliable for modern GPUs (>4 GB). It is reported with low confidence.
- **SMART/storage reliability counters**: not collected in this version; physical disks show SMART as `Unavailable` with the attempted source noted. The data model is in place for a future release.
- **CPU current clock and load**: not guaranteed available on all systems; reported as `Unavailable` when missing.
- **Old driver candidate**: a local, date-based heuristic only — **not** a check against the latest version online.
- **PnP driver list**: reflects installed PnP signed drivers, not the full set of Windows kernel drivers.
- **TPM/BitLocker**: some properties require administrator permission and are reported as `PermissionRequired` without elevation.

## Unsupported hardware property behavior

When a property cannot be read, the app distinguishes *null* (not provided by the scanner) from *Unavailable* (queried but not exposed) and never substitutes `0` or fake data for a failed query. Scanner failures surface in the Scan Diagnostics view with source, warnings, errors, and the list of unavailable properties.

## Export formats

- **JSON** — the full canonical report, indented, with schema and application version. Honors masking settings.
- **CSV** — multiple section files (`summary.csv`, `system.csv`, `processors.csv`, `memory-*.csv`, `graphics.csv`, `displays.csv`, `physical-disks.csv`, `partitions.csv`, `volumes.csv`, `batteries.csv`, `security.csv`, `network-adapters.csv`, `scan-diagnostics.csv`, and optionally `drivers.csv`) packaged in a ZIP. UTF-8 with BOM, RFC-4180 escaping, configurable delimiter.
- **PDF** — structured report (cover/metadata, summary, health, OS, hardware, storage, battery, security, network, optional driver list, diagnostics) with page numbers and a generated timestamp. Handles missing properties and long tables.

Export file names use the safe format `GetToKnowYourDevice_DEVICE_YYYY-MM-DD_HH-mm-ss.ext` with a sanitized device name.

## Third-party dependency licenses

| Dependency | Purpose | License |
| --- | --- | --- |
| Microsoft.WindowsAppSDK | WinUI 3 / Windows App SDK | MIT |
| CommunityToolkit.Mvvm | MVVM source generators | MIT |
| Microsoft.Extensions.DependencyInjection / Logging | DI and logging abstractions | MIT |
| Microsoft.Data.Sqlite | SQLite scan history | MIT |
| System.Management | WMI/CIM access | MIT |
| QuestPDF | PDF export | Community License (free for individuals and companies under the revenue threshold; MIT-style terms otherwise) |

`QuestPDF` is configured with `LicenseType.Community`. Review the QuestPDF license terms if your organization exceeds its revenue threshold.

## Development roadmap

- Collect SMART / storage reliability counters (`MSFT_StorageReliabilityCounter`).
- Wi-Fi detail (SSID/BSSID/signal) and opt-in network diagnostics (gateway, DNS, latency, connectivity, public IP).
- Comparison export (JSON/CSV/PDF) and richer comparison filters in the UI.
- Cross-platform scanner implementations behind the existing `IDeviceScanner<T>` abstraction.
- Localization (the architecture is language-ready).
