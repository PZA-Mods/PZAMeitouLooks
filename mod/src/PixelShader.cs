// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The pixel-art look, ported from our Dust effect (pza_pixelart_ps.hlsl, HLSL) to the GLSL 330 Meitou's passes are written in
// (MeitouClient PostProcessShaders: compiled by shaderc, run on Vulkan). Same maths, line for line: chunky pixels, a small palette, ordered
// dither; the optional tone shaping (shadow lift faded by the frame's darkness, highlight hold) and the CRT section (scanlines, softness,
// colour bleed); the N64 de-dither. Left out for now: the table palettes (Lospec, Custom 16, Palette maker: Dust palettes 6 and up) and the
// camera-following grid.
//
// Rows: Dust's picture has row 0 at the top. Meitou's Vulkan targets hold the picture bottom-up (IGpuContext.RenderTargetsBottomUp), so with
// uFlipY the shader works in top-down "picture" pixels (the grid starts at the top-left as in Dust; scanlines darken the bottom of each art
// pixel) and only flips when it reads the texture.
namespace PZA.Looks.Meitou;

public static class PixelShader
{
    /// <summary>Desert 16 (PZA), 0..255: the shader's PAL16 and, for pattern dithering, its OKLab values (worked out here, baked into the
    /// shader as constants).</summary>
    public static readonly (int R, int G, int B)[] Desert16 =
    [
        (26, 20, 22), (59, 42, 38), (94, 60, 46), (138, 82, 56), (180, 116, 72), (214, 160, 104), (236, 210, 160), (246, 238, 216),
        (110, 106, 94), (160, 154, 136), (76, 94, 72), (124, 140, 90), (58, 78, 106), (94, 138, 168), (156, 196, 210), (140, 46, 34),
    ];

