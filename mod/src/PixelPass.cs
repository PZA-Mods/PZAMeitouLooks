// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// Where the looks go (MeitouClient docs/render-post.md; PostProcess.End, 0.20.1): scene -> upscaler (TAA / FSR / DLSS) -> depth of field ->
// exposure -> bloom -> RunCompositeChain [composite: exposure, tone map, grade, dither -> FXAA or SMAA -> heat haze] -> the frame's target ->
// later, the interface layer (UiLayer) on top. The looks belong on the finished LDR picture: after tone mapping (as in Dust, which runs them
// after its tonemap), after the anti-aliasing and the upscaler (they would smear the grid, and the temporal ones would take the dither for
// noise and fight it), and before the interface (menus and text stay sharp). Order as in Dust: Tilt-Shift (205), then Pixel Art (215).
//
// Meitou has no hook there, so a Harmony prefix on RunCompositeChain swaps its `final` argument for a display-size picture of ours (A); the
// chain draws its last pass into that, and the postfix draws: Tilt-Shift A -> B (or -> the real target), then the look B (or A) -> the real
// target. Nothing on, HDR output or a failure: nothing is swapped and the chain runs untouched.
using HarmonyLib;
using Meitou.Game.Rendering.Gpu;
using Meitou.Game.Rendering.Post;

namespace PZA.Looks.Meitou;

static class PixelPass
{
    public static readonly LooksSettings Settings = new();

    /// <summary>Our pictures (PostProcess.Target2D): A the chain's output, B Tilt-Shift's when the look follows it.</summary>
    static object? s_a, s_b;
    static int s_width, s_height;

    /// <summary>What the prefix chose for this frame (read once per frame).</summary>
    static PixelLook? s_frameLook;
    static TiltLook? s_frameTilt;
    static bool s_failed, s_hdrLogged;
    static string? s_lastChoice;
    public static int FramesDrawn { get; private set; }
    /// <summary>Test runs: PZA_LOOKS_TILT_DEBUG=1 shows Tilt-Shift's blur mask (black = sharp) instead of the picture.</summary>
    public static bool TiltMask;

    public static string Apply(Harmony harmony)
    {
        harmony.Patch(Access.RunCompositeChain, prefix: new HarmonyMethod(typeof(PixelPass), nameof(Prefix)),
            postfix: new HarmonyMethod(typeof(PixelPass), nameof(Postfix)));
        return "patched PostProcess.RunCompositeChain (the looks on the finished picture, before the interface)";
    }

    static void Prefix(PostProcess __instance, ref PassTargets final, out PassTargets? __state)
    {
        __state = null;
        (s_frameLook, s_frameTilt) = (null, null);
        if (s_failed) return;
        var look = Looks.Find(Settings.Look);
        var tilt = Tilts.Find(Settings.TiltShift);
        LogChoice(look, tilt);
        if (look is null && tilt is null) return;
        try
        {
            if (__instance.Options.HdrPeak > 1)
            {
                if (!s_hdrLogged) Log.Line("HDR output is on: the looks are made for the SDR picture and stay off");
                s_hdrLogged = true;
                return;
            }
            var (w, h) = Access.DisplaySize(__instance);
            if (w <= 0 || h <= 0) return;
            if (tilt is not null && !TiltPass.Ready(__instance)) tilt = null;   // no depth yet (first frames): the look alone
            if (look is null && tilt is null) return;
            Ensure(__instance, w, h, look is not null && tilt is not null);
            __state = final;
            (s_frameLook, s_frameTilt) = (look, tilt);
            final = PassTargets.Of(Access.TextureOf(s_a!), null);
        }
        catch (Exception e)
        {
            __state = null;
            Fail("prepare", e);
        }
    }

    static void Postfix(PostProcess __instance, PassTargets? __state)
    {
        if (__state is null) return;
        try
        {
            var source = s_a!;
            if (s_frameTilt is { } tilt)
            {
                TiltPass.Set(__instance, tilt, source, (s_width, s_height), Settings.Split, TiltMask);
                if (s_frameLook is null) Access.DrawFinal(__instance, TiltPass.Program!, __state);
                else
                {
                    Access.Draw(__instance, TiltPass.Program!, s_b!);
                    source = s_b!;
                }
                Access.Stamp(__instance, "pza tilt-shift");
            }
            if (s_frameLook is { } look)
            {
                LookPass.Set(__instance, look, source, (s_width, s_height), Settings.Split);
                Access.DrawFinal(__instance, LookPass.Program!, __state);
                Access.Stamp(__instance, "pza looks");
            }
            if (FramesDrawn++ == 0) Log.Line($"first frame drawn: look '{s_frameLook?.Name ?? Looks.Off}', Tilt-Shift '{s_frameTilt?.Name ?? Looks.Off}' at {s_width}x{s_height}");
        }
        catch (Exception e)
        {
            Fail("draw", e);
        }
    }

