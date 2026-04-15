using OpenUsage.Core.Models;

namespace OpenUsage.Core.Interfaces;

public interface IPluginLoader
{
    List<LoadedPlugin> LoadPlugins(string pluginsDirectory);
}
