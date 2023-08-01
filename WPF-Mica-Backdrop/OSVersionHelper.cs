using System;

namespace WPFMicaBackdrop;

public static class OSVersionHelper
{
    public static bool IsWindows10_1809_AtLatest { get; }

    public static bool IsWindows10_1903_AtLatest { get; }

    public static bool IsWindows11_AtLatest { get; }

    static OSVersionHelper()
    {
        IsWindows10_1809_AtLatest = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 17763;
        IsWindows10_1903_AtLatest = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 18362;
        IsWindows11_AtLatest = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
    }
}
