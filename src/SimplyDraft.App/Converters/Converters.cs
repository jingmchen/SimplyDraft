using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace SimplyDraft.App.Converters;

// Transforms data between ViewModel and View when types need reformatting

// All converters are static singletons to prevent unnecessary instance creations (Avalonia allocates a new converter instance every time a binding is evaluated)
// All converters are valid for static singletons as they are stateless
// - Convert() takes inputs and returns an output with no stored state between calls
// All converters have a private constructor to enforce singleton at compile time (prevent new Converter() elsewhere)

// For calling in .axaml files, declare in namespace: xmlns:conv="clr-namespace:SimplyDraft.App.Converters"
// For binding in .axaml files, call for example,
// <TextBlock Text="{Binding IsExpanded, Converter={x:Static conv:BoolToExpandIconConverter.Instance}}" />

/// <summary>Returns 0.4 opacity when true (cut items), 1.0 otherwise.</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new ();
    private BoolToOpacityConverter() {} // Enforce singleton at compile time
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b) return AvaloniaProperty.UnsetValue;
        return b ? 0.4 : 1.0;
    }
    // One-way converter
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class PathToBitmapConverter : IValueConverter
{
    public static readonly PathToBitmapConverter Instance = new();
    private PathToBitmapConverter() {}
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path)) return null;
        try {return new Bitmap(path);}
        catch {return null;}
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}