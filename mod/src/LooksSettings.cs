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
        "PZA PS1 - CRT", "PZA Soft 240 - Standard", "PZA Diorama - Pixel", "Custom"])]
    public string Look { get; set; } = "PZA Pixel - 1";

    public bool PixelOn => Look != Looks.Off;

    [Setting(Strings.En.PxSize, Hint = Strings.En.PxSizeHint, Min = 1, Max = 16, Step = 0.01, SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
    public float PxSize { get; set; } = 4.09f;

    [Setting(Strings.En.PxPalette, Hint = Strings.En.PxPaletteHint, Options = ["Desert 16", "LEAF 2-bit", "Full colour", "PS1 15-bit", "N64 16-bit",
        "8-bit 3-3-2"], SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
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

    [Setting(Strings.En.PxNative, Hint = Strings.En.PxNativeHint, Options = ["Off", "224 lines", "240 lines", "480 lines"], SubGroup = Strings.En.PixelGroup, VisibleWhen = nameof(PixelOn))]
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

    // ---- Outline (0.4.0) -------------------------------------------------------------------------------------------------------------------
    /// <summary>0.4.0: PZA Outline, first in the chain (on the scene as Meitou draws it, before Tilt-Shift, Fisheye and the look). Off = none.</summary>
    [Setting(Strings.En.OutlineLabel, Hint = Strings.En.OutlineHint, Options = ["Off", "PZA Outline", "PZA Outline - Ink", "PZA Outline - Soft",
        "PZA Outline - Tilt-Shift", "Custom"])]
    public string Outline { get; set; } = "Off";

    public bool OutlineOn => Outline != Looks.Off;

    [Setting(Strings.En.OlDepth, Hint = Strings.En.OlDepthHint, Min = 0.0005, Max = 0.05, Step = 0.0005, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
    public float OlDepth { get; set; } = 0.003f;

    [Setting(Strings.En.OlNormal, Hint = Strings.En.OlNormalHint, Min = 0, Max = 1, Step = 0.01, SubGroup = Strings.En.OutlineGroup, VisibleWhen = nameof(OutlineOn))]
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

    // ---- Compare ---------------------------------------------------------------------------------------------------------------------------
    /// <summary>Compare: the left half of the screen as Meitou draws it, the right half with the look.</summary>
    [Setting(Strings.En.SplitLabel, Hint = Strings.En.SplitHint)]
    public bool Split { get; set; }
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
        public const string OutlineHint = "Draws lines round characters, buildings and creases from the scene's depth, like ink on a drawing. Ink = thick and dark, Soft = faint, Tilt-Shift = for use with Tilt-Shift. Picking one fills the Outline sliders below.";
        public const string SplitLabel = "Compare (left half without)";
        public const string SplitHint = "Shows the left half of the screen without the looks, for a before / after.";

        public const string Strength = "Strength";

        public const string PixelGroup = "Pixel look";
        public const string PxSize = "Pixel size";
        public const string PxSizeHint = "Screen pixels per art pixel at 1080 lines (scaled with your screen height). Bigger = chunkier.";
        public const string PxPalette = "Palette";
        public const string PxPaletteHint = "Desert 16 = PZA's 16 desert colours; LEAF 2-bit = 4 shades; Full colour = every hue with fewer levels; PS1 15-bit, N64 16-bit, 8-bit 3-3-2 = those consoles' colour depths.";
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
        public const string OlMaxDepthHint = "How far away lines are still drawn, as a share of the view distance (1 = everything).";
    }
}
