// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The game's post-chain members this mod reaches, resolved once by name (MeitouClient 0.20.1, src/Meitou.Game/Rendering/Post/PostProcess.cs).
// They are internal or private, so a game update can rename them: ResolveAll lists what is missing and the mod then stays off, saying so.
using System.Reflection;
using Meitou.Game.Rendering.Gpu;
using Meitou.Game.Rendering.Post;

namespace PZA.Looks.Meitou;

static class Access
{
    const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>PostProcess.RunCompositeChain(PostOptions, PassTargets final, bool ao, bool auto, Target2D? bloom): the composite and the LDR passes
    /// after it (FXAA or SMAA, the heat haze), whose last pass draws into <c>final</c>, the frame's target, before the interface layer.</summary>
    public static MethodInfo? RunCompositeChain;
    /// <summary>PostProcess.Make(batch, w, h, format, min filter, name, levels): a post target (internal Target2D).</summary>
    static MethodInfo? s_make;
    /// <summary>PostProcess.Bind(LegacyProgram, SamplerSlot, Target2D?).</summary>
    static MethodInfo? s_bind;
    /// <summary>PostProcess.DrawFinal(LegacyProgram, PassTargets): a full-screen pass into the final target, display-size viewport.</summary>
    static MethodInfo? s_drawFinal;
    /// <summary>PostProcess.Stamp(string): a GPU time stamp closing a named section (the chain's own cost readout).</summary>
    static MethodInfo? s_stamp;
    /// <summary>PostProcess._displayWidth / _displayHeight.</summary>
    static FieldInfo? s_displayWidth, s_displayHeight;
    /// <summary>PostProcess.Target2D.Texture.</summary>
    static FieldInfo? s_targetTexture;
    /// <summary>PostProcessShaders.Vertex: the chain's full-screen triangle (vUv 0..1).</summary>
    public static string? Vertex;
    /// <summary>PostProcess.Draw(LegacyProgram, Target2D): a full-screen pass into one of the chain's targets.</summary>
    static MethodInfo? s_draw;
    /// <summary>For Tilt-Shift: PostProcess.SceneDepthTarget (Target2D?), ViewRotation (Matrix4x4), FovY (float), Planes (Vector2).</summary>
    static PropertyInfo? s_depth, s_viewRotation, s_fovY, s_planes;
    /// <summary>Optional (Outline surface normals): PostProcess._giAlbedo (Target2D?) and GiAlbedoTarget (GpuTexture? — non-null when populated this frame).</summary>
    static FieldInfo? s_giAlbedo;
    static PropertyInfo? s_giAlbedoTarget;

