using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CameraControl.Core.Classes;

// 오토사이징 캘리브레이션: a square board of known size (default 500 mm) photographed near the image centre.
// The board is separated from the background by an Otsu threshold and a flood fill from the centre (so it may be dark on
// light or light on dark, and printed marks on it are ignored), its outer edges are fitted with lines and intersected
// into four corners. From them: X/Y scale (mm/px), keystone (top/bottom and left/right edge ratios), rotation and the
// perspective homography image px -> board mm. The result is kept as one text value (Format/Parse) that is stored in
// Settings.SquareCalibration and written to the measurement copies (치수측정\*_M.jpeg) only.
public sealed class SquareBoardCalibration
{
    public const string Prefix = "clorium-scale:v1";
    // JPEG EXIF XPKeywords (Windows 태그), UTF-16 with a terminating NUL: where the measurement copy keeps Format().
    public const string MetadataQuery = "/app1/ifd/{ushort=40094}";

    public double BoardMm { get; private set; }
    public double ScaleX { get; private set; }
    public double ScaleY { get; private set; }
    public double Scale => (ScaleX + ScaleY) / 2.0;
    // (top - bottom) / mean width, (left - right) / mean height: 0 = no perspective, positive = top / left edge longer.
    public double KeystoneH { get; private set; }
    public double KeystoneV { get; private set; }
    public double RotationDeg { get; private set; }
    // Row-major 3x3 with h33 = 1: board_mm = H * (x, y, 1) / w.
    public double[] Homography { get; private set; }
    // TL, TR, BR, BL in image pixels.
    public double[] Corners { get; private set; }
    public DateTime Measured { get; private set; }

    public sealed class DetectionException : Exception
    {
        public DetectionException(string message) : base(message) { }
    }

