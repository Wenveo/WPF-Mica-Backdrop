using System.Windows;

namespace WPFMicaBackdrop.Controls;

public sealed class XamlControlsResources : ResourceDictionary
{
    public XamlControlsTheme ActualTheme
    {
        get => ThemeListener.Shared.ActualTheme;
    }

    public XamlControlsTheme RequestedTheme
    {
        get => ThemeListener.Shared.RequestedTheme;
        set => ThemeListener.Shared.RequestedTheme = value;
    }
}
