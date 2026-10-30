namespace CameraControl.Devices;

// Deliberately not configurable: only the replacement capture service may be enabled.
public static class LegacyCapturePolicy
{
    public static bool FocusStackingEnabled => false;
    public static bool MovieRecordingEnabled => false;
    public const string DisabledMessage = "기존 수퍼포커스/동영상 촬영은 G2에서 비활성화되었습니다.";
    public static void RequireMovieRecording()
    {
        if (!MovieRecordingEnabled) throw new System.NotSupportedException(DisabledMessage);
    }
}