    // bgra: 32-bit BGRA pixels (stride = width * 4).
    public static SquareBoardCalibration Detect(byte[] bgra, int width, int height, double boardMm)
    {
        if (bgra == null || width < 64 || height < 64 || bgra.Length < width * height * 4) throw new DetectionException("사진을 읽지 못했습니다.");
        if (!(boardMm > 0)) throw new DetectionException("정사각형 판 크기를 확인해 주세요.");
        byte[] gray = new byte[width * height];
        int[] hist = new int[256];
        for (int i = 0, p = 0; i < gray.Length; i++, p += 4)
        {
            byte g = (byte)((bgra[p] * 29 + bgra[p + 1] * 150 + bgra[p + 2] * 77) >> 8);
            gray[i] = g;
            hist[g]++;
        }
        int threshold = Otsu(hist, gray.Length);
        // Board class = majority class of the central window (the operator centres the board).
        int cx0 = width * 45 / 100, cx1 = width * 55 / 100, cy0 = height * 45 / 100, cy1 = height * 55 / 100;
        int dark = 0, total = 0;
        for (int y = cy0; y < cy1; y++) for (int x = cx0; x < cx1; x++) { total++; if (gray[y * width + x] <= threshold) dark++; }
        bool boardDark = dark * 2 >= total;
        bool[] mask = new bool[width * height];
        var stack = new Stack<int>();
        for (int y = cy0; y < cy1; y += 4) for (int x = cx0; x < cx1; x += 4)
        {
            int i = y * width + x;
            if (!mask[i] && (gray[i] <= threshold) == boardDark) { mask[i] = true; stack.Push(i); }
        }
        long area = 0;
        while (stack.Count > 0)
        {
            int i = stack.Pop();
            area++;
            int x = i % width, y = i / width;
            if (x > 0) Visit(i - 1); if (x < width - 1) Visit(i + 1);
            if (y > 0) Visit(i - width); if (y < height - 1) Visit(i + width);
        }
        void Visit(int j) { if (!mask[j] && (gray[j] <= threshold) == boardDark) { mask[j] = true; stack.Push(j); } }
        if (area < (long)width * height / 50) throw new DetectionException("정사각형 판을 찾지 못했습니다. 판을 화면 가운데에 크게 놓고 배경과 색이 구분되게 촬영해 주세요.");

        // Outer silhouette: first/last board pixel of every row and column.
        var left = new List<(double x, double y)>(); var right = new List<(double x, double y)>();
        var top = new List<(double x, double y)>(); var bottom = new List<(double x, double y)>();
        for (int y = 0; y < height; y++)
        {
            int row = y * width, a = -1, b = -1;
            for (int x = 0; x < width; x++) if (mask[row + x]) { a = x; break; }
            if (a < 0) continue;
            for (int x = width - 1; x >= a; x--) if (mask[row + x]) { b = x; break; }
            left.Add((a, y)); right.Add((b + 1, y));
        }
        for (int x = 0; x < width; x++)
        {
            int a = -1, b = -1;
            for (int y = 0; y < height; y++) if (mask[y * width + x]) { a = y; break; }
            if (a < 0) continue;
            for (int y = height - 1; y >= a; y--) if (mask[y * width + x]) { b = y; break; }
            top.Add((x, a)); bottom.Add((x, b + 1));
        }
        if (left.Min(p => p.x) <= 1 || right.Max(p => p.x) >= width - 1 || top.Min(p => p.y) <= 1 || bottom.Max(p => p.y) >= height - 1)
            throw new DetectionException("정사각형 판이 사진 가장자리에 닿았습니다. 판 전체가 여백과 함께 보이도록 촬영해 주세요.");

        // Rough corners from the silhouette extremes, then robust edge lines between them.
        var all = left.Concat(right).Concat(top).Concat(bottom).ToList();
        var tl = all.OrderBy(p => p.x + p.y).First(); var br = all.OrderByDescending(p => p.x + p.y).First();
        var tr = all.OrderByDescending(p => p.x - p.y).First(); var bl = all.OrderBy(p => p.x - p.y).First();
        var topLine = FitY(top, Math.Min(tl.x, tr.x), Math.Max(tl.x, tr.x), -1);
        var bottomLine = FitY(bottom, Math.Min(bl.x, br.x), Math.Max(bl.x, br.x), 1);
        var leftLine = FitX(left, Math.Min(tl.y, bl.y), Math.Max(tl.y, bl.y), -1);
        var rightLine = FitX(right, Math.Min(tr.y, br.y), Math.Max(tr.y, br.y), 1);
        var c0 = Intersect(topLine, leftLine); var c1 = Intersect(topLine, rightLine);
        var c2 = Intersect(bottomLine, rightLine); var c3 = Intersect(bottomLine, leftLine);

        double wTop = Dist(c0, c1), wBottom = Dist(c3, c2), hLeft = Dist(c0, c3), hRight = Dist(c1, c2);
        double w = (wTop + wBottom) / 2, h = (hLeft + hRight) / 2;
        if (w < 32 || h < 32 || w / h > 1.35 || h / w > 1.35)
            throw new DetectionException("찾은 판이 정사각형으로 보이지 않습니다. 판을 카메라와 마주 보게 놓고 다시 촬영해 주세요.");
        var result = new SquareBoardCalibration
        {
            BoardMm = boardMm,
            ScaleX = boardMm / w,
            ScaleY = boardMm / h,
            KeystoneH = (wTop - wBottom) / w,
            KeystoneV = (hLeft - hRight) / h,
            RotationDeg = Math.Atan2(c1.y - c0.y, c1.x - c0.x) * 180.0 / Math.PI,
            Corners = new[] { c0.x, c0.y, c1.x, c1.y, c2.x, c2.y, c3.x, c3.y },
            Measured = DateTime.Now
        };
        result.Homography = SolveHomography(result.Corners, boardMm);
        return result;
    }

