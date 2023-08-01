using System;
using System.Windows;
using System.Windows.Threading;

namespace WPFMicaBackdrop.Backdrop;

public interface ISystemBackdrop : IDisposable
{
    void InitializeWithWindow(Window window);
}

public static class SystemBackdrop
{
    public static readonly DependencyProperty BackdropProperty =
        DependencyProperty.RegisterAttached("Backdrop", typeof(ISystemBackdrop),
            typeof(SystemBackdrop), new FrameworkPropertyMetadata(OnBackdropChangedCallback));

    public static void OnBackdropChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var oldBackdrop = e.OldValue as ISystemBackdrop;
        oldBackdrop?.Dispose();

        var newBackdrop = e.NewValue as ISystemBackdrop;
        if (newBackdrop is not null && d is Window window)
        {
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                newBackdrop.InitializeWithWindow(window);
            });
        }
    }

    public static ISystemBackdrop GetBackdrop(Window window)
    {
        return (ISystemBackdrop)window.GetValue(BackdropProperty);
    }

    public static void SetBackdrop(Window window, ISystemBackdrop value)
    {
        window.SetValue(BackdropProperty, value);
    }
}
