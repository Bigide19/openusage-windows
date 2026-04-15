namespace OpenUsage.Core.Interfaces;

public interface IHotKeyService : IDisposable
{
    void Register(string shortcutString);
    void Unregister();
    event EventHandler? HotKeyPressed;
}
