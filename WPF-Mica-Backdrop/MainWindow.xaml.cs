using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

using WPFMicaBackdrop.Controls;

namespace WPFMicaBackdrop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    internal WindowInteropHelper WindowHelper { get; private set; }

    internal HwndSource WindowSource { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowHelper = new WindowInteropHelper(this);
        WindowSource = (HwndSource)PresentationSource.FromVisual(this);

        ThemeListener.Shared.ThemeChanged += OnThemeChanged;
        OnThemeChangedImpl(ThemeListener.Shared.ActualTheme);
    }

    private void OnThemeChanged(object sender, XamlControlsThemeChangedEventArgs e)
    {
        OnThemeChangedImpl(e.ActualTheme);
    }

    private void OnThemeChangedImpl(XamlControlsTheme newTheme)
    {
        unsafe
        {
            var isDark = newTheme is not XamlControlsTheme.Light;

            // Fallback color
            WindowSource.CompositionTarget.BackgroundColor = isDark
                ? Color.FromArgb(255, 32, 32, 32)
                : Color.FromArgb(255, 243, 243, 243);

            if (OSVersionHelper.IsWindows10_1809_AtLatest)
            {
                var windowHandle = (HWND)WindowHelper.Handle;
                PInvoke.DwmSetWindowAttribute(windowHandle, OSVersionHelper.IsWindows11_AtLatest
                    ? DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE
                    : (DWMWINDOWATTRIBUTE)19, &isDark, (uint)Marshal.SizeOf(isDark));

                PInvoke.UpdateWindow(windowHandle);
            }
        }
    }
}
