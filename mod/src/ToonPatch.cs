// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.5.0: PZA Toon, the light banded WHERE MEITOU COMPUTES IT (route C of our Toon feasibility study). Every lit
// program (terrain, buildings, characters, foliage, grass) goes through Meitou's one lighting function, kenshiLight (AtmosphereShaders),
// and is compiled through ShaderLibrary.Legacy / Native / NativeTessellated. A Harmony prefix on those three rewrites ONE line of the
// fragment source before it compiles: the light term (diffuse + envDiffuse) goes through pzaToonLight (two edges, three levels, soft steps,
// a shadow tint, a rim on the silhouette) and the albedo through pzaToonAlbedo (saturation). Specular, the lamps and the GI writes are
// untouched. Better than a post pass: the bands follow the LIGHT, never the texture's brightness, and characters and foliage are in.
//
// Limits (said on the Settings page): the programs compile once, so Toon is FROZEN at the game's start (the values are baked into the
// source as constants; a later change applies at the next start); the lamps keep Meitou's light; Dust's Flatten is not in (it needs the
// texture's neighbours). Fragile on purpose-built text: the line must match exactly, else nothing is changed and the log says so; the
// offline check reads Meitou's own shader source for every release it is run on.
using System.Globalization;
using HarmonyLib;

namespace PZA.Looks.Meitou;

public static class ToonPatch
{
    /// <summary>The 6-argument kenshiLight's head: our functions go right before it.</summary>
    public const string Anchor = "vec3 kenshiLight(vec3 albedo, vec3 n, vec3 v, float gloss, vec3 world, vec3 diffuseNl)";
    /// <summary>Meitou's line that combines the sun's and the sky's diffuse light with the albedo (0.20.1 .. 0.22.2, the same text).</summary>
    public const string LitLine = "vec3 lit = albedo * (diffuse + envDiffuse) + sunSpecular + envSpecular;";
    public const string LitNew = "vec3 lit = pzaToonAlbedo(albedo) * pzaToonLight(diffuse + envDiffuse, vec3(ATMO_PI * sun * fresnelDiffuse) + envDiffuse, n, v) + sunSpecular + envSpecular;";

    /// <summary>The Toon this game runs with (frozen at Initialize; null = Toon off, nothing patched).</summary>
    public static ToonLook? Frozen { get; private set; }
    public static int Patched { get; private set; }
    public static int Mismatched { get; private set; }
    static string? s_functions;
    static bool s_mismatchLogged;

    static string F(float v) => (float.IsFinite(v) ? v : 0f).ToString("0.0#####", CultureInfo.InvariantCulture);

    /// <summary>Our GLSL functions with the look's values baked in (Meitou's native programs take no new loose uniforms).</summary>
    public static string Functions(ToonLook t) => $$"""

        // ---- PZA's Post-FX: PZA Toon (0.5.0), '{{t.Name.Replace("*", "")}}' ----
        const float PZA_TOON_STRENGTH = {{F(t.Strength)}};
        const float PZA_TOON_AUTO = {{(t.AutoLight ? "1.0" : "0.0")}};
        const float PZA_TOON_REF = {{F(t.LightRef)}};
        const float PZA_TOON_SHADOW_EDGE = {{F(t.ShadowEdge)}};
        const float PZA_TOON_HIGH_EDGE = {{F(t.HighlightEdge)}};
        const float PZA_TOON_SOFT = {{F(Math.Max(t.Softness, 0.0005f))}};
        const vec3 PZA_TOON_LEVELS = vec3({{F(t.ShadowLevel)}}, {{F(t.MidLevel)}}, {{F(t.LitLevel)}});
        const vec3 PZA_TOON_TINT = vec3({{F(t.TintR)}}, {{F(t.TintG)}}, {{F(t.TintB)}});
        const float PZA_TOON_TINT_AMOUNT = {{F(t.TintAmount)}};
        const float PZA_TOON_RIM = {{F(t.Rim)}};
        const vec3 PZA_TOON_RIM_COLOUR = vec3({{F(t.RimR)}}, {{F(t.RimG)}}, {{F(t.RimB)}});
        const float PZA_TOON_SATURATION = {{F(t.Saturation)}};

        float pzaToonLuma(vec3 c) { return dot(c, vec3(0.2126, 0.7152, 0.0722)); }

        // The banded level for a light 0 (none) .. 1 (the full sun on a facing surface): shadow, mid, lit, joined by soft steps.
        float pzaToonLevel(float t)
        {
            float level = mix(PZA_TOON_LEVELS.x, PZA_TOON_LEVELS.y, smoothstep(PZA_TOON_SHADOW_EDGE - PZA_TOON_SOFT, PZA_TOON_SHADOW_EDGE + PZA_TOON_SOFT, t));
            return mix(level, PZA_TOON_LEVELS.z, smoothstep(PZA_TOON_HIGH_EDGE - PZA_TOON_SOFT, PZA_TOON_HIGH_EDGE + PZA_TOON_SOFT, t));
        }

        vec3 pzaToonLight(vec3 light, vec3 full, vec3 n, vec3 v)
        {
            float lref = PZA_TOON_AUTO > 0.5 ? max(pzaToonLuma(full), 1e-4) : max(PZA_TOON_REF, 1e-4);
            float l = pzaToonLuma(light);
            float t = clamp(l / lref, 0.0, 1.5);
            float level = pzaToonLevel(t) / max(PZA_TOON_LEVELS.z, 1e-3);   // the lit band = the full light
            vec3 hue = l > 1e-6 ? light / l : vec3(1.0);
            vec3 banded = hue * level * lref;
            float shade = 1.0 - smoothstep(PZA_TOON_SHADOW_EDGE - PZA_TOON_SOFT, PZA_TOON_SHADOW_EDGE + PZA_TOON_SOFT, t);
            banded *= mix(vec3(1.0), PZA_TOON_TINT, clamp(shade * PZA_TOON_TINT_AMOUNT, 0.0, 1.0));
            float rim = pow(1.0 - clamp(dot(n, v), 0.0, 1.0), 3.0) * PZA_TOON_RIM;
            banded += PZA_TOON_RIM_COLOUR * rim * lref * (0.35 + 0.65 * (1.0 - shade));
            return mix(light, banded, clamp(PZA_TOON_STRENGTH, 0.0, 1.0));
        }

        vec3 pzaToonAlbedo(vec3 albedo)
        {
            vec3 s = mix(vec3(pzaToonLuma(albedo)), albedo, max(PZA_TOON_SATURATION, 0.0));
            return mix(albedo, max(s, vec3(0.0)), clamp(PZA_TOON_STRENGTH, 0.0, 1.0));
        }
        // ---- end PZA Toon ----

        """;

