using OpenUsage.Core.Models;

namespace OpenUsage.Core.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
    event EventHandler<AppSettings>? SettingsChanged;
}
