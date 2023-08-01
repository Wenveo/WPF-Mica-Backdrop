using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

using WPFMicaBackdrop.Controls;

using WinUIColor = Windows.UI.Color;

namespace WPFMicaBackdrop.Backdrop;

public sealed class MicaBackdrop : ISystemBackdrop
{
    private static readonly WinUIColor s_darkThemeColor = WinUIColor.FromArgb(255, 32, 32, 32);
    private static readonly float s_darkThemeTintOpacity = 0.8f;

    private static readonly WinUIColor s_lightThemeColor = WinUIColor.FromArgb(255, 243, 243, 243);
    private static readonly float s_lightThemeTintOpacity = 0.5f;

    public static bool IsSupported { get; } =
        Windows.Foundation.Metadata.ApiInformation.IsMethodPresent(
            "Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush");

    private readonly Compositor _compositor;
    private readonly ContainerVisual _containerVisual;
    private readonly SpriteVisual _spriteVisual;
    private DesktopWindowTarget _target;
    private Window _parentWindow;
    private IntPtr _hWnd;
    private bool _isActive;

    private WinUIColor _fallbackColor;
    private WinUIColor _tintColor;
    private float _tintOpacity;
    private float _luminosityOpacity;

    public WinUIColor FallbackColor
    {
        get => _fallbackColor;
        set => UpdateProperty(ref _fallbackColor, value);
    }

    public WinUIColor TintColor
    {
        get => _tintColor;
        set => UpdateProperty(ref _tintColor, value);
    }

    public float TintOpacity
    {
        get => _tintOpacity;
        set => UpdateProperty(ref _tintOpacity, value);
    }

    public float LuminosityOpacity
    {
        get => _luminosityOpacity;
        set => UpdateProperty(ref _luminosityOpacity, value);
    }

    public bool AllowCustomColors
    {
        get; set;
    }

    public MicaBackdrop()
    {
        _compositor = App.Compositor;

        _spriteVisual = _compositor.CreateSpriteVisual();
        _spriteVisual.RelativeSizeAdjustment = new Vector2(1, 1);

        _containerVisual = _compositor.CreateContainerVisual();
        _containerVisual.Children.InsertAtTop(_spriteVisual);
        _containerVisual.RelativeSizeAdjustment = new Vector2(1, 1);

        // Default Values
        _tintOpacity = 0.05f;
        _luminosityOpacity = 0.15f;

        _tintColor = Windows.UI.Colors.Teal;
        _fallbackColor = _tintColor;
    }

    public void InitializeWithWindow(Window window)
    {
        _hWnd = new WindowInteropHelper(window).Handle;
        var interop = (ICompositorDesktopInterop)(object)_compositor;
        interop.CreateDesktopWindowTarget(_hWnd, true, out var target);

        var rawObject = Marshal.GetObjectForIUnknown(target);
        _target = (DesktopWindowTarget)rawObject;
        _target.Root = _containerVisual;

        _parentWindow = window;
        _isActive = _parentWindow.IsActive;
        SubscribeOrUnSubscribeEvents(true);

        if (!AllowCustomColors)
        {
            ApplyTheme(ThemeListener.Shared.ActualTheme is XamlControlsTheme.Dark);
        }
    }

    public void Dispose()
    {
        SubscribeOrUnSubscribeEvents(false);
    }

    private void SubscribeOrUnSubscribeEvents(bool subscribe)
    {
        void OnWindowActivated(object sender, EventArgs e) => OnActivated();

        void OnWindowDeactivated(object sender, EventArgs e) => OnDeactivated();

        void OnThemeChanged(object sender, XamlControlsThemeChangedEventArgs args) => ApplyTheme(args.ActualTheme is XamlControlsTheme.Dark);

        if (_parentWindow is null)
        {
            return;
        }

        if (subscribe)
        {
            _parentWindow.Activated += OnWindowActivated;
            _parentWindow.Deactivated += OnWindowDeactivated;
            ThemeListener.Shared.ThemeChanged += OnThemeChanged;
        }
        else
        {
            _parentWindow.Activated -= OnWindowActivated;
            _parentWindow.Deactivated -= OnWindowDeactivated;
            ThemeListener.Shared.ThemeChanged -= OnThemeChanged;
        }
    }


    private void OnActivated()
    {
        if (_isActive)
        {
            return;
        }

        var newBrush = IsSupported ? SystemBackdropBrushFactory.BuildMicaEffectBrush(_compositor, _tintColor, _tintOpacity, _luminosityOpacity) : _compositor.CreateColorBrush(_tintColor);

        UpdateBrush(newBrush);
        _isActive = true;
    }

    private void OnDeactivated()
    {
        UpdateBrush(_compositor.CreateColorBrush(_tintColor));
        _isActive = false;
    }

    private void ApplyTheme(bool isDark)
    {
        if (!AllowCustomColors)
        {
            if (isDark)
            {
                _tintColor = s_darkThemeColor;
                _tintOpacity = s_darkThemeTintOpacity;
                _luminosityOpacity = 1.0f;
            }
            else
            {
                _tintColor = s_lightThemeColor;
                _tintOpacity = s_lightThemeTintOpacity;
                _luminosityOpacity = 1.0f;
            }

            _fallbackColor = _tintColor;
            Update();
        }
    }

    private void Update()
    {
        _compositor.DispatcherQueue.TryEnqueue(() =>
        {
            if (_isActive)
            {
                _isActive = false;
                OnActivated();
            }
            else
            {
                OnDeactivated();
            }
        });
    }

    private void UpdateProperty<T>(ref T field, T newValue)
    {
        field = newValue;
        _compositor.DispatcherQueue.TryEnqueue(() =>
        {
            if (_isActive)
            {
                _isActive = false;
                OnActivated();
            }
            else
            {
                OnDeactivated();
            }
        });
    }

    private void UpdateBrush(CompositionBrush newBrush)
    {
        var oldBrush = _spriteVisual.Brush;
        if (oldBrush == null || oldBrush.Comment == "Crossfade" || (oldBrush is CompositionColorBrush && newBrush is CompositionColorBrush))
        {
            oldBrush?.Dispose();
            _spriteVisual.Brush = newBrush;
        }
        else
        {
            // Crossfade
            var crossFadeBrush = SystemBackdropBrushFactory.CreateCrossFadeEffectBrush(_compositor, oldBrush, newBrush);
            var animation = SystemBackdropBrushFactory.CreateCrossFadeAnimation(_compositor);
            _spriteVisual.Brush = crossFadeBrush;

            var crossFadeAnimationBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            crossFadeBrush.StartAnimation("CrossFade.CrossFade", animation);
            crossFadeAnimationBatch.End();

            crossFadeAnimationBatch.Completed += (o, a) =>
            {
                crossFadeBrush.Dispose();
                oldBrush.Dispose();
                _spriteVisual.Brush = newBrush;
            };
        }
    }

    [ComImport]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr target);
    }
}
