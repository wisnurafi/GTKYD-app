using System.Runtime.Versioning;
using System.Text;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class AboutPage : Page
{
    public AboutViewModel ViewModel { get; }
    private readonly AppScanService _scanService;

    public AboutPage()
    {
        ViewModel = AppHost.Get<AboutViewModel>();
        _scanService = AppHost.Get<AppScanService>();
        InitializeComponent();
    }

    private void CopySummary_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ViewModel.ApplicationName} v{ViewModel.ApplicationVersion}");
        var r = _scanService.CurrentReport;
        if (r is not null)
        {
            sb.AppendLine($"Device: {r.DeviceIdentity.DeviceName}");
            sb.AppendLine($"OS: {r.OperatingSystem.Edition} {r.OperatingSystem.Version}");
            sb.AppendLine($"Health: {r.Health.Score}/100");
        }
        else
        {
            sb.AppendLine("No scan has been run yet.");
        }

        var pkg = new DataPackage();
        pkg.SetText(sb.ToString());
        Clipboard.SetContent(pkg);
    }
}
