using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenUsage.Core.Models;
using OpenUsage.ViewModels.Messages;

namespace OpenUsage.ViewModels;

public partial class OverviewViewModel : ObservableObject
{
    public ObservableCollection<ProviderCardViewModel> Providers { get; } = [];

    [ObservableProperty]
    private bool _isRefreshing;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAll()
    {
        IsRefreshing = true;
        try
        {
            WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage(null));
            await Task.CompletedTask;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public void Initialize(List<PluginMeta> plugins)
    {
        Providers.Clear();
        foreach (var plugin in plugins)
        {
            Providers.Add(new ProviderCardViewModel(plugin));
        }
    }

    public void UpdateProviderData(string providerId, PluginOutput output)
    {
        var card = Providers.FirstOrDefault(
            p => string.Equals(p.Meta.Id, providerId, StringComparison.OrdinalIgnoreCase));
        card?.UpdateData(output);
    }
}
