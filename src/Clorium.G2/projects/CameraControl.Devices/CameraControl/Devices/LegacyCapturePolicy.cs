namespace CameraControl.Devices;

// Deliberately not configurable: legacy movie recording stays off in G2. (The legacy focus stacking it also
// guarded was removed on 2026-11-03; 수퍼포커스 replaces it.)
public static class LegacyCapturePolicy
{
    public static bool MovieRecordingEnabled => false;
    public const string DisabledMessage = "기존 동영상 촬영은 G2에서 비활성화되었습니다.";
    public static void RequireMovieRecording()
    {
        if (!MovieRecordingEnabled) throw new System.NotSupportedException(DisabledMessage);
    }
}