    /// <summary>The source with Toon in, or null when it is not a lit program (no kenshiLight). mismatch = kenshiLight is there but its
    /// line is not Meitou's line as we know it (then the source is left as it was).</summary>
    public static string? Rewrite(string source, ToonLook t, out bool mismatch)
    {
        mismatch = false;
        int at = source.IndexOf(Anchor, StringComparison.Ordinal);
        if (at < 0) return null;
        int lit = source.IndexOf(LitLine, at, StringComparison.Ordinal);
        if (lit < 0 || source.IndexOf(LitLine, lit + LitLine.Length, StringComparison.Ordinal) >= 0 || source.Contains("pzaToonLight", StringComparison.Ordinal))
        {
            mismatch = true;
            return null;
        }
        string body = source[..lit] + LitNew + source[(lit + LitLine.Length)..];
        return body[..at] + Functions(t) + body[at..];
    }

    /// <summary>Freezes Toon for this game and patches the three compile points (nothing when Toon is Off). Returns the log line.</summary>
    public static string Apply(Harmony harmony, ToonLook? toon)
    {
        Frozen = toon;
        if (toon is null) return "toon: Off (Meitou's lighting untouched)";
        s_functions = Functions(toon);
        var lib = typeof(global::Meitou.Game.Rendering.Gpu.ShaderLibrary);
        int n = 0;
        foreach (var m in lib.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (m.Name is not ("Legacy" or "Native" or "NativeTessellated")) continue;
            if (!m.GetParameters().Any(p => p.Name == "fragmentGlsl" && p.ParameterType == typeof(string))) continue;
            harmony.Patch(m, prefix: new HarmonyMethod(typeof(ToonPatch), nameof(Prefix)));
            n++;
        }
        return n == 0 ? "toon: stays OFF: ShaderLibrary has no Legacy / Native / NativeTessellated with a fragmentGlsl"
            : $"toon: '{toon.Name}' frozen for this game (a change applies at the next start); {n} compile points patched";
    }

    static void Prefix(ref string fragmentGlsl, string name)
    {
        if (Frozen is not { } t || string.IsNullOrEmpty(fragmentGlsl)) return;
        try
        {
            var done = Rewrite(fragmentGlsl, t, out bool mismatch);
            if (mismatch)
            {
                Mismatched++;
                if (!s_mismatchLogged) Log.Line($"toon: '{name}' has kenshiLight but not the light line we know: left as Meitou wrote it (a newer Meitou?)");
                s_mismatchLogged = true;
                return;
            }
            if (done is null) return;
            fragmentGlsl = done;
            Patched++;
            if ((Patched & (Patched - 1)) == 0) Log.Line($"toon: banded the light in {Patched} program(s), latest '{name}'");
        }
        catch (Exception e)
        {
            Log.Line($"toon: '{name}' left as Meitou wrote it ({e.GetType().Name}: {e.Message})");
        }
    }
}

/// <summary>pzaToonLight's level in C# (the check and the fuzzer): the light's share of the full light t -> the banded level / the lit level.</summary>
public static class ToonMath
{
    static double Smooth(double e0, double e1, double x)
    {
        double k = Math.Clamp((x - e0) / (e1 - e0), 0, 1);
        return k * k * (3 - 2 * k);
    }

    /// <summary>As Functions bakes a value: a broken one (NaN, infinite) becomes 0.</summary>
    static double B(float v) => float.IsFinite(v) ? v : 0;

    public static double Level(ToonLook t, double share)
    {
        double s = Math.Max(B(t.Softness), 0.0005);
        double x = Math.Clamp(double.IsFinite(share) ? share : 0, 0, 1.5);
        double se = B(t.ShadowEdge), he = B(t.HighlightEdge), sl = B(t.ShadowLevel), ml = B(t.MidLevel), ll = B(t.LitLevel);
        double level = sl + (ml - sl) * Smooth(se - s, se + s, x);
        level += (ll - level) * Smooth(he - s, he + s, x);
        return level / Math.Max(ll, 1e-3);
    }
}