    /// <summary>The programs (first time) and our pictures at the display size (again after a resize; the old ones are freed after the frames in flight).</summary>
    static void Ensure(PostProcess pp, int w, int h, bool needB)
    {
        LookPass.Ensure(pp);
        TiltPass.Ensure(pp);
        if (s_a is not null && (s_width != w || s_height != h))
        {
            Access.TextureOf(s_a).Dispose();
            if (s_b is not null) Access.TextureOf(s_b).Dispose();
            (s_a, s_b) = (null, null);
        }
        s_a ??= Access.MakeTarget(pp, w, h, "pza looks picture");
        if (needB) s_b ??= Access.MakeTarget(pp, w, h, "pza looks tilt-shift");
        (s_width, s_height) = (w, h);
    }

    static void LogChoice(PixelLook? look, TiltLook? tilt)
    {
        string choice = $"look '{look?.Name ?? Looks.Off}', Tilt-Shift '{tilt?.Name ?? Looks.Off}'";
        if (choice == s_lastChoice) return;
        s_lastChoice = choice;
        Log.Line(choice);
    }

    static void Fail(string where, Exception e)
    {
        s_failed = true;
        var inner = e is System.Reflection.TargetInvocationException { InnerException: { } ie } ? ie : e;
        Log.Line($"OFF for this session: {where} failed ({inner.GetType().Name}: {inner.Message})");
    }
}

/// <summary>The pixel-art look (PixelShader).</summary>
static class LookPass
{
    public static LegacyProgram? Program;
    static SamplerSlot s_image;
    static UniformHandle s_viewport, s_pixelSize, s_nativeLines, s_dither, s_contrast, s_shadowLift, s_highlightHold, s_scanlines, s_crtSoft,
        s_bleed, s_strength, s_palette, s_levels, s_sampling, s_liftNight, s_keepBlack, s_split, s_flipY;

    public static void Ensure(PostProcess pp)
    {
        if (Program is not null) return;
        var p = Program = LegacyProgram.Create(pp.Gpu, Access.Vertex!, PixelShader.Fragment, "pza looks pixel art");
        s_image = p.Sampler("uImage");
        (s_viewport, s_pixelSize, s_nativeLines, s_dither, s_contrast) = (p.Uniform("uViewport"), p.Uniform("uPixelSize"), p.Uniform("uNativeLines"),
            p.Uniform("uDither"), p.Uniform("uContrast"));
        (s_shadowLift, s_highlightHold, s_scanlines, s_crtSoft, s_bleed) = (p.Uniform("uShadowLift"), p.Uniform("uHighlightHold"),
            p.Uniform("uScanlines"), p.Uniform("uCrtSoft"), p.Uniform("uBleed"));
        (s_strength, s_palette, s_levels, s_sampling) = (p.Uniform("uStrength"), p.Uniform("uPalette"), p.Uniform("uLevels"), p.Uniform("uSampling"));
        (s_liftNight, s_keepBlack, s_split, s_flipY) = (p.Uniform("uLiftNight"), p.Uniform("uKeepBlack"), p.Uniform("uSplit"), p.Uniform("uFlipY"));
        Log.Line("pixel art program compiled");
    }

    public static void Set(PostProcess pp, PixelLook l, object source, (int W, int H) size, bool split)
    {
        var p = Program!;
        Access.Bind(pp, p, s_image, source);
        p.Set(s_viewport, size.W, (float)size.H);
        p.Set(s_pixelSize, l.PixelSize);
        p.Set(s_nativeLines, l.NativeLines);
        p.Set(s_dither, l.Dither);
        p.Set(s_contrast, l.Contrast);
        p.Set(s_shadowLift, l.ShadowLift);
        p.Set(s_highlightHold, l.HighlightHold);
        p.Set(s_scanlines, l.Scanlines);
        p.Set(s_crtSoft, l.CrtSoft);
        p.Set(s_bleed, l.Bleed);
        p.Set(s_strength, 1f);
        p.Set(s_palette, l.Palette);
        p.Set(s_levels, l.Levels);
        p.Set(s_sampling, l.Sampling);
        p.Set(s_liftNight, 1);
        p.Set(s_keepBlack, 1);
        p.Set(s_split, split ? 1 : 0);
        p.Set(s_flipY, pp.Gpu.RenderTargetsBottomUp ? 1 : 0);
    }
}

/// <summary>PZA Tilt-Shift (TiltShader): the camera's maths of the Dust effect's PZATiltShift.cpp (the plane, the flat-ground factor).</summary>
public static class TiltPass
{
    public static LegacyProgram? Program;
    static SamplerSlot s_image, s_depth;
    static UniformHandle s_viewport, s_planes, s_strength, s_centre, s_width, s_falloff, s_blur, s_top, s_bottom, s_saturation, s_contrast,
        s_planeN, s_tan, s_aspect, s_gain, s_pivotZ, s_mode, s_flipY, s_split, s_debug;
    static bool s_logged;

