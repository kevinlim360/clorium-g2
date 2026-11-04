using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace CameraControl.Intro
{
    /// <summary>
    /// 앞/뒷면이 있는 리본을 중심선 + 비틀림 각도로 계산해 매 프레임 직접 그리는 요소.
    ///  - 0.10 ~ 1.70s  박스 오른쪽 위 모서리에서 비틀리며 날아와 원 궤도를 한 바퀴 돌고 고리로 닫힘
    ///  - 1.55 ~ 2.55s  고리가 네 조각으로 나뉘어, 원을 따라 휘감기며 각 로고 리본 윤곽으로 변형
    ///  - 2.50 ~ 2.78s  사라지면서 원본 벡터 리본으로 교체 (CloriumIntro 쪽에서 페이드인)
    /// 좌표는 원본 .ai 좌표계 그대로이며, Canvas 의 (0,0)에 놓고 쓰면 됩니다.
    /// </summary>
    public sealed class RibbonFx : FrameworkElement
    {
        public const double EndTime = 2.8;

        private const double CX = 879, CY = 482.5, R0 = 330;
        private const int K = 4;
        private const double ThetaTail = 160;               // 고리가 닫혔을 때 꼬리 각도(°)

        private static readonly double Len = 2 * Math.PI * R0;  // 리본 길이 = 원 둘레
        private static readonly int M = RibbonEdges.Outer.Length / 2;
        private static readonly Point[][] TgtOuter = new Point[K][];
        private static readonly Point[][] TgtInner = new Point[K][];

        // 중심선 (진입 베지어 + 시계 방향 540° 원)
        private static readonly List<Point> P = new();
        private static readonly List<double> S = new();
        private static readonly double HeadEnd;

        // 색: 꼬리(밝은 하늘색) → 머리(진한 남색)
        private static readonly double[,] Stops =
        {
            { 0x99, 0xd5, 0xe9 }, { 0x7e, 0xb3, 0xda }, { 0x58, 0x7e, 0xbf }, { 0x30, 0x53, 0xa3 }, { 0x05, 0x35, 0x91 }
        };

        private static readonly Pen EdgePenBase = MakeFrozenPen(Brushes.White, 7);

        private readonly Stopwatch _clock = new();
        private readonly Dictionary<int, (Brush b, Pen p)> _paint = new();
        private bool _running;
        private double _time = -1;

        static RibbonFx()
        {
            // 로고 리본 k = 리본0 을 시계 방향 90°·k 회전
            for (int k = 0; k < K; k++)
            {
                TgtOuter[k] = new Point[M];
                TgtInner[k] = new Point[M];
                for (int j = 0; j < M; j++)
                {
                    TgtOuter[k][j] = Rot(new Point(RibbonEdges.Outer[2 * j], RibbonEdges.Outer[2 * j + 1]), k);
                    TgtInner[k][j] = Rot(new Point(RibbonEdges.Inner[2 * j], RibbonEdges.Inner[2 * j + 1]), k);
                }
            }

            Point b0 = new(2230, -580), b1 = new(1660, -260), b2 = new(CX + R0, -40), b3 = new(CX + R0, CY);
            for (int i = 0; i <= 300; i++)
            {
                double t = i / 300.0, u = 1 - t;
                Add(new Point(u * u * u * b0.X + 3 * u * u * t * b1.X + 3 * u * t * t * b2.X + t * t * t * b3.X,
                              u * u * u * b0.Y + 3 * u * u * t * b1.Y + 3 * u * t * t * b2.Y + t * t * t * b3.Y));
            }
            double approach = S[S.Count - 1];
            for (int i = 1; i <= 900; i++)
            {
                double a = (540.0 * i / 900) * Math.PI / 180;
                Add(new Point(CX + R0 * Math.Cos(a), CY + R0 * Math.Sin(a)));
            }
            HeadEnd = approach + (ThetaTail + 360) / 360 * Len;
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

            double opacity = t < 2.5 ? 1 : 1 - (t - 2.5) / 0.28;
            dc.PushOpacity(Math.Max(0, opacity));

            double u = Clamp01((t - 0.10) / 1.6);
            double head = HeadEnd * (1 - Math.Pow(1 - u, 2.1));      // 빠르게 진입 → 감속하며 닫힘
            double tail = head - Len;
            double hw = 78 + 17 * Smooth((u - 0.45) / 0.55);          // 반폭
            double twist = 1 - Smooth((t - 0.75) / 0.95);             // 비틀림 풀림
            double spin = t * 2.4;                                    // 비틀림 마디가 흐름

            var A = new Point[M];
            var B = new Point[M];
            var cols = new Color[M];

            for (int k = 0; k < K; k++)
            {
                double e = InOut((t - (1.55 + 0.045 * k)) / 0.85);    // 조각별 변형 진행
                for (int j = 0; j < M; j++)
                {
                    double fj = (double)j / (M - 1);
                    double s = tail + Len * (k + fj) / K;
                    At(s, out var p, out var n);
                    double um = (head - s) / Len;                     // 0 = 머리, 1 = 꼬리
                    double phi = twist * (2 * Math.PI * 1.25 * um + Math.Sin(spin + um * 3) * 0.6);
                    double cs = Math.Cos(phi);
                    double w = hw * cs;
                    var a = Polar(p + n * w, TgtOuter[k][j], e);
                    var b = Polar(p - n * w, TgtInner[k][j], e);

                    // 변형 중 꼬리 → 머리 방향으로 한 번 감기며 뒤집힘 (끝나면 정확히 로고 윤곽)
                    double flip = 2 * Math.PI * (1 - Smooth(e * 1.6 - fj * 0.6));
                    double cf = Math.Cos(flip);
                    var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                    A[j] = mid + (a - mid) * cf;
                    B[j] = mid + (b - mid) * cf;

                    double c2 = e > 0 ? cf : cs;
                    double sang = e > 0 ? flip : phi;
                    double spec = 0.32 * Math.Pow(Math.Max(0, Math.Sin(sang + 0.5)), 24);
                    double f = (1 - um) + (fj - (1 - um)) * e;
                    cols[j] = Shade(f, Math.Abs(c2), c2 < 0, spec);
                }

                for (int j = 0; j < M - 1; j++)
                {
                    var g = new StreamGeometry();
                    using (var c = g.Open())
                    {
                        c.BeginFigure(A[j], true, true);
                        c.LineTo(A[j + 1], true, false);
                        c.LineTo(B[j + 1], true, false);
                        c.LineTo(B[j], true, false);
                    }
                    g.Freeze();
                    var (br, pen) = Paint(cols[j]);
                    dc.DrawGeometry(br, pen, g);                       // 같은 색 테두리로 이음새 숨김
                }

                if (e > 0)                                            // 흰 분리선 (로고의 흰 틈)
                {
                    var outline = new StreamGeometry();
                    using (var c = outline.Open())
                    {
                        c.BeginFigure(A[0], false, true);
                        for (int j = 1; j < M; j++) c.LineTo(A[j], true, true);
                        for (int j = M - 1; j >= 0; j--) c.LineTo(B[j], true, true);
                    }
                    outline.Freeze();
                    dc.PushOpacity(e);
                    dc.DrawGeometry(null, EdgePenBase, outline);
                    dc.Pop();
                }
            }

            dc.Pop();
            if (_paint.Count > 6000) _paint.Clear();
        }

        // ─────────────────────────────────────────────────────────────

        private static void Add(Point p)
        {
            S.Add(P.Count == 0 ? 0 : S[S.Count - 1] + (p - P[P.Count - 1]).Length);
            P.Add(p);
        }

        /// <summary>중심선 위 거리 s 의 위치와 바깥쪽 법선</summary>
        private static void At(double s, out Point pos, out Vector normal)
        {
            if (s <= 0)
            {
                var d = P[1] - P[0]; d.Normalize();
                pos = P[0] + d * s;
                normal = new Vector(d.Y, -d.X);
                return;
            }
            int lo = 0, hi = S.Count - 1;
            while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (S[mid] <= s) lo = mid; else hi = mid; }
            double seg = S[hi] - S[lo];
            double f = Math.Min(1, seg <= 0 ? 0 : (s - S[lo]) / seg);
            var tv = P[hi] - P[lo];
            pos = P[lo] + tv * f;
            tv.Normalize();
            normal = new Vector(tv.Y, -tv.X);
        }

        /// <summary>중심 기준 극좌표 보간 — 원을 따라 휘감기며 조여듦</summary>
        private static Point Polar(Point from, Point to, double e)
        {
            double r1 = Math.Sqrt((from.X - CX) * (from.X - CX) + (from.Y - CY) * (from.Y - CY));
            double a1 = Math.Atan2(from.Y - CY, from.X - CX);
            double r2 = Math.Sqrt((to.X - CX) * (to.X - CX) + (to.Y - CY) * (to.Y - CY));
            double a2 = Math.Atan2(to.Y - CY, to.X - CX);
            while (a2 - a1 > Math.PI) a2 -= 2 * Math.PI;
            while (a2 - a1 < -Math.PI) a2 += 2 * Math.PI;
            double r = r1 + (r2 - r1) * e, a = a1 + (a2 - a1) * e;
            return new Point(CX + r * Math.Cos(a), CY + r * Math.Sin(a));
        }

        private static Point Rot(Point p, int k)
        {
            double dx = p.X - CX, dy = p.Y - CY;
            for (int i = 0; i < k; i++) { double t = dx; dx = -dy; dy = t; }
            return new Point(CX + dx, CY + dy);
        }

        private static Color Shade(double f, double shade, bool back, double spec)
        {
            int n = Stops.GetLength(0);
            f = Clamp01(f) * (n - 1);
            int i = Math.Min(n - 2, (int)Math.Floor(f));
            double g = f - i;
            byte Ch(int j)
            {
                double v = Stops[i, j] + (Stops[i + 1, j] - Stops[i, j]) * g;
                if (back) v += (255 - v) * 0.38;                       // 뒷면은 한 톤 밝게
                v = v * (0.55 + 0.45 * shade) + 255 * spec;            // 면 방향 명암 + 반사광
                return (byte)Math.Round(Math.Min(255, v));
            }
            return Color.FromRgb(Ch(0), Ch(1), Ch(2));
        }

        private (Brush, Pen) Paint(Color c)
        {
            int key = (c.R << 16) | (c.G << 8) | c.B;
            if (_paint.TryGetValue(key, out var v)) return v;
            var b = new SolidColorBrush(c); b.Freeze();
            var p = new Pen(b, 2.4) { LineJoin = PenLineJoin.Round }; p.Freeze();
            _paint[key] = (b, p);
            return (b, p);
        }

        private static Pen MakeFrozenPen(Brush b, double w)
        {
            var p = new Pen(b, w) { LineJoin = PenLineJoin.Round };
            p.Freeze();
            return p;
        }

        private static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;
        private static double Smooth(double x) { x = Clamp01(x); return x * x * (3 - 2 * x); }
        private static double InOut(double x) { x = Clamp01(x); return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2; }
    }
}
