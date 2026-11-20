// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
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
    static FisheyeLook? s_frameFish;
    static OutlineLook? s_frameLine;
    static bool s_failed, s_hdrLogged;
    static string? s_lastChoice;
    public static int FramesDrawn { get; private set; }
    /// <summary>Test runs: PZA_LOOKS_TILT_DEBUG=1 shows Tilt-Shift's blur mask (black = sharp) instead of the picture.</summary>
    public static bool TiltMask;
    /// <summary>Test runs: PZA_LOOKS_OUTLINE_DEBUG=1 shows the Outline's lines alone (black on white).</summary>
    public static bool OutlineMask;

    public static string Apply(Harmony harmony)
    {
        harmony.Patch(Access.RunCompositeChain, prefix: new HarmonyMethod(typeof(PixelPass), nameof(Prefix)),
            postfix: new HarmonyMethod(typeof(PixelPass), nameof(Postfix)));
        return "patched PostProcess.RunCompositeChain (the looks on the finished picture, before the interface)";
    }

    static void Prefix(PostProcess __instance, ref PassTargets final, out PassTargets? __state)
    {
        __state = null;
        (s_frameLook, s_frameTilt, s_frameFish, s_frameLine) = (null, null, null, null);
        if (s_failed) return;
        // 0.4.0: the sliders' values (a preset filled them; Custom = the player's own)
        var look = LookValues.Pixel(Settings);
        var tilt = LookValues.Tilt(Settings);
        var fish = LookValues.Fisheye(Settings);
        var line = LookValues.Outline(Settings);
        LogChoice(look, tilt, fish, line);
        if (look is null && tilt is null && fish is null && line is null) return;
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
            if (!TiltPass.Ready(__instance)) (tilt, line) = (null, null);   // no depth yet (first frames): the others alone
            if (look is null && tilt is null && fish is null && line is null) return;
            int passes = (look is null ? 0 : 1) + (tilt is null ? 0 : 1) + (fish is null ? 0 : 1) + (line is null ? 0 : 1);
            Ensure(__instance, w, h, passes >= 2);
            __state = final;
            (s_frameLook, s_frameTilt, s_frameFish, s_frameLine) = (look, tilt, fish, line);
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
            // 0.3.0: Tilt-Shift, Fisheye, the look, in Dust's order; each reads the last one's picture (A, B, A...) and the last draws the frame.
            // 0.4.0: the Outline first (Dust: Outline at POST_LIGHTING 50, before them all), on the picture its depth belongs to
            var steps = new List<(LegacyProgram P, Action<object> Set, string Stamp)>(4);
            var size = (s_width, s_height);
            if (s_frameLine is { } line)
                steps.Add((OutlinePass.Program!, src => OutlinePass.Set(__instance, line, src, Settings.Split, OutlineMask), "pza outline"));
            if (s_frameTilt is { } tilt)
                steps.Add((TiltPass.Program!, src => TiltPass.Set(__instance, tilt, src, size, Settings.Split, TiltMask), "pza tilt-shift"));
            if (s_frameFish is { } fish)
                steps.Add((FisheyePass.Program!, src => FisheyePass.Set(__instance, fish, src, size, Settings.Split), "pza fisheye"));
            if (s_frameLook is { } look)
                steps.Add((LookPass.Program!, src => LookPass.Set(__instance, look, src, size, Settings.Split), "pza looks"));
            object source = s_a!;
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].Set(source);
                if (i == steps.Count - 1) Access.DrawFinal(__instance, steps[i].P, __state);
                else
                {
                    var into = ReferenceEquals(source, s_a) ? s_b! : s_a!;
                    Access.Draw(__instance, steps[i].P, into);
                    source = into;
                }
                Access.Stamp(__instance, steps[i].Stamp);
            }
            if (FramesDrawn++ == 0)
                Log.Line($"first frame drawn: look '{s_frameLook?.Name ?? Looks.Off}', Tilt-Shift '{s_frameTilt?.Name ?? Looks.Off}', "
                    + $"Fisheye '{s_frameFish?.Name ?? Looks.Off}', Outline '{s_frameLine?.Name ?? Looks.Off}' at {s_width}x{s_height}");
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
        FisheyePass.Ensure(pp);
        OutlinePass.Ensure(pp);
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

    static void LogChoice(PixelLook? look, TiltLook? tilt, FisheyeLook? fish, OutlineLook? line)
    {
        // the names only (a slider dragged on the Settings page would log every step otherwise)
        string choice = $"look '{look?.Name ?? Looks.Off}', Tilt-Shift '{tilt?.Name ?? Looks.Off}', Fisheye '{fish?.Name ?? Looks.Off}', "
            + $"Outline '{line?.Name ?? Looks.Off}'";
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
        p.Set(s_strength, l.Strength);
        p.Set(s_palette, l.Palette);
        p.Set(s_levels, l.Levels);
        p.Set(s_sampling, l.Sampling);
        p.Set(s_liftNight, l.LiftNight ? 1 : 0);
        p.Set(s_keepBlack, l.KeepBlack ? 1 : 0);
        p.Set(s_split, split ? 1 : 0);
        p.Set(s_flipY, pp.Gpu.RenderTargetsBottomUp ? 1 : 0);
    }
}

