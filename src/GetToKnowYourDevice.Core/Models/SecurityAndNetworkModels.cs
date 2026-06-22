namespace GetToKnowYourDevice.Core.Models;

public sealed class SecurityInfo
{
    public List<AntivirusProduct> AntivirusProducts { get; set; } = [];
    public FirewallStatus Firewall { get; set; } = new();
    public DeviceSecurity Device { get; set; } = new();
}

public sealed class AntivirusProduct
{
    public string? Provider { get; set; }
    public string? ProductName { get; set; }
    public string? ProductState { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? RealTimeProtectionEnabled { get; set; }
    public string? SignatureVersion { get; set; }
    public DateTimeOffset? SignatureLastUpdated { get; set; }
    public string? EngineVersion { get; set; }
    public DateTimeOffset? LastQuickScan { get; set; }
    public DateTimeOffset? LastFullScan { get; set; }
    public bool? TamperProtectionEnabled { get; set; }
    public string? ProductPath { get; set; }
}

public sealed class FirewallStatus
{
    public FirewallProfile Domain { get; set; } = new() { ProfileName = "Domain" };
    public FirewallProfile Private { get; set; } = new() { ProfileName = "Private" };
    public FirewallProfile Public { get; set; } = new() { ProfileName = "Public" };
}

public sealed class FirewallProfile
{
    public string? ProfileName { get; set; }
    public bool? IsEnabled { get; set; }
    public string? DefaultInboundAction { get; set; }
    public string? DefaultOutboundAction { get; set; }
    public bool? NotificationsEnabled { get; set; }
}

public sealed class DeviceSecurity
{
    public bool? SecureBootSupported { get; set; }
    public bool? SecureBootEnabled { get; set; }
    public bool? TpmDetected { get; set; }
    public string? TpmVersion { get; set; }
    public string? TpmManufacturer { get; set; }
    public bool? TpmReady { get; set; }
    public bool? TpmEnabled { get; set; }
    public bool? TpmActivated { get; set; }
    public bool? TpmOwned { get; set; }
    public bool? DeviceEncryptionSupported { get; set; }
    public List<BitLockerVolume> BitLockerVolumes { get; set; } = [];
    public bool? UacEnabled { get; set; }
    public string? UacLevel { get; set; }
    public string? CoreIsolationStatus { get; set; }
    public string? MemoryIntegrityStatus { get; set; }
    public string? VirtualizationBasedSecurityStatus { get; set; }
    public string? CredentialGuardStatus { get; set; }
    public string? HvciStatus { get; set; }
    public bool? WindowsHelloAvailable { get; set; }
    public string? WindowsUpdateStatus { get; set; }
    public bool? PendingReboot { get; set; }
    public string? AutomaticUpdateStatus { get; set; }
}

public sealed class BitLockerVolume
{
    public string? DriveLetter { get; set; }
    public string? ProtectionStatus { get; set; }
    public double? EncryptionPercent { get; set; }
}

public sealed class NetworkInfo
{
    public NetworkSummary Summary { get; set; } = new();
    public List<NetworkAdapter> Adapters { get; set; } = [];
    public WifiInfo? Wifi { get; set; }
}

public sealed class NetworkSummary
{
    public bool? InternetConnected { get; set; }
    public string? ActiveInterface { get; set; }
    public string? ConnectionType { get; set; }
    public string? NetworkCategory { get; set; }
    public string? NetworkProfile { get; set; }
    public string? LocalIPv4 { get; set; }
    public string? LocalIPv6 { get; set; }
    public string? DefaultGateway { get; set; }
    public List<string> DnsServers { get; set; } = [];
    public bool? DhcpEnabled { get; set; }
    public long? LinkSpeedBps { get; set; }
}

public sealed class NetworkAdapter
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? AdapterType { get; set; }
    public bool? IsPhysical { get; set; }
    public string? MacAddress { get; set; }
    public List<string> IPv4Addresses { get; set; } = [];
    public List<string> IPv6Addresses { get; set; } = [];
    public List<string> SubnetMasks { get; set; } = [];
    public List<int> PrefixLengths { get; set; } = [];
    public List<string> DefaultGateways { get; set; } = [];
    public List<string> DnsServers { get; set; } = [];
    public bool? DhcpEnabled { get; set; }
    public string? DhcpServer { get; set; }
    public DateTimeOffset? DhcpLeaseObtained { get; set; }
    public DateTimeOffset? DhcpLeaseExpires { get; set; }
    public long? LinkSpeedBps { get; set; }
    public long? ReceiveSpeedBps { get; set; }
    public long? TransmitSpeedBps { get; set; }
    public string? OperationalStatus { get; set; }
    public string? ConnectionStatus { get; set; }
    public int? Mtu { get; set; }
    public string? DriverVersion { get; set; }
    public DateTimeOffset? DriverDate { get; set; }
    public int? InterfaceIndex { get; set; }
    public string? Guid { get; set; }
    public string? PnpDeviceId { get; set; }
}

public sealed class WifiInfo
{
    public string? Ssid { get; set; }
    public string? Bssid { get; set; }
    public int? SignalQuality { get; set; }
    public string? AuthenticationType { get; set; }
    public string? EncryptionType { get; set; }
    public int? Channel { get; set; }
    public string? FrequencyBand { get; set; }
    public string? RadioType { get; set; }
    public long? ReceiveRateBps { get; set; }
    public long? TransmitRateBps { get; set; }
    public string? InterfaceState { get; set; }
}
