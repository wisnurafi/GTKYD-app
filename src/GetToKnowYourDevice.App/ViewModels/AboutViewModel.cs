using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Static app info for the About page: versions, license, privacy/local-first statements.</summary>
public sealed partial class AboutViewModel : ViewModelBase
{
    public string ApplicationName => "Get To Know Your Device";

    public string ApplicationVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

    public string Description =>
        "A local-first Windows device inspection, system information, and diagnostic reporting tool.";

    public string DotNetVersion => Environment.Version.ToString();
    public string WindowsAppSdkVersion => "2.2.0 (stable)";

    public string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public string LicenseInfo => "Application: MIT (placeholder). See README for details.";

    public string ThirdPartyLicenses =>
        "QuestPDF (Community/MIT-style), CommunityToolkit.Mvvm (MIT), " +
        "Microsoft.Data.Sqlite (MIT), System.Management (MIT).";

    public string PrivacyStatement =>
        "This application runs locally. By default it performs no telemetry, analytics, cloud sync, " +
        "or external network requests. External diagnostics and public IP lookup are opt-in only.";

    public string LocalFirstStatement =>
        "All scan data and history are stored on this device. Nothing is uploaded automatically.";

    public string RepositoryPlaceholder => "https://example.com/GetToKnowYourDevice";
}
