// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0 / 0.7.0: PZA Outline, our own edge pass with full Dust Rendering Framework parity (the Dust Outline settings were the guide; all maths
// is written fresh in GLSL 330):
// - DEPTH lines (silhouettes): point-sampled texelFetch Laplacian of INVERSE view depth (1/z) over the four neighbours, divided by the centre's
//   1/z. 1/z is linear on any flat surface, so a grazing floor gives 0; a silhouette jump gives a strong line; point sampling avoids bilinear
//   depth smearing under upscalers (FSR/DLSS/TAA).
// - CREASE / NORMAL lines (folds): Roberts Cross sum of both diagonal normal differences across a Thickness-sized quad. When Meitou's packed
//   surface target (_giAlbedo, GI/SSR) is populated, reads the real octahedral world-space shading normals directly (zero silhouette halo, zero
//   far-terrain noise). Otherwise (or on pixels without a surface normal, e.g. foliage/water), falls back to minimum-depth-delta view-space
//   normals with depth-jump rejection so silhouettes never tilt the normal or create thick ghost creases.
// - DISTANCE CUTOFF & FADE: MaxDepth scales a fixed 5,000-unit reference distance (capped to the view's far plane), so Ultra's 0.5 = 2,500 units
//   (active play area and town, never distant mountains 30,000+ units away on Meitou's 60,000+ far plane), with a smooth fade over the outer 35%.
// - DEBUG VIEW: Red = depth edges, Green = normal/crease edges (matching Dust's DebugView).
namespace PZA.Looks.Meitou;

public static class OutlineShader
{
    /// <summary>Reference distance (world units) for MaxDepth = 1.0: 5,000 units (~500 m), so Ultra's 0.5 = 2,500 units (whole town &amp; mid-ground,
    /// without outlining distant mountains on Meitou's 60,000+ view distance).</summary>
    public const float ReferenceFar = 5000f;

    public static string Fragment(string depthConvention, bool useSurface = false) =>
        "#version 330 core\n" + depthConvention + Head + (useSurface ? SurfaceHelper : DepthOnlyHelper) + Tail;

    const string Head = """
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;     // the picture so far (LDR, display size)
        uniform sampler2D uDepth;     // the scene depth (render size)
        uniform vec2 uPlanes;
        uniform float uDepthThr, uNormalThr, uThickness, uStrength, uMaxDepth, uRefFar, uTanHalfFov, uAspect;
        uniform vec3 uColour;
        uniform int uFlipY, uSplit, uDebug;

        float ViewZAt(ivec2 px, ivec2 size)
        {
            float d = texelFetch(uDepth, clamp(px, ivec2(0), size - 1), 0).r;
            return depthIsSky(d) ? 0.0 : depthViewZ(d, uPlanes);
        }
        float InvZ(float z) { return z > 0.0 ? 1.0 / max(z, 1e-3) : 0.0; }   // fuzz 2026-11-19: a near-zero depth overflowed 1/z (NaN lines)
        vec3 ViewPosAt(ivec2 px, ivec2 size, float z)
        {
            vec2 uv = (vec2(px) + 0.5) / vec2(size);
            float picY = uFlipY != 0 ? 1.0 - uv.y : uv.y;
            return vec3((uv.x * 2.0 - 1.0) * uAspect * uTanHalfFov, (1.0 - picY * 2.0) * uTanHalfFov, 1.0) * z;
        }

        // Unit-safe normal difference (1 - dot(na, nb), exact 0 for identical inputs, 0 for degenerate/zero normals).
        float NormalDiff(vec3 a, vec3 b)
        {
            float aa = dot(a, a), bb = dot(b, b);
            if (aa <= 1e-4 || bb <= 1e-4) return 0.0;
            vec3 d = a * inversesqrt(aa) - b * inversesqrt(bb);
            return 0.5 * dot(d, d);
        }

        // Silhouette-safe view-space normal from depth: picks the nearer neighbour on each axis so edges do not tilt the normal.
        vec3 DepthNormalAt(ivec2 p, ivec2 size, float z)
        {
            if (z <= 0.0) return vec3(0.0);
            float wC = InvZ(z);
            float zL = ViewZAt(p - ivec2(1, 0), size), zR = ViewZAt(p + ivec2(1, 0), size);
            float zU = ViewZAt(p - ivec2(0, 1), size), zD = ViewZAt(p + ivec2(0, 1), size);
            float dL = zL > 0.0 ? abs(InvZ(zL) - wC) : 1e30;
            float dR = zR > 0.0 ? abs(InvZ(zR) - wC) : 1e30;
            float dU = zU > 0.0 ? abs(InvZ(zU) - wC) : 1e30;
            float dD = zD > 0.0 ? abs(InvZ(zD) - wC) : 1e30;
            if (min(dL, dR) > 0.1 * wC || min(dU, dD) > 0.1 * wC) return vec3(0.0);
            vec3 pC = ViewPosAt(p, size, z);
            vec3 dx = dR < dL ? ViewPosAt(p + ivec2(1, 0), size, zR) - pC : pC - ViewPosAt(p - ivec2(1, 0), size, zL);
            vec3 dy = dD < dU ? ViewPosAt(p + ivec2(0, 1), size, zD) - pC : pC - ViewPosAt(p - ivec2(0, 1), size, zU);
            vec3 n = cross(dx, dy);
            float nn = dot(n, n);
            if (nn <= 1e-12) return vec3(0.0);
            n *= inversesqrt(nn);
            return dot(n, -pC) < 0.0 ? -n : n;
        }
        """;

