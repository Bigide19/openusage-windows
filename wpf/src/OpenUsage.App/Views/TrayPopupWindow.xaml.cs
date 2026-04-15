using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using OpenUsage.ViewModels;

namespace OpenUsage.App.Views;

public partial class TrayPopupWindow : Window
{
    private bool _isPinned;

    public TrayPopupWindow()
    {
        InitializeComponent();
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't drag if click is on an interactive control
        if (e.OriginalSource is DependencyObject src && IsInteractive(src))
            return;

        try { DragMove(); } catch { }
    }

    private static bool IsInteractive(DependencyObject element)
    {
        var cur = element;
        while (cur is not null)
        {
            if (cur is System.Windows.Controls.Primitives.ButtonBase
                or System.Windows.Controls.Primitives.RangeBase
                or System.Windows.Controls.ComboBox
                or System.Windows.Controls.ListBox
                or System.Windows.Controls.TextBox
                or System.Windows.Controls.Primitives.ScrollBar)
                return true;
            cur = System.Windows.Media.VisualTreeHelper.GetParent(cur)
                  ?? LogicalTreeHelper.GetParent(cur);
        }
        return false;
    }

    public void ShowPopup()
    {
        PositionNearTray();
        Show();
        Activate();
    }

    public void HidePopup()
    {
        Hide();
    }

    private void SideNav_PinToggled(bool isPinned)
    {
        _isPinned = isPinned;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (!_isPinned)
            HidePopup();
    }

    private void SideNav_ProviderSelected(string providerId)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.NavigateToProvider(providerId);
        }
    }

    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // Determine taskbar position
        bool taskbarAtBottom = workArea.Bottom < screenHeight;
        bool taskbarAtTop = workArea.Top > 0;
        bool taskbarAtRight = workArea.Right < screenWidth;
        bool taskbarAtLeft = workArea.Left > 0;

        // Get cursor position as proxy for tray icon location
        GetCursorPos(out var cursorPos);
        var dpi = GetDpiScale();

        double cursorX = cursorPos.X / dpi;
        double cursorY = cursorPos.Y / dpi;

        if (taskbarAtBottom)
        {
            Left = Math.Max(workArea.Left, Math.Min(cursorX - Width / 2, workArea.Right - Width));
            Top = workArea.Bottom - Height;
        }
        else if (taskbarAtTop)
        {
            Left = Math.Max(workArea.Left, Math.Min(cursorX - Width / 2, workArea.Right - Width));
            Top = workArea.Top;
        }
        else if (taskbarAtRight)
        {
            Left = workArea.Right - Width;
            Top = Math.Max(workArea.Top, Math.Min(cursorY - Height / 2, workArea.Bottom - Height));
        }
        else if (taskbarAtLeft)
        {
            Left = workArea.Left;
            Top = Math.Max(workArea.Top, Math.Min(cursorY - Height / 2, workArea.Bottom - Height));
        }
        else
        {
            // Fallback: bottom right
            Left = workArea.Right - Width;
            Top = workArea.Bottom - Height;
        }
    }

    private double GetDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformToDevice.M11;
        return 1.0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
