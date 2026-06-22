using System.Runtime.Versioning;
using GetToKnowYourDevice.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class DriversPage : Page
{
    public DriversViewModel ViewModel { get; }

    public DriversPage()
    {
        ViewModel = AppHost.Get<DriversViewModel>();
        InitializeComponent();
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.Filter = FilterCombo.SelectedIndex switch
        {
            1 => DriverFilter.Signed,
            2 => DriverFilter.Unsigned,
            3 => DriverFilter.Error,
            4 => DriverFilter.Microsoft,
            5 => DriverFilter.ThirdParty,
            6 => DriverFilter.OldCandidate,
            _ => DriverFilter.All
        };
    }
}
