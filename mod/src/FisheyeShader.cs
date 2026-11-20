// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// PZA Fisheye, ported from our Dust effect (pza_fisheye_ps.hlsl and FisheyeMath.h, same author) to GLSL 330: Meitou's normal lens becomes an
// EQUIDISTANT fisheye. For each output radius r the fisheye shows the angle t = tMax * r / R; the source is read at tan(t) / tanHalfFov.
// Fill keeps the corners (no black), Full width keeps the whole width (round, black corners). The lens comes from the chain's own field of
// view (PostProcess.FovY), so PZA's Camera's 0.5x lens widens it as on Kenshi. Radially symmetric round the centre, so Meitou's bottom-up
// rows need no flip. Runs after Tilt-Shift and before the pixel look (Dust: Tilt-Shift 205, Fisheye 207, Pixel Art 215), so art pixels stay square.
namespace PZA.Looks.Meitou;

public static class FisheyeShader
{
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform float uStrength, uTanHalfFov, uAspect, uEdgeDark;
        uniform int uFit, uSplit;

        float SourceRadius(float r)
        {
            float R = uFit == 1 ? uAspect : sqrt(uAspect * uAspect + 1.0);
            float tMax = min(atan(R * uTanHalfFov), 1.55);   // 0.3.0 (fuzz): a lens so wide its corner angle passes ~89 deg keeps Fill filled
            float t = tMax * (r / R);
            if (t >= 1.5607) return uStrength > 0.0 ? 1e6 : r;   // past ~89.4 deg: far outside the image (black), as FisheyeMath.h
            return mix(r, tan(t) / uTanHalfFov, clamp(uStrength, 0.0, 1.0));
        }

        void main()
        {
            // centre-relative position in half screen heights (x spans -aspect..aspect, y spans -1..1)
            vec2 p = vec2((vUv.x - 0.5) * 2.0 * uAspect, (vUv.y - 0.5) * 2.0);
            float r = length(p);
            vec2 src = vUv;
            if (r > 1e-5)
            {
                float rho = SourceRadius(r);
                vec2 q = p * (rho / r);
                src = vec2(q.x / (2.0 * uAspect) + 0.5, q.y * 0.5 + 0.5);
            }
            bool outside = any(lessThan(src, vec2(0.0))) || any(greaterThan(src, vec2(1.0)));
            vec3 c = outside ? vec3(0.0) : textureLod(uImage, src, 0.0).rgb;
            float rn = r / sqrt(uAspect * uAspect + 1.0);
            c *= mix(1.0, 1.0 - rn * rn, clamp(uEdgeDark, 0.0, 1.0) * clamp(uStrength, 0.0, 1.0));   // real fisheyes darken toward the corners
            if (uSplit != 0 && vUv.x < 0.5) c = textureLod(uImage, vUv, 0.0).rgb;                    // compare: the left half without
            fragColour = vec4(c, 1.0);
        }
        """;

    /// <summary>The shader's SourceRadius in C#, for the offline check and the fuzz harness: the radius (half screen heights) the fisheye
    /// reads for an output radius r. fit 1 = full width.</summary>
    public static double SourceRadius(double r, double aspect, double tanHalfFov, int fit, double strength)
    {
        double R = fit == 1 ? aspect : Math.Sqrt(aspect * aspect + 1);
        double tMax = Math.Min(Math.Atan(R * tanHalfFov), 1.55);   // as the shader (fuzz 2026-11-19: only lenses with R x tan > 48 change)
        double t = tMax * (r / R);
        if (t >= 1.5607) return strength > 0 ? 1e6 : r;
        double s = Math.Clamp(strength, 0, 1);
        return r + (Math.Tan(t) / tanHalfFov - r) * s;
    }
}
