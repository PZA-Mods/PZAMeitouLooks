// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The Settings > Mods group (MeitouClient docs/modding.md "Settings"). Read by the passes every frame.
// 0.4.0 (user 2026-11-19: "much more customizability in the mod menu's settings, sliders"): every effect is a preset list (Off, the named
// presets, Custom) plus its own sub-group of sliders, shown while the effect is on. Picking a preset fills its sliders (LookValues); moving a
// slider turns that effect's list to Custom. The passes draw the SLIDERS' values, so a preset and its sliders are always the same picture.
// Every step below lands on each preset's values exactly (the offline check proves it), so picking a preset changes nothing by rounding.
// UI strings live in Strings (translatable; en now, ru / zh / ja before a 1.0).
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public sealed class LooksSettings
{
    // ---- Pixel look ------------------------------------------------------------------------------------------------------------------------
    /// <summary>The look (Off = Meitou's picture untouched; Custom = the sliders as they are).</summary>
    [Setting(Strings.En.LookLabel, Hint = Strings.En.LookHint, Options = ["Off", "PZA Pixel - 1", "PZA Pixel - 2", "PZA Pixel - 3",
        "PZA Pixel - 1 Night", "PZA - PS1 Main", "PZA PS1 - Standard",
        "PZA PS1 - CRT", "PZA Soft 240 - Standard", "PZA Diorama - Pixel", "PZA Pixel - Desert 8", "PZA Pixel - Desert 32",
        "PZA Pixel - Desert Night", "PZA Game Boy", "Custom"])]
    public string Look { get; set; } = "Off";

    public bool PixelOn => Look != Looks.Off;

    [Setting(Strings.En.PxSize, Hint = Strings.En.PxSizeHint, Min = 1, Max = 16, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxSize { get; set; } = 4.09f;

    [Setting(Strings.En.PxPalette, Hint = Strings.En.PxPaletteHint, Options = ["Desert 16", "LEAF 2-bit", "Full colour", "PS1 15-bit", "N64 16-bit",
        "8-bit 3-3-2", "Desert 8", "Desert 32", "Desert Night 16", "Game Boy"], SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public string PxPalette { get; set; } = "Desert 16";

    [Setting(Strings.En.PxLevels, Hint = Strings.En.PxLevelsHint, Min = 2, Max = 32, Step = 1, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public int PxLevels { get; set; } = 6;

    [Setting(Strings.En.PxDither, Hint = Strings.En.PxDitherHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxDither { get; set; } = 0.6f;

    [Setting(Strings.En.PxContrast, Hint = Strings.En.PxContrastHint, Min = 0.5, Max = 2, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxContrast { get; set; } = 1.15f;

    [Setting(Strings.En.PxShadowLift, Hint = Strings.En.PxShadowLiftHint, Min = 0, Max = 1, Step = 0.002, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxShadowLift { get; set; }

    [Setting(Strings.En.PxLiftNight, Hint = Strings.En.PxLiftNightHint, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public bool PxLiftNight { get; set; } = true;

    [Setting(Strings.En.PxHighlightHold, Hint = Strings.En.PxHighlightHoldHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxHighlightHold { get; set; }

    [Setting(Strings.En.PxScanlines, Hint = Strings.En.PxScanlinesHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxScanlines { get; set; }

    [Setting(Strings.En.PxCrtSoft, Hint = Strings.En.PxCrtSoftHint, Min = 0, Max = 1, Step = 0.005, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxCrtSoft { get; set; }

    [Setting(Strings.En.PxBleed, Hint = Strings.En.PxBleedHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxBleed { get; set; }

    [Setting(Strings.En.PxSampling, Hint = Strings.En.PxSamplingHint, Options = ["Centre", "Cell average"], SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public string PxSampling { get; set; } = "Centre";

    [Setting(Strings.En.PxNative, Hint = Strings.En.PxNativeHint, Options = ["Off", "224 lines", "240 lines", "480 lines", "144 lines"], SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public string PxNative { get; set; } = "Off";

    [Setting(Strings.En.PxKeepBlack, Hint = Strings.En.PxKeepBlackHint, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public bool PxKeepBlack { get; set; } = true;

    [Setting(Strings.En.Strength, Hint = Strings.En.PxStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxStrength { get; set; } = 1f;

    // ---- Tilt-Shift ------------------------------------------------------------------------------------------------------------------------
    /// <summary>Tilt-Shift, before the look (as in Dust: Tilt-Shift 205, Pixel Art 215). Off = none.</summary>
    [Setting(Strings.En.TiltLabel, Hint = Strings.En.TiltHint, Options = ["Off", "PZA Miniature", "PZA HD-2D", "PZA Diorama", "Custom"])]
    public string TiltShift { get; set; } = "Off";

    public bool TiltOn => TiltShift != Looks.Off;

    [Setting(Strings.En.TsWidth, Hint = Strings.En.TsWidthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsWidth { get; set; } = 0.3f;

    [Setting(Strings.En.TsFalloff, Hint = Strings.En.TsFalloffHint, Min = 0.01, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsFalloff { get; set; } = 0.3f;

    [Setting(Strings.En.TsBlur, Hint = Strings.En.TsBlurHint, Min = 0, Max = 20, Step = 0.5, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsBlur { get; set; } = 9f;

    [Setting(Strings.En.TsTop, Hint = Strings.En.TsTopHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsTop { get; set; } = 1f;

    [Setting(Strings.En.TsBottom, Hint = Strings.En.TsBottomHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsBottom { get; set; } = 0.85f;

    [Setting(Strings.En.TsUpright, Hint = Strings.En.TsUprightHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsUpright { get; set; } = 1f;

    [Setting(Strings.En.TsCentre, Hint = Strings.En.TsCentreHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsCentre { get; set; } = 0.45f;

    [Setting(Strings.En.TsSaturation, Hint = Strings.En.TsSaturationHint, Min = 0, Max = 2, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsSaturation { get; set; } = 1.12f;

    [Setting(Strings.En.TsContrast, Hint = Strings.En.TsContrastHint, Min = 0.5, Max = 2, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsContrast { get; set; } = 1f;

    [Setting(Strings.En.Strength, Hint = Strings.En.TsStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TiltGroup, VisibleWhen = nameof(TiltOn))]
    public float TsStrength { get; set; } = 1f;

    // ---- Fisheye ---------------------------------------------------------------------------------------------------------------------------
    /// <summary>0.3.0: Fisheye, between Tilt-Shift and the look (as in Dust: Tilt-Shift 205, Fisheye 207, Pixel Art 215). Off = none.</summary>
    [Setting(Strings.En.FisheyeLabel, Hint = Strings.En.FisheyeHint, Options = ["Off", "PZA Fisheye", "PZA Fisheye - Round", "Custom"])]
    public string Fisheye { get; set; } = "Off";

    public bool FisheyeOn => Fisheye != Looks.Off;

    [Setting(Strings.En.Strength, Hint = Strings.En.FeStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.FisheyeGroup, VisibleWhen = nameof(FisheyeOn))]
    public float FeStrength { get; set; } = 1f;

    [Setting(Strings.En.FeFit, Hint = Strings.En.FeFitHint, Options = ["Fill", "Full width"], SubGroup = Strings.En.FisheyeGroup, VisibleWhen = nameof(FisheyeOn))]
    public string FeFit { get; set; } = "Fill";

    [Setting(Strings.En.FeEdgeDark, Hint = Strings.En.FeEdgeDarkHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.FisheyeGroup, VisibleWhen = nameof(FisheyeOn))]
    public float FeEdgeDark { get; set; } = 0.25f;

    // ---- Outline (0.4.0 / 0.7.0) -----------------------------------------------------------------------------------------------------------
    /// <summary>0.4.0 / 0.7.0: PZA Outline, first in the chain (on the scene as Meitou draws it, before Tilt-Shift, Fisheye and the look). Off = none.</summary>
    [Setting(Strings.En.OutlineLabel, Hint = Strings.En.OutlineHint, Options = ["Off", "PZA Outline", "PZA Outline - Ink", "PZA Outline - Soft",
        "PZA Outline - Tilt-Shift", "PZA Outline - Stylized", "Custom"])]
    public string Outline { get; set; } = "Off";

    public bool OutlineOn => Outline != Looks.Off;

    [Setting(Strings.En.OlDepth, Hint = Strings.En.OlDepthHint, Min = 0.0005, Max = 0.05, Step = 0.0005, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlDepth { get; set; } = 0.003f;

    [Setting(Strings.En.OlNormal, Hint = Strings.En.OlNormalHint, Min = 0, Max = 2, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlNormal { get; set; } = 0.8f;

    [Setting(Strings.En.OlThickness, Hint = Strings.En.OlThicknessHint, Min = 1, Max = 4, Step = 1, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public int OlThickness { get; set; } = 1;

    [Setting(Strings.En.Strength, Hint = Strings.En.OlStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlStrength { get; set; } = 0.8f;

    [Setting(Strings.En.OlRed, Hint = Strings.En.OlColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlRed { get; set; }

    [Setting(Strings.En.OlGreen, Hint = Strings.En.OlColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlGreen { get; set; }

    [Setting(Strings.En.OlBlue, Hint = Strings.En.OlColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlBlue { get; set; }

    [Setting(Strings.En.OlMaxDepth, Hint = Strings.En.OlMaxDepthHint, Min = 0.01, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlMaxDepth { get; set; } = 0.5f;

    [Setting(Strings.En.OlDebug, Hint = Strings.En.OlDebugHint, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public bool OlDebug { get; set; }

    // ---- Kuwahara (0.5.0) ----
    /// <summary>0.5.0: PZA Kuwahara, the painted look (non-linear smoothing that keeps edges). Off = none.</summary>
    [Setting(Strings.En.KuwaharaLabel, Hint = Strings.En.KuwaharaHint, Options = ["Off", "PZA Kuwahara - Default", "PZA Kuwahara - Painted Miniature",
        "PZA Kuwahara - Soft", "PZA Kuwahara - Heavy", "Custom"])]
    public string Kuwahara { get; set; } = "Off";

    public bool KuwaharaOn => Kuwahara != Looks.Off;

    [Setting(Strings.En.KwRadius, Hint = Strings.En.KwRadiusHint, Min = 1, Max = 8, Step = 0.5, SubGroup = Strings.En.KwGroup, VisibleWhen = nameof(KuwaharaOn))]
    public float KwRadius { get; set; } = 5f;

    [Setting(Strings.En.KwSharpness, Hint = Strings.En.KwSharpnessHint, Min = 1, Max = 16, Step = 0.5, SubGroup = Strings.En.KwGroup, VisibleWhen = nameof(KuwaharaOn))]
    public float KwSharpness { get; set; } = 10f;

    [Setting(Strings.En.Strength, Hint = Strings.En.KwStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.KwGroup, VisibleWhen = nameof(KuwaharaOn))]
    public float KwStrength { get; set; } = 1f;

    // ---- N64 (0.5.0) ----
    /// <summary>0.5.0: PZA N64, fog in the scene's own colour and colour strength. Off = none.</summary>
    [Setting(Strings.En.N64Label, Hint = Strings.En.N64Hint, Options = ["Off", "PZA N64 - Default", "PZA N64 - Soft 240", "Custom"])]
    public string N64 { get; set; } = "Off";

    public bool N64On => N64 != Looks.Off;

    [Setting(Strings.En.NfFogStart, Hint = Strings.En.NfFogStartHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfFogStart { get; set; } = 0.01f;

    [Setting(Strings.En.NfFogEnd, Hint = Strings.En.NfFogEndHint, Min = 0.01, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfFogEnd { get; set; } = 0.12f;

    [Setting(Strings.En.NfFogAmount, Hint = Strings.En.NfFogAmountHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfFogAmount { get; set; } = 0.65f;

    [Setting(Strings.En.NfFogSky, Hint = Strings.En.NfFogSkyHint, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public bool NfFogSky { get; set; }

    [Setting(Strings.En.NfTintR, Hint = Strings.En.NfTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfTintR { get; set; } = 1f;

    [Setting(Strings.En.NfTintG, Hint = Strings.En.NfTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfTintG { get; set; } = 0.96f;

    [Setting(Strings.En.NfTintB, Hint = Strings.En.NfTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfTintB { get; set; } = 0.9f;

    [Setting(Strings.En.NfSaturation, Hint = Strings.En.NfSaturationHint, Min = 0, Max = 2, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfSaturation { get; set; } = 1.15f;

    [Setting(Strings.En.Strength, Hint = Strings.En.NfStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NfGroup, VisibleWhen = nameof(N64On))]
    public float NfStrength { get; set; } = 1f;

    // ---- Toon (0.5.0) ----
    /// <summary>0.5.0: PZA Toon, the light banded inside Meitou's lighting (ToonPatch). Frozen at the game's start. Off = none.</summary>
    [Setting(Strings.En.ToonLabel, Hint = Strings.En.ToonHint, Options = ["Off", "PZA Toon - Default", "PZA Toon - Kenshi", "PZA Toon - 1", "PZA Toon - Kenshi 3-Band",
        "PZA Toon - Cel", "PZA Toon - Anime", "PZA Toon - Wind Waker", "PZA PS1 - Toon", "Custom"])]
    public string Toon { get; set; } = "Off";

    public bool ToonOn => Toon != Looks.Off;

    [Setting(Strings.En.Strength, Hint = Strings.En.TnStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnStrength { get; set; } = 1f;

    [Setting(Strings.En.TnAutoLight, Hint = Strings.En.TnAutoLightHint, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public bool TnAutoLight { get; set; } = true;

    [Setting(Strings.En.TnLightRef, Hint = Strings.En.TnLightRefHint, Min = 0.1, Max = 4, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnLightRef { get; set; } = 1f;

    [Setting(Strings.En.TnShadowEdge, Hint = Strings.En.TnShadowEdgeHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnShadowEdge { get; set; } = 0.45f;

    [Setting(Strings.En.TnHighEdge, Hint = Strings.En.TnHighEdgeHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnHighEdge { get; set; } = 0.75f;

    [Setting(Strings.En.TnSoftness, Hint = Strings.En.TnSoftnessHint, Min = 0, Max = 0.25, Step = 0.001, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnSoftness { get; set; } = 0.008f;

    [Setting(Strings.En.TnShadowLevel, Hint = Strings.En.TnLevelHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnShadowLevel { get; set; } = 0.22f;

    [Setting(Strings.En.TnMidLevel, Hint = Strings.En.TnLevelHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnMidLevel { get; set; } = 0.5f;

    [Setting(Strings.En.TnLitLevel, Hint = Strings.En.TnLevelHint, Min = 0.05, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnLitLevel { get; set; } = 0.75f;

    [Setting(Strings.En.TnTintR, Hint = Strings.En.TnTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnTintR { get; set; } = 0.72f;

    [Setting(Strings.En.TnTintG, Hint = Strings.En.TnTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnTintG { get; set; } = 0.6f;

    [Setting(Strings.En.TnTintB, Hint = Strings.En.TnTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnTintB { get; set; } = 0.8f;

    [Setting(Strings.En.TnTintAmount, Hint = Strings.En.TnTintAmountHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnTintAmount { get; set; } = 0.7f;

    [Setting(Strings.En.TnRim, Hint = Strings.En.TnRimHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnRim { get; set; } = 0.35f;

    [Setting(Strings.En.TnRimR, Hint = Strings.En.TnRimColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnRimR { get; set; } = 1f;

    [Setting(Strings.En.TnRimG, Hint = Strings.En.TnRimColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnRimG { get; set; } = 0.8f;

    [Setting(Strings.En.TnRimB, Hint = Strings.En.TnRimColourHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnRimB { get; set; } = 0.55f;

    [Setting(Strings.En.TnSaturation, Hint = Strings.En.TnSaturationHint, Min = 0, Max = 2, Step = 0.01, SubGroup = Strings.En.TnGroup, VisibleWhen = nameof(ToonOn))]
    public float TnSaturation { get; set; } = 1.15f;

    // ---- Combo (0.6.0) ----
    /// <summary>0.6.0: one pick for several effects. Picking a combo sets the other lists; changing any of them afterwards shows Custom.</summary>
    [Setting(Strings.En.ComboLabel, Hint = Strings.En.ComboHint, Options = ["Off", "PZA Combo - Painted Miniature", "PZA Combo - Ink Miniature",
        "PZA Combo - Retro Cel", "PZA Combo - CRT Anime", "PZA Combo - Soft 240 Toon", "PZA Combo - 16-Bit Miniature", "PZA Combo - HD-2D",
        "PZA Combo - Diorama", "PZA Combo - Film Noir", "PZA Combo - Comic Book", "PZA Combo - Handheld", "PZA Combo - Found Footage", "Custom"])]
    public string Combo { get; set; } = "Off";

    // ---- Noir (0.6.0) ----
    [Setting(Strings.En.NoirLabel, Hint = Strings.En.NoirHint, Options = ["Off", "PZA Noir - Default", "PZA Noir - Sin City", "PZA Noir - Silver Screen", "Custom"])]
    public string Noir { get; set; } = "Off";

    public bool NoirOn => Noir != Looks.Off;

    [Setting(Strings.En.NrContrast, Hint = Strings.En.NrContrastHint, Min = 0.5, Max = 2.5, Step = 0.05, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrContrast { get; set; } = 1.45f;

    [Setting(Strings.En.NrCrush, Hint = Strings.En.NrCrushHint, Min = 0, Max = 0.5, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrCrush { get; set; } = 0.08f;

    [Setting(Strings.En.NrTintR, Hint = Strings.En.NrTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrTintR { get; set; } = 0.85f;

    [Setting(Strings.En.NrTintG, Hint = Strings.En.NrTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrTintG { get; set; } = 0.9f;

    [Setting(Strings.En.NrTintB, Hint = Strings.En.NrTintHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrTintB { get; set; } = 1f;

    [Setting(Strings.En.NrTintAmount, Hint = Strings.En.NrTintAmountHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrTintAmount { get; set; } = 0.2f;

    [Setting(Strings.En.NrKeep, Hint = Strings.En.NrKeepHint, Options = ["None", "Red", "Amber", "Green", "Blue"], SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public string NrKeep { get; set; } = "None";

    [Setting(Strings.En.NrKeepAmount, Hint = Strings.En.NrKeepAmountHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrKeepAmount { get; set; } = 0f;

    [Setting(Strings.En.NrEdge, Hint = Strings.En.NrEdgeHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrEdge { get; set; } = 0.4f;

    [Setting(Strings.En.Strength, Hint = Strings.En.NrStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.NrGroup, VisibleWhen = nameof(NoirOn))]
    public float NrStrength { get; set; } = 1f;

    // ---- Halftone (0.6.0) ----
    [Setting(Strings.En.HalftoneLabel, Hint = Strings.En.HalftoneHint, Options = ["Off", "PZA Halftone - Default", "PZA Halftone - Newspaper", "PZA Halftone - Pop Art", "Custom"])]
    public string Halftone { get; set; } = "Off";

    public bool HalftoneOn => Halftone != Looks.Off;

    [Setting(Strings.En.HtDotSize, Hint = Strings.En.HtDotSizeHint, Min = 2, Max = 16, Step = 0.5, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtDotSize { get; set; } = 6f;

    [Setting(Strings.En.HtAngle, Hint = Strings.En.HtAngleHint, Min = 0, Max = 90, Step = 1, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtAngle { get; set; } = 45f;

    [Setting(Strings.En.HtThreshold, Hint = Strings.En.HtThresholdHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtThreshold { get; set; } = 0.55f;

    [Setting(Strings.En.HtPaper, Hint = Strings.En.HtPaperHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtPaper { get; set; } = 0f;

    [Setting(Strings.En.HtInkR, Hint = Strings.En.HtInkHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtInkR { get; set; } = 0.08f;

    [Setting(Strings.En.HtInkG, Hint = Strings.En.HtInkHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtInkG { get; set; } = 0.06f;

    [Setting(Strings.En.HtInkB, Hint = Strings.En.HtInkHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtInkB { get; set; } = 0.07f;

    [Setting(Strings.En.HtColour, Hint = Strings.En.HtColourHint, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public bool HtColour { get; set; }

    [Setting(Strings.En.Strength, Hint = Strings.En.HtStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.HtGroup, VisibleWhen = nameof(HalftoneOn))]
    public float HtStrength { get; set; } = 0.9f;

    // ---- Screen (0.6.0) ----
    [Setting(Strings.En.ScreenLabel, Hint = Strings.En.ScreenHint, Options = ["Off", "PZA Screen - Default", "PZA Screen - Lens Fringe", "PZA Screen - VHS",
        "PZA Screen - VHS Worn", "Custom"])]
    public string Screen { get; set; } = "Off";

    public bool ScreenOn => Screen != Looks.Off;

    [Setting(Strings.En.ScFringe, Hint = Strings.En.ScFringeHint, Min = 0, Max = 0.01, Step = 0.0005, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScFringe { get; set; } = 0.0015f;

    [Setting(Strings.En.ScWobble, Hint = Strings.En.ScWobbleHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScWobble { get; set; } = 0.35f;

    [Setting(Strings.En.ScRoll, Hint = Strings.En.ScRollHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScRoll { get; set; } = 0.15f;

    [Setting(Strings.En.ScFlicker, Hint = Strings.En.ScFlickerHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScFlicker { get; set; } = 0.25f;

    [Setting(Strings.En.ScTracking, Hint = Strings.En.ScTrackingHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScTracking { get; set; } = 0f;

    [Setting(Strings.En.ScNoise, Hint = Strings.En.ScNoiseHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScNoise { get; set; } = 0.08f;

    [Setting(Strings.En.ScBleed, Hint = Strings.En.ScBleedHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScBleed { get; set; } = 0f;

    [Setting(Strings.En.ScSoften, Hint = Strings.En.ScSoftenHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScSoften { get; set; } = 0.15f;

    [Setting(Strings.En.Strength, Hint = Strings.En.ScStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.ScGroup, VisibleWhen = nameof(ScreenOn))]
    public float ScStrength { get; set; } = 1f;

    // ---- Meitou look (0.7.3) ----
    /// <summary>0.7.3: Meitou's own picture settings laid over the player's (Meitou 0.24.1+ only; hidden before). Off = the player's own.</summary>
    [Setting(Strings.En.MeitouLookLabel, Hint = Strings.En.MeitouLookHint, Options = ["Off", "PZA Meitou - Clean", "PZA Meitou - Realistic",
        "PZA Meitou - Cinematic", "PZA Meitou - Film"], VisibleWhen = nameof(MeitouGraphics))]
    public string MeitouLook { get; set; } = "Off";

    public bool MeitouGraphics => MeitouLetterbox.Active;

    // ---- Letterbox (0.6.3) ----
    [Setting(Strings.En.LetterboxLabel, Hint = Strings.En.LetterboxHint, Options = ["Off", "PZA Letterbox - 2.39:1 Cinema",
        "PZA Letterbox - 2.00:1 Univisium", "PZA Letterbox - 1.85:1 Widescreen", "PZA Letterbox - Soft", "Custom"])]
    public string Letterbox { get; set; } = "Off";

    public bool LetterboxOn => Letterbox != Looks.Off;

    /// <summary>0.7.3: softness and opacity exist only where PZA Letterbox draws its own bars (Meitou's own Letterbox is crisp and solid).</summary>
    public bool LbOwnBars => LetterboxOn && !MeitouLetterbox.Active;

    [Setting(Strings.En.LbAspect, Hint = Strings.En.LbAspectHint, Min = 1.33, Max = 3.0, Step = 0.01, SubGroup = Strings.En.LbGroup, VisibleWhen = nameof(LetterboxOn))]
    public float LbAspect { get; set; } = 2.39f;

    [Setting(Strings.En.LbSoftness, Hint = Strings.En.LbSoftnessHint, Min = 0, Max = 0.15, Step = 0.005, SubGroup = Strings.En.LbGroup, VisibleWhen = nameof(LbOwnBars))]
    public float LbSoftness { get; set; } = 0f;

    [Setting(Strings.En.Strength, Hint = Strings.En.LbStrengthHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.LbGroup, VisibleWhen = nameof(LbOwnBars))]
    public float LbStrength { get; set; } = 1f;

    // ---- Compare & Presets -----------------------------------------------------------------------------------------------------------------
    /// <summary>Compare: the left half of the screen as Meitou draws it, the right half with the look.</summary>
    [Setting(Strings.En.SplitLabel, Hint = Strings.En.SplitHint)]
    public bool Split { get; set; }

    /// <summary>0.7.0: opens the named Preset Manager window (snaps back off immediately).</summary>
    [Setting(Strings.En.PresetsLabel, Hint = Strings.En.PresetsHint)]
    public bool OpenPresets { get; set; }
}

static class Strings
{
    public static class En
    {
        public const string LookLabel = "Look";
        public const string LookHint = "PZA's looks from Dust, by name. Off leaves Meitou's picture as it is; Custom keeps your own slider values. Picking a look fills the Pixel look sliders below. Menus and text stay sharp.";
        public const string TiltLabel = "Tilt-Shift";
        public const string TiltHint = "A real tilt-shift from depth: the character the camera follows stays sharp from feet to head, nearer and farther things blur like a miniature photo. Miniature = a wide sharp band, HD-2D and Diorama = a narrow one. Picking one fills the Tilt-Shift sliders below.";
        public const string FisheyeLabel = "Fisheye";
        public const string FisheyeHint = "Bends the view like a fisheye lens. Fisheye fills the whole screen; Round shows everything across with black corners, like a real round fisheye. Pairs with PZA's Camera's 0.5x lens.";
        public const string OutlineLabel = "Outline";
        public const string OutlineHint = "Draws lines round characters, buildings and creases from the scene's depth and normals, matching Dust's Outline. PZA Outline = Ultra preset, Ink = thick and dark, Soft = faint, Tilt-Shift = for Tilt-Shift, Stylized = lighter creases. Picking one fills the Outline sliders below.";
        public const string KuwaharaLabel = "Kuwahara";
        public const string KuwaharaHint = "A painted look: smooths areas into strokes while keeping edges sharp (non-linear smoothing). Default = plain strokes; Painted Miniature = the Dust preset; Heavy = big strokes (costs more). Picking one fills the Kuwahara sliders below.";
        public const string KwGroup = "Kuwahara";
        public const string KwRadius = "Brush size";
        public const string KwRadiusHint = "How big the strokes are, in pixels at 1080 lines.";
        public const string KwSharpness = "Edge keeping";
        public const string KwSharpnessHint = "How firmly edges stay sharp (higher = crisper edges, flatter areas).";
        public const string KwStrengthHint = "Mixes the painted picture with Meitou's (1 = fully painted).";
        public const string N64Label = "N64";
        public const string N64Hint = "The Nintendo 64's short draw distance: far things fade into fog in the scene's own colour, and colours get a little stronger. Default = fog you see in ordinary views, sky clear; Soft 240 = the Dust preset. Picking one fills the N64 sliders below.";
        public const string NfGroup = "N64";
        public const string NfFogStart = "Fog start";
        public const string NfFogStartHint = "Where the fog begins (0 = at the camera, 1 = the far horizon).";
        public const string NfFogEnd = "Fog end";
        public const string NfFogEndHint = "Where the fog is full (0 = at the camera, 1 = the far horizon).";
        public const string NfFogAmount = "Fog amount";
        public const string NfFogAmountHint = "How thick the fog is at its end (1 = the fog colour alone).";
        public const string NfFogSky = "Fog the sky";
        public const string NfFogSkyHint = "The sky takes the fog too (the N64 look), or keeps its own colour.";
        public const string NfTintR = "Fog red";
        public const string NfTintG = "Fog green";
        public const string NfTintB = "Fog blue";
        public const string NfTintHint = "Tints the fog (1, 1, 1 = the scene's own average colour).";
        public const string NfSaturation = "Colour strength";
        public const string NfSaturationHint = "Saturation (1 = unchanged).";
        public const string NfStrengthHint = "Mixes the N64 look with Meitou's picture (1 = the full look).";
        public const string ToonLabel = "Toon (applies at the next start)";
        public const string ToonHint = "Cartoon lighting: the light falls into a few flat bands with a tinted shadow and a rim, inside Meitou's own lighting, so the bands follow the light and never the textures, characters included. Changes apply at the NEXT START of the game. Default = plain bands; Kenshi = the Dust preset.";
        public const string TnGroup = "Toon (next start)";
        public const string TnStrengthHint = "Mixes the toon light with Meitou's (1 = full toon).";
        public const string TnAutoLight = "Follow the sun";
        public const string TnAutoLightHint = "The bands follow the sun's own strength, so dawn and noon both band well (off: a fixed light reference).";
        public const string TnLightRef = "Light reference";
        public const string TnLightRefHint = "The light that counts as full, when Follow the sun is off.";
        public const string TnShadowEdge = "Shadow edge";
        public const string TnShadowEdgeHint = "Below this share of the full light, a surface is in the shadow band.";
        public const string TnHighEdge = "Highlight edge";
        public const string TnHighEdgeHint = "Above this share of the full light, a surface is in the lit band.";
        public const string TnSoftness = "Band softness";
        public const string TnSoftnessHint = "How soft the steps between bands are (0 = hard cel edges).";
        public const string TnShadowLevel = "Shadow level";
        public const string TnMidLevel = "Mid level";
        public const string TnLitLevel = "Lit level";
        public const string TnLevelHint = "The brightness of that band (the lit level is the full light).";
        public const string TnTintR = "Shadow red";
        public const string TnTintG = "Shadow green";
        public const string TnTintB = "Shadow blue";
        public const string TnTintHint = "The shadow band's tint colour.";
        public const string TnTintAmount = "Shadow tint";
        public const string TnTintAmountHint = "How much the shadow band takes the tint.";
        public const string TnRim = "Rim light";
        public const string TnRimHint = "A light along silhouettes, like a cartoon's rim.";
        public const string TnRimR = "Rim red";
        public const string TnRimG = "Rim green";
        public const string TnRimB = "Rim blue";
        public const string TnRimColourHint = "The rim light's colour.";
        public const string TnSaturation = "Colour strength";
        public const string TnSaturationHint = "Saturation of the surfaces' own colours (1 = unchanged).";
        public const string ComboLabel = "Combo";
        public const string ComboHint = "One pick for several effects at once (PZA's Dust combos and new ones). Changing any effect afterwards shows Custom here; picking Off turns every effect off. Combos with Toon: Toon applies at the next start.";
        public const string NoirLabel = "Film noir";
        public const string NoirHint = "Hard black and white: deep blacks, a silver tint, darker edges. Sin City keeps one colour (red). Pairs with Meitou's own film grain and vignette (Settings > Graphics).";
        public const string NrGroup = "Film noir";
        public const string NrContrast = "Contrast";
        public const string NrContrastHint = "Black-and-white contrast (1 = as it is).";
        public const string NrCrush = "Black crush";
        public const string NrCrushHint = "How much of the dark end turns pure black.";
        public const string NrTintR = "Tint red";
        public const string NrTintG = "Tint green";
        public const string NrTintB = "Tint blue";
        public const string NrTintHint = "The black and white's tint colour (silver blue, warm print...).";
        public const string NrTintAmount = "Tint amount";
        public const string NrTintAmountHint = "How much of the tint colour shows.";
        public const string NrKeep = "Keep colour";
        public const string NrKeepHint = "One colour that stays in colour (Sin City's red).";
        public const string NrKeepAmount = "Keep amount";
        public const string NrKeepAmountHint = "How strongly the kept colour shows.";
        public const string NrEdge = "Edge darkening";
        public const string NrEdgeHint = "Darker corners, like an old film print.";
        public const string NrStrengthHint = "Mixes the noir with Meitou's picture (1 = full noir).";
        public const string HalftoneLabel = "Halftone";
        public const string HalftoneHint = "Printed dots like a comic book or newspaper. Default = ink dots in the shadows; Newspaper = every tone as dots on paper; Pop Art = cyan, magenta and yellow dots.";
        public const string HtGroup = "Halftone";
        public const string HtDotSize = "Dot size";
        public const string HtDotSizeHint = "Dot spacing in pixels at 1080 lines.";
        public const string HtAngle = "Screen angle";
        public const string HtAngleHint = "The angle of the dot rows, in degrees.";
        public const string HtThreshold = "Shadow start";
        public const string HtThresholdHint = "Where the dots start: low = only the darkest shadows, 1 = every tone.";
        public const string HtPaper = "Paper";
        public const string HtPaperHint = "0 = the picture shows between the dots, 1 = flat paper.";
        public const string HtInkR = "Ink red";
        public const string HtInkG = "Ink green";
        public const string HtInkB = "Ink blue";
        public const string HtInkHint = "The ink colour of single-colour dots.";
        public const string HtColour = "Colour dots";
        public const string HtColourHint = "Cyan, magenta and yellow dots instead of one ink.";
        public const string HtStrengthHint = "Mixes the dots with Meitou's picture (1 = full).";
        public const string ScreenLabel = "Screen";
        public const string ScreenHint = "The screen itself, drawn over everything: Default = an old CRT's wiggle; Lens Fringe = colour fringing at the edges; VHS and VHS Worn = tape.";
        public const string ScGroup = "Screen";
        public const string ScFringe = "Colour fringe";
        public const string ScFringeHint = "Red and blue drift apart toward the edges (chromatic aberration).";
        public const string ScWobble = "Wobble";
        public const string ScWobbleHint = "The lines wobble sideways like an old CRT.";
        public const string ScRoll = "Roll";
        public const string ScRollHint = "The picture drifts slowly up and down (vertical hold).";
        public const string ScFlicker = "Flicker";
        public const string ScFlickerHint = "The brightness flickers.";
        public const string ScTracking = "Tracking";
        public const string ScTrackingHint = "A torn band rolls down the picture, like a worn tape.";
        public const string ScNoise = "Noise";
        public const string ScNoiseHint = "Moving snow over the picture.";
        public const string ScBleed = "Colour smear";
        public const string ScBleedHint = "Colour smears sideways while the outlines stay sharp, like tape.";
        public const string ScSoften = "Softness";
        public const string ScSoftenHint = "A softer, blurrier picture.";
        public const string ScStrengthHint = "Mixes the screen effect with Meitou's picture (1 = full).";
        public const string SplitLabel = "Compare (left half without)";
        public const string SplitHint = "Shows the left half of the screen without the looks, for a before / after.";
        public const string PresetsLabel = "Presets manager";
        public const string PresetsHint = "Opens the named Preset Manager window: save, load and delete custom Post-FX presets, or copy/import PZAFX1:... share codes.";

        public const string Strength = "Strength";

        public const string PixelGroup = "Pixel look";
        public const string PxSize = "Pixel size";
        public const string PxSizeHint = "Screen pixels per art pixel at 1080 lines (scaled with your screen height). Bigger = chunkier.";
        public const string PxPalette = "Palette";
        public const string PxPaletteHint = "Desert 16 = PZA's 16 desert colours; LEAF 2-bit = 4 shades; Full colour = every hue with fewer levels; PS1 15-bit, N64 16-bit, 8-bit 3-3-2 = those consoles' colour depths; Desert 8 / 32 / Night 16 = PZA's other desert palettes; Game Boy = its four greens.";
        public const string PxLevels = "Levels";
        public const string PxLevelsHint = "Steps per colour channel for the Full colour palette (fewer = more banding).";
        public const string PxDither = "Dither";
        public const string PxDitherHint = "How much the dither pattern mixes neighbouring colours (0 = flat bands).";
        public const string PxContrast = "Contrast";
        public const string PxContrastHint = "Contrast before the palette (1 = as Meitou draws it).";
        public const string PxShadowLift = "Shadow lift";
        public const string PxShadowLiftHint = "Brightens the darkest parts so night scenes keep detail.";
        public const string PxLiftNight = "Lift only at night";
        public const string PxLiftNightHint = "Fades the shadow lift out in daylight.";
        public const string PxHighlightHold = "Highlight hold";
        public const string PxHighlightHoldHint = "Keeps bright highlights from flattening into the palette's top colour.";
        public const string PxScanlines = "Scanlines";
        public const string PxScanlinesHint = "Dark lines between the rows, like a CRT television.";
        public const string PxCrtSoft = "CRT softness";
        public const string PxCrtSoftHint = "Blurs the pixel edges like a CRT's glow.";
        public const string PxBleed = "Colour smear";
        public const string PxBleedHint = "Smears colour sideways like a composite video cable.";
        public const string PxSampling = "Sampling";
        public const string PxSamplingHint = "Centre = each art pixel takes its centre's colour (crisp); Cell average = the average of its cell (smoother, less shimmer).";
        public const string PxNative = "Native lines";
        public const string PxNativeHint = "Locks the pixel size to an old console's line count (Off = use Pixel size).";
        public const string PxKeepBlack = "Keep pure black";
        public const string PxKeepBlackHint = "Desert 16 keeps true black as an extra colour (outlines and deep shadows stay black).";
        public const string PxStrengthHint = "Mixes the look with Meitou's picture (1 = the full look).";

        public const string TiltGroup = "Tilt-Shift";
        public const string TsWidth = "Sharp band";
        public const string TsWidthHint = "How deep the sharp zone round the character is, in screen heights.";
        public const string TsFalloff = "Falloff";
        public const string TsFalloffHint = "How quickly the blur grows outside the sharp band.";
        public const string TsBlur = "Blur size";
        public const string TsBlurHint = "The biggest blur, in pixels at 1080 lines.";
        public const string TsTop = "Far blur";
        public const string TsTopHint = "Blur on the far side of the sharp band.";
        public const string TsBottom = "Near blur";
        public const string TsBottomHint = "Blur on the near side of the sharp band.";
        public const string TsUpright = "Upright";
        public const string TsUprightHint = "1 = the sharp zone stands straight up (a character is sharp from feet to head, the miniature look); 0 = it faces the camera like a normal lens.";
        public const string TsCentre = "Band centre (no camera point)";
        public const string TsCentreHint = "Where the sharp band sits on screen (0 top, 1 bottom) when PZA's Camera does not give the character's point.";
        public const string TsSaturation = "Saturation";
        public const string TsSaturationHint = "Colour strength (1 = unchanged); miniatures look more saturated.";
        public const string TsContrast = "Contrast";
        public const string TsContrastHint = "Contrast (1 = unchanged).";
        public const string TsStrengthHint = "How much of the blur is used (1 = all of it).";

        public const string FisheyeGroup = "Fisheye";
        public const string FeStrengthHint = "0 = the normal lens, 1 = a full fisheye.";
        public const string FeFit = "Fit";
        public const string FeFitHint = "Fill = the corners stay filled; Full width = round, with black corners.";
        public const string FeEdgeDark = "Edge darkening";
        public const string FeEdgeDarkHint = "Darkens toward the corners like a real fisheye lens.";

        public const string OutlineGroup = "Outline";
        public const string OlDepth = "Depth sensitivity";
        public const string OlDepthHint = "Lines where the depth jumps (outlines round characters and buildings). Smaller = more lines.";
        public const string OlNormal = "Crease sensitivity";
        public const string OlNormalHint = "Lines where surfaces fold (roof edges, wall corners). Smaller = more lines; 0 = no crease lines.";
        public const string OlThickness = "Thickness";
        public const string OlThicknessHint = "Line width in pixels.";
        public const string OlStrengthHint = "How dark the lines are (1 = the full line colour).";
        public const string OlRed = "Line red";
        public const string OlGreen = "Line green";
        public const string OlBlue = "Line blue";
        public const string OlColourHint = "The line colour (0, 0, 0 = black).";
        public const string OlMaxDepth = "Line distance";
        public const string OlMaxDepthHint = "How far away lines are drawn (0.5 = 2,500 units, 1 = 5,000 units), fading smoothly before the cutoff so distant mountains stay clean.";
        public const string OlDebug = "Debug view";
        public const string OlDebugHint = "Shows raw edge detection instead of the scene: red = depth edges, green = normal/crease edges (matching Dust's Debug View).";

        public const string MeitouLookLabel = "Meitou look";
        public const string MeitouLookHint = "Meitou's own picture settings, laid over yours while it is picked (never saved into Meitou's settings): Clean turns Meitou's vignette, film grain and depth of field off for the stylised looks; Realistic = hybrid tone map, grade, soft bloom, vignette and grain, light shafts; Cinematic = ACES, shallow depth of field, stronger vignette and grain; Film = heavy grain and vignette. Off gives your own settings back. Meitou 0.24.1 and newer.";
        public const string LetterboxLabel = "Letterbox";
        public const string LetterboxHint = "Cinema black bars at the chosen picture shape. On Meitou 0.24.1 and newer this drives Meitou's own Letterbox (no second set of bars; Off gives your Meitou Letterbox setting back); on older Meitou PZA draws the bars, last over every other look. Menus and text stay on top.";
        public const string LbGroup = "Letterbox";
        public const string LbAspect = "Aspect ratio";
        public const string LbAspectHint = "Target frame ratio (2.39 = anamorphic cinema, 2.00 = Univisium, 1.85 = widescreen). Wider ratios add taller bars. Meitou's own Letterbox takes 1.33 to 2.76 (wider values are held at 2.76).";
        public const string LbSoftness = "Edge softness";
        public const string LbSoftnessHint = "Feathers the inner edge of the bars (0 = crisp cinema bars). Only on Meitou before 0.24.1, where PZA draws the bars.";
        public const string LbStrengthHint = "Bar opacity (1 = solid black). Only on Meitou before 0.24.1, where PZA draws the bars.";
    }
}
