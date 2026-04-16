using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace OpenUsage.App.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void ProjectUrl_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.AboutViewModel vm)
        {
            Process.Start(new ProcessStartInfo(vm.ProjectUrl) { UseShellExecute = true });
        }
    }

    private void UpstreamUrl_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.AboutViewModel vm)
        {
            Process.Start(new ProcessStartInfo(vm.UpstreamUrl) { UseShellExecute = true });
        }
    }
}
