// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.5.0: PZA N64, the part of our Dust PZA N64 effect a post pass can do: distance fog in the scene's OWN colour (the N64's short draw
// distance faded into the picture's average, tinted), and colour strength. The fog colour is the mean of a fixed 6 x 4 grid of the picture,
// the same taps for every pixel (cheap, cache-friendly). Distances are shares of Kenshi's far clip (55,000 units), as in Dust; the view
// depth comes from Meitou's DepthConvention.Glsl. Texture blur and smoothed light are not here: they belong to the materials, which a
// post pass does not see.
namespace PZA.Looks.Meitou;

public static class N64Shader
{
    public static string Fragment(string depthConvention) => "#version 330 core\n" + depthConvention + Body;

    const string Body = """
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform sampler2D uDepth;
        uniform vec2 uPlanes;
        uniform float uFogStart, uFogEnd, uFogAmount, uSaturation, uStrength;
        uniform vec3 uTint;
        uniform int uFogSky, uSplit;

        float Fog(float z)
        {
            float a = max(uFogStart, 0.0), b = max(uFogEnd, a + 1.0);
            return clamp((z - a) / (b - a), 0.0, 1.0) * clamp(uFogAmount, 0.0, 1.0);
        }

        void main()
        {
            vec3 orig = textureLod(uImage, vUv, 0.0).rgb;
            vec3 avg = vec3(0.0);
            for (int j = 0; j < 4; ++j)
            for (int i = 0; i < 6; ++i)
                avg += textureLod(uImage, vec2((float(i) + 0.5) / 6.0, (float(j) + 0.5) / 4.0), 0.0).rgb;
            avg /= 24.0;
            float d = textureLod(uDepth, vUv, 0.0).r;   // depth and picture share orientation (both render targets)
            bool sky = depthIsSky(d);
            float f = sky ? (uFogSky != 0 ? clamp(uFogAmount, 0.0, 1.0) : 0.0) : Fog(depthViewZ(d, uPlanes));
            vec3 c = mix(orig, avg * uTint, f);
            float l = dot(c, vec3(0.299, 0.587, 0.114));
            c = mix(vec3(l), c, max(uSaturation, 0.0));
            c = mix(orig, clamp(c, 0.0, 1.0), clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && vUv.x < 0.5) c = orig;
            fragColour = vec4(c, 1.0);
        }
        """;

    /// <summary>The shader's Fog in C# (for the check and the fuzzer): 0 before the start, the amount past the end, linear between.</summary>
    public static double Fog(double z, double start, double end, double amount)
    {
        double a = Math.Max(double.IsFinite(start) ? start : 0, 0), b = Math.Max(double.IsFinite(end) ? end : a + 1, a + 1);
        double zz = double.IsFinite(z) ? z : b;
        return Math.Clamp((zz - a) / (b - a), 0, 1) * Math.Clamp(double.IsFinite(amount) ? amount : 0, 0, 1);
    }
}
