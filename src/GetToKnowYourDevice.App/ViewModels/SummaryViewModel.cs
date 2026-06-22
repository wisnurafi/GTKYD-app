using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Projects the dashboard summary: identity, OS, hardware cards, and health score.</summary>
public sealed partial class SummaryViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    [ObservableProperty] private DeviceIdentity? _identity;
    [ObservableProperty] private OperatingSystemInfo? _os;
    [ObservableProperty] private HealthInfo? _health;
    [ObservableProperty] private string? _cpuSummary;
    [ObservableProperty] private string? _ramSummary;
    [ObservableProperty] private string? _gpuSummary;
    [ObservableProperty] private string? _storageSummary;
    [ObservableProperty] private string? _batterySummary;
    [ObservableProperty] private string? _networkSummary;
    [ObservableProperty] private string? _securitySummary;
    [ObservableProperty] private string? _uptimeDisplay;

    protected override void Project(CanonicalReport r)
    {
        Identity = r.DeviceIdentity;
        Os = r.OperatingSystem;
        Health = r.Health;

        var cpu = r.Hardware.Processors.FirstOrDefault();
        CpuSummary = cpu is null ? "Unavailable"
            : $"{cpu.Name} ({cpu.PhysicalCores}C / {cpu.LogicalProcessors}T)";

        var mem = r.Hardware.MemorySummary;
        RamSummary = mem.InstalledBytes is { } b
            ? $"{Format.Bytes(b)} ({mem.UsagePercent:0.#}% used)" : "Unavailable";

        GpuSummary = r.Hardware.GraphicsAdapters.FirstOrDefault()?.Name ?? "Unavailable";

        var sys = r.Storage.Summary;
        StorageSummary = sys.TotalBytes is > 0
            ? $"{Format.Bytes(sys.FreeBytes)} free of {Format.Bytes(sys.TotalBytes)}" : "Unavailable";

        var bat = r.Batteries.FirstOrDefault();
        BatterySummary = bat is null ? "No battery"
            : bat.HealthPercent is { } h ? $"{h:0.#}% health" : $"{bat.ChargePercent:0}% charge";

        NetworkSummary = r.Network.Summary.ActiveInterface ?? "Not connected";

        var avEnabled = r.Security.AntivirusProducts.Any(a => a.IsEnabled == true);
        SecuritySummary = r.Security.AntivirusProducts.Count == 0 ? "Unknown"
            : avEnabled ? "Protected" : "Attention required";

        UptimeDisplay = r.OperatingSystem.Uptime is { } up
            ? $"{(int)up.TotalDays}d {up.Hours}h {up.Minutes}m" : "Unavailable";
    }

    protected override void Clear()
    {
        Identity = null; Os = null; Health = null;
        CpuSummary = RamSummary = GpuSummary = StorageSummary = null;
        BatterySummary = NetworkSummary = SecuritySummary = UptimeDisplay = null;
    }
}
