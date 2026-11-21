// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.5.0: PZA Kuwahara, our own generalized Kuwahara (non-linear, edge-keeping smoothing: the "painted" look). Around each pixel, 8 soft
// sectors of a disc gather a weighted mean and variance; each sector's weight falls with its variance (Sharpness), so the calmest side of an
// edge wins and edges stay sharp while flat areas turn to strokes. Sector weights are polynomial windows on the rotated tap offset (no atan
// per tap); a Gaussian falloff with the distance. Radius is in pixels at 1080 rows; taps are strided above radius 4 so a big brush and 4K stay cheap.
// Radially symmetric, so Meitou's bottom-up rows need no flip. Runs on the finished picture (Dust: Kuwahara is a post effect too).
namespace PZA.Looks.Meitou;

public static class KuwaharaShader
{
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColour;
        uniform sampler2D uImage;
        uniform vec2 uViewport;
        uniform float uRadius, uSharpness, uStrength;
        uniform int uSplit;

        const int SECTORS = 8;
        const float ZETA = 0.33;
        // eta = (zeta + cos(pi / 8)) / sin(pi / 8)^2: a sector's window reaches 0 at its neighbours' centre lines
        const float ETA = (0.33 + 0.9238795) / (0.3826834 * 0.3826834);

        void main()
        {
            vec3 orig = textureLod(uImage, vUv, 0.0).rgb;
            float r = clamp(uRadius * uViewport.y / 1080.0, 1.0, 24.0);
            int reach = int(min(ceil(r), 4.0));            // taps per side; strided above 4 (0.5.0 tuning: the bigger Default brush, same cost)
            float stride = r / float(reach);
            vec2 px = 1.0 / uViewport;
            vec3 m[SECTORS]; vec3 s[SECTORS]; float w[SECTORS];
            for (int k = 0; k < SECTORS; ++k) { m[k] = vec3(0.0); s[k] = vec3(0.0); w[k] = 0.0; }
            for (int j = -reach; j <= reach; ++j)
            for (int i = -reach; i <= reach; ++i)
            {
                vec2 d = vec2(float(i), float(j)) / float(reach);   // -1..1
                float d2 = dot(d, d);
                if (d2 > 1.0) continue;
                vec3 c = textureLod(uImage, vUv + vec2(float(i), float(j)) * stride * px, 0.0).rgb;
                float g = exp(-3.0 * d2);
                for (int k = 0; k < SECTORS; ++k)
                {
                    float a = float(k) * 0.7853982;
                    float u = d.x * cos(a) + d.y * sin(a), v = -d.x * sin(a) + d.y * cos(a);
                    float h = max(0.0, (u + ZETA) - ETA * v * v);
                    float wt = h * h * g;
                    m[k] += wt * c; s[k] += wt * c * c; w[k] += wt;
                }
            }
            vec3 sum = vec3(0.0); float asum = 0.0;
            for (int k = 0; k < SECTORS; ++k)
            {
                if (w[k] <= 0.0) continue;
                vec3 mean = m[k] / w[k];
                vec3 var = abs(s[k] / w[k] - mean * mean);
                float sigma = sqrt(var.r + var.g + var.b);
                float alpha = 1.0 / (1.0 + pow(sigma * 20.0 + 1e-4, max(uSharpness, 0.0)));
                sum += alpha * mean; asum += alpha;
            }
            vec3 painted = asum > 0.0 ? sum / asum : orig;
            vec3 c = mix(orig, painted, clamp(uStrength, 0.0, 1.0));
            if (uSplit != 0 && vUv.x < 0.5) c = orig;
            fragColour = vec4(c, 1.0);
        }
        """;
}

/// <summary>The shader in C#, for the offline check and the fuzz harness: the same taps, windows and weights on a picture given as a function
/// (x, y) -> colour, pixel coordinates, clamped at the edges.</summary>
public static class KuwaharaMath
{
    const int Sectors = 8;
    const double Zeta = 0.33, Eta = (0.33 + 0.9238795) / (0.3826834 * 0.3826834);

    /// <summary>The window of sector k for a normalised offset d (|d| &lt;= 1), before the Gaussian.</summary>
    public static double Window(int k, double dx, double dy)
    {
        double a = k * Math.PI / 4;
        double u = dx * Math.Cos(a) + dy * Math.Sin(a), v = -dx * Math.Sin(a) + dy * Math.Cos(a);
        double h = Math.Max(0, u + Zeta - Eta * v * v);
        return h * h;
    }

    public static (double R, double G, double B) Filter(Func<int, int, (double R, double G, double B)> img, int x, int y, int w, int h,
        double radius, double sharpness, double strength)
    {
        (double R, double G, double B) At(int i, int j)
        {
            var c = img(Math.Clamp(i, 0, w - 1), Math.Clamp(j, 0, h - 1));
            return (Fin(c.R), Fin(c.G), Fin(c.B));
        }
        static double Fin(double v) => double.IsFinite(v) ? v : 0;
        var orig = At(x, y);
        double r = Math.Clamp(double.IsFinite(radius) ? radius * h / 1080.0 : 1, 1, 24);
        int reach = (int)Math.Min(Math.Ceiling(r), 4);
        double stride = r / reach;
        var m = new double[Sectors, 3]; var s = new double[Sectors, 3]; var wt = new double[Sectors];
        for (int j = -reach; j <= reach; j++)
            for (int i = -reach; i <= reach; i++)
            {
                double dx = (double)i / reach, dy = (double)j / reach, d2 = dx * dx + dy * dy;
                if (d2 > 1) continue;
                var c = At(x + (int)Math.Round(i * stride), y + (int)Math.Round(j * stride));
                double g = Math.Exp(-3 * d2);
                for (int k = 0; k < Sectors; k++)
                {
                    double ww = Window(k, dx, dy) * g;
                    m[k, 0] += ww * c.R; m[k, 1] += ww * c.G; m[k, 2] += ww * c.B;
                    s[k, 0] += ww * c.R * c.R; s[k, 1] += ww * c.G * c.G; s[k, 2] += ww * c.B * c.B;
                    wt[k] += ww;
                }
            }
        double sr = 0, sg = 0, sb = 0, asum = 0;
        double q = double.IsFinite(sharpness) ? Math.Max(sharpness, 0) : 8;
        for (int k = 0; k < Sectors; k++)
        {
            if (wt[k] <= 0) continue;
            double mr = m[k, 0] / wt[k], mg = m[k, 1] / wt[k], mb = m[k, 2] / wt[k];
            double var = Math.Abs(s[k, 0] / wt[k] - mr * mr) + Math.Abs(s[k, 1] / wt[k] - mg * mg) + Math.Abs(s[k, 2] / wt[k] - mb * mb);
            double alpha = 1 / (1 + Math.Pow(Math.Sqrt(var) * 20 + 1e-4, q));
            sr += alpha * mr; sg += alpha * mg; sb += alpha * mb; asum += alpha;
        }
        var painted = asum > 0 ? (sr / asum, sg / asum, sb / asum) : orig;
        double t = Math.Clamp(double.IsFinite(strength) ? strength : 1, 0, 1);
        return (orig.R + (painted.Item1 - orig.R) * t, orig.G + (painted.Item2 - orig.G) * t, orig.B + (painted.Item3 - orig.B) * t);
    }
}
