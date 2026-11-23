// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The named looks: the pixel-art effect values of the user's Dust presets of the same names (the pixel-art effect's ini in each Dust preset folder,
// read 2026-11-17). The player picks a look by name; 0.4.0 adds sliders under each list (user 2026-11-19), which a preset fills (LookValues).
// The Dust presets' other effects (Dust's LUT grade, bloom, outline, vignette...) are not ours: their Meitou counterparts are a Meitou look
// preset file of the same name (presets\*.json, the port notes "Dust's own settings").
namespace PZA.Looks.Meitou;

/// <summary>One look's pixel-art effect values, as the shader takes them (Dust's ini keys in the comments).</summary>
public sealed record PixelLook(
    string Name,
    float PixelSize,        // PixelSize (screen pixels per art pixel at 1080 rows, scaled with the height)
    int Palette,            // Palette: 0 Desert 16, 1 LEAF 2-bit, 2 full colour fewer levels, 3 PS1 15-bit, 4 N64 16-bit, 5 8-bit 3-3-2,
                            //   0.6.0: 6 Desert 8, 7 Desert 32, 8 Desert Night 16, 9 Game Boy
    int Levels,             // Levels (palette 2)
    float Dither,           // Dither
    float Contrast,         // Contrast
    float ShadowLift = 0,   // ShadowLift (faded out in daylight: LiftNight=1 in every look)
    float HighlightHold = 0,// HighlightHold
    float Scanlines = 0,    // Scanlines
    float CrtSoft = 0,      // CrtSoftness
    float Bleed = 0,        // ColourBleed
    int Sampling = 0,       // Sampling: 0 centre, 1 cell average
    float NativeLines = 0,  // NativeResolution: 0 off, else the line count (240)
    float Strength = 1,     // 0.4.0 sliders: mix with Meitou's picture
    bool LiftNight = true,  // LiftNight (1 in every look)
    bool KeepBlack = true); // KeepBlack (Desert 16's 17th colour; 1 in every look)

public static class Looks
{
    public const string Off = "Off";
    /// <summary>0.4.0: the lists' last option: the sliders as the player set them.</summary>
    public const string Custom = "Custom";

    /// <summary>Every look, in the order the Settings page lists them (after Off).</summary>
    public static readonly PixelLook[] All =
    [
        new("PZA Pixel - 1", 4.09f, 0, 6, 0.6f, 1.15f),
        new("PZA Pixel - 2", 2.5f, 0, 6, 0.6f, 1.15f),
        new("PZA Pixel - 3", 8.0f, 0, 6, 0.6f, 1.15f),
        // 0.3.0 (parity with the Dust presets, user 2026-11-19): Pixel 1 for the night, the lift fades out in daylight (LiftNight)
        new("PZA Pixel - 1 Night", 4.09f, 0, 6, 0.6f, 1.0f, ShadowLift: 0.6f),
        new("PZA - PS1 Main", 4.09f, 2, 8, 0.6f, 1.0f, HighlightHold: 0.5f, Scanlines: 0.6f, CrtSoft: 0.05f, Bleed: 0.4f),
        new("PZA PS1 - Standard", 4.0f, 3, 16, 1.0f, 1.05f, ShadowLift: 0.012f, Scanlines: 0.35f, CrtSoft: 0.4f, Bleed: 0.3f),   // 0.3.0
        new("PZA PS1 - CRT", 4.0f, 3, 16, 0.04f, 1.05f, ShadowLift: 0.012f, Scanlines: 1f, CrtSoft: 0.385f, Bleed: 1f),
        new("PZA Soft 240 - Standard", 4.0f, 4, 16, 1.0f, 1.0f, CrtSoft: 0.35f, Sampling: 1, NativeLines: 240),
        // 0.3.0: "PZA Diorama - Pixel"'s fine full-colour pixels (pairs with Tilt-Shift "PZA Diorama")
        new("PZA Diorama - Pixel", 2.5f, 2, 16, 0f, 1.0f, HighlightHold: 0.5f, Sampling: 1),
        // 0.6.0: Dust's table palettes, and the Game Boy (4 greens, 144 lines, the screen's own size)
        new("PZA Pixel - Desert 8", 4.09f, 6, 6, 0.6f, 1.15f),
        new("PZA Pixel - Desert 32", 4.09f, 7, 6, 0.6f, 1.15f),
        new("PZA Pixel - Desert Night", 4.09f, 8, 6, 0.6f, 1.1f),
        new("PZA Game Boy", 4f, 9, 6, 0.6f, 1.1f, Sampling: 1, NativeLines: 144),
    ];