    const string SurfaceHelper = """
        uniform usampler2D uSurface;  // Meitou's _giAlbedo (octahedral world-space shading normals when bit 8 of w is set)

        vec3 OctDecode(vec2 p)
        {
            vec3 d = vec3(p.x, 1.0 - abs(p.x) - abs(p.y), p.y);
            if (d.y < 0.0) d.xz = (1.0 - abs(d.zx)) * vec2(d.x >= 0.0 ? 1.0 : -1.0, d.z >= 0.0 ? 1.0 : -1.0);
            return normalize(d);
        }

        bool SurfaceNormalAt(ivec2 p, ivec2 size, out vec3 n)
        {
            uvec4 s = texelFetch(uSurface, clamp(p, ivec2(0), size - 1), 0);
            if ((s.w & 256u) == 0u) { n = vec3(0.0); return false; }
            n = OctDecode(vec2(s.xy) / 65535.0 * 2.0 - 1.0);
            return true;
        }

        float NormalEdge(ivec2 c, ivec2 size, int t, float zC, float zR, float zD)
        {
            ivec2 pR = c + ivec2(t, 0), pD = c + ivec2(0, t), pRD = c + ivec2(t, t);
            vec3 n00, n10, n01, n11;
            if (SurfaceNormalAt(c, size, n00) && SurfaceNormalAt(pR, size, n10)
                && SurfaceNormalAt(pD, size, n01) && SurfaceNormalAt(pRD, size, n11))
            {
                float fold = NormalDiff(n00, n11) + NormalDiff(n10, n01);
                return smoothstep(uNormalThr, 1.5 * uNormalThr + 1e-5, fold);
            }
            float zRD = ViewZAt(pRD, size);
            if (min(min(zR, zD), zRD) <= 0.0) return 0.0;
            n00 = DepthNormalAt(c, size, zC);
            n10 = DepthNormalAt(pR, size, zR);
            n01 = DepthNormalAt(pD, size, zD);
            n11 = DepthNormalAt(pRD, size, zRD);
            float fold = NormalDiff(n00, n11) + NormalDiff(n10, n01);
            return smoothstep(uNormalThr, 1.5 * uNormalThr + 1e-5, fold);
        }
        """;