/// <summary>PZA Fisheye (FisheyeShader, 0.3.0). The lens is the chain's own field of view, so PZA's Camera's 0.5x lens widens it, as on Kenshi.</summary>
static class FisheyePass
{
    public static LegacyProgram? Program;
    static SamplerSlot s_image;
    static UniformHandle s_strength, s_tan, s_aspect, s_edge, s_fit, s_split;

    public static void Ensure(PostProcess pp)
    {
        if (Program is not null) return;
        var p = Program = LegacyProgram.Create(pp.Gpu, Access.Vertex!, FisheyeShader.Fragment, "pza looks fisheye");
        s_image = p.Sampler("uImage");
        (s_strength, s_tan, s_aspect, s_edge, s_fit, s_split) = (p.Uniform("uStrength"), p.Uniform("uTanHalfFov"), p.Uniform("uAspect"),
            p.Uniform("uEdgeDark"), p.Uniform("uFit"), p.Uniform("uSplit"));
        Log.Line("fisheye program compiled");
    }

    /// <summary>tan of half the vertical field of view, kept to a lens the maths can take (5 to 170 degrees; a broken value = Kenshi's 45).</summary>
    public static float TanHalf(float fovY) => float.IsFinite(fovY) && fovY > 0.087f && fovY < 2.967f ? MathF.Tan(fovY * 0.5f) : MathF.Tan(0.3927f);

    public static void Set(PostProcess pp, FisheyeLook f, object source, (int W, int H) size, bool split)
    {
        var p = Program!;
        Access.Bind(pp, p, s_image, source);
        p.Set(s_strength, f.Strength);
        p.Set(s_tan, TanHalf(Access.FovY(pp)));
        p.Set(s_aspect, size.W / (float)size.H);
        p.Set(s_edge, f.EdgeDark);
        p.Set(s_fit, f.Fit);
        p.Set(s_split, split ? 1 : 0);
    }
}

/// <summary>0.3.0: the focus point PZA's Camera for Meitou publishes every frame (PZA.Camera.Meitou.CameraFocus: Point, the followed
/// character's tracked point without Off-centre's shift; Eye; Frame, +1 a frame). Read by name, so neither mod references the other.</summary>
public static class CameraFocusLink
{
    static System.Reflection.FieldInfo? s_point, s_eye, s_frame;
    static bool s_looked;
    static long s_lastFrame = long.MinValue;
    static int s_sameFor;