    /// <summary>Off and every look's name: the Settings page's options.</summary>
    public static readonly string[] Names = [Off, .. All.Select(l => l.Name), Custom];

    /// <summary>The look of that name; null for Off or an unknown name.</summary>
    public static PixelLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>One Tilt-Shift's values, from the PZATiltShift.ini of the user's Dust presets (keys in the comments). Every one: depth focus
/// (FocusMode 1), upright plane (Upright 1), the camera's point (FocusPick 1).</summary>
public sealed record TiltLook(
    string Name,
    float FocusCentre,   // FocusCentre (band centre, 0 top .. 1 bottom; the fallback focus taps sit there)
    float FocusWidth,    // FocusWidth (sharp half-width, screen heights)
    float Falloff,       // Falloff
    float BlurPx,        // BlurSize (pixels at 1080 rows)
    float Top,           // TopAmount (the far side)
    float Bottom,        // BottomAmount (the near side)
    float Saturation,    // Saturation
    float Contrast,      // Contrast
    float Upright = 1,   // 0.4.0 sliders: Upright (1 in every PZA preset)
    float Strength = 1); // Strength

public static class Tilts
{
    /// <summary>Every Tilt-Shift, in the Settings page's order (after Off).</summary>
    public static readonly TiltLook[] All =
    [
        // "PZA Combo - 16-Bit Miniature (Pixel 1 + Tilt-Shift)": the wide band the user voted for (ts-16bit-band-1109)
        new("PZA Miniature", 0.45f, 0.3f, 0.3f, 9f, 1f, 0.85f, 1.12f, 1f),
        // "PZA Combo - HD-2D (Pixel + Tilt-Shift)"
        new("PZA HD-2D", 0.45f, 0.12f, 0.3f, 9f, 1f, 0.85f, 1.12f, 1f),
        // "PZA Diorama - Real (Tilt-Shift)"
        new("PZA Diorama", 0.45f, 0.12f, 0.3f, 10f, 1f, 0.9f, 1.15f, 1.06f),
    ];

    /// <summary>Off and every Tilt-Shift's name: the Settings page's options.</summary>
    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static TiltLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>One Fisheye (0.3.0), from the Dust Fisheye effect's settings (keys in the comments).</summary>
public sealed record FisheyeLook(
    string Name,
    int Fit,           // Fit: 0 Fill (the corners stay corners, no black), 1 Full width (round, black corners)
    float Strength,    // Strength: 0 the normal lens .. 1 a full fisheye
    float EdgeDark);   // EdgeDark: darkening toward the corners

public static class Fisheyes
{
    /// <summary>Every Fisheye, in the Settings page's order (after Off). Dust's defaults: strength 1, edge darkening 0.25.</summary>
    public static readonly FisheyeLook[] All =
    [
        new("PZA Fisheye", 0, 1f, 0.25f),
        new("PZA Fisheye - Round", 1, 1f, 0.25f),
    ];

