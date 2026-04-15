using System.ComponentModel;
using System.Runtime.InteropServices;
using OpenUsage.Core.Interfaces;

namespace OpenUsage.Services;

public sealed class HotKeyService : IHotKeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x1000;
    private static readonly IntPtr HwndMessage = new(-3);

    #region P/Invoke

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, IntPtr lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    #endregion

    [Flags]
    private enum HotKeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    private IntPtr _hwnd;
    private WndProcDelegate? _wndProc; // prevent GC collection of the delegate
    private bool _registered;
    private bool _disposed;
    private ushort _classAtom;

    public event EventHandler? HotKeyPressed;

    public void Register(string shortcutString)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HotKeyService));

        Unregister();
        EnsureWindow();

        var (modifiers, vk) = ParseShortcut(shortcutString);

        if (!RegisterHotKey(_hwnd, HotkeyId, (uint)modifiers, vk))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register hotkey '{shortcutString}'. Win32 error: {error}");
        }

        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        UnregisterHotKey(_hwnd, HotkeyId);
        _registered = false;
    }

    private void EnsureWindow()
    {
        if (_hwnd != IntPtr.Zero) return;

        var hInstance = GetModuleHandle(null);
        _wndProc = WndProc;

        var className = "OpenUsage_HotKey_" + Environment.ProcessId;
        var wc = new WNDCLASS
        {
            lpfnWndProc = _wndProc,
            hInstance = hInstance,
            lpszClassName = className
        };

        _classAtom = RegisterClassW(ref wc);
        if (_classAtom == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register window class.");

        _hwnd = CreateWindowExW(
            0, (IntPtr)_classAtom, "OpenUsage_HotKey",
            0, 0, 0, 0, 0,
            HwndMessage, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create message window.");
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            return IntPtr.Zero;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static (HotKeyModifiers modifiers, uint vk) ParseShortcut(string shortcut)
    {
        var modifiers = HotKeyModifiers.None;
        uint vk = 0;

        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var upper = part.ToUpperInvariant();
            switch (upper)
            {
                case "CTRL" or "CONTROL":
                    modifiers |= HotKeyModifiers.Ctrl;
                    break;
                case "ALT":
                    modifiers |= HotKeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotKeyModifiers.Shift;
                    break;
                case "WIN" or "WINDOWS":
                    modifiers |= HotKeyModifiers.Win;
                    break;
                default:
                    vk = MapKeyNameToVk(upper)
                         ?? throw new ArgumentException($"Unknown key: '{part}' in shortcut '{shortcut}'");
                    break;
            }
        }

        if (vk == 0)
            throw new ArgumentException($"No valid key found in shortcut '{shortcut}'");

        return (modifiers, vk);
    }

    private static uint? MapKeyNameToVk(string keyName)
    {
        // Single character keys (A-Z, 0-9)
        if (keyName.Length == 1)
        {
            var ch = keyName[0];
            if (ch is >= 'A' and <= 'Z') return ch; // VK_A..VK_Z = 0x41..0x5A
            if (ch is >= '0' and <= '9') return ch; // VK_0..VK_9 = 0x30..0x39
        }

        // Function keys
        if (keyName.StartsWith("F") && int.TryParse(keyName[1..], out var fNum) && fNum is >= 1 and <= 24)
            return (uint)(0x70 + fNum - 1); // VK_F1 = 0x70

        // Named keys
        return keyName switch
        {
            "SPACE" => 0x20u,
            "ENTER" or "RETURN" => 0x0Du,
            "TAB" => 0x09u,
            "ESCAPE" or "ESC" => 0x1Bu,
            "BACKSPACE" or "BACK" => 0x08u,
            "DELETE" or "DEL" => 0x2Eu,
            "INSERT" or "INS" => 0x2Du,
            "HOME" => 0x24u,
            "END" => 0x23u,
            "PAGEUP" or "PGUP" => 0x21u,
            "PAGEDOWN" or "PGDN" => 0x22u,
            "UP" => 0x26u,
            "DOWN" => 0x28u,
            "LEFT" => 0x25u,
            "RIGHT" => 0x27u,
            "PRINTSCREEN" or "PRTSC" => 0x2Cu,
            "SCROLLLOCK" => 0x91u,
            "PAUSE" => 0x13u,
            "NUMLOCK" => 0x90u,
            "CAPSLOCK" => 0x14u,
            "OEM_PLUS" or "PLUS" => 0xBBu,
            "OEM_MINUS" or "MINUS" => 0xBDu,
            "OEM_PERIOD" or "PERIOD" => 0xBEu,
            "OEM_COMMA" or "COMMA" => 0xBCu,
            _ => null
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Unregister();

        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}
