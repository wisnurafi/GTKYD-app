using System.Runtime.Versioning;
using GetToKnowYourDevice.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class SecurityPage : Page
{
    public SecurityViewModel ViewModel { get; }

    public SecurityPage()
    {
        ViewModel = AppHost.Get<SecurityViewModel>();
        InitializeComponent();
    }
}
