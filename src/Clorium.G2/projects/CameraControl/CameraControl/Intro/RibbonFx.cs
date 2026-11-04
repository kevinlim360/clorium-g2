using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace CameraControl.Intro
{
    /// <summary>
    /// 3D 리본 엔진 — 네 모서리에서 모여드는 네 개의 리본 (매 프레임 직접 그리는 요소).
    /// 각 리본은 최종 로고 리본 모양 그대로, 로고 가운데 정사각형의 변과 평행한 방향으로 미끄러져 들어옵니다.
    /// 마주 보는 리본(좌↔우, 위↔아래)은 반대 방향으로 평행하게 엇갈려 지나가고, 날아오는 동안
    /// 진행 방향을 축으로 굴러(3D 롤) 빛을 받다가 도착하며 평평해져 로고가 됩니다.
    ///  0.10 ~ 2.02s  비행·교차·정착 (위·아래 쌍은 0.12s 늦게 출발)
    ///  2.25 ~ 2.47s  (아래에서 먼저 나타난 원본 벡터 로고 위에서) 사라짐
    /// 좌표는 원본 .ai 좌표계 그대로이며, Canvas 의 (0,0)에 놓고 쓰면 됩니다.
    /// </summary>
    public sealed class RibbonFx : FrameworkElement
    {
        private const double CX = 879, CY = 482.5;
        private const int K = 4;
        private const double T0 = 0.10, TPair = 0.12, TFly = 1.80;  // 출발, 두 번째 쌍 지연, 비행 시간
        private const double Dist = 2500;                          // 출발 거리 (사각형 영역 밖 모서리 쪽)
        private const double Roll = 2 * Math.PI;                   // 비행 중 구르는 양 (한 바퀴)
        private const double TSwap = 2.25, TSwapD = 0.22;
        public const double EndTime = TSwap + TSwapD;

        private static readonly int M = RibbonEdges.Outer.Length / 2;
        private static readonly double[] ZLayer = { 36, 12, -12, -36 };   // 교차할 때 위아래 층

        private sealed class Rib
        {
            public Point[] O = Array.Empty<Point>(), I = Array.Empty<Point>();
            public Point G;               // 리본 중심
            public Vector D, Q;           // 진행 방향, 화면 안에서 그에 수직인 방향
            public double Delay, Z;
        }
        private static readonly Rib[] Ribs = new Rib[K];

        private static readonly double[,] Stops =
        {
            { 153, 213, 233 }, { 126, 179, 218 }, { 88, 126, 191 }, { 48, 83, 163 }, { 5, 53, 145 }
        };
        private static readonly Vec3 Light = new Vec3(-0.42, -0.62, 0.70).Norm();
        private static readonly Vec3 Half = new Vec3(Light.X, Light.Y, Light.Z + 1).Norm();
        private static readonly double Flat = Light.Z;

        private readonly Stopwatch _clock = new();
        private readonly Dictionary<int, (Brush fill, Pen seam, Pen edge)> _paint = new();
        private readonly List<Quad> _quads = new(K * 80);
        private bool _running;
        private double _time = -1;

        private struct Vec3
        {
            public double X, Y, Z;
            public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
            public Vec3 Norm() { double l = Math.Sqrt(X * X + Y * Y + Z * Z); if (l == 0) l = 1; return new Vec3(X / l, Y / l, Z / l); }
            public double Dot(Vec3 o) => X * o.X + Y * o.Y + Z * o.Z;
        }

        private struct Quad
        {
            public double Z;
            public Point A0, A1, B0, B1;
            public Color C;
        }

        static RibbonFx()
        {
            // 리본0 진행 방향: 안쪽 직선 변(정사각형의 한 변)과 평행, 오른쪽 위 → 왼쪽 아래
            var d0 = new Vector(652.74 - 863.95, 493.63 - 296.76);
            d0.Normalize();
            for (int k = 0; k < K; k++)
            {
                var r = new Rib { O = new Point[M], I = new Point[M] };
                double gx = 0, gy = 0;
                for (int j = 0; j < M; j++)
                {
                    r.O[j] = Rot(new Point(RibbonEdges.Outer[2 * j], RibbonEdges.Outer[2 * j + 1]), k);
                    r.I[j] = Rot(new Point(RibbonEdges.Inner[2 * j], RibbonEdges.Inner[2 * j + 1]), k);
                    gx += r.O[j].X + r.I[j].X; gy += r.O[j].Y + r.I[j].Y;
                }
                r.G = new Point(gx / (2 * M), gy / (2 * M));
                var d = d0;
                for (int i = 0; i < k; i++) d = new Vector(-d.Y, d.X);
                r.D = d;
                r.Q = new Vector(-d.Y, d.X);
                r.Delay = (k % 2) * TPair;
                r.Z = ZLayer[k];
                Ribs[k] = r;
            }
        }

        public RibbonFx()
        {
            IsHitTestVisible = false;
            Unloaded += (_, _) => Stop();
        }

        /// <summary>처음부터 재생</summary>
        public void Start()
        {
            Stop();
            _clock.Restart();
            _running = true;
            CompositionTarget.Rendering += OnFrame;
            _time = 0;
            InvalidateVisual();
        }

        /// <summary>정지 후 숨김</summary>
        public void Stop()
        {
            if (_running) CompositionTarget.Rendering -= OnFrame;
            _running = false;
            _clock.Stop();
            _time = -1;
            InvalidateVisual();
        }

        private void OnFrame(object sender, EventArgs e)
        {
            _time = _clock.Elapsed.TotalSeconds;
            if (_time >= EndTime)
            {
                CompositionTarget.Rendering -= OnFrame;
                _running = false;
                _time = -1;
            }
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            double t = _time;
            if (t < 0 || t >= EndTime) return;

            double opacity = t < TSwap ? 1 : 1 - (t - TSwap) / TSwapD;
            dc.PushOpacity(Math.Max(0, opacity));

            Build(t);
            foreach (var q in _quads)
            {
                var (fill, seam, edge) = Paint(q.C);
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(q.A0, true, true);
                    c.LineTo(q.A1, true, true);
                    c.LineTo(q.B1, true, true);
                    c.LineTo(q.B0, true, true);
                }
                g.Freeze();
                dc.DrawGeometry(fill, seam, g);          // 같은 색 테두리로 이음새 숨김
                dc.DrawLine(edge, q.A0, q.A1);           // 가장자리: 살짝 어두운 선으로 두께감
                dc.DrawLine(edge, q.B0, q.B1);
            }

            dc.Pop();
            if (_paint.Count > 6000) _paint.Clear();
        }

        // ─────────────────────────────────────────────────────────────

        private void Build(double t)
        {
            _quads.Clear();
            var A = new Point[M]; var B = new Point[M];
            var za = new double[M]; var zb = new double[M]; var th = new double[M];

            for (int k = 0; k < K; k++)
            {
                var R = Ribs[k];
                double p = Clamp01((t - T0 - R.Delay) / TFly);
                double e = EaseOut(p);
                double off = Dist * (1 - e);                          // 남은 거리
                double roll = Roll * Math.Pow(1 - e, 1.3);            // 구름 → 도착하며 0
                double flutter = 0.55 * Math.Pow(1 - e, 1.5);         // 길이 방향으로 살짝 일렁임
                double zLift = R.Z * (1 - e) * 6;
                double settle = Smooth((t - T0 - R.Delay - TFly * 0.82) / (TFly * 0.2));
                var shift = -R.D * off;

                for (int j = 0; j < M; j++)
                {
                    double fj = (double)j / (M - 1);
                    th[j] = roll + flutter * Math.Sin(fj * 5 + t * 7 + k);
                    A[j] = RollPt(R, R.O[j], th[j], shift, out za[j]);
                    B[j] = RollPt(R, R.I[j], th[j], shift, out zb[j]);
                }
                for (int j = 0; j < M - 1; j++)
                {
                    double a = (th[j] + th[j + 1]) / 2, c = Math.Cos(a), s = Math.Sin(a);
                    var N = new Vec3(-R.Q.X * s, -R.Q.Y * s, c).Norm();   // ẑ 를 진행축 둘레로 θ 회전
                    _quads.Add(new Quad
                    {
                        Z = zLift + (za[j] + za[j + 1] + zb[j] + zb[j + 1]) / 4 + k * 0.01,
                        A0 = A[j], A1 = A[j + 1], B0 = B[j], B1 = B[j + 1],
                        C = Lit(Base((j + 0.5) / (M - 1)), N, settle)
                    });
                }
            }
            _quads.Sort((x, y) => x.Z.CompareTo(y.Z));               // 먼 것부터 그림
        }

        /// <summary>리본 중심을 지나는 진행축 둘레로 θ 만큼 굴린 뒤 shift 만큼 이동</summary>
        private static Point RollPt(Rib r, Point p, double th, Vector shift, out double z)
        {
            var rel = p - r.G;
            double along = rel.X * r.D.X + rel.Y * r.D.Y, perp = rel.X * r.Q.X + rel.Y * r.Q.Y;
            z = perp * Math.Sin(th);
            return r.G + r.D * along + r.Q * (perp * Math.Cos(th)) + shift;
        }

        private static double[] Base(double f)
        {
            f = Clamp01(f) * 4;
            int i = Math.Min(3, (int)Math.Floor(f));
            double g = f - i;
            return new[]
            {
                Stops[i, 0] + (Stops[i + 1, 0] - Stops[i, 0]) * g,
                Stops[i, 1] + (Stops[i + 1, 1] - Stops[i, 1]) * g,
                Stops[i, 2] + (Stops[i + 1, 2] - Stops[i, 2]) * g
            };
        }

        /// <summary>조명 적용. settle = 1 이면 원본 색 그대로</summary>
        private static Color Lit(double[] c, Vec3 n, double settle)
        {
            if (n.Z < 0)
            {
                n = new Vec3(-n.X, -n.Y, -n.Z);
                for (int i = 0; i < 3; i++) c[i] += (255 - c[i]) * 0.22;   // 뒷면은 한 톤 밝게
            }
            double d = Math.Max(0, n.Dot(Light));
            double nh = Math.Max(0, n.Dot(Half));
            double sp = Math.Pow(nh, 60) * 0.55 + Math.Pow(nh, 8) * 0.10;  // 하이라이트 + 은은한 광택
            double k = 0.48 + 0.52 * d / Flat;
            k += (1 - k) * settle;
            sp *= 1 - settle;
            byte Ch(double v) => (byte)Math.Round(Math.Min(255, Math.Max(0, v * k + 255 * sp)));
            return Color.FromRgb(Ch(c[0]), Ch(c[1]), Ch(c[2]));
        }

        private static Point Rot(Point p, int k)
        {
            double dx = p.X - CX, dy = p.Y - CY;
            for (int i = 0; i < k; i++) { double t = dx; dx = -dy; dy = t; }
            return new Point(CX + dx, CY + dy);
        }

        private (Brush, Pen, Pen) Paint(Color c)
        {
            int key = (c.R << 16) | (c.G << 8) | c.B;
            if (_paint.TryGetValue(key, out var v)) return v;
            var fill = new SolidColorBrush(c); fill.Freeze();
            var seam = new Pen(fill, 3.2) { LineJoin = PenLineJoin.Round }; seam.Freeze();
            var eb = new SolidColorBrush(Color.FromArgb(140, (byte)(c.R * 0.62), (byte)(c.G * 0.62), (byte)(c.B * 0.66))); eb.Freeze();
            var edge = new Pen(eb, 3.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }; edge.Freeze();
            _paint[key] = (fill, seam, edge);
            return (fill, seam, edge);
        }

        private static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;
        private static double Smooth(double x) { x = Clamp01(x); return x * x * (3 - 2 * x); }
        private static double EaseOut(double x) { x = Clamp01(x); return 1 - Math.Pow(1 - x, 2.2); }
    }
}
