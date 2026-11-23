// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.6.0: three new passes, all our own code.
//   Noir      hard black and white: contrast round mid grey, crushed blacks, a silver tint, darker edges, optionally one colour kept (by its
//             OKLab hue, the way Sin City keeps red).
//   Halftone  printed dots on a rotated grid: mono ink dots where the picture is darker than the shadow start (comic), every tone on flat
//             paper (newspaper), or cyan / magenta / yellow dots at their own angles (pop art).
//   Screen    drawn LAST: lens colour fringe (chromatic aberration, radial), the CRT wiggle (per-line wobble, a slow vertical roll,
//             flicker) and VHS tape (a tracking band rolling down, noise, colour smear that keeps the brightness sharp, softness).
// Rows: Meitou's targets are bottom-up (uFlipY): line-based effects use top-down picture rows.
namespace PZA.Looks.Meitou;

public static class NoirShader
{
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform vec2 uViewport;
        uniform float uContrast, uCrush, uTintAmount, uKeepHue, uKeepAmount, uEdge, uStrength;
        uniform vec3 uTint;
        uniform int uSplit;

        const vec3 LUMA = vec3(0.2126, 0.7152, 0.0722);

        vec3 ToOklab(vec3 s)
        {
            vec3 c = mix(pow((s + 0.055) / 1.055, vec3(2.4)), s / 12.92, vec3(lessThanEqual(s, vec3(0.04045))));
            vec3 lms = vec3(dot(c, vec3(0.4122214708, 0.5363415953, 0.0514459929)), dot(c, vec3(0.2119034982, 0.6806995451, 0.1073969566)),
                            dot(c, vec3(0.0883024619, 0.2817188376, 0.6299787005)));
            vec3 q = pow(max(lms, 0.0), vec3(1.0 / 3.0));
            return vec3(dot(q, vec3(0.2104542553, 0.7936177850, -0.0040720468)), dot(q, vec3(1.9779984951, -2.4285922050, 0.4505937099)),
                        dot(q, vec3(0.0259040371, 0.7827717662, -0.8086757660)));
        }

        float Grey(float l)
        {
            l = clamp((l - 0.5) * uContrast + 0.5, 0.0, 1.0);
            float k = clamp(uCrush, 0.0, 0.9);
            return clamp((l - k) / (1.0 - k), 0.0, 1.0);
        }

        void main()
        {
            vec3 orig = textureLod(uImage, vUv, 0.0).rgb;
            float g = Grey(dot(orig, LUMA));
            vec3 bw = vec3(g) * mix(vec3(1.0), uTint, clamp(uTintAmount, 0.0, 1.0));
            if (uKeepHue >= 0.0 && uKeepAmount > 0.0)
            {
                vec3 lab = ToOklab(clamp(orig, 0.0, 1.0));
                float chroma = length(lab.yz);
                float hue = degrees(atan(lab.z, lab.y));
                float dh = abs(mod(hue - uKeepHue + 540.0, 360.0) - 180.0);
                float w = clamp(uKeepAmount, 0.0, 1.0) * (1.0 - smoothstep(12.0, 28.0, dh)) * smoothstep(0.04, 0.09, chroma);
                vec3 kept = orig * (g / max(dot(orig, LUMA), 1e-3));
                bw = mix(bw, clamp(kept, 0.0, 1.0), w);
            }
            vec2 q = (vUv - 0.5) * vec2(uViewport.x / uViewport.y, 1.0);
            bw *= 1.0 - clamp(uEdge, 0.0, 1.0) * smoothstep(0.35, 1.05, length(q));
            vec3 c = mix(orig, bw, clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && vUv.x < 0.5) c = orig;
            fragColour = vec4(c, 1.0);
        }
        """;
}

public static class HalftoneShader
{
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform vec2 uViewport;
        uniform float uDot, uAngle, uThreshold, uPaper, uStrength;
        uniform vec3 uInk;
        uniform int uColour, uFlipY, uSplit;

        const vec3 LUMA = vec3(0.2126, 0.7152, 0.0722);
        const vec3 PAPER = vec3(0.96, 0.94, 0.88);

        // the share of a dot's cell inked for a darkness 0..1, with dots only past the shadow start
        float Coverage(float dark)
        {
            float t = clamp(uThreshold, 0.0, 1.0);
            if (t <= 0.0) return 0.0;
            return clamp((dark - (1.0 - t)) / t, 0.0, 1.0);
        }

        // 1 inside the dot of the cell holding picture pixel p (grid at angle a degrees, spacing s pixels), 0 outside, soft at the rim
        float Dot(vec2 p, float a, float s, float cover)
        {
            float r = radians(a);
            vec2 q = mat2(cos(r), -sin(r), sin(r), cos(r)) * p / s;
            vec2 f = fract(q) - 0.5;
            float rad = sqrt(cover) * 0.75;   // 0.75 of a cell: the dots touch near full cover
            float aa = 1.0 / s;
            return 1.0 - smoothstep(rad - aa, rad + aa, length(f));
        }

        void main()
        {
            vec3 orig = textureLod(uImage, vUv, 0.0).rgb;
            vec2 p = vec2(vUv.x, uFlipY != 0 ? 1.0 - vUv.y : vUv.y) * uViewport;
            float s = max(2.0, uDot * uViewport.y / 1080.0);
            vec3 base = mix(orig, PAPER, clamp(uPaper, 0.0, 1.0));
            vec3 c;
            if (uColour != 0)
            {
                vec3 cmy = 1.0 - clamp(orig, 0.0, 1.0);
                float dc = Dot(p, uAngle + 15.0, s, Coverage(cmy.r));
                float dm = Dot(p, uAngle + 75.0, s, Coverage(cmy.g));
                float dy = Dot(p, uAngle, s, Coverage(cmy.b));
                c = base * (1.0 - vec3(dc, 0.0, 0.0)) * (1.0 - vec3(0.0, dm, 0.0)) * (1.0 - vec3(0.0, 0.0, dy));
            }
            else
            {
                float d = Dot(p, uAngle, s, Coverage(1.0 - dot(orig, LUMA)));
                c = mix(base, uInk, d);
            }
            c = mix(orig, c, clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && vUv.x < 0.5) c = orig;
            fragColour = vec4(c, 1.0);
        }
        """;
}