    const string DepthOnlyHelper = """
        float NormalEdge(ivec2 c, ivec2 size, int t, float zC, float zR, float zD)
        {
            ivec2 pR = c + ivec2(t, 0), pD = c + ivec2(0, t), pRD = c + ivec2(t, t);
            float zRD = ViewZAt(pRD, size);
            if (min(min(zR, zD), zRD) <= 0.0) return 0.0;
            vec3 n00 = DepthNormalAt(c, size, zC);
            vec3 n10 = DepthNormalAt(pR, size, zR);
            vec3 n01 = DepthNormalAt(pD, size, zD);
            vec3 n11 = DepthNormalAt(pRD, size, zRD);
            float fold = NormalDiff(n00, n11) + NormalDiff(n10, n01);
            return smoothstep(uNormalThr, 1.5 * uNormalThr + 1e-5, fold);
        }
        """;

    const string Tail = """
        // Returns vec2(depthEdge, normalEdge), both smoothly faded toward maxDist.
        vec2 Edges()
        {
            ivec2 size = textureSize(uDepth, 0);
            ivec2 c = clamp(ivec2(floor(vUv * vec2(size))), ivec2(0), size - 1);
            float maxDist = min(uMaxDepth * uRefFar, uPlanes.y * 0.99);
            float zC = ViewZAt(c, size);
            if (zC <= 0.0 || zC >= maxDist) return vec2(0.0);
            int t = max(1, int(round(uThickness)));
            float zL = ViewZAt(c - ivec2(t, 0), size), zR = ViewZAt(c + ivec2(t, 0), size);
            float zU = ViewZAt(c - ivec2(0, t), size), zD = ViewZAt(c + ivec2(0, t), size);
            float wC = InvZ(zC);   // > 0: zC > 0 here
            float lap = abs(InvZ(zL) + InvZ(zR) + InvZ(zU) + InvZ(zD) - 4.0 * wC) / wC;
            float depthE = smoothstep(uDepthThr, 1.5 * uDepthThr, lap);
            float normalE = uNormalThr > 0.0 ? NormalEdge(c, size, t, zC, zR, zD) : 0.0;
            float fade = 1.0 - smoothstep(maxDist * 0.65, maxDist, zC);
            return vec2(depthE, normalE) * fade;
        }

        void main()
        {
            vec3 c = textureLod(uImage, vUv, 0.0).rgb;
            vec2 edges = Edges();
            if (uDebug != 0)
            {
                if (uSplit != 0 && vUv.x < 0.5) { fragColour = vec4(c, 1.0); return; }
                fragColour = vec4(edges.x, edges.y, 0.0, 1.0);
                return;
            }
            float e = max(edges.x, edges.y);
            if (!(uSplit != 0 && vUv.x < 0.5)) c = mix(c, uColour, e * clamp(uStrength, 0.0, 1.0));
            fragColour = vec4(c, 1.0);
        }
        """;
}

/// <summary>The shader's Edges in C#, for the offline check and the fuzz harness: depth z(i, j) on a pixel grid (0 = sky), the same taps,
/// silhouette-safe depth normals, Roberts Cross sum, and reference-distance fade. (i, j) = the centre pixel; columns grow right, rows grow DOWN.</summary>
public static class OutlineMath
{
    public static double Edge(Func<int, int, double> z, int i, int j, int w, int h, double tanHalf, double depthThr, double normalThr,
        int thickness, double maxDepth, double far, double refFar = OutlineShader.ReferenceFar)
    {
        var (d, n) = Edges(z, i, j, w, h, tanHalf, depthThr, normalThr, thickness, maxDepth, far, refFar);
        return Math.Max(d, n);
    }