    static int Otsu(int[] hist, int count)
    {
        double sum = 0; for (int i = 0; i < 256; i++) sum += i * (double)hist[i];
        double sumB = 0, best = -1; long wB = 0; int t = 127;
        for (int i = 0; i < 256; i++)
        {
            wB += hist[i]; if (wB == 0) continue;
            long wF = count - wB; if (wF == 0) break;
            sumB += i * (double)hist[i];
            double mB = sumB / wB, mF = (sum - sumB) / wF, between = (double)wB * wF * (mB - mF) * (mB - mF);
            if (between > best) { best = between; t = i; }
        }
        return t;
    }

    // y = a x + b over the middle 70 % of the span (away from rounded/shadowed corners). outward: +1 when the outside of
    // the board is at larger v (bottom/right edge), -1 for the top/left edge.
    static (double a, double b, bool vertical) FitY(List<(double x, double y)> pts, double from, double to, int outward)
    {
        double m = (to - from) * 0.15;
        var use = pts.Where(p => p.x >= from + m && p.x <= to - m).ToList();
        var f = Refit(use.Select(p => (p.x, p.y)).ToList(), outward);
        return (f.a, f.b, false);
    }

    // x = a y + b (left/right edges).
    static (double a, double b, bool vertical) FitX(List<(double x, double y)> pts, double from, double to, int outward)
    {
        double m = (to - from) * 0.15;
        var use = pts.Where(p => p.y >= from + m && p.y <= to - m).ToList();
        var f = Refit(use.Select(p => (p.y, p.x)).ToList(), outward);
        return (f.a, f.b, true);
    }

    static (double a, double b) Refit(List<(double u, double v)> pts, int outward)
    {
        if (pts.Count < 8) throw new DetectionException("판의 가장자리를 충분히 찾지 못했습니다.");
        // Labels, holes or shadows that reach the edge only ever cut into the board (inward notches), while the true edge
        // is the outer envelope: drop points lying inward of the fitted line by more than antialias/noise and refit.
        var line = Least(pts);
        var use = pts;
        for (int pass = 0; pass < 8; pass++)
        {
            var fit = line;
            var kept = pts.Where(p => outward * (p.v - (fit.a * p.u + fit.b)) >= -1.0).ToList();
            if (kept.Count < 8 || kept.Count == use.Count) break;
            use = kept;
            line = Least(use);
        }
        return line;
    }

    static (double a, double b) Least(List<(double u, double v)> pts)
    {
        double n = pts.Count, su = 0, sv = 0, suu = 0, suv = 0;
        foreach (var p in pts) { su += p.u; sv += p.v; suu += p.u * p.u; suv += p.u * p.v; }
        double d = n * suu - su * su;
        if (Math.Abs(d) < 1e-9) throw new DetectionException("판의 가장자리를 계산하지 못했습니다.");
        double a = (n * suv - su * sv) / d;
        return (a, (sv - a * su) / n);
    }

    // horizontal: y = a x + b; vertical: x = a y + b.
    static (double x, double y) Intersect((double a, double b, bool vertical) h, (double a, double b, bool vertical) v)
    {
        // y = ha x + hb, x = va y + vb  ->  y = ha (va y + vb) + hb
        double den = 1 - h.a * v.a;
        if (Math.Abs(den) < 1e-9) throw new DetectionException("판의 모서리를 계산하지 못했습니다.");
        double y = (h.a * v.b + h.b) / den;
        return (v.a * y + v.b, y);
    }

    static double Dist((double x, double y) p, (double x, double y) q) => Math.Sqrt((p.x - q.x) * (p.x - q.x) + (p.y - q.y) * (p.y - q.y));

