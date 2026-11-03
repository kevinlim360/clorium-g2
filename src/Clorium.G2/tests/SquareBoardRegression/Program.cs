using System;
using System.Linq;
using CameraControl.Core.Classes;

// Synthetic photos: a quadrilateral board (known corners) on a background, optional marks, noise and vignetting.
// Checks corner accuracy, X/Y scale, keystone sign, rotation, homography, text round trip and the refusal cases.
static class Program
{
    static int count;

    static void Check(string name, bool ok)
    {
        if (!ok) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
        count++;
    }

    // Fills the quad (TL, TR, BR, BL) with `board` over `background`; 2x2 supersampling for antialiased edges.
    static byte[] Photo(int w, int h, double[] q, byte board, byte background, bool marks = false, int seed = 1)
    {
        var rnd = new Random(seed);
        var px = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int inside = 0;
                for (int s = 0; s < 4; s++) if (Inside(q, x + 0.25 + 0.5 * (s & 1), y + 0.25 + 0.5 * (s >> 1))) inside++;
                double v = (background * (4 - inside) + board * inside) / 4.0;
                // vignetting and noise like a studio photo
                double dx = (x - w / 2.0) / w, dy = (y - h / 2.0) / h;
                v *= 1 - 0.25 * (dx * dx + dy * dy);
                v += rnd.Next(-6, 7);
                if (marks && inside == 4 && (x % 120) < 30 && (y % 90) < 18) v = background; // printed labels/logos on the board
                byte b = (byte)Math.Max(0, Math.Min(255, v));
                int i = (y * w + x) * 4;
                px[i] = b; px[i + 1] = b; px[i + 2] = b; px[i + 3] = 255;
            }
        }
        return px;
    }

    static bool Inside(double[] q, double x, double y)
    {
        bool r = false;
        for (int i = 0, j = 3; i < 4; j = i++)
        {
            double xi = q[i * 2], yi = q[i * 2 + 1], xj = q[j * 2], yj = q[j * 2 + 1];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) r = !r;
        }
        return r;
    }

    static double MaxCornerError(SquareBoardCalibration c, double[] q) =>
        Enumerable.Range(0, 4).Max(i => Math.Sqrt(Math.Pow(c.Corners[i * 2] - q[i * 2], 2) + Math.Pow(c.Corners[i * 2 + 1] - q[i * 2 + 1], 2)));

    static void Refused(string name, Func<SquareBoardCalibration> run, string fragment)
    {
        try
        {
            run();
            Check(name, false);
        }
        catch (SquareBoardCalibration.DetectionException ex)
        {
            Check(name + " (" + ex.Message + ")", ex.Message.Contains(fragment));
        }
    }

    static int Main()
    {
        try
        {
            const int W = 1200, H = 800;
            // 1) front-on square 500 px -> 1.0 mm/px
            double[] q1 = { 350, 150, 850, 150, 850, 650, 350, 650 };
            var c1 = SquareBoardCalibration.Detect(Photo(W, H, q1, 30, 215), W, H, 500);
            Check("front-on: corners within 1.5 px", MaxCornerError(c1, q1) < 1.5);
            Check("front-on: X/Y scale 1.000 mm/px within 0.5 %", Math.Abs(c1.ScaleX - 1) < 0.005 && Math.Abs(c1.ScaleY - 1) < 0.005);
            Check("front-on: no keystone, no rotation", Math.Abs(c1.KeystoneH) < 0.005 && Math.Abs(c1.KeystoneV) < 0.005 && Math.Abs(c1.RotationDeg) < 0.2);
            var tl = c1.ToBoard(q1[0], q1[1]); var br = c1.ToBoard(q1[4], q1[5]);
            Check("front-on: homography maps corners to (0,0) and (500,500)", Math.Abs(tl.x) < 2 && Math.Abs(tl.y) < 2 && Math.Abs(br.x - 500) < 2 && Math.Abs(br.y - 500) < 2);

            // 2) non-square pixels: 600 x 500 px -> X 0.833, Y 1.0 mm/px
            double[] q2 = { 300, 150, 900, 150, 900, 650, 300, 650 };
            var c2 = SquareBoardCalibration.Detect(Photo(W, H, q2, 30, 215), W, H, 500);
            Check("stretched: separate X/Y scale", Math.Abs(c2.ScaleX - 500.0 / 600) < 0.004 && Math.Abs(c2.ScaleY - 1) < 0.005);

            // 3) keystone: top edge longer (camera tilted), board still 500 mm
            double[] q3 = { 330, 160, 870, 160, 830, 640, 370, 640 };
            var c3plain = SquareBoardCalibration.Detect(Photo(W, H, q3, 25, 220), W, H, 500);
            Check("keystone: corners within 1.5 px (" + MaxCornerError(c3plain, q3).ToString("0.00") + ")", MaxCornerError(c3plain, q3) < 1.5);
            var c3 = SquareBoardCalibration.Detect(Photo(W, H, q3, 25, 220, marks: true), W, H, 500);
            Check("keystone with printed marks: corners within 2 px (" + MaxCornerError(c3, q3).ToString("0.00") + ")", MaxCornerError(c3, q3) < 2.0);
            Check("keystone: top wider -> positive horizontal keystone (~+16 %)", c3.KeystoneH > 0.14 && c3.KeystoneH < 0.18 && Math.Abs(c3.KeystoneV) < 0.01);
            var mid = c3.ToBoard((q3[0] + q3[2] + q3[4] + q3[6]) / 4, (q3[1] + q3[3] + q3[5] + q3[7]) / 4);
            var bl = c3.ToBoard(q3[6], q3[7]);
            Check("keystone: homography corrects the trapezoid (BL -> (0,500))", Math.Abs(bl.x) < 2 && Math.Abs(bl.y - 500) < 2 && mid.x > 200 && mid.x < 300);

            // 4) rotated 5 degrees
            double a = 5 * Math.PI / 180, cx = 600, cy = 400, s = 240;
            double[] q4 = new double[8];
            double[,] unit = { { -1, -1 }, { 1, -1 }, { 1, 1 }, { -1, 1 } };
            for (int i = 0; i < 4; i++)
            {
                q4[i * 2] = cx + s * (unit[i, 0] * Math.Cos(a) - unit[i, 1] * Math.Sin(a));
                q4[i * 2 + 1] = cy + s * (unit[i, 0] * Math.Sin(a) + unit[i, 1] * Math.Cos(a));
            }
            var c4 = SquareBoardCalibration.Detect(Photo(W, H, q4, 35, 210), W, H, 500);
            Check("rotated 5°: rotation measured", Math.Abs(c4.RotationDeg - 5) < 0.3);
            Check("rotated 5°: scale 500/480 mm/px within 0.5 %", Math.Abs(c4.Scale - 500.0 / 480) / (500.0 / 480) < 0.005);

            // 5) light board on a dark background
            var c5 = SquareBoardCalibration.Detect(Photo(W, H, q1, 230, 40), W, H, 500);
            Check("light board on dark background", MaxCornerError(c5, q1) < 1.5);

            // 6) board size other than 500 mm
            var c6 = SquareBoardCalibration.Detect(Photo(W, H, q1, 30, 215), W, H, 300);
            Check("300 mm board -> 0.6 mm/px", Math.Abs(c6.Scale - 0.6) < 0.004);

            // 7) text round trip and summary
            var back = SquareBoardCalibration.Parse(c3.Format());
            Check("Format/Parse round trip", back != null && Math.Abs(back.ScaleX - c3.ScaleX) < 1e-7 && Math.Abs(back.KeystoneH - c3.KeystoneH) < 1e-7 && back.Homography.Zip(c3.Homography, (p, r) => Math.Abs(p - r) < 1e-6 * Math.Max(1, Math.Abs(r))).All(x => x));
            Check("format is one keyword starting with clorium-scale:v1", c3.Format().StartsWith("clorium-scale:v1;board=500;sx=") && !c3.Format().Contains(" "));
            Check("Describe: empty and garbage -> not calibrated", SquareBoardCalibration.Describe("").StartsWith("아직") && SquareBoardCalibration.Describe("clorium-scale:v1;sx=x").StartsWith("아직") && SquareBoardCalibration.Parse("0.2222") == null);
            Check("Describe: values in Korean", SquareBoardCalibration.Describe(c3.Format()).Contains("가로") && SquareBoardCalibration.Describe(c3.Format()).Contains("사다리꼴"));

            // 8) refusals
            double[] edge = { 0, 150, 700, 150, 700, 650, 0, 650 }; // covers the centre, cut by the left border
            Refused("board touching the photo edge is refused", () => SquareBoardCalibration.Detect(Photo(W, H, edge, 30, 215), W, H, 500), "가장자리");
            Refused("no board (flat photo) is refused", () => SquareBoardCalibration.Detect(Photo(W, H, new double[] { 599, 399, 601, 399, 601, 401, 599, 401 }, 215, 215), W, H, 500), "");
            double[] rect = { 200, 250, 1000, 250, 1000, 550, 200, 550 };
            Refused("long rectangle is not taken for the square board", () => SquareBoardCalibration.Detect(Photo(W, H, rect, 30, 215), W, H, 500), "정사각형");
            Refused("invalid board size is refused", () => SquareBoardCalibration.Detect(Photo(W, H, q1, 30, 215), W, H, 0), "크기");

            // 9) full-size photo performance (6000 x 4000)
            int bw = 6000, bh = 4000;
            double[] big = { 1750, 750, 4250, 750, 4250, 3250, 1750, 3250 };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cb = SquareBoardCalibration.Detect(Photo(bw, bh, big, 30, 215), bw, bh, 500);
            sw.Stop();
            Check("6000x4000 photo: 0.2 mm/px, " + sw.ElapsedMilliseconds + " ms", Math.Abs(cb.Scale - 0.2) < 0.001 && sw.ElapsedMilliseconds < 20000);

            Console.WriteLine("SQUARE_BOARD_PASSED=" + count);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }
}
