using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace OpenUsage.App.Views;

public partial class ProviderDetailPage : UserControl
{
    public ProviderDetailPage()
    {
        InitializeComponent();
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
