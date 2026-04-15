using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenUsage.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppVersion { get; init; } = "1.0.0";

    public string ProjectUrl => "https://github.com/robinebers/openusage";
}
