// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0: PZA Outline, our own edge pass from the scene depth (the Dust Outline effect's settings were the guide; this maths is written fresh).
// Two kinds of line, the stronger one wins:
// - DEPTH lines (silhouettes): the Laplacian of INVERSE view depth (1/z) over the four neighbours, divided by the centre's 1/z. 1/z is linear
//   across the screen on any flat surface, so a floor seen at a grazing angle gives exactly 0 (a plain depth Laplacian draws lines all over a
//   far floor); a jump between a character and what is behind it gives a big value. The line starts at the threshold and is full at 1.5x.
// - CREASE lines (folds): normals rebuilt from depth on a 2x2 block (forward differences of view positions), Roberts cross between the
//   diagonals, 1 - cos of the fold. Threshold 0 = no crease lines.
// Sky is never lined (a character against the sky still is: its neighbour's 1/z is 0). Beyond MaxDepth x the far plane nothing is lined.
// Runs FIRST (before Tilt-Shift, Fisheye and the look), on the picture the depth belongs to: the lines then blur, bend and pixelate with it.
// Depth is at the render size (it can differ from the display with an upscaler): taps step in depth pixels, Thickness of them.
namespace PZA.Looks.Meitou;

public static class OutlineShader
{
    public static string Fragment(string depthConvention) => "#version 330 core\n" + depthConvention + Body;

    const string Body = """
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;     // the picture so far (LDR, display size)
        uniform sampler2D uDepth;     // the scene depth (render size)
        uniform vec2 uPlanes;
        uniform float uDepthThr, uNormalThr, uThickness, uStrength, uMaxDepth, uTanHalfFov, uAspect;
        uniform vec3 uColour;
        uniform int uFlipY, uSplit, uDebug;

        // tex coordinates of a top-down picture point; view depth (sky = 0 marks it)
        vec2 TexOf(vec2 pic) { return uFlipY != 0 ? vec2(pic.x, 1.0 - pic.y) : pic; }
        float ViewZ(vec2 pic)
        {
            float d = textureLod(uDepth, TexOf(pic), 0.0).r;
            return depthIsSky(d) ? 0.0 : depthViewZ(d, uPlanes);
        }
        float InvZ(float z) { return z > 0.0 ? 1.0 / max(z, 1e-3) : 0.0; }   // fuzz 2026-11-19: a near-zero depth overflowed 1/z (NaN lines)
        vec3 ViewPos(vec2 pic, float z)
        {
            return vec3((pic.x * 2.0 - 1.0) * uAspect * uTanHalfFov, (1.0 - pic.y * 2.0) * uTanHalfFov, 1.0) * z;
        }

        float Edge(vec2 pic)
        {
            vec2 px = uThickness / vec2(textureSize(uDepth, 0));
            float zC = ViewZ(pic);
            if (zC <= 0.0 || zC > uMaxDepth * uPlanes.y) return 0.0;
            float zL = ViewZ(pic - vec2(px.x, 0.0)), zR = ViewZ(pic + vec2(px.x, 0.0));
            float zU = ViewZ(pic - vec2(0.0, px.y)), zD = ViewZ(pic + vec2(0.0, px.y));
            float wC = InvZ(zC);   // > 0: zC > 0 here
            float lap = abs(InvZ(zL) + InvZ(zR) + InvZ(zU) + InvZ(zD) - 4.0 * wC) / wC;
            float e = smoothstep(uDepthThr, 1.5 * uDepthThr, lap);
            if (uNormalThr > 0.0 && e < 1.0)
            {
                // the 2x2 block C, R, D, RD; each normal from its own right and down neighbours
                float zRD = ViewZ(pic + px), zR2 = ViewZ(pic + vec2(2.0 * px.x, 0.0)), zD2 = ViewZ(pic + vec2(0.0, 2.0 * px.y));
                float zR2D = ViewZ(pic + vec2(2.0 * px.x, px.y)), zRD2 = ViewZ(pic + vec2(px.x, 2.0 * px.y));
                if (min(min(min(zR, zD), min(zRD, zR2)), min(min(zD2, zR2D), zRD2)) > 0.0)
                {
                    vec2 dx = vec2(px.x, 0.0), dy = vec2(0.0, px.y);
                    vec3 pC = ViewPos(pic, zC), pR = ViewPos(pic + dx, zR), pD = ViewPos(pic + dy, zD), pRD = ViewPos(pic + px, zRD);
                    vec3 nC = normalize(cross(pR - pC, pD - pC));
                    vec3 nR = normalize(cross(ViewPos(pic + 2.0 * dx, zR2) - pR, pRD - pR));
                    vec3 nD = normalize(cross(pRD - pD, ViewPos(pic + dy + dy, zD2) - pD));
                    vec3 nRD = normalize(cross(ViewPos(pic + 2.0 * dx + dy, zR2D) - pRD, ViewPos(pic + dx + 2.0 * dy, zRD2) - pRD));
                    float fold = max(1.0 - dot(nC, nRD), 1.0 - dot(nR, nD));
                    e = max(e, smoothstep(uNormalThr, 1.5 * uNormalThr + 1e-5, fold));
                }
            }
            return e;
        }

        void main()
        {
            vec2 pic = vec2(vUv.x, uFlipY != 0 ? 1.0 - vUv.y : vUv.y);
            vec3 c = textureLod(uImage, vUv, 0.0).rgb;
            float e = Edge(pic);
            if (uDebug != 0) { fragColour = vec4(vec3(1.0 - e), 1.0); return; }
            if (!(uSplit != 0 && vUv.x < 0.5)) c = mix(c, uColour, e * clamp(uStrength, 0.0, 1.0));
            fragColour = vec4(c, 1.0);
        }
        """;
}

