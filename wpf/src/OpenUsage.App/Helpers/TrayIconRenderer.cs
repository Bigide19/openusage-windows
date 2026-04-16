using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenUsage.App.Helpers;

internal static class TrayIconRenderer
{
    // Render at 64 px so text glyphs stay crisp when Windows scales down to
    // the tray's display size. 32 px had too few pixels for readable digits.
    private const int IconSize = 64;
    private const double CornerRadius = 12;

    private static readonly Color DefaultBrand = Color.FromRgb(0x3A, 0x3A, 0x3C);

    public static Color ParseBrandColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return DefaultBrand;
        try
        {
            var converted = ColorConverter.ConvertFromString(hex);
            return converted is Color c ? c : DefaultBrand;
        }
        catch (FormatException) { return DefaultBrand; }
    }

    public static ImageSource RenderPercent(int percent, Color brandColor, string? iconUrl = null)
    {
        percent = Math.Clamp(percent, 0, 999);
        var text = percent.ToString(CultureInfo.InvariantCulture);

        var fontSize = text.Length switch
        {
            1 => 44.0,
            2 => 36.0,
            _ => 26.0,
        };

        var bgBrush = new SolidColorBrush(brandColor);
        bgBrush.Freeze();

        // Brand color at ~60% for the SVG — recognizable on the taskbar.
        var watermarkBrush = new SolidColorBrush(Color.FromArgb(160, brandColor.R, brandColor.G, brandColor.B));
        watermarkBrush.Freeze();

        var shadowBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
        shadowBrush.Freeze();

        var typeface = new Typeface(
            new FontFamily("Segoe UI"),
            FontStyles.Normal,
            FontWeights.Bold,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            pixelsPerDip: 1.0);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // 1) Transparent background
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, IconSize, IconSize));

            // 2) Provider SVG icon in brand color
            DrawSvgWatermark(dc, iconUrl, watermarkBrush);

            // 3) Bold white number with dark drop shadow for readability
            //    on both light and dark taskbars.
            var x = (IconSize - formatted.Width) / 2.0;
            var y = (IconSize - formatted.Height) / 2.0;

            var shadowFormatted = new FormattedText(
                text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, fontSize, shadowBrush, pixelsPerDip: 1.0);
            dc.DrawText(shadowFormatted, new Point(x + 1.5, y + 1.5));
            dc.DrawText(formatted, new Point(x, y));
        }

        return RenderToIco(visual);
    }

    private static void DrawSvgWatermark(DrawingContext dc, string? iconUrl, Brush brush)
    {
        if (string.IsNullOrEmpty(iconUrl) ||
            !iconUrl.StartsWith("data:image/svg+xml;base64,"))
            return;

        List<string> pathStrings;
        try
        {
            var base64 = iconUrl["data:image/svg+xml;base64,".Length..];
            var svgXml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            pathStrings = SvgIconHelper.ExtractSvgPaths(svgXml);
        }
        catch { return; }

        if (pathStrings.Count == 0) return;

        var combined = new GeometryGroup { FillRule = FillRule.Nonzero };
        foreach (var d in pathStrings)
        {
            try { combined.Children.Add(Geometry.Parse(d)); }
            catch { /* skip */ }
        }

        var bounds = combined.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var margin = IconSize * 0.075;
        var target = IconSize - margin * 2;
        var scale = Math.Min(target / bounds.Width, target / bounds.Height);
        var offsetX = (IconSize - bounds.Width * scale) / 2 - bounds.X * scale;
        var offsetY = (IconSize - bounds.Height * scale) / 2 - bounds.Y * scale;

        dc.PushTransform(new TransformGroup
        {
            Children =
            {
                new ScaleTransform(scale, scale),
                new TranslateTransform(offsetX, offsetY)
            }
        });
        dc.DrawGeometry(brush, null, combined);
        dc.Pop();
    }

    private static ImageSource RenderToIco(DrawingVisual visual)
    {
        var bmp = new RenderTargetBitmap(IconSize, IconSize, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();

        var pngEncoder = new PngBitmapEncoder();
        pngEncoder.Frames.Add(BitmapFrame.Create(bmp));
        using var pngStream = new MemoryStream();
        pngEncoder.Save(pngStream);
        var pngBytes = pngStream.ToArray();

        var path = Path.Combine(Path.GetTempPath(), "openusage-tray-icon.ico");
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);
            // width/height: 0 means 256 in ICO spec; for 64 use the literal
            writer.Write((byte)IconSize);
            writer.Write((byte)IconSize);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)pngBytes.Length);
            writer.Write((uint)(6 + 16));
            writer.Write(pngBytes);
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