    /// <summary>The camera's point in Looks' view space (x right, y up, z forward), or null when PZA's Camera is not loaded, has not written
    /// a frame lately (5 of ours), or gives a point behind the eye. view = the chain's ViewRotation (a LookAt: forward is -z).</summary>
    public static System.Numerics.Vector3? ViewPoint(System.Numerics.Matrix4x4 view)
    {
        if (!Find()) return null;
        long frame = (long)s_frame!.GetValue(null)!;
        s_sameFor = frame == s_lastFrame ? s_sameFor + 1 : 0;
        s_lastFrame = frame;
        if (frame == 0 || s_sameFor > 5) return null;
        var point = (System.Numerics.Vector3)s_point!.GetValue(null)!;
        var eye = (System.Numerics.Vector3)s_eye!.GetValue(null)!;
        return ToView(point - eye, view);
    }

    /// <summary>An eye-relative point turned into view space; null when it is not finite or not at least half a unit in front.</summary>
    public static System.Numerics.Vector3? ToView(System.Numerics.Vector3 rel, System.Numerics.Matrix4x4 view)
    {
        var v = System.Numerics.Vector3.TransformNormal(rel, view);
        var p = new System.Numerics.Vector3(v.X, v.Y, -v.Z);
        return float.IsFinite(p.X + p.Y + p.Z) && p.Z > 0.5f ? p : null;
    }

    static bool Find()
    {
        if (s_looked) return s_point is not null;
        s_looked = true;
        var t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PZA.Camera.Meitou.CameraFocus")).FirstOrDefault(x => x is not null);
        const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
        (s_point, s_eye, s_frame) = (t?.GetField("Point", F), t?.GetField("Eye", F), t?.GetField("Frame", F));
        bool ok = s_point?.FieldType == typeof(System.Numerics.Vector3) && s_eye?.FieldType == typeof(System.Numerics.Vector3)
            && s_frame?.FieldType == typeof(long);
        if (!ok) s_point = null;
        Log.Line(ok ? "tilt-shift: PZA's Camera found, the focus follows its character point (Off-centre included)"
            : "tilt-shift: PZA's Camera's focus point not found, the focus is the screen centre at the camera's distance");
        return ok;
    }
}

