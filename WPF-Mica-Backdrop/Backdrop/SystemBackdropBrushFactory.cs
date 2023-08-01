using System;

using Microsoft.Graphics.Canvas.Effects;

using Windows.UI.Composition;

namespace WPFMicaBackdrop.Backdrop;

internal static class SystemBackdropBrushFactory
{
    public static CompositionBrush BuildMicaEffectBrush(Compositor compositor, Windows.UI.Color tintColor, float tintOpacity, float luminosityOpacity)
    {
        // Tint Color.
        var tintColorEffect = new ColorSourceEffect
        {
            Color = tintColor,
            Name = "TintColor"
        };

        // OpacityEffect applied to Tint.
        var tintOpacityEffect = new OpacityEffect
        {
            Name = "TintOpacity",
            Opacity = tintOpacity,
            Source = tintColorEffect
        };

        // Apply Luminosity:

        // Luminosity Color.
        var luminosityColorEffect = new ColorSourceEffect { Color = tintColor };

        // OpacityEffect applied to Luminosity.
        var luminosityOpacityEffect = new OpacityEffect
        {
            Name = "LuminosityOpacity",
            Opacity = luminosityOpacity,
            Source = luminosityColorEffect
        };

        // Luminosity Blend.
        // NOTE: There is currently a bug where the names of BlendEffectMode::Luminosity and BlendEffectMode::Color are flipped.
        var luminosityBlendEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Color,
            Background = new CompositionEffectSourceParameter("BlurredWallpaperBackdrop"),
            Foreground = luminosityOpacityEffect
        };

        // Apply Tint:

        // Color Blend.
        // NOTE: There is currently a bug where the names of BlendEffectMode::Luminosity and BlendEffectMode::Color are flipped.
        var colorBlendEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Luminosity,
            Background = luminosityBlendEffect,
            Foreground = tintOpacityEffect
        };

        var micaEffectBrush = compositor.CreateEffectFactory(colorBlendEffect).CreateBrush();
        var blurredWallpaperBackdropBrush = compositor.TryCreateBlurredWallpaperBackdropBrush();
        micaEffectBrush.SetSourceParameter("BlurredWallpaperBackdrop", blurredWallpaperBackdropBrush);

        return micaEffectBrush;
    }

    public static CompositionBrush CreateCrossFadeEffectBrush(Compositor compositor, CompositionBrush from, CompositionBrush to)
    {
        var crossFadeEffect = new CrossFadeEffect
        {
            Name = "Crossfade",
            Source1 = new CompositionEffectSourceParameter("source1"),
            Source2 = new CompositionEffectSourceParameter("source2"),
            CrossFade = 0
        };

        var crossFadeEffectBrush = compositor.CreateEffectFactory(crossFadeEffect, new string[] { "Crossfade.CrossFade" }).CreateBrush();
        crossFadeEffectBrush.Comment = "Crossfade";
        // The inputs have to be swapped here to work correctly...
        crossFadeEffectBrush.SetSourceParameter("source1", to);
        crossFadeEffectBrush.SetSourceParameter("source2", from);

        return crossFadeEffectBrush;
    }

    public static ScalarKeyFrameAnimation CreateCrossFadeAnimation(Compositor compositor)
    {
        var animation = compositor.CreateScalarKeyFrameAnimation();
        var linearEasing = compositor.CreateLinearEasingFunction();
        animation.InsertKeyFrame(0.0f, 0.0f, linearEasing);
        animation.InsertKeyFrame(1.0f, 1.0f, linearEasing);
        animation.Duration = TimeSpan.FromMilliseconds(250);
        return animation;
    }
}

