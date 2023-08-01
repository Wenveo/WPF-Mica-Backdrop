
using System;

namespace WPFMicaBackdrop.Controls;

public sealed class XamlControlsThemeChangedEventArgs : EventArgs
{
    public XamlControlsTheme ActualTheme { get; }

    public XamlControlsTheme RequestedTheme { get; }

    public XamlControlsThemeChangedEventArgs(XamlControlsTheme actualTheme, XamlControlsTheme requestedTheme)
    {
        ActualTheme = actualTheme;
        RequestedTheme = requestedTheme;
    }
}