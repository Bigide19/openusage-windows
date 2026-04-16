using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace OpenUsage.App.Helpers;

/// <summary>
/// Converts SVG data URLs (data:image/svg+xml;base64,...) to WPF Viewbox elements.
/// Falls back to a colored circle with initial letter.
/// </summary>
public static class SvgIconHelper
{
    /// <summary>
    /// Create a FrameworkElement from an SVG data URL or return a fallback icon.
    /// </summary>
    public static FrameworkElement CreateIcon(string? iconUrl, string name, string? brandColor, double size = 20)
    {
        if (!string.IsNullOrEmpty(iconUrl) && iconUrl.StartsWith("data:image/svg+xml;base64,"))
        {
            try
            {
                var base64 = iconUrl["data:image/svg+xml;base64,".Length..];
                var svgBytes = Convert.FromBase64String(base64);
                var svgXml = System.Text.Encoding.UTF8.GetString(svgBytes);

                // Try to convert SVG to XAML DrawingGroup
                var element = RenderSvgAsPath(svgXml, brandColor, size);
                if (element != null) return element;
            }
            catch
            {
                // Fall through to fallback
            }
        }

        return CreateFallbackIcon(name, brandColor, size);
    }

    private static FrameworkElement? RenderSvgAsPath(string svgXml, string? brandColor, double size)
    {
        try
        {
            // Simple approach: render SVG text as a glyph-like display
            // For complex SVGs, we show the first letter with brand color
            // This is a pragmatic choice since full SVG→XAML conversion is complex

            var color = ParseColor(brandColor) ?? Colors.Gray;

            var grid = new Grid { Width = size, Height = size };
            var border = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B))
            };
            grid.Children.Add(border);

            // Extract viewBox and paths from SVG for simple icon rendering
            var paths = ExtractSvgPaths(svgXml);
            if (paths.Count > 0)
            {
                var canvas = new Canvas
                {
                    Width = size,
                    Height = size,
                    ClipToBounds = true
                };

                foreach (var pathData in paths)
                {
                    try
                    {
                        var geometry = Geometry.Parse(pathData);
                        var bounds = geometry.Bounds;
                        if (bounds.Width <= 0 || bounds.Height <= 0) continue;

                        var scale = Math.Min(size * 0.7 / bounds.Width, size * 0.7 / bounds.Height);
                        var offsetX = (size - bounds.Width * scale) / 2 - bounds.X * scale;
                        var offsetY = (size - bounds.Height * scale) / 2 - bounds.Y * scale;

                        var path = new System.Windows.Shapes.Path
                        {
                            Data = geometry,
                            Fill = new SolidColorBrush(color),
                            RenderTransform = new TransformGroup
                            {
                                Children =
                                {
                                    new ScaleTransform(scale, scale),
                                    new TranslateTransform(offsetX, offsetY)
                                }
                            }
                        };
                        canvas.Children.Add(path);
                    }
                    catch { /* skip invalid path */ }
                }

                if (canvas.Children.Count > 0)
                {
                    grid.Children.Clear();
                    grid.Children.Add(canvas);
                    return grid;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    internal static List<string> ExtractSvgPaths(string svgXml)
    {
        var paths = new List<string>();
        try
        {
            using var reader = XmlReader.Create(new StringReader(svgXml));
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "path")
                {
                    var d = reader.GetAttribute("d");
                    if (!string.IsNullOrEmpty(d))
                        paths.Add(d);
                }
            }
        }
        catch { /* ignore parse errors */ }
        return paths;
    }

    public static FrameworkElement CreateFallbackIcon(string name, string? brandColor, double size = 20)
    {
        var color = ParseColor(brandColor) ?? Colors.Gray;
        var initial = name.Length > 0 ? name[..1].ToUpperInvariant() : "?";

        var grid = new Grid { Width = size, Height = size };
        grid.Children.Add(new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B))
        });
        grid.Children.Add(new TextBlock
        {
            Text = initial,
            FontSize = size * 0.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        return grid;
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        try
        {
            var c = ColorConverter.ConvertFromString(hex);
            return c is Color color ? color : null;
        }
        catch { return null; }
    }
}
