// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// PZA Tilt-Shift, ported from our Dust effect (pza_tiltshift_ps.hlsl, version 2.3) to GLSL 330 for Meitou.
// Real tilt-shift from depth: the sharp zone is a PLANE in the world through the focus point, holding the camera's left-right axis; "upright"
// tilts it from facing the camera (an ordinary lens) to standing straight up (the miniature look), so a character standing in the sharp zone
// stays sharp from feet to head and a near wall blurs even in the middle of the screen. The blur grows with a pixel's distance from the plane,
// measured in screen heights at the focus point and divided by the flat-ground factor, so the band settings mean what they meant in Dust.
// A blur tap only counts if its own blur reaches the pixel (no halo round sharp characters). Golden-angle disc gather, 40 taps.
//
// Meitou's inputs (instead of Dust's camera data and Kenshi's distance / farClip depth):
// - the scene depth (render size, reversed-Z), turned into view depth by Meitou's own DepthConvention.Glsl with the chain's planes;
// - the focus point: Meitou's strategy camera looks straight at its pivot, and BeginScene copies the pivot's distance into
//   PostOptions.LensFocus every frame, so the point is the screen centre at that distance (Dust 2.3 read Kenshi's camera point the same way).
//   No distance (0): Dust 2.1's "nearest upright thing in a 3x5 patch at the centre", else 2.0's band median;
// - rows: Meitou's targets are bottom-up (uFlipY), so view-space y and the band positions use top-down picture coordinates.
namespace PZA.Looks.Meitou;

public static class TiltShader
{
    public static string Fragment(string depthConvention) => "#version 330 core\n" + depthConvention + Body;