    /// <summary>Off and every Fisheye's name: the Settings page's options.</summary>
    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static FisheyeLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.4.0: one Outline, our own edge pass (the Dust Outline effect's settings as a guide; the maths is ours, OutlineShader).</summary>
public sealed record OutlineLook(
    string Name,
    float Depth,       // depth sensitivity: the inverse-depth jump (share of the centre's) that starts a line; a line is full at 1.5x
    float Normal,      // crease sensitivity: 1 - cos of the fold between neighbouring normals (0 = no crease lines)
    int Thickness,     // tap distance in depth pixels (1..4)
    float Strength,    // mix toward the colour
    float Red, float Green, float Blue,
    float MaxDepth);   // share of the view's far plane beyond which no line is drawn

public static class Outlines
{
    /// <summary>Every Outline, in the Settings page's order (after Off).</summary>
    public static readonly OutlineLook[] All =
    [
        new("PZA Outline", 0.003f, 0.8f, 1, 0.8f, 0f, 0f, 0f, 0.5f),
        new("PZA Outline - Ink", 0.003f, 0.6f, 2, 1.0f, 0f, 0f, 0f, 0.3f),
        new("PZA Outline - Soft", 0.003f, 0.8f, 1, 0.6f, 0f, 0f, 0f, 0.5f),
        new("PZA Outline - Tilt-Shift", 0.005f, 0.7f, 1, 0.75f, 0f, 0f, 0f, 0.5f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static OutlineLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.5.0: one Kuwahara (our own generalized Kuwahara: 8 soft sectors, the calmest wins), from the Dust Kuwahara effect's settings.</summary>
public sealed record KuwaharaLook(
    string Name,
    float Radius,      // Radius (pixels at 1080 rows, scaled with the height)
    float Sharpness,   // Sharpness (how strongly the calmest sector wins)
    float Strength);   // Strength (mix with the picture)

public static class Kuwaharas
{
    /// <summary>Every Kuwahara, in the Settings page's order (after Off). Default = tuned to show the paint plainly (user 2026-11-20:
    /// "tune them"); Painted Miniature = the Dust preset "PZA Combo - Painted Miniature"'s Kuwahara, unchanged.</summary>
    public static readonly KuwaharaLook[] All =
    [
        new("PZA Kuwahara - Default", 5f, 10f, 1f),
        new("PZA Kuwahara - Painted Miniature", 3f, 8f, 0.8f),
        new("PZA Kuwahara - Soft", 2f, 6f, 0.6f),
        new("PZA Kuwahara - Heavy", 8f, 12f, 1f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static KuwaharaLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.5.0: one N64 (fog in the scene's own colour, and colour strength), from the Dust PZA N64 effect's settings. Fog distances are
/// shares of Kenshi's own far clip (55,000 units), as in Dust; texture blur and smoothed light are not in (they need the engine).</summary>
public sealed record N64Look(
    string Name,
    float FogStart,    // FogStart (share of the far clip)
    float FogEnd,      // FogEnd
    float FogAmount,   // FogAmount
    float TintR, float TintG, float TintB,   // FogTint
    float Saturation,  // Saturation
    float Strength,    // Strength
    bool FogSky = true);

public static class N64s
{
    public const float FarClip = 55000f;

    /// <summary>Every N64, in the Settings page's order (after Off). Default = tuned so the fog shows in ordinary views and the sky stays
    /// clear (user 2026-11-20: "tune them"); Soft 240 = the Dust preset "PZA Combo - Soft 240 Toon"'s PZA N64, unchanged.</summary>
    public static readonly N64Look[] All =
    [
        new("PZA N64 - Default", 0.01f, 0.12f, 0.65f, 1f, 0.96f, 0.9f, 1.15f, 1f, FogSky: false),
        new("PZA N64 - Soft 240", 0.04f, 0.3f, 0.6f, 1f, 0.96f, 0.9f, 1.15f, 1f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static N64Look? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.5.0: one Toon (the light banded where Meitou computes it, ToonPatch), from the user's Dust PZA Toon presets (PZAToon.ini). The
/// light is split at two edges into three levels (shadow, mid, lit); the shadow band takes a tint; a rim lights the silhouette. Flatten is
/// not in (it needs the texture's neighbours, which Meitou's lighting does not see).</summary>
public sealed record ToonLook(
    string Name,
    float Strength,       // Strength
    bool AutoLight,       // AutoLight: the bands follow the sun's own strength (else LightRef)
    float LightRef,       // LightRef
    float ShadowEdge,     // ShadowEdge
    float HighlightEdge,  // HighlightEdge
    float Softness,       // Softness
    float ShadowLevel,    // ShadowLevel
    float MidLevel,       // MidLevel
    float LitLevel,       // LitLevel
    float TintR, float TintG, float TintB,   // ShadowTint
    float TintAmount,     // TintAmount
    float Rim,            // Rim
    float RimR, float RimG, float RimB,      // RimColour
    float Saturation);    // Saturation

public static class Toons
{
    /// <summary>Every Toon, in the Settings page's order (after Off). Default = tuned for plain bands (harder steps, a deeper shadow band,
    /// a brighter lit band, a stronger rim; user 2026-11-20: "tune them"); Kenshi = the user's Dust "PZA Toon - Kenshi", unchanged.</summary>
    public static readonly ToonLook[] All =
    [
        new("PZA Toon - Default", 1f, true, 1f, 0.45f, 0.75f, 0.008f, 0.22f, 0.5f, 0.75f, 0.72f, 0.6f, 0.8f, 0.7f, 0.35f, 1f, 0.8f, 0.55f, 1.15f),
        new("PZA Toon - Kenshi", 1f, true, 1f, 0.4f, 0.68f, 0.03f, 0.3f, 0.5f, 0.68f, 0.72f, 0.6f, 0.8f, 0.6f, 0.25f, 1f, 0.8f, 0.55f, 1.1f),
        new("PZA Toon - 1", 0.49f, false, 1.69f, 0.35f, 0.05f, 0.01f, 0.1f, 0.1f, 0.5f, 0.62f, 0.66f, 1f, 0.6f, 0.25f, 1f, 0.95f, 0.85f, 1.1f),
        new("PZA Toon - Kenshi 3-Band", 1f, true, 1f, 0.4f, 0.55f, 0.03f, 0.3f, 0.45f, 0.6f, 0.72f, 0.6f, 0.8f, 0.6f, 0.25f, 1f, 0.8f, 0.55f, 1.1f),
        new("PZA Toon - Cel", 1f, true, 1f, 0.42f, 0.55f, 0.006f, 0.25f, 0.45f, 0.62f, 0.72f, 0.6f, 0.8f, 0.7f, 0.3f, 1f, 0.8f, 0.55f, 1.2f),
        new("PZA Toon - Anime", 1f, true, 1f, 0.38f, 0.95f, 0.004f, 0.28f, 0.5f, 0.6f, 0.75f, 0.6f, 0.8f, 0.6f, 0f, 1f, 0.8f, 0.55f, 1.35f),
        new("PZA Toon - Wind Waker", 1f, true, 1f, 0.33f, 0.36f, 0.01f, 0.2f, 0.45f, 0.5f, 0.72f, 0.6f, 0.8f, 0.5f, 0.25f, 1f, 0.8f, 0.55f, 1.15f),
        new("PZA PS1 - Toon", 0.5f, true, 1f, 0.4f, 0.68f, 0.03f, 0.36f, 0.5f, 0.6f, 0.72f, 0.6f, 0.8f, 0.4f, 0.25f, 1f, 0.8f, 0.55f, 1.05f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static ToonLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.6.0: one Screen look, drawn LAST over everything (ScreenShader): lens colour fringe (chromatic aberration, which Meitou does
/// not have), the CRT wiggle (line wobble, a slow roll, flicker) and VHS tape (tracking band, noise, colour smear, softness). Values 0..1,
/// fringe in screen widths at the corner (every PZA preset at or under 0.002, the user's chromatic aberration cap).</summary>
public sealed record ScreenLook(
    string Name,
    float Fringe, float Wobble, float Roll, float Flicker, float Tracking, float Noise, float Bleed, float Soften, float Strength = 1);

public static class Screens
{
    public static readonly ScreenLook[] All =
    [
        new("PZA Screen - Default", 0.0015f, 0.35f, 0.15f, 0.25f, 0f, 0.08f, 0f, 0.15f),       // the CRT wiggle (Planned since 0.4.0)
        new("PZA Screen - Lens Fringe", 0.002f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
        new("PZA Screen - VHS", 0.002f, 0.25f, 0f, 0.1f, 0.5f, 0.3f, 0.6f, 0.45f),
        new("PZA Screen - VHS Worn", 0.002f, 0.5f, 0.05f, 0.2f, 1f, 0.55f, 0.85f, 0.6f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static ScreenLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.6.0: one Halftone (HalftoneShader): printed dots. Mono ink dots where the picture is darker than the shadow start (comic),
/// or every tone on flat paper (newspaper), or cyan / magenta / yellow dots (pop art).</summary>
public sealed record HalftoneLook(
    string Name,
    float DotSize,      // dot spacing in pixels at 1080 lines
    float Angle,        // screen angle in degrees
    float Threshold,    // shadow start: 0 = no dots, 1 = dots on every tone
    float Paper,        // 0 = the picture between the dots, 1 = flat paper
    float InkR, float InkG, float InkB,
    bool Colour,        // cyan / magenta / yellow dots instead of one ink
    float Strength);

public static class Halftones
{
    public static readonly HalftoneLook[] All =
    [
        new("PZA Halftone - Default", 6f, 45f, 0.55f, 0f, 0.08f, 0.06f, 0.07f, false, 0.9f),    // comic: ink dots in the shadows
        new("PZA Halftone - Newspaper", 5f, 45f, 1f, 1f, 0.1f, 0.09f, 0.08f, false, 1f),
        new("PZA Halftone - Pop Art", 8f, 15f, 1f, 1f, 0f, 0f, 0f, true, 1f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static HalftoneLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.6.0: one film noir (NoirShader): hard black and white with crushed blacks, a silver tint, darker edges, and optionally ONE
/// colour kept (Sin City's red).</summary>
public sealed record NoirLook(
    string Name,
    float Contrast,      // 0.5..2.5
    float Crush,         // 0..0.5: how much of the dark end turns pure black
    float TintR, float TintG, float TintB,
    float TintAmount,
    int Keep,            // NoirKeep: 0 none, 1 red, 2 amber, 3 green, 4 blue
    float KeepAmount,
    float Edge,          // edge darkening
    float Strength);

public static class Noirs
{
    /// <summary>The hues a noir can keep (OKLab hue angles, degrees), in the Keep list's order after None.</summary>
    public static readonly float[] KeepHues = [0f, 25f, 70f, 145f, 255f];

    public static readonly NoirLook[] All =
    [
        new("PZA Noir - Default", 1.45f, 0.08f, 0.85f, 0.9f, 1f, 0.2f, 0, 0f, 0.4f, 1f),
        new("PZA Noir - Sin City", 1.7f, 0.15f, 1f, 1f, 1f, 0f, 1, 1f, 0.3f, 1f),
        new("PZA Noir - Silver Screen", 1.15f, 0f, 1f, 0.96f, 0.88f, 0.25f, 0, 0f, 0.3f, 1f),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static NoirLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}

/// <summary>0.6.0: a Combo, one pick for several effects (the user's Dust Combo presets and new ones). Each entry: list key -> preset name;
/// a list not named is set Off.</summary>
public sealed record ComboLook(string Name, (string List, string Preset)[] Sets);

public static class Combos
{
    public static readonly ComboLook[] All =
    [
        new("PZA Combo - Painted Miniature", [("Kuwahara", "PZA Kuwahara - Painted Miniature"), ("Toon", "PZA Toon - Cel"), ("TiltShift", "PZA Diorama")]),
        new("PZA Combo - Ink Miniature", [("Toon", "PZA Toon - Anime"), ("Outline", "PZA Outline - Ink"), ("TiltShift", "PZA Miniature")]),
        new("PZA Combo - Retro Cel", [("Look", "PZA PS1 - Standard"), ("Toon", "PZA Toon - Cel"), ("Outline", "PZA Outline - Ink")]),
        new("PZA Combo - CRT Anime", [("Look", "PZA PS1 - CRT"), ("Toon", "PZA Toon - Anime"), ("Screen", "PZA Screen - Default")]),
        new("PZA Combo - Soft 240 Toon", [("Look", "PZA Soft 240 - Standard"), ("Toon", "PZA Toon - Cel"), ("N64", "PZA N64 - Soft 240")]),
        new("PZA Combo - 16-Bit Miniature", [("Look", "PZA Pixel - 1"), ("TiltShift", "PZA Miniature")]),
        new("PZA Combo - HD-2D", [("Look", "PZA Pixel - 2"), ("TiltShift", "PZA HD-2D")]),
        new("PZA Combo - Diorama", [("Look", "PZA Diorama - Pixel"), ("TiltShift", "PZA Diorama")]),
        new("PZA Combo - Film Noir", [("Noir", "PZA Noir - Default"), ("Outline", "PZA Outline - Soft")]),
        new("PZA Combo - Comic Book", [("Toon", "PZA Toon - Anime"), ("Outline", "PZA Outline - Ink"), ("Halftone", "PZA Halftone - Default")]),
        new("PZA Combo - Handheld", [("Look", "PZA Game Boy")]),
        new("PZA Combo - Found Footage", [("Screen", "PZA Screen - VHS Worn")]),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name), Looks.Custom];

    public static ComboLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));
}
