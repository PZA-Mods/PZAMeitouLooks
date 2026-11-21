// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0: the presets and the sliders. A preset picked (on the Settings page, through LooksApi, or kept from the last game) copies its values
// into its effect's sliders; a slider moved by the player turns that effect's list to Custom. The passes always draw the sliders (Pixel(),
// Tilt(), Fisheye(), Outline() below), so what the list says and what the screen shows cannot drift apart.
// Changes go through Meitou's settings group when there is one (saved, shown on the page); else straight to the properties (the offline check).
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public static class LookValues
{
    public static readonly string[] Palettes = ["Desert 16", "LEAF 2-bit", "Full colour", "PS1 15-bit", "N64 16-bit", "8-bit 3-3-2"];
    public static readonly string[] Samplings = ["Centre", "Cell average"];
    public static readonly string[] Natives = ["Off", "224 lines", "240 lines", "480 lines"];
    static readonly int[] NativeLines = [0, 224, 240, 480];
    public static readonly string[] Fits = ["Fill", "Full width"];

    /// <summary>Which list each slider belongs to (the slider's key prefix).</summary>
    public static string? ListOf(string key) => key.Length < 3 ? null : key[..2] switch
    {
        "Px" => nameof(LooksSettings.Look),
        "Ts" => nameof(LooksSettings.TiltShift),
        "Fe" => nameof(LooksSettings.Fisheye),
        "Ol" => nameof(LooksSettings.Outline),
        "Kw" => nameof(LooksSettings.Kuwahara),   // 0.5.0
        "Nf" => nameof(LooksSettings.N64),
        "Tn" => nameof(LooksSettings.Toon),
        _ => null,
    };

    /// <summary>Every effect's preset list, in the Settings page's order.</summary>
    public static readonly string[] Lists = [nameof(LooksSettings.Look), nameof(LooksSettings.TiltShift), nameof(LooksSettings.Fisheye),
        nameof(LooksSettings.Outline), nameof(LooksSettings.Kuwahara), nameof(LooksSettings.N64), nameof(LooksSettings.Toon)];

    [ThreadStatic] static bool t_applying;

    /// <summary>Hooks the group (after PzaSettings.Keep): a list change applies its preset, a slider change makes its list Custom. Then
    /// puts every named preset's values back into its sliders (0.2.x / 0.3.x saved no sliders; a later version may tune a preset).</summary>
    public static void Hook(ModSettingsGroup? group, LooksSettings s)
    {
        if (group is not null) group.Changed += setting => OnChanged(group, s, setting.Key);
        ApplyAll(group, s);
    }

    public static void ApplyAll(ModSettingsGroup? group, LooksSettings s)
    {
        foreach (var list in Lists)
            ApplyPreset(group, s, list);
    }

    /// <summary>The group's Changed handler (public for the offline check, which calls it as the group would).</summary>
    public static void OnChanged(ModSettingsGroup? group, LooksSettings s, string key)
    {
        if (t_applying) return;
        if (ListOf(key) is { } list)
        {
            string now = (string)typeof(LooksSettings).GetProperty(list)!.GetValue(s)!;
            if (now != Looks.Custom && now != Looks.Off) Run(() => Set(group, s, list, Looks.Custom));
            return;
        }
        ApplyPreset(group, s, key);
    }

    /// <summary>Copies the list's named preset (if it names one) into its sliders. Off and Custom leave the sliders as they are.</summary>
    public static void ApplyPreset(ModSettingsGroup? group, LooksSettings s, string list)
    {
        switch (list)
        {
            case nameof(LooksSettings.Look) when Looks.Find(s.Look) is { } l:
                Run(() =>
                {
                    Set(group, s, nameof(s.PxSize), l.PixelSize);
                    Set(group, s, nameof(s.PxPalette), Palettes[Math.Clamp(l.Palette, 0, Palettes.Length - 1)]);
                    Set(group, s, nameof(s.PxLevels), l.Levels);
                    Set(group, s, nameof(s.PxDither), l.Dither);
                    Set(group, s, nameof(s.PxContrast), l.Contrast);
                    Set(group, s, nameof(s.PxShadowLift), l.ShadowLift);
                    Set(group, s, nameof(s.PxLiftNight), l.LiftNight);
                    Set(group, s, nameof(s.PxHighlightHold), l.HighlightHold);
                    Set(group, s, nameof(s.PxScanlines), l.Scanlines);
                    Set(group, s, nameof(s.PxCrtSoft), l.CrtSoft);
                    Set(group, s, nameof(s.PxBleed), l.Bleed);
                    Set(group, s, nameof(s.PxSampling), Samplings[Math.Clamp(l.Sampling, 0, 1)]);
                    Set(group, s, nameof(s.PxNative), NativeName(l.NativeLines));
                    Set(group, s, nameof(s.PxKeepBlack), l.KeepBlack);
                    Set(group, s, nameof(s.PxStrength), l.Strength);
                });
                break;
            case nameof(LooksSettings.TiltShift) when Tilts.Find(s.TiltShift) is { } t:
                Run(() =>
                {
                    Set(group, s, nameof(s.TsCentre), t.FocusCentre);
                    Set(group, s, nameof(s.TsWidth), t.FocusWidth);
                    Set(group, s, nameof(s.TsFalloff), t.Falloff);
                    Set(group, s, nameof(s.TsBlur), t.BlurPx);
                    Set(group, s, nameof(s.TsTop), t.Top);
                    Set(group, s, nameof(s.TsBottom), t.Bottom);
                    Set(group, s, nameof(s.TsSaturation), t.Saturation);
                    Set(group, s, nameof(s.TsContrast), t.Contrast);
                    Set(group, s, nameof(s.TsUpright), t.Upright);
                    Set(group, s, nameof(s.TsStrength), t.Strength);
                });
                break;
            case nameof(LooksSettings.Fisheye) when Fisheyes.Find(s.Fisheye) is { } f:
                Run(() =>
                {
                    Set(group, s, nameof(s.FeStrength), f.Strength);
                    Set(group, s, nameof(s.FeFit), Fits[Math.Clamp(f.Fit, 0, 1)]);
                    Set(group, s, nameof(s.FeEdgeDark), f.EdgeDark);
                });
                break;
            case nameof(LooksSettings.Outline) when Outlines.Find(s.Outline) is { } o:
                Run(() =>
                {
                    Set(group, s, nameof(s.OlDepth), o.Depth);
                    Set(group, s, nameof(s.OlNormal), o.Normal);
                    Set(group, s, nameof(s.OlThickness), o.Thickness);
                    Set(group, s, nameof(s.OlStrength), o.Strength);
                    Set(group, s, nameof(s.OlRed), o.Red);
                    Set(group, s, nameof(s.OlGreen), o.Green);
                    Set(group, s, nameof(s.OlBlue), o.Blue);
                    Set(group, s, nameof(s.OlMaxDepth), o.MaxDepth);
                });
                break;
            case nameof(LooksSettings.Kuwahara) when Kuwaharas.Find(s.Kuwahara) is { } k:
                Run(() =>
                {
                    Set(group, s, nameof(s.KwRadius), k.Radius);
                    Set(group, s, nameof(s.KwSharpness), k.Sharpness);
                    Set(group, s, nameof(s.KwStrength), k.Strength);
                });
                break;
            case nameof(LooksSettings.N64) when N64s.Find(s.N64) is { } f:
                Run(() =>
                {
                    Set(group, s, nameof(s.NfFogStart), f.FogStart);
                    Set(group, s, nameof(s.NfFogEnd), f.FogEnd);
                    Set(group, s, nameof(s.NfFogAmount), f.FogAmount);
                    Set(group, s, nameof(s.NfFogSky), f.FogSky);
                    Set(group, s, nameof(s.NfTintR), f.TintR);
                    Set(group, s, nameof(s.NfTintG), f.TintG);
                    Set(group, s, nameof(s.NfTintB), f.TintB);
                    Set(group, s, nameof(s.NfSaturation), f.Saturation);
                    Set(group, s, nameof(s.NfStrength), f.Strength);
                });
                break;
            case nameof(LooksSettings.Toon) when Toons.Find(s.Toon) is { } t:
                Run(() =>
                {
                    Set(group, s, nameof(s.TnStrength), t.Strength);
                    Set(group, s, nameof(s.TnAutoLight), t.AutoLight);
                    Set(group, s, nameof(s.TnLightRef), t.LightRef);
                    Set(group, s, nameof(s.TnShadowEdge), t.ShadowEdge);
                    Set(group, s, nameof(s.TnHighEdge), t.HighlightEdge);
                    Set(group, s, nameof(s.TnSoftness), t.Softness);
                    Set(group, s, nameof(s.TnShadowLevel), t.ShadowLevel);
                    Set(group, s, nameof(s.TnMidLevel), t.MidLevel);
                    Set(group, s, nameof(s.TnLitLevel), t.LitLevel);
                    Set(group, s, nameof(s.TnTintR), t.TintR);
                    Set(group, s, nameof(s.TnTintG), t.TintG);
                    Set(group, s, nameof(s.TnTintB), t.TintB);
                    Set(group, s, nameof(s.TnTintAmount), t.TintAmount);
                    Set(group, s, nameof(s.TnRim), t.Rim);
                    Set(group, s, nameof(s.TnRimR), t.RimR);
                    Set(group, s, nameof(s.TnRimG), t.RimG);
                    Set(group, s, nameof(s.TnRimB), t.RimB);
                    Set(group, s, nameof(s.TnSaturation), t.Saturation);
                });
                break;
        }
    }

    static string NativeName(float lines)
    {
        int i = Array.IndexOf(NativeLines, (int)MathF.Round(lines));
        return Natives[i < 0 ? 0 : i];
    }

    static void Run(Action a)
    {
        bool was = t_applying;
        t_applying = true;
        try { a(); }
        finally { t_applying = was; }
    }

    static void Set(ModSettingsGroup? group, LooksSettings s, string key, object value)
    {
        var setting = group?.Settings.FirstOrDefault(x => x.Key == key);
        if (setting is not null) { setting.Set(value); return; }
        var p = typeof(LooksSettings).GetProperty(key)!;
        p.SetValue(s, p.PropertyType == typeof(float) ? Convert.ToSingle(value) : value);
    }

    // ---- what the passes draw: the sliders (null = the effect is Off) ---------------------------------------------------------------------
    static float F(float v, float lo, float hi, float fallback) => float.IsFinite(v) ? Math.Clamp(v, lo, hi) : fallback;

    public static PixelLook? Pixel(LooksSettings s) => s.Look == Looks.Off || !Looks.Names.Contains(s.Look) ? null : new(
        s.Look,
        F(s.PxSize, 1, 16, 4.09f),
        Math.Max(0, Array.IndexOf(Palettes, s.PxPalette)),
        Math.Clamp(s.PxLevels, 2, 32),
        F(s.PxDither, 0, 1, 0.6f),
        F(s.PxContrast, 0.5f, 2, 1),
        F(s.PxShadowLift, 0, 1, 0),
        F(s.PxHighlightHold, 0, 1, 0),
        F(s.PxScanlines, 0, 1, 0),
        F(s.PxCrtSoft, 0, 1, 0),
        F(s.PxBleed, 0, 1, 0),
        s.PxSampling == Samplings[1] ? 1 : 0,
        NativeLines[Math.Max(0, Array.IndexOf(Natives, s.PxNative))],
        F(s.PxStrength, 0, 1, 1),
        s.PxLiftNight,
        s.PxKeepBlack);

    public static TiltLook? Tilt(LooksSettings s) => s.TiltShift == Looks.Off || !Tilts.Names.Contains(s.TiltShift) ? null : new(
        s.TiltShift,
        F(s.TsCentre, 0, 1, 0.45f),
        F(s.TsWidth, 0, 1, 0.3f),
        F(s.TsFalloff, 0.01f, 1, 0.3f),
        F(s.TsBlur, 0, 20, 9),
        F(s.TsTop, 0, 1, 1),
        F(s.TsBottom, 0, 1, 0.85f),
        F(s.TsSaturation, 0, 2, 1),
        F(s.TsContrast, 0.5f, 2, 1),
        F(s.TsUpright, 0, 1, 1),
        F(s.TsStrength, 0, 1, 1));

    public static FisheyeLook? Fisheye(LooksSettings s) => s.Fisheye == Looks.Off || !Fisheyes.Names.Contains(s.Fisheye) ? null : new(
        s.Fisheye,
        s.FeFit == Fits[1] ? 1 : 0,
        F(s.FeStrength, 0, 1, 1),
        F(s.FeEdgeDark, 0, 1, 0.25f));

    public static OutlineLook? Outline(LooksSettings s) => s.Outline == Looks.Off || !Outlines.Names.Contains(s.Outline) ? null : new(
        s.Outline,
        F(s.OlDepth, 0.0005f, 0.05f, 0.003f),
        F(s.OlNormal, 0, 1, 0.8f),
        Math.Clamp(s.OlThickness, 1, 4),
        F(s.OlStrength, 0, 1, 0.8f),
        F(s.OlRed, 0, 1, 0), F(s.OlGreen, 0, 1, 0), F(s.OlBlue, 0, 1, 0),
        F(s.OlMaxDepth, 0.01f, 1, 0.5f));

    public static KuwaharaLook? Kuwahara(LooksSettings s) => s.Kuwahara == Looks.Off || !Kuwaharas.Names.Contains(s.Kuwahara) ? null : new(
        s.Kuwahara,
        F(s.KwRadius, 1, 8, 3),
        F(s.KwSharpness, 1, 16, 8),
        F(s.KwStrength, 0, 1, 0.8f));

    public static N64Look? N64(LooksSettings s) => s.N64 == Looks.Off || !N64s.Names.Contains(s.N64) ? null : new(
        s.N64,
        F(s.NfFogStart, 0, 1, 0.04f),
        F(s.NfFogEnd, 0.01f, 1, 0.3f),
        F(s.NfFogAmount, 0, 1, 0.6f),
        F(s.NfTintR, 0, 1, 1), F(s.NfTintG, 0, 1, 0.96f), F(s.NfTintB, 0, 1, 0.9f),
        F(s.NfSaturation, 0, 2, 1.15f),
        F(s.NfStrength, 0, 1, 1),
        s.NfFogSky);

    public static ToonLook? Toon(LooksSettings s) => s.Toon == Looks.Off || !Toons.Names.Contains(s.Toon) ? null : new(
        s.Toon,
        F(s.TnStrength, 0, 1, 1),
        s.TnAutoLight,
        F(s.TnLightRef, 0.1f, 4, 1),
        F(s.TnShadowEdge, 0, 1, 0.4f),
        F(s.TnHighEdge, 0, 1, 0.68f),
        F(s.TnSoftness, 0, 0.25f, 0.03f),
        F(s.TnShadowLevel, 0, 1, 0.3f),
        F(s.TnMidLevel, 0, 1, 0.5f),
        F(s.TnLitLevel, 0.05f, 1, 0.68f),
        F(s.TnTintR, 0, 1, 0.72f), F(s.TnTintG, 0, 1, 0.6f), F(s.TnTintB, 0, 1, 0.8f),
        F(s.TnTintAmount, 0, 1, 0.6f),
        F(s.TnRim, 0, 1, 0.25f),
        F(s.TnRimR, 0, 1, 1), F(s.TnRimG, 0, 1, 0.8f), F(s.TnRimB, 0, 1, 0.55f),
        F(s.TnSaturation, 0, 2, 1.1f));
}
