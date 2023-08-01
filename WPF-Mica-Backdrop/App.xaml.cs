using System.Windows;

using Windows.UI.Composition;

namespace WPFMicaBackdrop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static Compositor Compositor { get; }

    public static new App Current => (App)Application.Current;

    static App()
    {
        WindowsSystemDispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();
        Compositor = new Compositor() { Comment = "MainCompositor" };
    }
}
