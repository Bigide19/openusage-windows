using System.Windows;
using System.Windows.Controls;
using OpenUsage.App.Helpers;
using OpenUsage.ViewModels;

namespace OpenUsage.App.Views.Controls;

public partial class ProviderCard : UserControl
{
    public ProviderCard()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ProviderCardViewModel vm)
        {
            IconContainer.Child = SvgIconHelper.CreateIcon(
                vm.Meta.IconUrl, vm.Meta.Name, vm.Meta.BrandColor, 28);
        }
    }
}