    /// <summary>The scene depth exists (from the chain's first Begin).</summary>
    public static bool Ready(PostProcess pp) => Access.DepthTarget(pp) is not null;

    public static void Ensure(PostProcess pp)
    {
        if (Program is not null) return;
        var p = Program = LegacyProgram.Create(pp.Gpu, Access.Vertex!, TiltShader.Fragment(global::Meitou.Game.Rendering.Frame.DepthConvention.Glsl),
            "pza looks tilt-shift");
        (s_image, s_depth) = (p.Sampler("uImage"), p.Sampler("uDepth"));
        (s_viewport, s_planes, s_strength, s_centre, s_width, s_falloff) = (p.Uniform("uViewport"), p.Uniform("uPlanes"), p.Uniform("uStrength"),
            p.Uniform("uFocusCentre"), p.Uniform("uFocusWidth"), p.Uniform("uFalloff"));
        (s_blur, s_top, s_bottom, s_saturation, s_contrast) = (p.Uniform("uBlurPx"), p.Uniform("uTop"), p.Uniform("uBottom"), p.Uniform("uSaturation"),
            p.Uniform("uContrast"));
        (s_planeN, s_tan, s_aspect, s_gain, s_pivotZ) = (p.Uniform("uPlaneN"), p.Uniform("uTanHalfFov"), p.Uniform("uAspect"), p.Uniform("uGroundGain"),
            p.Uniform("uPivotZ"));
        (s_mode, s_flipY, s_split, s_debug) = (p.Uniform("uFocusMode"), p.Uniform("uFlipY"), p.Uniform("uSplit"), p.Uniform("uDebug"));
        Log.Line("tilt-shift program compiled");
    }

    /// <summary>The camera's pitch below the horizon from the view's rotation (System.Numerics LookAt: M13, M23, M33 = the view's back axis,
    /// so the forward axis's height is -M23).</summary>
    public static float PitchBelowHorizon(System.Numerics.Matrix4x4 view) => MathF.Max(0f, MathF.Asin(Math.Clamp(view.M23, -1f, 1f)));

    /// <summary>Dust's plane: normal (0, sin a, cos a) with a = upright x pitch (upright 1 in every PZA preset); the flat-ground factor
    /// sin a + cos a cot p, clamped 0.25..2.</summary>
    public static (System.Numerics.Vector3 N, float Gain) Plane(float pitch, float upright = 1f)
    {
        float a = Math.Clamp(upright, 0f, 1f) * pitch;
        var n = new System.Numerics.Vector3(0f, MathF.Sin(a), MathF.Cos(a));
        float sp = MathF.Max(MathF.Sin(pitch), 0.05f);
        return (n, Math.Clamp(n.Y + n.Z * MathF.Cos(pitch) / sp, 0.25f, 2f));
    }

    public static void Set(PostProcess pp, TiltLook t, object source, (int W, int H) size, bool split, bool mask)
    {
        var p = Program!;
        Access.Bind(pp, p, s_image, source);
        Access.Bind(pp, p, s_depth, Access.DepthTarget(pp)!);
        float pitch = PitchBelowHorizon(Access.ViewRotation(pp));
        var (n, gain) = Plane(pitch);
        float tan = MathF.Tan(Access.FovY(pp) * 0.5f);
        float pivot = pp.Options.LensFocus > 0 && float.IsFinite(pp.Options.LensFocus) ? pp.Options.LensFocus : 0f;
        p.Set(s_viewport, size.W, (float)size.H);
        p.Set(s_planes, Access.Planes(pp));
        p.Set(s_strength, 1f);
        p.Set(s_centre, t.FocusCentre);
        p.Set(s_width, t.FocusWidth);
        p.Set(s_falloff, t.Falloff);
        p.Set(s_blur, t.BlurPx);
        p.Set(s_top, t.Top);
        p.Set(s_bottom, t.Bottom);
        p.Set(s_saturation, t.Saturation);
        p.Set(s_contrast, t.Contrast);
        p.Set(s_planeN, n);
        p.Set(s_tan, tan);
        p.Set(s_aspect, size.W / (float)size.H);
        p.Set(s_gain, gain);
        p.Set(s_pivotZ, pivot);
        p.Set(s_mode, 1);
        p.Set(s_flipY, pp.Gpu.RenderTargetsBottomUp ? 1 : 0);
        p.Set(s_split, split ? 1 : 0);
        p.Set(s_debug, mask ? 2 : 0);
        if (!s_logged)
        {
            s_logged = true;
            Log.Line($"tilt-shift: pitch {pitch * 180 / MathF.PI:0.0} deg below the horizon, flat-ground factor {gain:0.00}, fov tan {tan:0.000}, "
                + $"focus {(pivot > 0 ? $"the camera's point at {pivot:0} units" : "nearest upright thing at the centre")}, planes {Access.Planes(pp)}");
        }
    }
}
