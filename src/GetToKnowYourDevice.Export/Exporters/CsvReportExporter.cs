using System.IO.Compression;
using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using GetToKnowYourDevice.Export.Csv;

namespace GetToKnowYourDevice.Export.Exporters;

/// <summary>
/// Exports the report as multiple section CSVs packed into a ZIP (the nested report does not
/// flatten cleanly into one table). Masking is applied before any rows are written.
/// </summary>
public sealed class CsvReportExporter(IPrivacyMasker masker) : IReportExporter
{
    public ExportFormat Format => ExportFormat.Csv;

    public ExportArtifact Export(CanonicalReport report, ExportOptions options)
    {
        var r = masker.Mask(report, options.Masking);
        var d = options.CsvDelimiter;

        var files = new Dictionary<string, byte[]>
        {
            ["summary.csv"] = BuildSummary(r, d),
            ["system.csv"] = BuildSystem(r, d),
            ["processors.csv"] = BuildProcessors(r, d),
            ["memory-summary.csv"] = BuildMemorySummary(r, d),
            ["memory-modules.csv"] = BuildMemoryModules(r, d),
            ["graphics.csv"] = BuildGraphics(r, d),
            ["displays.csv"] = BuildDisplays(r, d),
            ["physical-disks.csv"] = BuildPhysicalDisks(r, d),
            ["partitions.csv"] = BuildPartitions(r, d),
            ["volumes.csv"] = BuildVolumes(r, d),
            ["batteries.csv"] = BuildBatteries(r, d),
            ["security.csv"] = BuildSecurity(r, d),
            ["network-adapters.csv"] = BuildNetworkAdapters(r, d),
            ["scan-diagnostics.csv"] = BuildDiagnostics(r, d),
        };
        if (options.IncludeFullDriverList)
            files["drivers.csv"] = BuildDrivers(r, d);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, bytes) in files)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                using var es = entry.Open();
                es.Write(bytes, 0, bytes.Length);
            }
        }

        return new ExportArtifact
        {
            FileName = ExportFileNaming.BuildFileName(r.DeviceIdentity.DeviceName, DateTimeOffset.Now, "zip"),
            Content = ms.ToArray(),
            ContentType = "application/zip"
        };
    }

    private static byte[] BuildSummary(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Property", "Value"]);
        w.WriteRow(["DeviceName", r.DeviceIdentity.DeviceName]);
        w.WriteRow(["Manufacturer", r.DeviceIdentity.Manufacturer]);
        w.WriteRow(["Model", r.DeviceIdentity.Model]);
        w.WriteRow(["OS", r.OperatingSystem.Edition]);
        w.WriteRow(["OSVersion", r.OperatingSystem.Version]);
        w.WriteRow(["HealthScore", CsvWriter.Format(r.Health.Score)]);
        w.WriteRow(["ScanStatus", r.ReportMetadata.OverallStatus.ToString()]);
        w.WriteRow(["ScanDate", CsvWriter.Format(r.ReportMetadata.StartedAt)]);
        return w.ToBytes();
    }

    private static byte[] BuildSystem(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        var s = r.Hardware.System;
        w.WriteRow(["Property", "Value"]);
        w.WriteRow(["Manufacturer", s.Manufacturer]);
        w.WriteRow(["Model", s.Model]);
        w.WriteRow(["ProductName", s.ProductName]);
        w.WriteRow(["SystemSku", s.SystemSku]);
        w.WriteRow(["SerialNumber", s.SerialNumber]);
        w.WriteRow(["Uuid", s.Uuid]);
        w.WriteRow(["ChassisType", s.ChassisType]);
        w.WriteRow(["IsVirtualMachine", CsvWriter.Format(s.IsVirtualMachine)]);
        return w.ToBytes();
    }

    private static byte[] BuildProcessors(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Name", "Manufacturer", "Architecture", "PhysicalCores", "LogicalProcessors",
            "MaxClockMhz", "L2CacheKb", "L3CacheKb", "Status"]);
        foreach (var p in r.Hardware.Processors)
            w.WriteRow([p.Name, p.Manufacturer, p.Architecture, CsvWriter.Format(p.PhysicalCores),
                CsvWriter.Format(p.LogicalProcessors), CsvWriter.Format(p.MaxClockMhz),
                CsvWriter.Format(p.L2CacheKb), CsvWriter.Format(p.L3CacheKb), p.Status]);
        return w.ToBytes();
    }

    private static byte[] BuildMemorySummary(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        var m = r.Hardware.MemorySummary;
        w.WriteRow(["Property", "Value"]);
        w.WriteRow(["InstalledBytes", CsvWriter.Format(m.InstalledBytes)]);
        w.WriteRow(["UsableBytes", CsvWriter.Format(m.UsableBytes)]);
        w.WriteRow(["AvailableBytes", CsvWriter.Format(m.AvailableBytes)]);
        w.WriteRow(["TotalSlots", CsvWriter.Format(m.TotalSlots)]);
        w.WriteRow(["UsedSlots", CsvWriter.Format(m.UsedSlots)]);
        w.WriteRow(["MaxCapacityBytes", CsvWriter.Format(m.MaxCapacityBytes)]);
        return w.ToBytes();
    }

    private static byte[] BuildMemoryModules(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Manufacturer", "PartNumber", "SerialNumber", "CapacityBytes", "SpeedMhz",
            "MemoryType", "FormFactor", "DeviceLocator", "Status"]);
        foreach (var m in r.Hardware.MemoryModules)
            w.WriteRow([m.Manufacturer, m.PartNumber, m.SerialNumber, CsvWriter.Format(m.CapacityBytes),
                CsvWriter.Format(m.SpeedMhz), m.MemoryType, m.FormFactor, m.DeviceLocator, m.Status]);
        return w.ToBytes();
    }

    private static byte[] BuildGraphics(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Name", "Vendor", "DriverVersion", "DriverDate", "CurrentResolution",
            "RefreshRateHz", "Status"]);
        foreach (var g in r.Hardware.GraphicsAdapters)
            w.WriteRow([g.Name, g.Vendor, g.DriverVersion, CsvWriter.Format(g.DriverDate),
                g.CurrentResolution, CsvWriter.Format(g.RefreshRateHz), g.Status]);
        return w.ToBytes();
    }

    private static byte[] BuildDisplays(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["MonitorName", "Manufacturer", "Resolution", "RefreshRateHz", "IsPrimary"]);
        foreach (var disp in r.Hardware.Displays)
            w.WriteRow([disp.MonitorName, disp.Manufacturer, disp.Resolution,
                CsvWriter.Format(disp.RefreshRateHz), CsvWriter.Format(disp.IsPrimary)]);
        return w.ToBytes();
    }

    private static byte[] BuildPhysicalDisks(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["FriendlyName", "Model", "SerialNumber", "MediaType", "BusType",
            "CapacityBytes", "HealthStatus", "OperationalStatus"]);
        foreach (var disk in r.Storage.PhysicalDisks)
            w.WriteRow([disk.FriendlyName, disk.Model, disk.SerialNumber, disk.MediaType, disk.BusType,
                CsvWriter.Format(disk.CapacityBytes), disk.HealthStatus, disk.OperationalStatus]);
        return w.ToBytes();
    }

    private static byte[] BuildPartitions(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["DiskNumber", "PartitionNumber", "DriveLetter", "Type", "SizeBytes",
            "IsBoot", "IsSystem"]);
        foreach (var p in r.Storage.Partitions)
            w.WriteRow([CsvWriter.Format(p.DiskNumber), CsvWriter.Format(p.PartitionNumber),
                p.DriveLetter, p.Type, CsvWriter.Format(p.SizeBytes),
                CsvWriter.Format(p.IsBoot), CsvWriter.Format(p.IsSystem)]);
        return w.ToBytes();
    }

    private static byte[] BuildVolumes(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["DriveLetter", "Label", "FileSystem", "TotalBytes", "FreeBytes",
            "UsagePercent", "DriveType", "BitLockerProtectionStatus"]);
        foreach (var v in r.Storage.Volumes)
            w.WriteRow([v.DriveLetter, v.Label, v.FileSystem, CsvWriter.Format(v.TotalBytes),
                CsvWriter.Format(v.FreeBytes), CsvWriter.Format(v.UsagePercent), v.DriveType,
                v.BitLockerProtectionStatus]);
        return w.ToBytes();
    }

    private static byte[] BuildBatteries(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["DeviceName", "Chemistry", "DesignCapacityMwh", "FullChargeCapacityMwh",
            "HealthPercent", "WearPercent", "CycleCount", "ChargePercent"]);
        foreach (var b in r.Batteries)
            w.WriteRow([b.DeviceName, b.Chemistry, CsvWriter.Format(b.DesignCapacityMwh),
                CsvWriter.Format(b.FullChargeCapacityMwh), CsvWriter.Format(b.HealthPercent),
                CsvWriter.Format(b.WearPercent), CsvWriter.Format(b.CycleCount),
                CsvWriter.Format(b.ChargePercent)]);
        return w.ToBytes();
    }

    private static byte[] BuildSecurity(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        var s = r.Security.Device;
        w.WriteRow(["Property", "Value"]);
        w.WriteRow(["SecureBootEnabled", CsvWriter.Format(s.SecureBootEnabled)]);
        w.WriteRow(["TpmDetected", CsvWriter.Format(s.TpmDetected)]);
        w.WriteRow(["TpmVersion", s.TpmVersion]);
        w.WriteRow(["UacEnabled", CsvWriter.Format(s.UacEnabled)]);
        w.WriteRow(["PendingReboot", CsvWriter.Format(s.PendingReboot)]);
        foreach (var av in r.Security.AntivirusProducts)
        {
            w.WriteRow(["Antivirus", av.ProductName]);
            w.WriteRow(["  Enabled", CsvWriter.Format(av.IsEnabled)]);
            w.WriteRow(["  RealTimeProtection", CsvWriter.Format(av.RealTimeProtectionEnabled)]);
        }
        return w.ToBytes();
    }

    private static byte[] BuildNetworkAdapters(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Name", "Description", "MacAddress", "IPv4", "OperationalStatus", "LinkSpeedBps"]);
        foreach (var a in r.Network.Adapters)
            w.WriteRow([a.Name, a.Description, a.MacAddress, string.Join(" ", a.IPv4Addresses),
                a.OperationalStatus, CsvWriter.Format(a.LinkSpeedBps)]);
        return w.ToBytes();
    }

    private static byte[] BuildDrivers(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["DeviceName", "DeviceClass", "Manufacturer", "DriverVersion", "DriverDate",
            "IsSigned", "Signer", "Status"]);
        foreach (var dr in r.Drivers)
            w.WriteRow([dr.DeviceName, dr.DeviceClass, dr.Manufacturer, dr.DriverVersion,
                CsvWriter.Format(dr.DriverDate), CsvWriter.Format(dr.IsSigned), dr.Signer, dr.Status]);
        return w.ToBytes();
    }

    private static byte[] BuildDiagnostics(CanonicalReport r, string d)
    {
        var w = new CsvWriter(d);
        w.WriteRow(["Scanner", "Status", "Source", "DurationMs", "RequiresElevation",
            "Warnings", "Errors"]);
        foreach (var diag in r.ScanDiagnostics)
            w.WriteRow([diag.ScannerName, diag.Status.ToString(), diag.Source,
                CsvWriter.Format(diag.DurationMs), CsvWriter.Format(diag.RequiresElevation),
                CsvWriter.Format(diag.Warnings.Count), CsvWriter.Format(diag.Errors.Count)]);
        return w.ToBytes();
    }
}
