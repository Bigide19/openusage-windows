using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenUsage.Core.Enums;
using OpenUsage.Core.Models;
using OpenUsage.ViewModels.Messages;

namespace OpenUsage.ViewModels;

public partial class ProviderCardViewModel : ObservableObject
{
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromMinutes(5);

    [ObservableProperty]
    private PluginMeta _meta = null!;

    [ObservableProperty]
    private PluginOutput? _data;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private DateTime? _lastManualRefreshAt;

    public ProviderCardViewModel(PluginMeta meta)
    {
        _meta = meta;
    }

    public IReadOnlyList<MetricLine> OverviewLines
    {
        get
        {
            if (Data?.Lines is null)
                return [];

            // If no manifest lines defined or no overview scope, show all lines
            var overviewLabels = Meta?.Lines?
                .Where(ml => ml.Scope == MetricLineScope.Overview)
                .Select(ml => ml.Label)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (overviewLabels is null or { Count: 0 })
                return Data.Lines;

            // Show lines matching overview labels + always show error badges
            return Data.Lines
                .Where(l => overviewLabels.Contains(l.Label)
                         || (l is BadgeMetricLine badge && badge.Label == "Error"))
                .ToList();
        }
    }

    public ProgressMetricLine? PrimaryProgress
    {
        get
        {
            var progressLines = OverviewLines.OfType<ProgressMetricLine>().ToList();
            if (progressLines.Count == 0)
                return null;

            var candidates = Meta?.PrimaryCandidates;
            if (candidates is { Count: > 0 })
            {
                foreach (var candidate in candidates)
                {
                    var match = progressLines.FirstOrDefault(
                        p => string.Equals(p.Label, candidate, StringComparison.OrdinalIgnoreCase));
                    if (match is not null)
                        return match;
                }
            }

            return progressLines.FirstOrDefault();
        }
    }

    public bool CanRefresh =>
        LastManualRefreshAt is null ||
        DateTime.UtcNow - LastManualRefreshAt.Value >= RefreshCooldown;

    [RelayCommand]
    private void NavigateToDetail()
    {
        WeakReferenceMessenger.Default.Send(new NavigateToProviderMessage(Meta.Id));
    }

    [RelayCommand]
    private void Refresh()
    {
        if (!CanRefresh)
            return;

        LastManualRefreshAt = DateTime.UtcNow;
        WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage(Meta.Id));
    }

    public void UpdateData(PluginOutput output)
    {
        Data = output;
        IsLoading = false;
        Error = null;
        OnPropertyChanged(nameof(OverviewLines));
        OnPropertyChanged(nameof(PrimaryProgress));
    }

    partial void OnDataChanged(PluginOutput? value)
    {
        OnPropertyChanged(nameof(OverviewLines));
        OnPropertyChanged(nameof(PrimaryProgress));
    }

    partial void OnLastManualRefreshAtChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(CanRefresh));
    }
}
