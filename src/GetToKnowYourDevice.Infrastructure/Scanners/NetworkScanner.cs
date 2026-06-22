using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Reads network adapters and a summary using the managed NetworkInformation API (no elevation).
/// External diagnostics and public IP lookup are intentionally not performed here; those are
/// opt-in actions triggered explicitly by the user from the Network page.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NetworkScanner(WmiQueryRunner wmi, ILogger<NetworkScanner> logger) : ScannerBase(logger)
{
    public override string Name => "Network";
    public override ScanCategory Category => ScanCategory.Network;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "API:NetworkInterface + WMI:Win32_NetworkAdapter";
        var net = report.Network;
        var any = false;

        // Driver info per adapter name from WMI (best-effort).
        var driverByName = new Dictionary<string, (string? ver, DateTimeOffset? date, string? pnp, string? mfg)>(
            StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var r in wmi.Query(
                "SELECT Name, Manufacturer, PNPDeviceID, ServiceName FROM Win32_NetworkAdapter", ct: ct))
            {
                var n = r.GetString("Name");
                if (n is not null)
                    driverByName[n] = (null, null, r.GetString("PNPDeviceID"), r.GetString("Manufacturer"));
            }
        }
        catch (Exception ex) { builder.Warn($"Win32_NetworkAdapter query failed: {ex.Message}"); }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            ct.ThrowIfCancellationRequested();
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            var props = nic.GetIPProperties();
            var adapter = new NetworkAdapter
            {
                Name = nic.Name,
                Description = nic.Description,
                AdapterType = nic.NetworkInterfaceType.ToString(),
                MacAddress = FormatMac(nic.GetPhysicalAddress().GetAddressBytes()),
                OperationalStatus = nic.OperationalStatus.ToString(),
                LinkSpeedBps = nic.Speed > 0 ? nic.Speed : null,
                IsPhysical = nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                    or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet
                    or NetworkInterfaceType.FastEthernetT or NetworkInterfaceType.FastEthernetFx
            };

            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    adapter.IPv4Addresses.Add(ua.Address.ToString());
                    adapter.PrefixLengths.Add(ua.PrefixLength);
                }
                else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    adapter.IPv6Addresses.Add(ua.Address.ToString());
                }
            }
            adapter.DefaultGateways.AddRange(props.GatewayAddresses.Select(g => g.Address.ToString()));
            adapter.DnsServers.AddRange(props.DnsAddresses.Select(d => d.ToString()));

            try
            {
                var v4 = props.GetIPv4Properties();
                adapter.DhcpEnabled = v4?.IsDhcpEnabled;
                adapter.Mtu = v4?.Mtu;
                adapter.InterfaceIndex = v4?.Index;
            }
            catch { /* some adapters lack IPv4 props */ }

            adapter.DhcpServer = props.DhcpServerAddresses.FirstOrDefault()?.ToString();
            adapter.Guid = nic.Id;
            if (nic.Description is not null && driverByName.TryGetValue(nic.Description, out var di))
            {
                adapter.Manufacturer = di.mfg;
                adapter.PnpDeviceId = di.pnp;
            }

            net.Adapters.Add(adapter);
            any = true;
        }

        BuildSummary(net);
        if (!any) builder.Unavailable("Network", "no adapters found");
        return Task.FromResult(any);
    }

    private static void BuildSummary(NetworkInfo net)
    {
        var active = net.Adapters.FirstOrDefault(a =>
            a.OperationalStatus == "Up" && a.IPv4Addresses.Count > 0 && a.DefaultGateways.Count > 0);
        if (active is null) return;

        var s = net.Summary;
        s.ActiveInterface = active.Name;
        s.ConnectionType = active.AdapterType;
        s.LocalIPv4 = active.IPv4Addresses.FirstOrDefault();
        s.LocalIPv6 = active.IPv6Addresses.FirstOrDefault();
        s.DefaultGateway = active.DefaultGateways.FirstOrDefault();
        s.DnsServers = active.DnsServers.Distinct().ToList();
        s.DhcpEnabled = active.DhcpEnabled;
        s.LinkSpeedBps = active.LinkSpeedBps;
        s.InternetConnected = NetworkInterface.GetIsNetworkAvailable();
    }

    private static string? FormatMac(byte[] bytes)
        => bytes.Length == 0 ? null : string.Join(":", bytes.Select(b => b.ToString("X2")));
}
