using System.Runtime.Versioning;
using GetToKnowYourDevice.App.ViewModels;
using GetToKnowYourDevice.Core.Persistence;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class ScanHistoryPage : Page
{
    public ScanHistoryViewModel ViewModel { get; }

    public ScanHistoryPage()
    {
        ViewModel = AppHost.Get<ScanHistoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private async void Search_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        => await ViewModel.LoadAsync();

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ScanHistoryRecord r)
            await ViewModel.OpenCommand.ExecuteAsync(r);
    }

    private async void Pin_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ScanHistoryRecord r)
            await ViewModel.TogglePinCommand.ExecuteAsync(r);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ScanHistoryRecord r) return;

        var dialog = new ContentDialog
        {
            Title = "Delete scan",
            Content = $"Delete this {r.ScanType} scan from history? This cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteCommand.ExecuteAsync(r);
    }
}
