using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenUsage.ViewModels.Messages;

namespace OpenUsage.ViewModels;

public partial class TrayIconViewModel : ObservableObject
{
    public event Action? QuitRequested;

    [RelayCommand]
    private void TogglePanel()
    {
        WeakReferenceMessenger.Default.Send(new PanelToggleMessage());
    }

    [RelayCommand]
    private void ShowStats()
    {
        WeakReferenceMessenger.Default.Send(new PanelToggleMessage());
        WeakReferenceMessenger.Default.Send(new NavigateToOverviewMessage());
    }

    [RelayCommand]
    private void ShowSettings()
    {
        WeakReferenceMessenger.Default.Send(new PanelToggleMessage());
        WeakReferenceMessenger.Default.Send(new NavigateToSettingsMessage());
    }

    [RelayCommand]
    private void ShowAbout()
    {
        WeakReferenceMessenger.Default.Send(new ShowAboutMessage());
    }

    [RelayCommand]
    private void Quit()
    {
        QuitRequested?.Invoke();
    }
}
