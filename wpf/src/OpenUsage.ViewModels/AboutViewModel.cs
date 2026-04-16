using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenUsage.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppVersion { get; init; } = "0.1.1";

    public string ProjectUrl => "https://github.com/Bigide19/openusage-windows";

    public string UpstreamUrl => "https://github.com/robinebers/openusage";
}