    /// <summary>Resolves every member; returns the missing ones (empty = all there).</summary>
    public static List<string> ResolveAll()
    {
        var missing = new List<string>();
        var pp = typeof(PostProcess);
        var target2D = pp.GetNestedType("Target2D", BindingFlags.NonPublic | BindingFlags.Public);
        RunCompositeChain = pp.GetMethod("RunCompositeChain", Inst);
        s_make = pp.GetMethod("Make", Inst);
        s_bind = target2D is null ? null : pp.GetMethod("Bind", Inst, [typeof(LegacyProgram), typeof(SamplerSlot), target2D]);
        s_drawFinal = pp.GetMethod("DrawFinal", Inst, [typeof(LegacyProgram), typeof(PassTargets)]);
        s_stamp = pp.GetMethod("Stamp", Inst, [typeof(string)]);
        s_displayWidth = pp.GetField("_displayWidth", Inst);
        s_displayHeight = pp.GetField("_displayHeight", Inst);
        s_targetTexture = target2D?.GetField("Texture");
        var shaders = pp.Assembly.GetType("Meitou.Game.Rendering.Post.PostProcessShaders");
        Vertex = shaders?.GetField("Vertex", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetRawConstantValue() as string;

        s_draw = target2D is null ? null : pp.GetMethod("Draw", Inst, [typeof(LegacyProgram), target2D]);
        s_depth = pp.GetProperty("SceneDepthTarget", Inst);
        s_viewRotation = pp.GetProperty("ViewRotation", Inst);
        s_fovY = pp.GetProperty("FovY", Inst);
        s_planes = pp.GetProperty("Planes", Inst);
        s_giAlbedo = pp.GetField("_giAlbedo", Inst);
        s_giAlbedoTarget = pp.GetProperty("GiAlbedoTarget", Inst);
        Need(missing, s_draw, "PostProcess.Draw(LegacyProgram, Target2D)");
        Need(missing, s_depth is not null && s_depth.PropertyType == target2D, "PostProcess.SceneDepthTarget");
        Need(missing, s_viewRotation?.PropertyType == typeof(System.Numerics.Matrix4x4), "PostProcess.ViewRotation");
        Need(missing, s_fovY?.PropertyType == typeof(float), "PostProcess.FovY");
        Need(missing, s_planes?.PropertyType == typeof(System.Numerics.Vector2), "PostProcess.Planes");
        Need(missing, RunCompositeChain is not null && Final(RunCompositeChain), "PostProcess.RunCompositeChain(.., PassTargets final, ..)");
        Need(missing, s_make is not null && s_make.GetParameters().Length == 7, "PostProcess.Make");
        Need(missing, s_bind, "PostProcess.Bind(LegacyProgram, SamplerSlot, Target2D)");
        Need(missing, s_drawFinal, "PostProcess.DrawFinal");
        Need(missing, s_stamp, "PostProcess.Stamp");
        Need(missing, s_displayWidth is not null && s_displayHeight is not null, "PostProcess._displayWidth / _displayHeight");
        Need(missing, s_targetTexture, "PostProcess.Target2D.Texture");
        Need(missing, Vertex, "PostProcessShaders.Vertex");
        return missing;
    }

    static bool Final(MethodInfo m) => m.GetParameters().Any(p => p.Name == "final" && p.ParameterType == typeof(PassTargets));

    static void Need(List<string> missing, object? what, string name)
    {
        if (what is null || what is false) missing.Add(name);
    }

    public static (int Width, int Height) DisplaySize(PostProcess pp) => ((int)s_displayWidth!.GetValue(pp)!, (int)s_displayHeight!.GetValue(pp)!);

    /// <summary>A display-size RGBA8 post target with a linear filter, made the way the chain makes its LDR picture (MakeLdrTargets).</summary>
    public static object MakeTarget(PostProcess pp, int width, int height, string name)
    {
        using var batch = pp.Gpu.Uploads.Begin();
        return s_make!.Invoke(pp, [batch, width, height, InternalFormat.Rgba8, TextureMinFilter.Linear, name, 1])!;
    }

    public static GpuTexture TextureOf(object target2D) => (GpuTexture)s_targetTexture!.GetValue(target2D)!;

    public static void Bind(PostProcess pp, LegacyProgram p, SamplerSlot slot, object target2D) => s_bind!.Invoke(pp, [p, slot, target2D]);

    public static void DrawFinal(PostProcess pp, LegacyProgram p, PassTargets final) => s_drawFinal!.Invoke(pp, [p, final]);

    public static void Stamp(PostProcess pp, string name) => s_stamp!.Invoke(pp, [name]);

    public static void Draw(PostProcess pp, LegacyProgram p, object target2D) => s_draw!.Invoke(pp, [p, target2D]);

    /// <summary>The scene depth (Target2D) or null before the targets are made.</summary>
    public static object? DepthTarget(PostProcess pp) => s_depth!.GetValue(pp);

    public static System.Numerics.Matrix4x4 ViewRotation(PostProcess pp) => (System.Numerics.Matrix4x4)s_viewRotation!.GetValue(pp)!;

    public static float FovY(PostProcess pp) => (float)s_fovY!.GetValue(pp)!;

    public static System.Numerics.Vector2 Planes(PostProcess pp) => (System.Numerics.Vector2)s_planes!.GetValue(pp)!;

    /// <summary>The packed surface target (_giAlbedo, Rgba32Ui with octahedral world normals) when GI or SSR populated it this frame; null otherwise.</summary>
    public static object? SurfaceTarget(PostProcess pp)
    {
        if (s_giAlbedo is null || s_giAlbedoTarget is null) return null;
        return s_giAlbedoTarget.GetValue(pp) is not null ? s_giAlbedo.GetValue(pp) : null;
    }
}
