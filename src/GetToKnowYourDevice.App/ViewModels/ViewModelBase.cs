using CommunityToolkit.Mvvm.ComponentModel;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Base for all view models. Adds a common busy/error surface.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasError;

    protected void SetError(string? message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrEmpty(message);
    }

    protected void ClearError() => SetError(null);
}