/// <summary>The shader's Edge in C#, for the offline check and the fuzz harness: depth z(i, j) on a pixel grid (0 = sky), the same taps and
/// thresholds. (i, j) = the centre pixel; columns grow right, rows grow DOWN (top-down picture coordinates).</summary>
public static class OutlineMath
{
    public static double Edge(Func<int, int, double> z, int i, int j, int w, int h, double tanHalf, double depthThr, double normalThr,
        int thickness, double maxDepth, double far)
    {
        int t = Math.Clamp(thickness, 1, 4);
        double Z(int di, int dj)
        {
            double v = z(Math.Clamp(i + di * t, 0, w - 1), Math.Clamp(j + dj * t, 0, h - 1));
            return double.IsFinite(v) && v > 0 ? Math.Max(v, 1e-3) : 0;   // as the shader's InvZ floor (fuzz 2026-11-19)
        }
        static double Inv(double v) => v > 0 ? 1 / v : 0;
        double aspect = w / (double)h;
        System.Numerics.Vector3 P(int di, int dj, double zz)
        {
            double u = (i + di * t + 0.5) / w, v = (j + dj * t + 0.5) / h;
            return new((float)((u * 2 - 1) * aspect * tanHalf * zz), (float)((1 - v * 2) * tanHalf * zz), (float)zz);
        }
        double zC = Z(0, 0);
        if (zC <= 0 || zC > maxDepth * far) return 0;
        double zL = Z(-1, 0), zR = Z(1, 0), zU = Z(0, -1), zD = Z(0, 1);
        double wC = Inv(zC);
        double lap = Math.Abs(Inv(zL) + Inv(zR) + Inv(zU) + Inv(zD) - 4 * wC) / wC;
        double e = Smooth(depthThr, 1.5 * depthThr, lap);
        if (normalThr > 0 && e < 1)
        {
            double zRD = Z(1, 1), zR2 = Z(2, 0), zD2 = Z(0, 2), zR2D = Z(2, 1), zRD2 = Z(1, 2);
            if (Math.Min(Math.Min(Math.Min(zR, zD), Math.Min(zRD, zR2)), Math.Min(Math.Min(zD2, zR2D), zRD2)) > 0)
            {
                var pC = P(0, 0, zC); var pR = P(1, 0, zR); var pD = P(0, 1, zD); var pRD = P(1, 1, zRD);
                var nC = N(pR - pC, pD - pC);
                var nR = N(P(2, 0, zR2) - pR, pRD - pR);
                var nD = N(pRD - pD, P(0, 2, zD2) - pD);
                var nRD = N(P(2, 1, zR2D) - pRD, P(1, 2, zRD2) - pRD);
                double fold = Math.Max(1 - System.Numerics.Vector3.Dot(nC, nRD), 1 - System.Numerics.Vector3.Dot(nR, nD));
                if (double.IsFinite(fold)) e = Math.Max(e, Smooth(normalThr, 1.5 * normalThr + 1e-5, fold));
            }
        }
        return e;
    }

    static System.Numerics.Vector3 N(System.Numerics.Vector3 a, System.Numerics.Vector3 b) => System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(a, b));

    /// <summary>GLSL smoothstep.</summary>
    public static double Smooth(double e0, double e1, double x)
    {
        double k = Math.Clamp((x - e0) / (e1 - e0), 0, 1);
        return k * k * (3 - 2 * k);
    }
}