public static class ScreenShader
{
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform vec2 uViewport;
        uniform float uTime, uFringe, uWobble, uRoll, uFlicker, uTracking, uNoise, uBleed, uSoften, uStrength;
        uniform int uFlipY, uSplit;

        float Hash(vec2 v) { return fract(sin(dot(v, vec2(12.9898, 78.233))) * 43758.5453); }

        vec3 At(vec2 uv) { return textureLod(uImage, clamp(uv, vec2(0.0), vec2(1.0)), 0.0).rgb; }

        vec3 Fringed(vec2 uv, float f)
        {
            vec2 d = (uv - 0.5) * f;
            return vec3(At(uv + d).r, At(uv).g, At(uv - d).b);
        }

        // YIQ: brightness and colour apart, so colour can smear while the brightness stays sharp (composite video)
        vec3 ToYiq(vec3 c) { return vec3(dot(c, vec3(0.299, 0.587, 0.114)), dot(c, vec3(0.596, -0.274, -0.322)), dot(c, vec3(0.211, -0.523, 0.312))); }
        vec3 FromYiq(vec3 y) { return vec3(y.x + 0.956 * y.y + 0.621 * y.z, y.x - 0.272 * y.y - 0.647 * y.z, y.x - 1.106 * y.y + 1.703 * y.z); }

        void main()
        {
            vec3 orig = At(vUv);
            float py = uFlipY != 0 ? 1.0 - vUv.y : vUv.y;   // 0 at the top
            float line = floor(py * uViewport.y);
            float t = uTime;
            vec2 px = 1.0 / uViewport;

            // the CRT wiggle: each line wobbles sideways, the picture rolls slowly, the brightness flickers
            float dx = uWobble * 0.0018 * (sin(py * 220.0 + t * 5.0) + 0.6 * sin(py * 37.0 - t * 2.3) + 0.4 * (Hash(vec2(line, floor(t * 30.0))) - 0.5));
            float roll = uRoll * 0.015 * sin(t * 0.35);
            // VHS: a tracking band rolls down the picture, tearing its lines sideways
            float band = fract(t * 0.07);
            float inBand = (1.0 - smoothstep(0.0, 0.05, abs(py - band))) * uTracking;
            dx += inBand * 0.025 * (Hash(vec2(line, floor(t * 24.0))) - 0.5);
            vec2 uv = vUv + vec2(dx, uFlipY != 0 ? -roll : roll);

            vec3 c = Fringed(uv, clamp(uFringe, 0.0, 0.05));
            if (uSoften > 0.0)
            {
                vec3 blur = (Fringed(uv - vec2(px.x * 1.5, 0.0), uFringe) + Fringed(uv + vec2(px.x * 1.5, 0.0), uFringe)) * 0.5;
                c = mix(c, blur, clamp(uSoften, 0.0, 1.0) * 0.6);
            }
            if (uBleed > 0.0)
            {
                vec3 iq = vec3(0.0);
                for (int i = 1; i <= 6; ++i) iq += ToYiq(At(uv - vec2(px.x * float(i) * 1.5, 0.0)));
                vec3 y = ToYiq(c);
                y.yz = mix(y.yz, iq.yz / 6.0, clamp(uBleed, 0.0, 1.0) * 0.8);
                y.yz *= 1.0 - 0.25 * clamp(uBleed, 0.0, 1.0);   // tape colour is weaker
                c = FromYiq(y);
            }
            float grain = Hash(gl_FragCoord.xy + vec2(floor(t * 60.0) * 13.0, 7.0)) - 0.5;
            c += grain * (uNoise * 0.16 + inBand * 0.35);
            c *= 1.0 + (Hash(vec2(floor(t * 30.0), 3.0)) - 0.5) * 0.08 * uFlicker;
            c = clamp(c, 0.0, 1.0);
            c = mix(orig, c, clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && vUv.x < 0.5) c = orig;
            fragColour = vec4(c, 1.0);
        }
        """;
}

/// <summary>The new passes' maths in C# (the offline check and the fuzzer).</summary>
public static class Effects060Math
{
    /// <summary>NoirShader's Grey: the black-and-white level of a brightness l.</summary>
    public static double NoirGrey(double l, double contrast, double crush)
    {
        double x = Math.Clamp(((double.IsFinite(l) ? l : 0) - 0.5) * (double.IsFinite(contrast) ? contrast : 1) + 0.5, 0, 1);
        double k = Math.Clamp(double.IsFinite(crush) ? crush : 0, 0, 0.9);
        return Math.Clamp((x - k) / (1 - k), 0, 1);
    }

    /// <summary>NoirShader's hue distance in degrees, 0..180, whatever the angles.</summary>
    public static double HueDistance(double a, double b)
    {
        double d = ((a - b + 540.0) % 360.0 + 360.0) % 360.0;
        return Math.Abs(d - 180.0);
    }

    /// <summary>HalftoneShader's Coverage.</summary>
    public static double Coverage(double dark, double threshold)
    {
        double t = Math.Clamp(double.IsFinite(threshold) ? threshold : 0, 0, 1);
        if (t <= 0) return 0;
        return Math.Clamp(((double.IsFinite(dark) ? dark : 0) - (1 - t)) / t, 0, 1);
    }
}
