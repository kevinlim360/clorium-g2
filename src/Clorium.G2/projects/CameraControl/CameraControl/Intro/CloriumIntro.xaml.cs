using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CameraControl.Intro
{
    /// <summary>
    /// CLORIUM 인트로 애니메이션 (C안 · 리본)
    /// 정사각형 흰색 박스 안에서 재생되며, 박스 밖으로 나간 부분은 잘립니다.
    ///  1) 박스 오른쪽 위 모서리에서 앞/뒷면이 있는 리본이 비틀리며 날아와 심볼 둘레를 한 바퀴 돎
    ///  2) 고리가 네 조각으로 나뉘어 원을 따라 휘감기며 꼬여, 로고 리본 윤곽이 됨 (RibbonFx)
    ///  3) 원본 벡터 리본으로 교체 → 하늘색 바가 지나가며 CLORIUM 공개
    ///  4) AI PHOTO STATION 이 한 글자씩 등장
    ///  5) 로딩이 계속되면 심볼이 3초마다 90°씩 회전 (4방향 대칭이라 이음새 없음)
    /// </summary>
    public partial class CloriumIntro : UserControl
    {
        /// <summary>인트로 본편 길이. 이 시점에 IntroCompleted 가 발생합니다.</summary>
        public static readonly TimeSpan IntroDuration = TimeSpan.FromSeconds(4.4);

        /// <summary>인트로 본편이 끝났을 때 발생합니다. (이후에는 대기 루프가 계속 재생됨)</summary>
        public event EventHandler IntroCompleted;

        /// <summary>컨트롤이 로드되면 자동으로 재생할지 여부 (기본값 true)</summary>
        public bool AutoPlay { get; set; } = true;

        // 심볼 중심 (원본 .ai 좌표)
        private const double CX = 879, CY = 482.5;

        // CSS cubic-bezier 와 동일한 이징 곡선
        private static readonly KeySpline SnapEase = Spline(0.7, 0, 0.2, 1);
        private static readonly KeySpline Ease = Spline(0.25, 0.1, 0.25, 1);

        private DispatcherTimer _doneTimer;
        private TaskCompletionSource<bool> _introTcs = NewTcs();

        public CloriumIntro()
        {
            InitializeComponent();
            Loaded += (_, _) => { if (AutoPlay) Play(); };
            Unloaded += (_, _) => _doneTimer?.Stop();
        }

        /// <summary>인트로 본편이 끝날 때까지 기다립니다.</summary>
        public Task WaitForIntroAsync() => _introTcs.Task;

        /// <summary>처음부터 다시 재생합니다.</summary>
        public void Play()
        {
            _doneTimer?.Stop();
            if (_introTcs.Task.IsCompleted) _introTcs = NewTcs();
            Stage.BeginAnimation(OpacityProperty, null);
            Stage.Opacity = 1;
            Stage.RenderTransform = Transform.Identity;

            Path[] ribs = { Rib0, Rib1, Rib2, Rib3 };

            // Windows '애니메이션 효과'가 꺼져 있으면 완성된 로고를 바로 표시
            if (!SystemParameters.ClientAreaAnimation)
            {
                ShowFinalState(ribs);
                Dispatcher.BeginInvoke(new Action(Complete), DispatcherPriority.Loaded);
                return;
            }

            // ── 1~2) 리본 비행 + 꼬임 (0 ~ 2.8s) ─────────────────────
            TickLayer.RenderTransform = Transform.Identity;
            Fx.Start();

            // ── 3) 원본 벡터 리본으로 교체 (2.5s ~ 2.78s) ───────────────
            foreach (var r in ribs)
            {
                r.Opacity = 0;
                r.BeginAnimation(OpacityProperty, KF(2.5, (0, 0, null), (0.28, 1, null)));
            }

            // ── 워드마크 와이프 (2.6s ~ 3.5s) ──────────────────────────
            var wipe = new ScaleTransform(0, 1, -20, 0);
            WordLayer.Clip = new RectangleGeometry(new Rect(-20, 1095, 1773, 290)) { Transform = wipe };
            wipe.BeginAnimation(ScaleTransform.ScaleXProperty, KF(2.6, (0, 0, null), (0.9, 1, SnapEase)));

            var barMove = new TranslateTransform(-30, 0);
            WipeBar.RenderTransform = barMove;
            WipeBar.Opacity = 0;
            barMove.BeginAnimation(TranslateTransform.XProperty, KF(2.6, (0, -30, null), (0.9, 1745, SnapEase)));
            WipeBar.BeginAnimation(OpacityProperty,
                KF(2.6, (0, 0, null), (0.054, 1, null), (0.9, 1, null), (1.25, 0, Ease)));

            // ── 4) 태그라인: 한 글자씩 (3.4s ~) ───────────────────────
            int n = 0;
            foreach (UIElement glyph in TagLayer.Children)
            {
                double begin = 3.4 + 0.05 * n++;
                var tt = new TranslateTransform(0, -14);
                glyph.RenderTransform = tt;
                glyph.Opacity = 0;
                glyph.BeginAnimation(OpacityProperty, KF(begin, (0, 0, null), (0.45, 1, Ease)));
                tt.BeginAnimation(TranslateTransform.YProperty, KF(begin, (0, -14, null), (0.45, 0, Ease)));
            }

            // ── 5) 대기 루프: 3초마다 90° 딸깍 (4.75s ~) ──────────────
            var tick = new RotateTransform(0, CX, CY);
            TickLayer.RenderTransform = tick;
            var tickAnim = KF(4.75, (0, 0, null), (0.9, 90, SnapEase), (3.0, 90, null));
            tickAnim.RepeatBehavior = RepeatBehavior.Forever;
            tick.BeginAnimation(RotateTransform.AngleProperty, tickAnim);

            _doneTimer = new DispatcherTimer { Interval = IntroDuration };
            _doneTimer.Tick += (_, _) => { _doneTimer.Stop(); Complete(); };
            _doneTimer.Start();
        }

        /// <summary>G2: 애니메이션 없이 완성된 로고만 표시합니다 (Branding.ShowStartupScreenAnimation = false).</summary>
        public void ShowFinal()
        {
            _doneTimer?.Stop();
            Stage.BeginAnimation(OpacityProperty, null);
            Stage.Opacity = 1;
            ShowFinalState(new[] { Rib0, Rib1, Rib2, Rib3 });
            Complete();
        }

        /// <summary>로딩이 끝났을 때 호출: 흰 박스는 그대로 두고 로고만 0.5초 동안 살짝 커지며 사라집니다.</summary>
        public Task HideAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            var scale = new ScaleTransform(1, 1);
            Stage.RenderTransform = scale;

            var dur = new Duration(TimeSpan.FromSeconds(0.5));
            var fade = new DoubleAnimation(1, 0, dur) { EasingFunction = new QuadraticEase() };
            fade.Completed += (_, _) => tcs.TrySetResult(true);
            var grow = new DoubleAnimation(1, 1.03, dur) { EasingFunction = new QuadraticEase() };

            Stage.BeginAnimation(OpacityProperty, fade);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            return tcs.Task;
        }

        // ─────────────────────────────────────────────────────────────

        private void Complete()
        {
            _introTcs.TrySetResult(true);
            IntroCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void ShowFinalState(Path[] ribs)
        {
            Fx.Stop();
            TickLayer.RenderTransform = Transform.Identity;
            foreach (var r in ribs)
            {
                r.BeginAnimation(OpacityProperty, null);
                r.Opacity = 1;
            }
            WordLayer.Clip = null;
            WipeBar.BeginAnimation(OpacityProperty, null);
            WipeBar.Opacity = 0;
            foreach (UIElement g in TagLayer.Children)
            {
                g.BeginAnimation(OpacityProperty, null);
                g.Opacity = 1;
                g.RenderTransform = Transform.Identity;
            }
        }

        /// <summary>
        /// 키프레임 애니메이션 생성 도우미.
        /// frames: (시작 기준 초, 값, 이 구간의 이징 — null 이면 선형)
        /// </summary>
        private static DoubleAnimationUsingKeyFrames KF(double beginSec,
            params (double t, double v, KeySpline spline)[] frames)
        {
            var a = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = TimeSpan.FromSeconds(beginSec),
                FillBehavior = FillBehavior.HoldEnd
            };
            foreach (var (t, v, s) in frames)
            {
                var kt = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t));
                a.KeyFrames.Add(s is null
                    ? new LinearDoubleKeyFrame(v, kt)
                    : new SplineDoubleKeyFrame(v, kt, s));
            }
            a.Duration = TimeSpan.FromSeconds(frames[frames.Length - 1].t);
            return a;
        }

        private static KeySpline Spline(double x1, double y1, double x2, double y2)
        {
            var k = new KeySpline(x1, y1, x2, y2);
            k.Freeze();
            return k;
        }

        private static TaskCompletionSource<bool> NewTcs() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