    const string Body = """
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;     // the picture so far (LDR, display size)
        uniform sampler2D uDepth;     // the scene depth (render size)
        uniform vec2 uViewport, uPlanes;
        uniform float uStrength, uFocusCentre, uFocusWidth, uFalloff, uBlurPx, uTop, uBottom, uSaturation, uContrast;
        uniform vec3 uPlaneN;         // the plane's normal in view space (x right, y up, z forward)
        uniform float uTanHalfFov, uAspect, uGroundGain, uPivotZ;   // uPivotZ: the focus point's view depth at the screen centre, 0 = none
        uniform int uFocusMode, uFlipY, uSplit, uDebug;

        const vec3 LUMA = vec3(0.299, 0.587, 0.114);
        const float GOLDEN = 2.39996323;
        const int TAPS = 40;

        // tex: texture coordinates; pic: the same point in top-down picture coordinates (0..1, y down)
        vec2 TexOf(vec2 pic) { return uFlipY != 0 ? vec2(pic.x, 1.0 - pic.y) : pic; }

        float RawDepth(vec2 pic) { return textureLod(uDepth, TexOf(pic), 0.0).r; }

        vec3 ViewPos(vec2 pic)
        {
            float d = RawDepth(pic);
            float z = depthIsSky(d) ? uPlanes.y : depthViewZ(d, uPlanes);   // sky = as far as the view goes
            return vec3((pic.x * 2.0 - 1.0) * uAspect * uTanHalfFov, (1.0 - pic.y * 2.0) * uTanHalfFov, 1.0) * z;
        }

        void Sort2(inout float a, inout float b) { float lo = min(a, b); b = max(a, b); a = lo; }
        float Median5(float a, float b, float c, float d, float e)
        {
            Sort2(a, b); Sort2(d, e); Sort2(a, d); Sort2(b, e); Sort2(c, d); Sort2(b, c); Sort2(c, d);
            return c;
        }

        void FocusBand(out float s0, out float H)   // 2.0: the median of 5 taps across the band centre
        {
            float s[5], z[5];
            for (int i = 0; i < 5; ++i)
            {
                vec3 p = ViewPos(vec2(0.40 + 0.05 * float(i), uFocusCentre));
                s[i] = dot(p, uPlaneN); z[i] = p.z;
            }
            s0 = Median5(s[0], s[1], s[2], s[3], s[4]);
            H = 2.0 * uTanHalfFov * max(Median5(z[0], z[1], z[2], z[3], z[4]), 0.5);
        }

        void FocusPlane(out float s0, out float H)
        {
            if (uPivotZ > 0.0)
            {   // 2.3: the camera's own point (the screen centre at the pivot's distance)
                vec3 p = vec3(0.0, 0.0, uPivotZ);
                s0 = dot(p, uPlaneN); H = 2.0 * uTanHalfFov * max(uPivotZ, 0.5);
                return;
            }
            // 2.1: the nearest upright thing in a 3x5 centre patch (two taps in a column at nearly the same distance), else 2.0
            float s[15], z[15];
            bool sky[15];
            for (int i = 0; i < 15; ++i)
            {
                vec2 pic = vec2(0.46 + 0.04 * float(i % 3), uFocusCentre + 0.03 * float(i / 3));
                sky[i] = depthIsSky(RawDepth(pic));
                vec3 p = ViewPos(pic);
                s[i] = dot(p, uPlaneN); z[i] = p.z;
            }
            float bestZ = 1e30, bestS = 0.0;
            for (int a = 0; a < 15; ++a)
                for (int b = a + 3; b < 15; b += 3)
                {
                    float zn = min(z[a], z[b]);
                    bool upright = !sky[a] && !sky[b] && abs(z[a] - z[b]) < 0.035 * zn;
                    if (upright && zn < bestZ) { bestZ = zn; bestS = (z[a] <= z[b]) ? s[a] : s[b]; }
                }
            if (bestZ < 1e29) { s0 = bestS; H = 2.0 * uTanHalfFov * max(bestZ, 0.5); }
            else FocusBand(s0, H);
        }

        float Shape(float signedDist)   // in screen heights; < 0 = the "top" side
        {
            float a = smoothstep(uFocusWidth, uFocusWidth + max(uFalloff, 1e-3), abs(signedDist));
            return a * (signedDist < 0.0 ? uTop : uBottom);
        }
        float DepthAmount(vec2 pic, float s0, float invGH) { return Shape(-(dot(ViewPos(pic), uPlaneN) - s0) * invGH); }

        vec3 Grade(vec3 c)
        {
            c = clamp((c - 0.5) * uContrast + 0.5, 0.0, 1.0);
            float l = dot(c, LUMA);
            return clamp(mix(vec3(l), c, uSaturation), 0.0, 1.0);
        }

        void main()
        {
            vec2 pic = uFlipY != 0 ? vec2(vUv.x, 1.0 - vUv.y) : vUv;
            vec3 orig = textureLod(uImage, vUv, 0.0).rgb;
            float s0 = 0.0, invGH = 0.0;
            float amt;
            if (uFocusMode == 1) { float H; FocusPlane(s0, H); invGH = 1.0 / max(uGroundGain * H, 1e-3); amt = DepthAmount(pic, s0, invGH); }
            else amt = Shape(pic.y - uFocusCentre);
            if (uDebug == 2) { fragColour = vec4(vec3(amt), 1.0); return; }   // the blur mask: black = sharp

            float scale = max(1.0, uViewport.y / 1080.0);
            float maxR = uBlurPx * scale;
            float radius = maxR * amt;
            vec3 c = orig;
            if (radius > 0.5)
            {
                vec3 sum = orig;
                float w = 1.0;
                for (int i = 1; i <= TAPS; ++i)
                {
                    float r = sqrt(float(i) / float(TAPS)) * radius;
                    float a = float(i) * GOLDEN;
                    vec2 off = vec2(cos(a), sin(a)) * r / uViewport;                // in picture units (y down)
                    vec2 tpic = pic + off;
                    float tw = (uFocusMode == 1) ? clamp(maxR * DepthAmount(tpic, s0, invGH) - r + 1.0, 0.0, 1.0) : 1.0;
                    sum += textureLod(uImage, TexOf(tpic), 0.0).rgb * tw;
                    w += tw;
                }
                c = sum / w;
            }
            vec3 outc = mix(orig, Grade(c), clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && pic.x < 0.5) outc = orig;
            fragColour = vec4(clamp(outc, 0.0, 1.0), 1.0);
        }
        """;
}