    public static (double Depth, double Normal) Edges(Func<int, int, double> z, int i, int j, int w, int h, double tanHalf, double depthThr,
        double normalThr, int thickness, double maxDepth, double far, double refFar = OutlineShader.ReferenceFar)
    {
        int t = Math.Clamp(thickness, 1, 4);
        double Z(int x, int y)
        {
            double v = z(Math.Clamp(x, 0, w - 1), Math.Clamp(y, 0, h - 1));
            return double.IsFinite(v) && v > 0 ? Math.Max(v, 1e-3) : 0;   // as the shader's InvZ floor (fuzz 2026-11-19)
        }
        static double Inv(double v) => v > 0 ? 1 / v : 0;
        double aspect = w / (double)h;
        System.Numerics.Vector3 P(int x, int y, double zz)
        {
            double u = (Math.Clamp(x, 0, w - 1) + 0.5) / w, v = (Math.Clamp(y, 0, h - 1) + 0.5) / h;
            return new((float)((u * 2 - 1) * aspect * tanHalf * zz), (float)((1 - v * 2) * tanHalf * zz), (float)zz);
        }
        System.Numerics.Vector3 DepthNormal(int x, int y, double zz)
        {
            if (zz <= 0) return default;
            double wc = Inv(zz);
            double zl = Z(x - 1, y), zr = Z(x + 1, y), zu = Z(x, y - 1), zd = Z(x, y + 1);
            double dl = zl > 0 ? Math.Abs(Inv(zl) - wc) : 1e30, dr = zr > 0 ? Math.Abs(Inv(zr) - wc) : 1e30;
            double du = zu > 0 ? Math.Abs(Inv(zu) - wc) : 1e30, dd = zd > 0 ? Math.Abs(Inv(zd) - wc) : 1e30;
            if (Math.Min(dl, dr) > 0.1 * wc || Math.Min(du, dd) > 0.1 * wc) return default;
            var pc = P(x, y, zz);
            var dx = dr < dl ? P(x + 1, y, zr) - pc : pc - P(x - 1, y, zl);
            var dy = dd < du ? P(x, y + 1, zd) - pc : pc - P(x, y - 1, zu);
            var n = System.Numerics.Vector3.Cross(dx, dy);
            float nn = n.LengthSquared();
            if (!float.IsFinite(nn) || nn <= 1e-12f) return default;
            n /= MathF.Sqrt(nn);
            return System.Numerics.Vector3.Dot(n, -pc) < 0 ? -n : n;
        }
        static double NormalDiff(System.Numerics.Vector3 a, System.Numerics.Vector3 b)
        {
            float aa = a.LengthSquared(), bb = b.LengthSquared();
            if (!float.IsFinite(aa) || !float.IsFinite(bb) || aa <= 1e-4f || bb <= 1e-4f) return 0;
            var d = a / MathF.Sqrt(aa) - b / MathF.Sqrt(bb);
            double r = 0.5 * d.LengthSquared();
            return double.IsFinite(r) ? r : 0;
        }

        double maxDist = Math.Min(maxDepth * refFar, far * 0.99);
        double zC = Z(i, j);
        if (zC <= 0 || zC >= maxDist) return (0, 0);
        double zL = Z(i - t, j), zR = Z(i + t, j), zU = Z(i, j - t), zD = Z(i, j + t);
        double wC = Inv(zC);
        double lap = Math.Abs(Inv(zL) + Inv(zR) + Inv(zU) + Inv(zD) - 4 * wC) / wC;
        double depthE = Smooth(depthThr, 1.5 * depthThr, lap);
        double normalE = 0;
        if (normalThr > 0)
        {
            double zRD = Z(i + t, j + t);
            if (Math.Min(Math.Min(zR, zD), zRD) > 0)
            {
                var n00 = DepthNormal(i, j, zC);
                var n10 = DepthNormal(i + t, j, zR);
                var n01 = DepthNormal(i, j + t, zD);
                var n11 = DepthNormal(i + t, j + t, zRD);
                double fold = NormalDiff(n00, n11) + NormalDiff(n10, n01);
                if (double.IsFinite(fold)) normalE = Smooth(normalThr, 1.5 * normalThr + 1e-5, fold);
            }
        }
        double fade = 1.0 - Smooth(maxDist * 0.65, maxDist, zC);
        return (depthE * fade, normalE * fade);
    }

    /// <summary>GLSL smoothstep (supports reversed edges e0 &gt; e1 for fade-out).</summary>
    public static double Smooth(double e0, double e1, double x)
    {
        if (e0 == e1) return x < e0 ? 0 : 1;
        double k = Math.Clamp((x - e0) / (e1 - e0), 0, 1);
        return k * k * (3 - 2 * k);
    }
}
