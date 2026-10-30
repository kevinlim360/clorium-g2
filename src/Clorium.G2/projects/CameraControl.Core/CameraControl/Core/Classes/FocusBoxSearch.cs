using System;
using System.Threading;
using System.Threading.Tasks;

namespace CameraControl.Core.Classes;

// Search from the near endpoint toward the far endpoint. Every accepted motor
// step is supplied by the caller, so autofocus never loses the A-relative count.
public sealed class FocusBoxSearch
{
    public async Task<int> FindAroundPeakAsync(Func<Task<double>> measure, Func<int,Task> move,
        int limit, CancellationToken token)
    {
        // A can start on either side of its selected subject.
        for (int i=0;i<4;i++) { token.ThrowIfCancellationRequested(); await move(-1); }
        double near = await measure();
        for (int i=0;i<8;i++) { token.ThrowIfCancellationRequested(); await move(1); }
        double far = await measure();
        for (int i=0;i<4;i++) { token.ThrowIfCancellationRequested(); await move(-1); }
        int direction = near >= far ? -1 : 1;
        int result = await FindFarPeakAsync(measure,step => move(step * direction),limit,token);
        return result * direction;
    }
    public async Task<int> FindFarPeakAsync(Func<Task<double>> measure, Func<int,Task> move,
        int limit, CancellationToken token)
    {
        if (limit < 8 || limit > 800) throw new ArgumentOutOfRangeException(nameof(limit));
        double initial = await measure(), best = initial;
        if (double.IsNaN(initial) || double.IsInfinity(initial) || initial < 1)
            throw new InvalidOperationException("박스 안에 무늬가 있는 피사체를 선택해 주세요. 선명도를 측정할 수 없습니다.");
        int bestAt = 0, current = 0, falling = 0, plateau = 0;
        bool found = false;
        for (int i = 1; i <= limit; i++)
        {
            token.ThrowIfCancellationRequested();
            await move(1); current++;
            double score = await measure();
            if (double.IsNaN(score) || double.IsInfinity(score) || score < 0)
                throw new InvalidOperationException("라이브뷰 선명도 측정에 실패했습니다.");
            if (score > best * 1.01) { best = score; bestAt = current; falling = plateau = 0; }
            else { falling = score < best * .90 ? falling + 1 : 0; plateau = score >= best * .98 ? plateau + 1 : 0; }
            // Falling edges confirm a peak, including an already focused target.
            // A plateau is accepted only after a substantial increase from A.
            if (falling >= 5 || (plateau >= 16 && best > initial * 1.4)) { found = true; break; }
        }
        if (!found) throw new InvalidOperationException("먼 박스의 초점 정점을 확인하지 못했습니다. 박스 위치를 바꾸거나 수동 화살표로 A/B를 지정해 주세요.");
        while (current > bestAt) { token.ThrowIfCancellationRequested(); await move(-1); current--; }
        return bestAt;
    }
}
