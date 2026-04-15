using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenUsage.Core.Enums;
using OpenUsage.Core.Models;
using OpenUsage.ViewModels.Messages;

namespace OpenUsage.ViewModels;

public partial class ProviderDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private PluginMeta? _meta;

    [ObservableProperty]
    private PluginOutput? _data;

    [ObservableProperty]
    private bool _isLoading;

    public IReadOnlyList<MetricLine> OverviewLines
    {
        get
        {
            if (Data?.Lines is null || Meta?.Lines is null)
                return [];

            var overviewLabels = Meta.Lines
                .Where(ml => ml.Scope == MetricLineScope.Overview)
                .Select(ml => ml.Label)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return Data.Lines
                .Where(l => overviewLabels.Contains(l.Label))
                .ToList();
        }
    }

    public IReadOnlyList<MetricLine> DetailLines
    {
        get
        {
            if (Data?.Lines is null || Meta?.Lines is null)
                return [];

            var detailLabels = Meta.Lines
                .Where(ml => ml.Scope == MetricLineScope.Detail)
                .Select(ml => ml.Label)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return Data.Lines
                .Where(l => detailLabels.Contains(l.Label))
                .ToList();
        }
    }

    public IReadOnlyList<PluginLink> Links => Meta?.Links ?? [];

    [RelayCommand]
    private async Task Refresh()
    {
        if (Meta is null) return;

        IsLoading = true;
        WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage(Meta.Id));
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void GoBack()
    {
        WeakReferenceMessenger.Default.Send(new NavigateToOverviewMessage());
    }

    public void LoadProvider(string providerId, PluginMeta meta, PluginOutput? data)
    {
        Meta = meta;
        Data = data;
        IsLoading = data is null;
    }

    partial void OnMetaChanged(PluginMeta? value)
    {
        OnPropertyChanged(nameof(Links));
        OnPropertyChanged(nameof(OverviewLines));
        OnPropertyChanged(nameof(DetailLines));
    }

    partial void OnDataChanged(PluginOutput? value)
    {
        OnPropertyChanged(nameof(OverviewLines));
        OnPropertyChanged(nameof(DetailLines));
    }
}
