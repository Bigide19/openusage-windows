using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenUsage.App.Helpers;
using OpenUsage.ViewModels;

namespace OpenUsage.App.Views.Controls;

public partial class SideNavigation : UserControl
{
    public static readonly DependencyProperty PluginsProperty =
        DependencyProperty.Register(nameof(Plugins), typeof(IEnumerable), typeof(SideNavigation),
            new PropertyMetadata(null, OnPluginsChanged));

    public static readonly DependencyProperty SelectedPageProperty =
        DependencyProperty.Register(nameof(SelectedPage), typeof(string), typeof(SideNavigation),
            new PropertyMetadata("Overview", OnSelectedPageChanged));

    public static readonly DependencyProperty SelectedProviderIdProperty =
        DependencyProperty.Register(nameof(SelectedProviderId), typeof(string), typeof(SideNavigation),
            new PropertyMetadata(null, OnSelectedPageChanged));

    public static readonly DependencyProperty NavigateToOverviewCommandProperty =
        DependencyProperty.Register(nameof(NavigateToOverviewCommand), typeof(System.Windows.Input.ICommand), typeof(SideNavigation));

    public static readonly DependencyProperty NavigateToSettingsCommandProperty =
        DependencyProperty.Register(nameof(NavigateToSettingsCommand), typeof(System.Windows.Input.ICommand), typeof(SideNavigation));

    public IEnumerable? Plugins
    {
        get => (IEnumerable?)GetValue(PluginsProperty);
        set => SetValue(PluginsProperty, value);
    }

    public string SelectedPage
    {
        get => (string)GetValue(SelectedPageProperty);
        set => SetValue(SelectedPageProperty, value);
    }

    public string? SelectedProviderId
    {
        get => (string?)GetValue(SelectedProviderIdProperty);
        set => SetValue(SelectedProviderIdProperty, value);
    }

    public System.Windows.Input.ICommand? NavigateToOverviewCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(NavigateToOverviewCommandProperty);
        set => SetValue(NavigateToOverviewCommandProperty, value);
    }

    public System.Windows.Input.ICommand? NavigateToSettingsCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(NavigateToSettingsCommandProperty);
        set => SetValue(NavigateToSettingsCommandProperty, value);
    }

    public event Action<string>? ProviderSelected;
    public event Action<bool>? PinToggled;

    private bool _isPinned;

    public SideNavigation()
    {
        InitializeComponent();
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        NavigateToOverviewCommand?.Execute(null);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        NavigateToSettingsCommand?.Execute(null);
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        PinIcon.Text = _isPinned ? "\uE840" : "\uE718";
        PinIcon.Foreground = _isPinned
            ? (Brush)FindResource("AccentHighlightBrush")
            : (Brush)FindResource("TextSecondaryBrush");
        PinToggled?.Invoke(_isPinned);
    }

    private static void OnPluginsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SideNavigation nav)
        {
            // Unsubscribe from old collection
            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= nav.OnPluginsCollectionChanged;

            // Subscribe to new collection
            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += nav.OnPluginsCollectionChanged;

            nav.RebuildPluginButtons();
        }
    }

    private void OnPluginsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildPluginButtons();
    }

    private static void OnSelectedPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SideNavigation nav)
            nav.UpdateActiveStates();
    }

    private void RebuildPluginButtons()
    {
        PluginPanel.Children.Clear();

        if (Plugins is null) return;

        foreach (var item in Plugins)
        {
            if (item is not ProviderCardViewModel vm) continue;

            var btn = new Button
            {
                Style = (Style)FindResource("NavButtonStyle"),
                Width = 48,
                Height = 44,
                ToolTip = vm.Meta.Name,
                DataContext = vm.Meta.Id,
                FocusVisualStyle = null,
                Focusable = false,
            };

            var icon = SvgIconHelper.CreateIcon(vm.Meta.IconUrl, vm.Meta.Name, vm.Meta.BrandColor, 22);
            btn.Content = icon;

            var providerId = vm.Meta.Id;
            btn.Click += (_, _) => ProviderSelected?.Invoke(providerId);

            PluginPanel.Children.Add(btn);
        }

        UpdateActiveStates();
    }

    private void SetFallbackContent(Button btn, string name)
    {
        btn.Content = new TextBlock
        {
            Text = name.Length > 0 ? name[..1].ToUpperInvariant() : "?",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void UpdateActiveStates()
    {
        var activeBrush = (Brush)FindResource("AccentHighlightBrush");
        var inactiveBrush = (Brush)FindResource("TextSecondaryBrush");

        // Home button
        HomeButton.Tag = SelectedPage == "Overview" ? "Active" : null;
        var homeStroke = SelectedPage == "Overview" ? activeBrush : inactiveBrush;
        HomeIconNeedle.Stroke = homeStroke;
        HomeIconArc.Stroke = homeStroke;

        // Settings button
        SettingsButton.Tag = SelectedPage == "Settings" ? "Active" : null;
        SettingsIcon.Foreground = SelectedPage == "Settings" ? activeBrush : inactiveBrush;

        // Plugin buttons: DataContext holds provider id, Tag drives active indicator
        foreach (var child in PluginPanel.Children)
        {
            if (child is not Button btn) continue;
            if (btn.DataContext is not string id) continue;

            var isActive = SelectedPage == "Detail" &&
                           string.Equals(SelectedProviderId, id, StringComparison.OrdinalIgnoreCase);
            btn.Tag = isActive ? "Active" : null;
        }
    }
}