    // Image corners (TL, TR, BR, BL) -> (0,0) (S,0) (S,S) (0,S).
    static double[] SolveHomography(double[] c, double s)
    {
        double[] u = { 0, s, s, 0 }, v = { 0, 0, s, s };
        double[,] m = new double[8, 9];
        for (int i = 0; i < 4; i++)
        {
            double x = c[i * 2], y = c[i * 2 + 1];
            int r = i * 2;
            m[r, 0] = x; m[r, 1] = y; m[r, 2] = 1; m[r, 6] = -u[i] * x; m[r, 7] = -u[i] * y; m[r, 8] = u[i];
            m[r + 1, 3] = x; m[r + 1, 4] = y; m[r + 1, 5] = 1; m[r + 1, 6] = -v[i] * x; m[r + 1, 7] = -v[i] * y; m[r + 1, 8] = v[i];
        }
        for (int col = 0; col < 8; col++)
        {
            int piv = col;
            for (int r = col + 1; r < 8; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[piv, col])) piv = r;
            if (Math.Abs(m[piv, col]) < 1e-12) throw new DetectionException("원근 보정값을 계산하지 못했습니다.");
            for (int k = 0; k < 9; k++) { double tmp = m[col, k]; m[col, k] = m[piv, k]; m[piv, k] = tmp; }
            for (int r = 0; r < 8; r++)
            {
                if (r == col) continue;
                double f = m[r, col] / m[col, col];
                for (int k = col; k < 9; k++) m[r, k] -= f * m[col, k];
            }
        }
        var hh = new double[9];
        for (int i = 0; i < 8; i++) hh[i] = m[i, 8] / m[i, i];
        hh[8] = 1;
        return hh;
    }

    // Image px -> board mm through the homography.
    public (double x, double y) ToBoard(double x, double y)
    {
        double[] h = Homography;
        double w = h[6] * x + h[7] * y + h[8];
        return ((h[0] * x + h[1] * y + h[2]) / w, (h[3] * x + h[4] * y + h[5]) / w);
    }

    static string N(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);

    public string Format() =>
        Prefix + ";board=" + N(BoardMm) + ";sx=" + N(ScaleX) + ";sy=" + N(ScaleY) + ";kh=" + N(KeystoneH) + ";kv=" + N(KeystoneV)
        + ";rot=" + N(RotationDeg) + ";h=" + string.Join(",", Homography.Select(N)) + ";corners=" + string.Join(",", Corners.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))
        + ";at=" + Measured.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    public static SquareBoardCalibration Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith(Prefix + ";", StringComparison.Ordinal)) return null;
        try
        {
            var kv = text.Substring(Prefix.Length + 1).Split(';').Select(p => p.Split(new[] { '=' }, 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
            double D(string k) => double.Parse(kv[k], CultureInfo.InvariantCulture);
            var r = new SquareBoardCalibration
            {
                BoardMm = D("board"), ScaleX = D("sx"), ScaleY = D("sy"), KeystoneH = D("kh"), KeystoneV = D("kv"), RotationDeg = D("rot"),
                Homography = kv["h"].Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray(),
                Corners = kv.TryGetValue("corners", out string c) ? c.Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray() : new double[0],
                Measured = kv.TryGetValue("at", out string at) ? DateTime.ParseExact(at, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) : DateTime.MinValue
            };
            return r.Homography.Length == 9 && r.ScaleX > 0 && r.ScaleY > 0 ? r : null;
        }
        catch (Exception ex) when (ex is FormatException || ex is KeyNotFoundException || ex is OverflowException || ex is IndexOutOfRangeException)
        {
            return null;
        }
    }

    public string Summary() =>
        "가로 " + ScaleX.ToString("0.00000", CultureInfo.InvariantCulture) + " · 세로 " + ScaleY.ToString("0.00000", CultureInfo.InvariantCulture) + " mm/px"
        + " · 사다리꼴 가로 " + (KeystoneH * 100).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "% / 세로 " + (KeystoneV * 100).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%"
        + " · 회전 " + RotationDeg.ToString("0.00", CultureInfo.InvariantCulture) + "°";

    public static string Describe(string text)
    {
        var r = Parse(text);
        return r == null ? "아직 정사각형 판으로 보정하지 않았습니다." : r.Summary() + (r.Measured > DateTime.MinValue ? " (" + r.Measured.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ")" : "");
    }
}
