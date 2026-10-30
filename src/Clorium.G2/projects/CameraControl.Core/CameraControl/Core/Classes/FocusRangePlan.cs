using System;
using System.Collections.Generic;

namespace CameraControl.Core.Classes;

// Positions count successful SMALL SDK moves, never metres or mixed drive magnitudes.
public sealed class FocusRangePlan
{
    public int Position { get; private set; }
    public int? A { get; private set; }
    public int? B { get; private set; }
    public bool LastMarkedB { get; private set; }
    public void Moved(int signedSmallStep) { Position = checked(Position + signedSmallStep); }
    public void MarkA() { A = Position; LastMarkedB = false; }
    public void MarkB() { B = Position; LastMarkedB = true; }
    public void ClearA() { A = null; }
    public void ClearB() { B = null; }
    public void Reset() { Position = 0; A = B = null; }
    public void Invalidate() { A = B = null; }

    public long QualityCount(int quality)
    {
        if (!A.HasValue || !B.HasValue) throw new InvalidOperationException("가까운 A와 먼 B를 모두 지정해 주세요.");
        if (B == A) throw new InvalidOperationException("A와 B를 서로 다른 초점으로 지정해 주세요.");
        if (quality < 0 || quality > 2) throw new ArgumentOutOfRangeException(nameof(quality));
        long high = (Math.Abs((long)B.Value - A.Value) + 2) / 3 + 1;
        // Nearest whole cut; both endpoints still require at least two images.
        return quality == 0 ? high : Math.Max(2, (high * (quality == 1 ? 2 : 1) + 1) / 3);
    }

    public string QualityDescription(int quality)
    {
        string label = quality == 0 ? "고" : quality == 1 ? "중" : "저";
        long count = QualityCount(quality), span = Math.Abs((long)B.Value - A.Value);
        long min = quality == 0 ? (span % 3 == 0 ? Math.Min(3,span) : span % 3) : span / (count - 1), max = quality == 0 ? Math.Min(3,span) : (span + count - 2) / (count - 1);
        return label + " · " + count + "컷 · " + min + (min == max ? "" : "–" + max) + "단위" + (count > 200 ? " (한도 초과)" : "");
    }

    public IReadOnlyList<int> PositionsForQuality(int quality)
    {
        long count = QualityCount(quality);
        if (count > 200) throw new InvalidOperationException("예상 컷수가 200장을 넘습니다. 더 낮은 품질을 선택하거나 A–B 범위를 줄여 주세요.");
        if (quality != 0) return PositionsForCount((int)count);
        long span = Math.Abs((long)B.Value - A.Value), direction = Math.Sign((long)B.Value - A.Value);
        var result = new List<int>();
        for (long offset = 0; offset < span; offset += 3) result.Add((int)(A.Value + direction * offset));
        result.Add(B.Value);
        return result.AsReadOnly();
    }

    public IReadOnlyList<int> PositionsForCount(int count)
    {
        QualityCount(0); // Validate anchors, including distinct endpoints.
        long span = Math.Abs((long)B.Value - A.Value), direction = Math.Sign((long)B.Value - A.Value);
        if (count < 2 || count > 200 || count > span + 1) throw new InvalidOperationException("컷수는 2–200이며 초점 단계 수보다 많을 수 없습니다.");
        var result = new List<int>();
        for (int i = 0; i < count; i++) result.Add((int)(A.Value + direction * ((long)i * span / (count - 1))));
        return result.AsReadOnly();
    }

    public IReadOnlyList<int> CapturePositionsForQuality(int quality)
    {
        return Orient(PositionsForQuality(quality));
    }
    public IReadOnlyList<int> CapturePositionsForCount(int count) => Orient(PositionsForCount(count));
    public int MaximumUserCuts => (int)Math.Min(200, QualityCount(0));
    public IReadOnlyList<int> CapturePositionsForUserCount(int count)
    {
        if(count < 2 || count > MaximumUserCuts) throw new InvalidOperationException("컷수는 2부터 고 품질 컷수(최대 200)까지 입력해 주세요.");
        return CapturePositionsForCount(count);
    }
    private IReadOnlyList<int> Orient(IReadOnlyList<int> positions)
    {
        var result = new List<int>(positions);
        long toA = Math.Abs((long)Position - A.Value), toB = Math.Abs((long)Position - B.Value);
        if (toB < toA || (toB == toA && LastMarkedB)) result.Reverse();
        return result.AsReadOnly();
    }

    public IReadOnlyList<int> Positions(int interval)
    {
        if (!A.HasValue || !B.HasValue) throw new InvalidOperationException("가까운 A와 먼 B를 모두 지정해 주세요.");
        if (B <= A) throw new InvalidOperationException("B는 A보다 먼 초점이어야 합니다. 가까운 곳을 A, 먼 곳을 B로 다시 지정해 주세요.");
        if (interval < 1 || interval > 20) throw new ArgumentOutOfRangeException(nameof(interval));
        long span = (long)B.Value - A.Value;
        long count = (span + interval - 1) / interval + 1;
        if (count > 200) throw new InvalidOperationException("예상 컷수가 200장을 넘습니다. 촬영 간격을 늘리거나 A–B 범위를 줄여 주세요.");
        var result = new List<int> { A.Value };
        for (long p = (long)A.Value + interval; p < B.Value; p += interval) result.Add((int)p);
        result.Add(B.Value);
        return result.AsReadOnly();
    }
}
