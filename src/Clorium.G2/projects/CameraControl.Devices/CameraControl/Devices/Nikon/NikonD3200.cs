using System.Runtime.InteropServices;
using System.Threading;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD3200 : NikonBase
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		CaptureInSdRam = false;
		return result;
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			DeviceReady();
			ErrorCodes.GetException(CaptureInSdRam ? ExecuteWithNoData(37056u, uint.MaxValue) : ExecuteWithNoData(4110u));
		}
		catch (COMException exception)
		{
			IsBusy = false;
			ErrorCodes.GetException(exception);
		}
		catch
		{
			IsBusy = false;
			throw;
		}
		finally
		{
			Monitor.Exit(Locker);
		}
	}

	protected override void GetAdditionalLiveViewData(LiveViewData viewData, byte[] result)
	{
		viewData.LiveViewImageWidth = BaseMTPCamera.ToInt16(result, 8);
		viewData.LiveViewImageHeight = BaseMTPCamera.ToInt16(result, 10);
		viewData.ImageWidth = BaseMTPCamera.ToInt16(result, 12);
		viewData.ImageHeight = BaseMTPCamera.ToInt16(result, 14);
		viewData.FocusFrameXSize = 800;
		viewData.FocusFrameYSize = 800;
		viewData.FocusX = BaseMTPCamera.ToInt16(result, 20);
		viewData.FocusY = BaseMTPCamera.ToInt16(result, 22);
		viewData.Focused = result[40] != 1;
	}

	public override void StartRecordMovie()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		SetProperty(4118u, new byte[1] { 1 }, 53744u);
		base.StartRecordMovie();
	}

	public override void StopRecordMovie()
	{
		base.StopRecordMovie();
		SetProperty(4118u, new byte[1], 53744u);
	}
}
