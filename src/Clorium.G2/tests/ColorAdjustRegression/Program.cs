using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CameraControl.Classes;
using ImageMagick;

// G2 색보정 엔진 회귀 검사 (실제 Magick.NET 14.12 / ImageMagick Q8 네이티브 사용).
// 실행: dotnet run --project tests/ColorAdjustRegression/ColorAdjustRegression.csproj -c Release
class Program
{
    static int passed;

    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("FAIL " + message);
    }

    static void Pass(string message)
    {
        passed++;
        Console.WriteLine("PASS " + message);
    }

    static byte[] Rgb(IMagickImage<byte> image) => image.ToByteArray(MagickFormat.Rgb);

    static string Sha(string path)
    {
        using var s = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(s));
    }

    // 600×400 test chart: horizontal grey ramp (top), hue sweep (middle), colour patches (bottom)
    static MagickImage Chart(uint w = 600, uint h = 400)
    {
        var px = new byte[w * h * 3];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (int)((y * w + x) * 3);
            byte r, g, b;
            if (y < h / 3) { r = g = b = (byte)(x * 255 / (w - 1)); }
            else if (y < 2 * h / 3)
            {
                double hue = 6.0 * x / w, f = hue - Math.Floor(hue);
                int k = (int)Math.Floor(hue) % 6;
                double v = 0.85, s = 0.7, p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
                double rr = new[] { v, q, p, p, t, v }[k], gg = new[] { t, v, v, q, p, p }[k], bb = new[] { p, p, t, v, v, q }[k];
                r = (byte)(rr * 255); g = (byte)(gg * 255); b = (byte)(bb * 255);
            }
            else { r = (byte)(40 + (x * 7) % 200); g = (byte)(30 + (y * 11) % 210); b = (byte)(20 + ((x + y) * 5) % 230); }
            px[i] = r; px[i + 1] = g; px[i + 2] = b;
        }
        var img = new MagickImage();
        img.ReadPixels(px, new PixelReadSettings(w, h, StorageType.Char, PixelMapping.RGB));
        return img;
    }

    static double Luma(byte[] rgb, int i) => G2ImageAdjust.LumaR * rgb[i] + G2ImageAdjust.LumaG * rgb[i + 1] + G2ImageAdjust.LumaB * rgb[i + 2];

    static void Main()
    {
        Console.WriteLine("engine: " + G2ImageAdjust.EngineVersion);
        string dir = Path.Combine(Path.GetTempPath(), "g2-color-adjust-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // 1. identity LUT and identity pipeline
            var id = new G2AdjustSettings();
            byte[] lut = G2ImageAdjust.BuildLut(id);
            Check(Enumerable.Range(0, 768).All(k => lut[k] == k / 3), "default settings LUT must be identity");
            Check(id.IsIdentity, "default settings must be identity");
            Pass("default settings are an exact identity (LUT and flags)");

            // 2. ImageMagick CLUT applies our LUT exactly per channel (every pixel), alpha untouched
            using (MagickImage src = Chart())
            {
                var s = new G2AdjustSettings { Exposure = 0.7, Brightness = 15, Contrast = 30, Temperature = 25, Tint = -10 };
                s.Red.Black = 12; s.Blue.Gamma = 1.3; s.Master.White = 240;
                byte[] expectLut = G2ImageAdjust.BuildLut(s);
                byte[] before = Rgb(src);
                using IMagickImage<byte> work = src.Clone();
                G2ImageAdjust.Apply(work, s);
                byte[] after = Rgb(work);
                int worst = 0;
                for (int i = 0; i < before.Length; i++)
                    worst = Math.Max(worst, Math.Abs(after[i] - expectLut[before[i] * 3 + i % 3]));
                Check(worst <= 1, "CLUT result differs from computed LUT by " + worst);
                Check(!work.HasAlpha, "adjustment must not add an alpha channel");
                byte[] shown = G2ImageAdjust.ToBgra(work);
                bool opaque = true; long sum = 0;
                for (int i = 0; i < shown.Length; i += 4) { opaque &= shown[i + 3] == 255; sum += shown[i] + shown[i + 1] + shown[i + 2]; }
                Check(opaque && sum > 0, "preview BGRA must be opaque and non-empty");
                using (IMagickImage<byte> sat = src.Clone())
                {
                    G2ImageAdjust.Apply(sat, new G2AdjustSettings { Exposure = 0.5, Saturation = 25, Temperature = 35 });
                    Check(!sat.HasAlpha, "tone+saturation must not add alpha");
                    byte[] b2 = G2ImageAdjust.ToBgra(sat);
                    long s2 = 0; for (int i = 0; i < b2.Length; i += 4) s2 += b2[i] + b2[i + 1] + b2[i + 2];
                    Check(s2 > 0, "tone+saturation preview empty");
                }
                Pass("ImageMagick CLUT matches the computed per-channel curve on every pixel (max diff " + worst + ")");
            }

            // 2b. same path as the window: JPEG on disk → Load → preview proxy → Apply → BGRA, and PNG save stays opaque
            {
                string dj = Path.Combine(dir, "diag.jpg");
                using (MagickImage c = Chart(1200, 800)) { c.Quality = 95; c.Write(dj); }
                using (MagickImage orig = G2ImageAdjust.Load(dj))
                using (IMagickImage<byte> prev = G2ImageAdjust.CreatePreview(orig, 600))
                {
                    Console.WriteLine("DIAG load: alpha=" + orig.HasAlpha + " type=" + orig.ColorType + " space=" + orig.ColorSpace + " channels=" + orig.ChannelCount);
                    G2ImageAdjust.ToBgra(prev); // the window exports the proxy once before adjusting clones of it
                    Console.WriteLine("DIAG preview: alpha=" + prev.HasAlpha + " type=" + prev.ColorType + " channels=" + prev.ChannelCount);
                    Check(!prev.HasAlpha, "exporting the preview must not add alpha");
                    var steps = new[] {
                        new G2AdjustSettings { Exposure = 0.5 },
                        new G2AdjustSettings { Saturation = 25 },
                        new G2AdjustSettings { Exposure = 0.5, Saturation = 25, Temperature = 35 } };
                    foreach (var st in steps)
                    {
                        using IMagickImage<byte> w2 = prev.Clone();
                        G2ImageAdjust.Apply(w2, st);
                        Check(!w2.HasAlpha, "Apply added alpha: " + st.Describe());
                        byte[] first = G2ImageAdjust.ToBgra(w2);
                        byte[] second = G2ImageAdjust.ToBgra(w2);
                        long low2 = 0; for (int i = 3; i < first.Length; i += 4) if (first[i] != 255 || second[i] != 255) low2++;
                        Check(first.AsSpan().SequenceEqual(second) && !w2.HasAlpha, "BGRA export must not change the image");
                        Console.WriteLine("DIAG " + st.Describe() + ": alpha=" + w2.HasAlpha + " channels=" + w2.ChannelCount + " nonOpaque=" + low2);
                        Check(low2 == 0, "preview BGRA not opaque after " + st.Describe());
                    }
                }
                string dp = Path.Combine(dir, "diag_cc.png");
                G2ImageAdjust.SaveAdjusted(dj, dp, new G2AdjustSettings { Exposure = 0.5, Saturation = 25, Temperature = 35 }, false);
                using (var o = new MagickImage(dp))
                {
                    Console.WriteLine("DIAG saved png: alpha=" + o.HasAlpha + " channels=" + o.ChannelCount);
                    if (o.HasAlpha)
                    {
                        byte[] a = o.ToByteArray(MagickFormat.Bgra); long low = 0; for (int i = 3; i < a.Length; i += 4) if (a[i] != 255) low++;
                        Check(low == 0, "saved PNG became transparent: " + low);
                    }
                }
                Pass("JPEG → preview → adjust path stays opaque (preview and saved PNG)");
            }

            // 3. exposure: +1 EV doubles linear light at mid grey; -1 EV halves it
            {
                var plus = G2ImageAdjust.BuildLut(new G2AdjustSettings { Exposure = 1 });
                var minus = G2ImageAdjust.BuildLut(new G2AdjustSettings { Exposure = -1 });
                double mid = G2ImageAdjust.SrgbToLinear(118 / 255.0);
                double up = G2ImageAdjust.SrgbToLinear(plus[118 * 3] / 255.0), dn = G2ImageAdjust.SrgbToLinear(minus[118 * 3] / 255.0);
                Check(Math.Abs(up / mid - 2) < 0.03 && Math.Abs(dn / mid - 0.5) < 0.02, "exposure ratio up=" + up / mid + " down=" + dn / mid);
                Check(plus[0] == 0 && minus[255 * 3] < 255 && plus[255 * 3] == 255, "exposure end points");
                Pass("exposure is linear-light: +1EV ×" + (up / mid).ToString("0.000") + ", −1EV ×" + (dn / mid).ToString("0.000"));
            }

            // 4. brightness / contrast keep black, white (and contrast keeps mid grey)
            {
                var b = G2ImageAdjust.BuildLut(new G2AdjustSettings { Brightness = 60 });
                var c = G2ImageAdjust.BuildLut(new G2AdjustSettings { Contrast = 60 });
                var c2 = G2ImageAdjust.BuildLut(new G2AdjustSettings { Contrast = -60 });
                Check(b[0] == 0 && b[765] == 255 && b[128 * 3] > 150, "brightness curve");
                Check(c[0] == 0 && c[765] == 255 && Math.Abs(c[128 * 3] - 128) <= 1 && c[64 * 3] < 64 && c[192 * 3] > 192, "contrast+ curve");
                Check(Math.Abs(c2[128 * 3] - 128) <= 1 && c2[0] > 0 && c2[765] < 255, "contrast- curve");
                bool mono = true;
                for (int i = 1; i < 256; i++) mono &= b[i * 3] >= b[(i - 1) * 3] && c[i * 3] >= c[(i - 1) * 3] && c2[i * 3] >= c2[(i - 1) * 3];
                Check(mono, "tone curves must be monotonic");
                Pass("brightness/contrast curves keep end points, mid grey and are monotonic");
            }

            // 5. saturation -100 = greyscale, +60 keeps Rec.709 luma
            using (MagickImage src = Chart())
            {
                byte[] before = Rgb(src);
                using IMagickImage<byte> grey = src.Clone();
                G2ImageAdjust.Apply(grey, new G2AdjustSettings { Saturation = -100 });
                byte[] g = Rgb(grey);
                int spread = 0; double lumaErr = 0;
                for (int i = 0; i < g.Length; i += 3)
                {
                    spread = Math.Max(spread, Math.Max(Math.Abs(g[i] - g[i + 1]), Math.Abs(g[i] - g[i + 2])));
                    lumaErr = Math.Max(lumaErr, Math.Abs(Luma(g, i) - Luma(before, i)));
                }
                Check(spread <= 1 && lumaErr <= 1.5, "saturation -100 spread " + spread + " luma " + lumaErr);
                using IMagickImage<byte> vivid = src.Clone();
                G2ImageAdjust.Apply(vivid, new G2AdjustSettings { Saturation = 60 });
                byte[] v = Rgb(vivid);
                double meanErr = 0; int n = 0;
                for (int i = 0; i < v.Length; i += 3)
                {
                    bool clipped = v[i] == 0 || v[i] == 255 || v[i + 1] == 0 || v[i + 1] == 255 || v[i + 2] == 0 || v[i + 2] == 255;
                    if (!clipped) { meanErr += Math.Abs(Luma(v, i) - Luma(before, i)); n++; }
                }
                Check(n > 1000 && meanErr / n < 1.0, "saturation +60 luma drift " + meanErr / n);
                Pass("saturation: −100 is neutral grey (spread " + spread + "), +60 keeps luma (mean drift " + (meanErr / n).ToString("0.00") + ")");
            }

            // 6. per-channel levels only touch their channel
            using (MagickImage src = Chart())
            {
                byte[] before = Rgb(src);
                using IMagickImage<byte> work = src.Clone();
                var s = new G2AdjustSettings();
                s.Red.Black = 60; s.Red.White = 200;
                G2ImageAdjust.Apply(work, s);
                byte[] after = Rgb(work);
                bool othersSame = true, redOk = true;
                for (int i = 0; i < after.Length; i += 3)
                {
                    othersSame &= after[i + 1] == before[i + 1] && after[i + 2] == before[i + 2];
                    if (before[i] <= 60) redOk &= after[i] == 0;
                    if (before[i] >= 200) redOk &= after[i] == 255;
                }
                Check(othersSame && redOk, "red levels leaked or wrong");
                Pass("R-channel levels change only red (G/B bit-identical)");
            }

            // 7. temperature: warmer raises R, lowers B, keeps luma of neutral grey
            {
                var warm = G2ImageAdjust.BuildLut(new G2AdjustSettings { Temperature = 60 });
                int i = 128 * 3;
                double lumaBefore = 128, lumaAfter = G2ImageAdjust.LumaR * warm[i] + G2ImageAdjust.LumaG * warm[i + 1] + G2ImageAdjust.LumaB * warm[i + 2];
                Check(warm[i] > 128 && warm[i + 2] < 128 && Math.Abs(lumaAfter - lumaBefore) < 4, "temperature on grey: " + warm[i] + "," + warm[i + 1] + "," + warm[i + 2]);
                var tint = G2ImageAdjust.BuildLut(new G2AdjustSettings { Tint = 60 });
                Check(tint[i + 1] < 128 && tint[i] > tint[i + 1] && tint[i + 2] > tint[i + 1], "tint+ must be magenta");
                Pass("temperature/tint move grey toward amber/magenta while keeping brightness");
            }

            // 8. save: original untouched, new file, size kept, ICC kept, identity PNG bit-exact
            string png = Path.Combine(dir, "Photo001_F.png");
            string jpg = Path.Combine(dir, "Photo001_F.jpg");
            using (MagickImage src = Chart())
            {
                src.SetProfile(ColorProfiles.AdobeRGB1998);
                src.Write(png);
                src.Quality = 96;
                src.Write(jpg);
            }
            string pngHash = Sha(png), jpgHash = Sha(jpg);
            string outPng = G2ImageAdjust.NextAdjustedName(png);
            Check(Path.GetFileName(outPng) == "Photo001_F_cc.png", "name " + outPng);
            G2ImageAdjust.SaveAdjusted(png, outPng, new G2AdjustSettings(), false);
            using (var a = new MagickImage(png)) using (var b = new MagickImage(outPng))
                Check(Rgb(a).AsSpan().SequenceEqual(Rgb(b)), "identity PNG save must be bit-exact");
            string outPng2 = G2ImageAdjust.NextAdjustedName(png);
            Check(Path.GetFileName(outPng2) == "Photo001_F_cc (1).png", "collision name " + outPng2);
            var adj = new G2AdjustSettings { Exposure = 0.5, Saturation = 20, Temperature = 10 };
            string outJpg = G2ImageAdjust.NextAdjustedName(jpg);
            G2ImageAdjust.SaveAdjusted(jpg, outJpg, adj, false);
            using (var o = new MagickImage(outJpg))
            {
                Check(o.Width == 600 && o.Height == 400, "size changed");
                IColorProfile icc = o.GetColorProfile();
                Check(icc != null && (icc.Description ?? "").IndexOf("Adobe", StringComparison.OrdinalIgnoreCase) >= 0, "ICC profile lost: " + icc?.Description);
                Check(o.Quality >= 90, "jpeg quality " + o.Quality);
            }
            Check(Sha(png) == pngHash && Sha(jpg) == jpgHash, "original file was modified");
            Check(!Directory.GetFiles(dir, ".~cc-*").Any(), "temporary file left behind");
            Pass("save writes a new _cc file (collision → (1)), keeps size/ICC/quality, leaves originals byte-identical, identity PNG bit-exact");

            // 9. refuse to overwrite the original or an existing file
            bool refused = false;
            try { G2ImageAdjust.SaveAdjusted(jpg, jpg, adj, true); } catch (InvalidOperationException) { refused = true; }
            Check(refused && Sha(jpg) == jpgHash, "must refuse writing over the source");
            refused = false;
            try { G2ImageAdjust.SaveAdjusted(jpg, outJpg, adj, false); } catch (IOException) { refused = true; }
            Check(refused, "must refuse existing destination without overwrite");
            refused = false;
            try { G2ImageAdjust.Load(Path.Combine(dir, "x.cr3")); } catch (FileNotFoundException) { refused = true; } catch (NotSupportedException) { refused = true; }
            Check(refused, "RAW must be refused");
            Pass("refuses overwriting the original/existing files and RAW input");

            // 10. greyscale source with colour balance becomes colour; preview proxy keeps aspect
            string greyPath = Path.Combine(dir, "grey.png");
            using (MagickImage src = Chart()) { src.ColorType = ColorType.Grayscale; src.Write(greyPath); }
            string greyOut = Path.Combine(dir, "grey_cc.png");
            G2ImageAdjust.SaveAdjusted(greyPath, greyOut, new G2AdjustSettings { Temperature = 50 }, false);
            using (var o = new MagickImage(greyOut))
            {
                byte[] p = Rgb(o);
                Check(p[300 * 3] != p[300 * 3 + 2], "grey source stayed grey after warm balance");
            }
            using (MagickImage src = Chart(3000, 2000))
            using (IMagickImage<byte> prev = G2ImageAdjust.CreatePreview(src, 2000))
            {
                Check(prev.Width == 2000 && prev.Height == 1333, "preview size " + prev.Width + "x" + prev.Height);
                byte[] bgra = G2ImageAdjust.ToBgra(prev);
                Check(bgra.Length == 2000 * 1333 * 4, "bgra length");
                G2Histogram h = G2ImageAdjust.Histogram(bgra);
                Check(h.Pixels == 2000 * 1333 && h.Red.Sum() == h.Pixels, "histogram totals");
            }
            Pass("greyscale input promoted to colour when needed; preview proxy keeps aspect; BGRA/histogram sizes");

            // 11. orientation: EXIF-rotated JPEG is saved upright
            string rot = Path.Combine(dir, "rot.jpg");
            using (MagickImage src = Chart())
            {
                src.SetProfile(new ExifProfile());
                src.Orientation = OrientationType.RightTop;
                src.Write(rot);
            }
            using (var check = new MagickImage(rot))
            {
                if (check.Orientation == OrientationType.RightTop)
                {
                    string rotOut = Path.Combine(dir, "rot_cc.jpg");
                    G2ImageAdjust.SaveAdjusted(rot, rotOut, new G2AdjustSettings { Contrast = 10 }, false);
                    using var o = new MagickImage(rotOut);
                    Check(o.Width == 400 && o.Height == 600 && (o.Orientation == OrientationType.TopLeft || o.Orientation == OrientationType.Undefined), "rotated output " + o.Width + "x" + o.Height + " " + o.Orientation);
                    Pass("EXIF orientation applied once; output is upright with TopLeft orientation");
                }
                else Console.WriteLine("SKIP orientation (writer did not keep EXIF orientation)");
            }

            // 12. high resolution: 6720×4480 (EOS R) full-resolution save
            string big = Path.Combine(dir, "eosr.jpg");
            using (MagickImage src = Chart(6720, 4480)) { src.Quality = 95; src.Write(big); }
            var sw = Stopwatch.StartNew();
            string bigOut = G2ImageAdjust.NextAdjustedName(big);
            G2ImageAdjust.SaveAdjusted(big, bigOut, new G2AdjustSettings { Exposure = 0.3, Contrast = 15, Saturation = 10, Temperature = -8 }, false);
            sw.Stop();
            using (var info = new MagickImage(bigOut)) Check(info.Width == 6720 && info.Height == 4480, "hi-res size");
            Pass("6720×4480 full-resolution save in " + sw.ElapsedMilliseconds + " ms");

            // 13. templates: add/update/rename/remove, persisted across reloads, duplicate/empty names refused, corrupt file kept aside
            {
                string tplPath = Path.Combine(dir, "tpl", "color-templates.xml");
                var store = new G2ColorTemplateStore(tplPath);
                store.Load();
                Check(store.Items.Count == 0, "new store must be empty");
                var warm = new G2AdjustSettings { Exposure = 0.35, Temperature = 20, Saturation = 10 };
                warm.Blue.Gamma = 0.9; warm.Red.Black = 4;
                store.Add("의류 화이트 배경", warm);
                store.Add("Cool", new G2AdjustSettings { Temperature = -15, Contrast = 12 });
                bool dup = false; try { store.Add("cool", new G2AdjustSettings()); } catch (InvalidOperationException) { dup = true; }
                bool empty = false; try { store.Add("  ", new G2AdjustSettings()); } catch (ArgumentException) { empty = true; }
                Check(dup && empty, "duplicate/empty template names must be refused");
                var again = new G2ColorTemplateStore(tplPath); again.Load();
                Check(again.Items.Count == 2 && again.Find("의류 화이트 배경").Settings.SameAs(warm), "templates not persisted exactly");
                again.Update("Cool", new G2AdjustSettings { Temperature = -25 });
                again.Rename("Cool", "Cool blue");
                var third = new G2ColorTemplateStore(tplPath); third.Load();
                Check(third.Find("Cool") == null && Math.Abs(third.Find("cool blue").Settings.Temperature + 25) < 1e-9, "update/rename not persisted");
                Check(third.Remove("Cool blue") && !third.Remove("nope"), "remove");
                var fourth = new G2ColorTemplateStore(tplPath); fourth.Load();
                Check(fourth.Items.Count == 1, "remove not persisted");
                File.WriteAllText(tplPath, "<broken");
                var broken = new G2ColorTemplateStore(tplPath); broken.Load();
                Check(broken.Items.Count == 0 && broken.QuarantinedFile != null && File.Exists(broken.QuarantinedFile) && !File.Exists(tplPath), "corrupt file must be moved aside, not deleted");
                Check(!Directory.GetFiles(Path.GetDirectoryName(tplPath), ".~tpl-*").Any(), "template temp file left");
                Pass("templates: add/update/rename/remove persist, duplicate/empty names refused, corrupt file kept as .bad-*");
            }

            // 14. batch: every image in ONE folder (no subfolders) → folder\_cc\, originals untouched, log written, cancel stops after current
            {
                string bdir = Path.Combine(dir, "batch");
                Directory.CreateDirectory(Path.Combine(bdir, "sub"));
                var hashes = new System.Collections.Generic.Dictionary<string, string>();
                string[] names = { "A_F.jpg", "B_B.jpg", "C_D.png", "D.tif" };
                foreach (string n in names)
                {
                    string pth = Path.Combine(bdir, n);
                    using (MagickImage c = Chart(900, 600)) { c.Quality = 92; c.Write(pth); }
                    hashes[pth] = Sha(pth);
                }
                using (MagickImage c = Chart(300, 200)) c.Write(Path.Combine(bdir, "sub", "skip.jpg"));
                File.WriteAllText(Path.Combine(bdir, "notes.txt"), "not an image");
                File.WriteAllText(Path.Combine(bdir, "broken.jpg"), "not a jpeg");
                var s = new G2AdjustSettings { Exposure = 0.4, Saturation = 15, Temperature = 12 };
                int reports = 0;
                var prog = new Progress<G2BatchProgress>(p => reports++);
                G2BatchResult r = G2ColorBatch.Run(bdir, s, "테스트", prog, System.Threading.CancellationToken.None);
                Check(r.Total == 5 && r.Saved == 4 && r.Errors.Count == 1 && r.Errors[0].StartsWith("broken.jpg"), "batch counts " + r.Total + "/" + r.Saved + "/" + r.Errors.Count);
                string outDir = Path.Combine(bdir, "_cc");
                foreach (string n in names)
                {
                    string o = Path.Combine(outDir, Path.GetFileNameWithoutExtension(n) + "_cc" + Path.GetExtension(n));
                    Check(File.Exists(o), "batch output missing " + o);
                    using (var a = new MagickImage(o)) using (var src = new MagickImage(Path.Combine(bdir, n)))
                    using (IMagickImage<byte> expect = src.Clone())
                    {
                        G2ImageAdjust.Apply(expect, s);
                        Check(a.Width == 900 && a.Height == 600 && !a.HasAlpha, "batch output size/alpha " + n);
                        if (!n.EndsWith(".jpg")) Check(Rgb(a).AsSpan().SequenceEqual(Rgb(expect)), "lossless batch output must equal single-image result " + n);
                    }
                }
                foreach (var kv in hashes) Check(Sha(kv.Key) == kv.Value, "batch modified original " + kv.Key);
                Check(!Directory.Exists(Path.Combine(bdir, "sub", "_cc")) && !File.Exists(Path.Combine(outDir, "skip_cc.jpg")), "subfolder must not be processed");
                Check(r.LogFile != null && File.ReadAllText(r.LogFile).Contains("템플릿: 테스트") && File.ReadAllText(r.LogFile).Contains("실패\tbroken.jpg"), "batch log");
                // re-run: new names, nothing overwritten
                G2BatchResult r2 = G2ColorBatch.Run(bdir, s, "테스트", null, System.Threading.CancellationToken.None);
                Check(r2.Saved == 4 && File.Exists(Path.Combine(outDir, "A_F_cc (1).jpg")), "second batch must not overwrite");
                // cancel before start → nothing saved, reported as cancelled
                using (var cts = new System.Threading.CancellationTokenSource())
                {
                    cts.Cancel();
                    G2BatchResult r3 = G2ColorBatch.Run(bdir, s, "테스트", null, cts.Token);
                    Check(r3.Cancelled && r3.Saved == 0, "cancelled batch");
                }
                Pass("batch: 1 folder only, 4/5 saved to _cc (broken file reported), equals single-image path, originals byte-identical, log, no overwrite on rerun, cancel");
            }

            Console.WriteLine("COLOR_ADJUST_PASSED=" + passed);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
