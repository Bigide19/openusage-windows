using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenUsage.Core.Models;
using OpenUsage.ViewModels.Messages;

namespace OpenUsage.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<NavigateToProviderMessage>,
    IRecipient<NavigateToOverviewMessage>,
    IRecipient<NavigateToSettingsMessage>
{
    [ObservableProperty]
    private string _currentPage = "Overview";

    [ObservableProperty]
    private string? _selectedProviderId;

    [ObservableProperty]
    private List<PluginMeta> _pluginMetas = [];

    public OverviewViewModel Overview { get; }
    public ProviderDetailViewModel ProviderDetail { get; }
    public SettingsViewModel Settings { get; }

    public bool IsOnSubPage => CurrentPage != "Overview";

    public string PageTitle => CurrentPage switch
    {
        "Overview" => "Overview",
        "Detail" => ProviderDetail.Meta?.Name ?? "Detail",
        "Settings" => "Settings",
        _ => CurrentPage
    };

    public string SelectedNavId => CurrentPage switch
    {
        "Overview" => "home",
        "Detail" => SelectedProviderId ?? "home",
        "Settings" => "settings",
        _ => "home"
    };

    public MainViewModel(
        OverviewViewModel overview,
        ProviderDetailViewModel providerDetail,
        SettingsViewModel settings)
    {
        Overview = overview;
        ProviderDetail = providerDetail;
        Settings = settings;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    [RelayCommand]
    private void NavigateToOverview()
    {
        CurrentPage = "Overview";
        SelectedProviderId = null;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPage = "Settings";
        SelectedProviderId = null;
    }

    public void NavigateToProvider(string id)
    {
        SelectedProviderId = id;

        // Load data into ProviderDetail from Overview's card
        var card = Overview.Providers.FirstOrDefault(
            p => string.Equals(p.Meta.Id, id, StringComparison.OrdinalIgnoreCase));
        if (card != null)
            ProviderDetail.LoadProvider(id, card.Meta, card.Data);

        CurrentPage = "Detail";
    }

    partial void OnCurrentPageChanged(string value)
    {
        OnPropertyChanged(nameof(IsOnSubPage));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SelectedNavId));
    }

    partial void OnSelectedProviderIdChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedNavId));
    }

    public void Receive(NavigateToProviderMessage message)
    {
        NavigateToProvider(message.ProviderId);
    }

    public void Receive(NavigateToOverviewMessage message)
    {
        NavigateToOverview();
    }

    public void Receive(NavigateToSettingsMessage message)
    {
        NavigateToSettings();
    }
}