    /// <summary>OKLab (Ottosson 2020) of an sRGB colour in 0..1, as the shader's ToOklab.</summary>
    public static (double L, double A, double B) Oklab(double r, double g, double b)
    {
        static double Lin(double s) => s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        double lr = Lin(r), lg = Lin(g), lb = Lin(b);
        double l = Math.Cbrt(0.4122214708 * lr + 0.5363415953 * lg + 0.0514459929 * lb);
        double m = Math.Cbrt(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
        double s = Math.Cbrt(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
        return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s, 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    /// <summary>"const vec3 PAL16LAB[17]": Desert 16 in OKLab, then black (the 17th colour, used while "keep true black" is on).</summary>
    static string LabTable()
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var items = Desert16.Select(c => Oklab(c.R / 255.0, c.G / 255.0, c.B / 255.0)).Append((0.0, 0.0, 0.0))
            .Select(v => string.Format(ci, "vec3({0:R}, {1:R}, {2:R})", v.Item1, v.Item2, v.Item3));
        return "const vec3 PAL16LAB[17] = vec3[](" + string.Join(", ", items) + ");\n";
    }

    public static readonly string Fragment = Head.Replace("//@PAL16LAB", LabTable(), StringComparison.Ordinal);

    const string Head = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;     // the composite's finished LDR picture (display size, after FXAA / SMAA / heat haze)
        uniform vec2 uViewport;       // display size in pixels
        uniform float uPixelSize, uNativeLines, uDither, uContrast, uShadowLift, uHighlightHold, uScanlines, uCrtSoft, uBleed, uStrength;
        uniform int uPalette, uLevels, uSampling, uLiftNight, uKeepBlack, uSplit, uFlipY;

        const vec3 LUMA = vec3(0.299, 0.587, 0.114);

        // Desert 16 (PZA)
        const vec3 PAL16[16] = vec3[](
            vec3(26, 20, 22) / 255.0, vec3(59, 42, 38) / 255.0, vec3(94, 60, 46) / 255.0, vec3(138, 82, 56) / 255.0,
            vec3(180, 116, 72) / 255.0, vec3(214, 160, 104) / 255.0, vec3(236, 210, 160) / 255.0, vec3(246, 238, 216) / 255.0,
            vec3(110, 106, 94) / 255.0, vec3(160, 154, 136) / 255.0, vec3(76, 94, 72) / 255.0, vec3(124, 140, 90) / 255.0,
            vec3(58, 78, 106) / 255.0, vec3(94, 138, 168) / 255.0, vec3(156, 196, 210) / 255.0, vec3(140, 46, 34) / 255.0);
        //@PAL16LAB
        // LEAF 2-bit (PZA)
        const vec3 PAL2[4] = vec3[](vec3(66, 24, 30) / 255.0, vec3(122, 60, 58) / 255.0, vec3(168, 106, 84) / 255.0, vec3(204, 150, 114) / 255.0);

        const float BAYER[16] = float[](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
        // the PlayStation's 4x4 dither table, in 8-bit steps, added before truncating to 5 bits per channel
        const float PS1DITHER[16] = float[](-4.0, 0.0, -3.0, 1.0, 2.0, -2.0, 3.0, -1.0, -3.0, 1.0, -4.0, 0.0, 3.0, -1.0, 2.0, -2.0);
        // N64 RDP "magic square" dither, 0..7 in 8-bit steps (community port of angrylion-rdp; unverified against hardware)
        const float N64MAGIC[16] = float[](0.0, 6.0, 1.0, 7.0, 4.0, 2.0, 5.0, 3.0, 3.0, 5.0, 2.0, 4.0, 7.0, 1.0, 6.0, 0.0);

        // p: top-down picture pixels (0,0 = top-left corner)
        vec3 SceneAt(vec2 p)
        {
            vec2 uv = p / uViewport;
            if (uFlipY != 0) uv.y = 1.0 - uv.y;
            return textureLod(uImage, uv, 0.0).rgb;
        }
        vec3 SceneUv(vec2 uv) { return SceneAt(uv * uViewport); }

        vec3 Tone(vec3 c)
        {
            float L = dot(c, LUMA);
            float lift = uShadowLift;
            if (uLiftNight != 0 && lift > 0.0)
            {
                float k = 0.0;
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) k += dot(SceneUv(vec2((float(i) + 0.5) / 4.0, (float(j) + 0.5) / 4.0)), LUMA);
                k /= 16.0;
                lift *= 1.0 - smoothstep(0.10, 0.24, k);   // frame average below 0.10 = night (full lift), above 0.24 = day (none)
            }
            c = c * (1.0 + lift * 3.2 * (1.0 - smoothstep(0.0, 0.4, L))) + lift * 0.045;
            c = mix(c, 0.5 + (c - 0.5) * 0.45, uHighlightHold * smoothstep(0.4, 0.85, L));
            return clamp(c, 0.0, 1.0);
        }

        vec3 ToOklab(vec3 s)
        {
            vec3 c = mix(pow((s + 0.055) / 1.055, vec3(2.4)), s / 12.92, vec3(lessThanEqual(s, vec3(0.04045))));
            vec3 lms = vec3(dot(c, vec3(0.4122214708, 0.5363415953, 0.0514459929)),
                            dot(c, vec3(0.2119034982, 0.6806995451, 0.1073969566)),
                            dot(c, vec3(0.0883024619, 0.2817188376, 0.6299787005)));
            vec3 q = pow(max(lms, 0.0), vec3(1.0 / 3.0));
            return vec3(dot(q, vec3(0.2104542553, 0.7936177850, -0.0040720468)),
                        dot(q, vec3(1.9779984951, -2.4285922050, 0.4505937099)),
                        dot(q, vec3(0.0259040371, 0.7827717662, -0.8086757660)));
        }
        vec3 Pal16(int i) { return i < 16 ? PAL16[i] : vec3(0.0); }

        // 0.2.4 (the user, 2026-11-17: dust storms through Desert 16 came out grey-green with orange specks): PATTERN dithering (after
        // Yliluoma). Of every pair of palette colours, the one whose MIX in OKLab best matches the art pixel's colour, at a ratio in 16ths;
        // the Bayer threshold then picks one of the two, so a cell area averages back to the real colour. A sandy haze is grey + orange in
        // the right ratio, not grey-green. The small penalty (0.15 x the pair's distance x r(1-r)) prefers close pairs: less speckle.
        // Black is the 17th colour while "keep true black" is on (Dust's v4 rule). Replaces the nearest-colour-plus-noise of Dust's Desert 16.
        // 0.2.5: only the pairs that hold the NEAREST colour (17 instead of 153): the same quality on a 400-colour test (mean OKLab error
        // 0.1215 against 0.1191, storm hazes within 10.7 deg of hue against 18.2), about 4.5 times cheaper (0.2.4 cost +3.2 ms at 4K).
        vec3 Pattern16(vec3 c, float d)
        {
            vec3 lc = ToOklab(c);
            int n = uKeepBlack != 0 ? 17 : 16;
            int near = 0;
            float nearD = 1e9;
            for (int i = 0; i < 17; ++i)
            {
                if (i >= n) break;
                vec3 q = lc - PAL16LAB[i];
                float dq = dot(q, q);
                if (dq < nearD) { nearD = dq; near = i; }
            }
            float best = 1e9;
            vec3 res = Pal16(near);
            for (int k = 0; k < 17; ++k)
            {
                if (k >= n) break;
                int a = min(near, k), b = max(near, k);
                vec3 la = PAL16LAB[a], dd = PAL16LAB[b] - la;
                float den = dot(dd, dd);
                float r = den < 1e-12 ? 0.0 : clamp(dot(lc - la, dd) / den, 0.0, 1.0);
                r = roundEven(r * 16.0) / 16.0;
                vec3 e3 = lc - (la + dd * r);
                float e = dot(e3, e3) + 0.15 * den * r * (1.0 - r);
                if (e < best) { best = e; res = d < r ? Pal16(b) : Pal16(a); }
            }
            return res;
        }

        vec3 QuantizeCell(vec2 cell, float ps)
        {
            vec2 origin = cell * ps;
            vec3 c;
            if (uSampling == 1)
            {
                int n = ps >= 6.0 ? 3 : 2;
                c = vec3(0.0);
                for (int j = 0; j < 3; ++j) for (int i = 0; i < 3; ++i)
                    if (i < n && j < n) c += SceneAt(origin + (vec2(i, j) + 0.5) * (ps / float(n)));
                c /= float(n * n);
            }
            else c = SceneAt(origin + ps * 0.5);
            c = clamp((c - 0.5) * uContrast + 0.5, 0.0, 1.0);
            c = Tone(c);
            int bi = (int(cell.x) & 3) + 4 * (int(cell.y) & 3);
            float d = (BAYER[bi] + 0.5) / 16.0;
            vec3 res;
            if (uPalette == 1)
            {
                float t = dot(c, LUMA) * 3.0 + (d - 0.5) * uDither;
                res = PAL2[clamp(int(floor(t + 0.5)), 0, 3)];
            }
            else if (uPalette == 2)
            {
                float n = float(max(2, uLevels)) - 1.0;
                res = floor(c * n + 0.5 + (d - 0.5) * uDither) / n;
            }
            else if (uPalette == 3)
            {
                vec3 v = clamp(c * 255.0 + PS1DITHER[bi] * uDither, 0.0, 255.0);
                res = floor(v / 8.0) / 31.0;
            }
            else if (uPalette == 4)
            {
                vec3 v = clamp(c * 255.0 + N64MAGIC[bi] * uDither, 0.0, 255.0);
                res = floor(v / 8.0) / 31.0;
            }
            else if (uPalette == 5)
            {
                vec3 n = vec3(7.0, 7.0, 3.0);
                res = floor(c * n + 0.5 + (d - 0.5) * uDither) / n;
            }
            else res = Pattern16(c, d);   // Desert 16 (0.2.4): pattern dithering; uDither does not apply (the mix ratio is the dither)
            return res;
        }

        vec3 PixelArt(vec2 gp, float scale)
        {
            // roundEven = HLSL round() on D3D (ties to even): Dust's 2.5 -> 2 and 1080 / 240 = 4.5 -> 4
            float ps = uNativeLines > 0.0 ? max(1.0, roundEven(uViewport.y / uNativeLines)) : max(1.0, roundEven(uPixelSize * scale));
            vec2 cell = floor(gp / ps);
            vec3 res = QuantizeCell(cell, ps);
            if (uPalette == 4)
            {   // the N64 video interface's de-dither filter
                vec3 sum = res * 2.0, wsum = vec3(2.0);
                for (int j = -1; j <= 1; ++j) for (int i = -1; i <= 1; ++i)
                {
                    if (i == 0 && j == 0) continue;
                    vec3 nb = QuantizeCell(cell + vec2(i, j), ps);
                    vec3 w = step(abs(nb - res), vec3(1.5 / 31.0));
                    sum += nb * w; wsum += w;
                }
                res = sum / wsum;
            }
            if (uCrtSoft > 0.0 || uBleed > 0.0)
            {
                vec3 l = QuantizeCell(cell - vec2(1.0, 0.0), ps);
                vec3 rr = QuantizeCell(cell + vec2(1.0, 0.0), ps);
                float fx = fract(gp.x / ps);
                vec3 nb = fx < 0.5 ? l : rr;
                float edge = 1.0 - abs(fx - 0.5) * 2.0;
                vec3 soft = mix(res, nb, uCrtSoft * 0.5 * edge);
                vec3 outc = soft;
                if (uBleed > 0.0)
                {   // composite video: the colour smears to the right over ~3 art pixels, the brightness does not
                    vec3 src[5];
                    src[0] = QuantizeCell(cell - vec2(3.0, 0.0), ps);
                    src[1] = QuantizeCell(cell - vec2(2.0, 0.0), ps);
                    src[2] = l; src[3] = res; src[4] = rr;
                    float t = 3.0 + fx;
                    vec3 csum = vec3(0.0);
                    float wsum = 0.0;
                    for (int k = 0; k < 5; ++k)
                    {
                        float u = t - (float(k) + 0.5);
                        float w = u >= -0.5 ? exp(-max(u, 0.0) / 1.3) : exp(-(-0.5 - u) * 4.0) * 0.6;
                        csum += (src[k] - dot(src[k], LUMA)) * w; wsum += w;
                    }
                    vec3 chroma = mix(soft - dot(soft, LUMA), csum / wsum, uBleed);
                    outc = dot(soft, LUMA) + chroma * (1.0 + 0.25 * uBleed);
                }
                res = outc;
            }
            if (uScanlines > 0.0 && ps >= 2.0)
            {
                float fy = fract(gp.y / ps);
                res *= 1.0 - uScanlines * 0.45 * smoothstep(0.55, 0.95, fy);
            }
            return clamp(res, 0.0, 1.0);
        }

        void main()
        {
            vec2 gp = vec2(gl_FragCoord.x, uFlipY != 0 ? uViewport.y - gl_FragCoord.y : gl_FragCoord.y);
            vec3 orig = SceneAt(gp);
            float scale = max(1.0, uViewport.y / 1080.0);
            vec3 styled = PixelArt(gp, scale);
            vec3 outc = mix(orig, styled, clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && gp.x < uViewport.x * 0.5) outc = orig;   // compare: the left half as Meitou draws it
            fragColour = vec4(clamp(outc, 0.0, 1.0), 1.0);
        }
        """;
}
