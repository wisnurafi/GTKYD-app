using System.Runtime.Versioning;
using GetToKnowYourDevice.App.ViewModels;
using GetToKnowYourDevice.App.Views;
using GetToKnowYourDevice.Core.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GetToKnowYourDevice.App;

/// <summary>
/// Application shell. Hosts navigation between section pages and the global scan progress bar.
/// The shell view model owns scan state; this code-behind only wires navigation and the scan
/// mode selector.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class MainWindow : Window
{
    public ShellViewModel Shell { get; }

    private static readonly Dictionary<string, Type> PageMap = new()
    {
        ["Summary"] = typeof(SummaryPage),
        ["Hardware"] = typeof(HardwarePage),
        ["Storage"] = typeof(StoragePage),
        ["Battery"] = typeof(BatteryPage),
        ["Drivers"] = typeof(DriversPage),
        ["Security"] = typeof(SecurityPage),
        ["Network"] = typeof(NetworkPage),
        ["RawReport"] = typeof(RawReportPage),
        ["ScanHistory"] = typeof(ScanHistoryPage),
        ["Compare"] = typeof(ComparePage),
        ["Settings"] = typeof(SettingsPage),
        ["About"] = typeof(AboutPage),
    };

    public MainWindow()
    {
        InitializeComponent();
        Shell = AppHost.Get<ShellViewModel>();
        Shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.IsScanning))
                ScanStatusBar.Visibility = Shell.IsScanning ? Visibility.Visible : Visibility.Collapsed;
        };
        ContentFrame.Navigate(typeof(SummaryPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag } && PageMap.TryGetValue(tag, out var pageType))
            ContentFrame.Navigate(pageType);
    }

    private void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        foreach (var mode in new[] { ScanMode.Quick, ScanMode.Full, ScanMode.Custom })
        {
            var item = new MenuFlyoutItem { Text = $"{mode} Scan", Tag = mode };
            item.Click += (_, _) =>
            {
                Shell.SelectedScanMode = mode;
                if (Shell.RunScanCommand.CanExecute(null))
                    Shell.RunScanCommand.Execute(null);
            };
            menu.Items.Add(item);
        }
        menu.ShowAt((FrameworkElement)sender);
    }
}
