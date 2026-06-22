using System.Runtime.Versioning;
using GetToKnowYourDevice.App.ViewModels;
using GetToKnowYourDevice.Core.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class SummaryPage : Page
{
    public SummaryViewModel ViewModel { get; }

    public SummaryPage()
    {
        ViewModel = AppHost.Get<SummaryViewModel>();
        InitializeComponent();
    }

    private void RunFirstScan_Click(object sender, RoutedEventArgs e)
    {
        var shell = AppHost.Get<ShellViewModel>();
        shell.SelectedScanMode = ScanMode.Quick;
        if (shell.RunScanCommand.CanExecute(null))
            shell.RunScanCommand.Execute(null);
    }
}
