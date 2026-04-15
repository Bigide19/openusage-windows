using OpenUsage.Core.Models;

namespace OpenUsage.ViewModels.Messages;

public record NavigateToProviderMessage(string ProviderId);

public record NavigateToOverviewMessage;

public record NavigateToSettingsMessage;

public record PluginDataUpdatedMessage(string ProviderId, PluginOutput Output);

public record RefreshRequestedMessage(string? ProviderId);

public record PanelToggleMessage;

public record ShowAboutMessage;