/// <summary>PZA Tilt-Shift (TiltShader): the camera's maths of the Dust effect's PZATiltShift.cpp (the plane, the flat-ground factor).</summary>
public static class TiltPass
{
    public static LegacyProgram? Program;
    static SamplerSlot s_image, s_depth;
    static UniformHandle s_viewport, s_planes, s_strength, s_centre, s_width, s_falloff, s_blur, s_top, s_bottom, s_saturation, s_contrast,
        s_planeN, s_tan, s_aspect, s_gain, s_pivotZ, s_pivotXY, s_mode, s_flipY, s_split, s_debug;
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
        s_pivotXY = p.Uniform("uPivotXY");
        (s_mode, s_flipY, s_split, s_debug) = (p.Uniform("uFocusMode"), p.Uniform("uFlipY"), p.Uniform("uSplit"), p.Uniform("uDebug"));
        Log.Line("tilt-shift program compiled");
    }

    /// <summary>The camera's pitch below the horizon from the view's rotation (System.Numerics LookAt: M13, M23, M33 = the view's back axis,
    /// so the forward axis's height is -M23).</summary>
    public static float PitchBelowHorizon(System.Numerics.Matrix4x4 view) =>
        float.IsFinite(view.M23) ? MathF.Max(0f, MathF.Asin(Math.Clamp(view.M23, -1f, 1f))) : 0f;   // fuzz 2026-11-18: a broken view gave NaN uniforms

    /// <summary>Dust's plane: normal (0, sin a, cos a) with a = upright x pitch (upright 1 in every PZA preset); the flat-ground factor
    /// sin a + cos a cot p, clamped 0.25..2.</summary>
    public static (System.Numerics.Vector3 N, float Gain) Plane(float pitch, float upright = 1f)
    {
        if (!float.IsFinite(pitch)) pitch = 0f;
        if (!float.IsFinite(upright)) upright = 1f;
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
        var view = Access.ViewRotation(pp);
        float pitch = PitchBelowHorizon(view);
        var (n, gain) = Plane(pitch, t.Upright);
        float tan = MathF.Tan(Access.FovY(pp) * 0.5f);
        // 0.3.0: PZA's Camera's character point when it is there (sharp on the character under Off-centre too); else the screen centre at the
        // strategy camera's pivot distance (PostOptions.LensFocus)
        var cam = CameraFocusLink.ViewPoint(view);
        float pivot = cam?.Z ?? (pp.Options.LensFocus > 0 && float.IsFinite(pp.Options.LensFocus) ? pp.Options.LensFocus : 0f);
        p.Set(s_pivotXY, cam?.X ?? 0f, cam?.Y ?? 0f);
        p.Set(s_viewport, size.W, (float)size.H);
        p.Set(s_planes, Access.Planes(pp));
        p.Set(s_strength, t.Strength);
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

/// <summary>0.4.0: PZA Outline (OutlineShader), lines from the scene depth, first in the chain.</summary>
static class OutlinePass
{
    public static LegacyProgram? Program;
    static SamplerSlot s_image, s_depth;
    static UniformHandle s_planes, s_depthThr, s_normalThr, s_thickness, s_strength, s_maxDepth, s_tan, s_aspect, s_colour, s_flipY, s_split, s_debug;
    static bool s_logged;

    public static void Ensure(PostProcess pp)
    {
        if (Program is not null) return;
        var p = Program = LegacyProgram.Create(pp.Gpu, Access.Vertex!, OutlineShader.Fragment(global::Meitou.Game.Rendering.Frame.DepthConvention.Glsl),
            "pza looks outline");
        (s_image, s_depth) = (p.Sampler("uImage"), p.Sampler("uDepth"));
        (s_planes, s_depthThr, s_normalThr, s_thickness, s_strength, s_maxDepth) = (p.Uniform("uPlanes"), p.Uniform("uDepthThr"),
            p.Uniform("uNormalThr"), p.Uniform("uThickness"), p.Uniform("uStrength"), p.Uniform("uMaxDepth"));
        (s_tan, s_aspect, s_colour) = (p.Uniform("uTanHalfFov"), p.Uniform("uAspect"), p.Uniform("uColour"));
        (s_flipY, s_split, s_debug) = (p.Uniform("uFlipY"), p.Uniform("uSplit"), p.Uniform("uDebug"));
        Log.Line("outline program compiled");
    }

    public static void Set(PostProcess pp, OutlineLook o, object source, bool split, bool mask)
    {
        var p = Program!;
        Access.Bind(pp, p, s_image, source);
        Access.Bind(pp, p, s_depth, Access.DepthTarget(pp)!);
        p.Set(s_planes, Access.Planes(pp));
        p.Set(s_depthThr, o.Depth);
        p.Set(s_normalThr, o.Normal);
        p.Set(s_thickness, (float)o.Thickness);
        p.Set(s_strength, o.Strength);
        p.Set(s_maxDepth, o.MaxDepth);
        p.Set(s_tan, FisheyePass.TanHalf(Access.FovY(pp)));
        p.Set(s_aspect, Access.DisplaySize(pp) is var (w, h) && h > 0 ? w / (float)h : 16f / 9f);
        p.Set(s_colour, new System.Numerics.Vector3(o.Red, o.Green, o.Blue));
        p.Set(s_flipY, pp.Gpu.RenderTargetsBottomUp ? 1 : 0);
        p.Set(s_split, split ? 1 : 0);
        p.Set(s_debug, mask ? 1 : 0);
        if (!s_logged)
        {
            s_logged = true;
            Log.Line($"outline: depth {o.Depth}, crease {o.Normal}, {o.Thickness} px, strength {o.Strength}, up to {o.MaxDepth:0.00} of the far plane, planes {Access.Planes(pp)}");
        }
    }
}
