using System;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;


namespace WPFMicaBackdrop.Controls;

public sealed class ThemeListener
{
    private static readonly Lazy<ThemeListener> s_lazy = new(() => new());

    public static ThemeListener Shared => s_lazy.Value;


    private XamlControlsTheme _requestedTheme;

    public XamlControlsTheme RequestedTheme
    {
        get => _requestedTheme;
        set
        {
            if (value != _requestedTheme)
            {
                _requestedTheme = value;
                OnRequestedThemeChanged(value);
            }
        }
    }

    public XamlControlsTheme ActualTheme { get; private set; }

    public bool IsHighContrast
    {
        get
        {
            unsafe
            {
                var highContrast = new HIGHCONTRASTW
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(HIGHCONTRASTW))
                };

                var ret = PInvoke.SystemParametersInfo(
                    SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST,
                    highContrast.cbSize, &highContrast, 0);

                return ret > 0 && (highContrast.dwFlags & HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON) == HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON;
            }
        }
    }

    public event EventHandler<XamlControlsThemeChangedEventArgs> ThemeChanged;

    private ThemeListener()
    {
        ApplyThemeForApp(ShouldUseDarkMode());
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private bool ShouldUseDarkMode()
    {
        static bool GetAppsUseDarkThemeFromRegistry()
        {
            const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string RegistryValueName = "AppsUseLightTheme";

            var registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            if (registryKey is null)
                return false;

            var registryValue = registryKey.GetValue(RegistryValueName);
            return registryValue is int i && i == 0;
        }

        var useDarkMode = OSVersionHelper.IsWindows10_1903_AtLatest
            ? UXTheme.ShouldSystemUseDarkMode() : OSVersionHelper.IsWindows10_1809_AtLatest
            ? UXTheme.ShouldAppsUseDarkMode() : GetAppsUseDarkThemeFromRegistry();

        return useDarkMode && !IsHighContrast;
    }

    private void OnRequestedThemeChanged(XamlControlsTheme value)
    {
        if (value is XamlControlsTheme.Default)
            ApplyThemeForApp(ShouldUseDarkMode());
        else
        {
            ApplyThemeForApp(value is XamlControlsTheme.Dark);
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (RequestedTheme is XamlControlsTheme.Default)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ApplyThemeForApp(ShouldUseDarkMode());
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void ApplyThemeForApp(bool isDark)
    {
        var newTheme = BooleanToTheme(isDark);
        if (newTheme == ActualTheme)
            return;

        ActualTheme = newTheme;

        if (OSVersionHelper.IsWindows10_1903_AtLatest)
        {
            UXTheme.SetPreferredAppMode(isDark ? UXTheme.PreferredAppMode.AllowDark : UXTheme.PreferredAppMode.Default);
            UXTheme.FlushMenuThemes();
        }
        else if (OSVersionHelper.IsWindows10_1809_AtLatest)
        {
            UXTheme.AllowDarkModeForApp(isDark);
            UXTheme.FlushMenuThemes();
        }

        ThemeChanged?.Invoke(this, new(isDark ? XamlControlsTheme.Dark : XamlControlsTheme.Light, RequestedTheme));
    }

    private XamlControlsTheme BooleanToTheme(bool isDark)
    {
        return isDark ? XamlControlsTheme.Dark : XamlControlsTheme.Light;
    }

    private static partial class UXTheme
    {
        // 1809
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        internal static extern int AllowDarkModeForApp([MarshalAs(UnmanagedType.Bool)] bool allow);

        // 1809
        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true)]
        internal static extern int FlushMenuThemes();

        // 1809
        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShouldAppsUseDarkMode();

        // 1903
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        internal static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

        // 1903
        [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShouldSystemUseDarkMode();

        internal enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight
        };
    }
}
