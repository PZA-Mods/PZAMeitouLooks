// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0: the presets and the sliders. A preset picked (on the Settings page, through LooksApi, or kept from the last game) copies its values
// into its effect's sliders; a slider moved by the player turns that effect's list to Custom. The passes always draw the sliders (Pixel(),
// Tilt(), Fisheye(), Outline() below), so what the list says and what the screen shows cannot drift apart.
// Changes go through Meitou's settings group when there is one (saved, shown on the page); else straight to the properties (the offline check).
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public static class LookValues
{
    public static readonly string[] Palettes = ["Desert 16", "LEAF 2-bit", "Full colour", "PS1 15-bit", "N64 16-bit", "8-bit 3-3-2",
        "Desert 8", "Desert 32", "Desert Night 16", "Game Boy"];   // 0.6.0: 6..9
    public static readonly string[] Samplings = ["Centre", "Cell average"];
    public static readonly string[] Natives = ["Off", "224 lines", "240 lines", "480 lines", "144 lines"];
    static readonly int[] NativeLines = [0, 224, 240, 480, 144];   // 144: the Game Boy's lines (0.6.0)
    public static readonly string[] Fits = ["Fill", "Full width"];
    public static readonly string[] Keeps = ["None", "Red", "Amber", "Green", "Blue"];   // 0.6.0: Noirs.KeepHues' order

    /// <summary>Which list each slider belongs to (the slider's key prefix).</summary>
    // 0.6.0: a list's own key is never a slider (the Screen list starts with Sc, its sliders' prefix)
    public static string? ListOf(string key) => key.Length < 3 || Array.IndexOf(Lists, key) >= 0 ? null : key[..2] switch
    {
        "Px" => nameof(LooksSettings.Look),
        "Ts" => nameof(LooksSettings.TiltShift),
        "Fe" => nameof(LooksSettings.Fisheye),
        "Ol" => nameof(LooksSettings.Outline),
        "Kw" => nameof(LooksSettings.Kuwahara),   // 0.5.0
        "Nf" => nameof(LooksSettings.N64),
        "Tn" => nameof(LooksSettings.Toon),
        "Sc" => nameof(LooksSettings.Screen),   // 0.6.0
        "Ht" => nameof(LooksSettings.Halftone),
        "Nr" => nameof(LooksSettings.Noir),
        _ => null,
    };

    /// <summary>Every effect's preset list, in the Settings page's order.</summary>
    public static readonly string[] Lists = [nameof(LooksSettings.Look), nameof(LooksSettings.TiltShift), nameof(LooksSettings.Fisheye),
        nameof(LooksSettings.Outline), nameof(LooksSettings.Kuwahara), nameof(LooksSettings.N64), nameof(LooksSettings.Toon),
        nameof(LooksSettings.Noir), nameof(LooksSettings.Halftone), nameof(LooksSettings.Screen)];   // 0.6.0: the Combo list is not one of these

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
        if (key == nameof(LooksSettings.Combo)) { ApplyCombo(group, s); return; }
        if (ListOf(key) is { } list)
        {
            string now = (string)typeof(LooksSettings).GetProperty(list)!.GetValue(s)!;
            if (now != Looks.Custom && now != Looks.Off) Run(() => Set(group, s, list, Looks.Custom));
            ComboCustom(group, s);
            return;
        }
        if (Array.IndexOf(Lists, key) >= 0) ComboCustom(group, s);
        ApplyPreset(group, s, key);
    }

    /// <summary>0.6.0: a combo sets every effect list: its named presets, Off for the rest (Combo Off turns every effect off); Custom leaves
    /// them as they are.</summary>
    public static void ApplyCombo(ModSettingsGroup? group, LooksSettings s)
    {
        if (s.Combo == Looks.Custom) return;
        var c = Combos.Find(s.Combo);
        if (c is null && s.Combo != Looks.Off) return;
        Run(() =>
        {
            foreach (var list in Lists)
            {
                string pick = Looks.Off;
                if (c is not null)
                    foreach (var (l, preset) in c.Sets)
                        if (l == list) pick = preset;
                Set(group, s, list, pick);
                ApplyPreset(group, s, list);
            }
        });
    }

    /// <summary>The player changed one effect after picking a combo: the combo list says Custom.</summary>
    static void ComboCustom(ModSettingsGroup? group, LooksSettings s)
    {
        if (Combos.Find(s.Combo) is not null) Run(() => Set(group, s, nameof(LooksSettings.Combo), Looks.Custom));
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
            case nameof(LooksSettings.Noir) when Noirs.Find(s.Noir) is { } n:
                Run(() =>
                {
                    Set(group, s, nameof(s.NrContrast), n.Contrast);
                    Set(group, s, nameof(s.NrCrush), n.Crush);
                    Set(group, s, nameof(s.NrTintR), n.TintR);
                    Set(group, s, nameof(s.NrTintG), n.TintG);
                    Set(group, s, nameof(s.NrTintB), n.TintB);
                    Set(group, s, nameof(s.NrTintAmount), n.TintAmount);
                    Set(group, s, nameof(s.NrKeep), Keeps[Math.Clamp(n.Keep, 0, Keeps.Length - 1)]);
                    Set(group, s, nameof(s.NrKeepAmount), n.KeepAmount);
                    Set(group, s, nameof(s.NrEdge), n.Edge);
                    Set(group, s, nameof(s.NrStrength), n.Strength);
                });
                break;
            case nameof(LooksSettings.Halftone) when Halftones.Find(s.Halftone) is { } h:
                Run(() =>
                {
                    Set(group, s, nameof(s.HtDotSize), h.DotSize);
                    Set(group, s, nameof(s.HtAngle), h.Angle);
                    Set(group, s, nameof(s.HtThreshold), h.Threshold);
                    Set(group, s, nameof(s.HtPaper), h.Paper);
                    Set(group, s, nameof(s.HtInkR), h.InkR);
                    Set(group, s, nameof(s.HtInkG), h.InkG);
                    Set(group, s, nameof(s.HtInkB), h.InkB);
                    Set(group, s, nameof(s.HtColour), h.Colour);
                    Set(group, s, nameof(s.HtStrength), h.Strength);
                });
                break;
            case nameof(LooksSettings.Screen) when Screens.Find(s.Screen) is { } c:
                Run(() =>
                {
                    Set(group, s, nameof(s.ScFringe), c.Fringe);
                    Set(group, s, nameof(s.ScWobble), c.Wobble);
                    Set(group, s, nameof(s.ScRoll), c.Roll);
                    Set(group, s, nameof(s.ScFlicker), c.Flicker);
                    Set(group, s, nameof(s.ScTracking), c.Tracking);
                    Set(group, s, nameof(s.ScNoise), c.Noise);
                    Set(group, s, nameof(s.ScBleed), c.Bleed);
                    Set(group, s, nameof(s.ScSoften), c.Soften);
                    Set(group, s, nameof(s.ScStrength), c.Strength);
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

    // 0.6.0
    public static NoirLook? Noir(LooksSettings s) => s.Noir == Looks.Off || !Noirs.Names.Contains(s.Noir) ? null : new(
        s.Noir,
        F(s.NrContrast, 0.5f, 2.5f, 1.45f),
        F(s.NrCrush, 0, 0.5f, 0.08f),
        F(s.NrTintR, 0, 1, 0.85f), F(s.NrTintG, 0, 1, 0.9f), F(s.NrTintB, 0, 1, 1),
        F(s.NrTintAmount, 0, 1, 0.2f),
        Math.Max(0, Array.IndexOf(Keeps, s.NrKeep)),
        F(s.NrKeepAmount, 0, 1, 0),
        F(s.NrEdge, 0, 1, 0.4f),
        F(s.NrStrength, 0, 1, 1));

    public static HalftoneLook? Halftone(LooksSettings s) => s.Halftone == Looks.Off || !Halftones.Names.Contains(s.Halftone) ? null : new(
        s.Halftone,
        F(s.HtDotSize, 2, 16, 6),
        F(s.HtAngle, 0, 90, 45),
        F(s.HtThreshold, 0, 1, 0.55f),
        F(s.HtPaper, 0, 1, 0),
        F(s.HtInkR, 0, 1, 0.08f), F(s.HtInkG, 0, 1, 0.06f), F(s.HtInkB, 0, 1, 0.07f),
        s.HtColour,
        F(s.HtStrength, 0, 1, 0.9f));

    public static ScreenLook? Screen(LooksSettings s) => s.Screen == Looks.Off || !Screens.Names.Contains(s.Screen) ? null : new(
        s.Screen,
        F(s.ScFringe, 0, 0.01f, 0.0015f),
        F(s.ScWobble, 0, 1, 0.35f),
        F(s.ScRoll, 0, 1, 0.15f),
        F(s.ScFlicker, 0, 1, 0.25f),
        F(s.ScTracking, 0, 1, 0),
        F(s.ScNoise, 0, 1, 0.08f),
        F(s.ScBleed, 0, 1, 0),
        F(s.ScSoften, 0, 1, 0.15f),
        F(s.ScStrength, 0, 1, 1));
}
