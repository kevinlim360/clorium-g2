using System;
using System.IO;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;
using CameraControl.windows;

namespace CameraControl.Classes;

// G2 ↔ 이미지보정 (G2ColorAdjustWnd: Magick.NET exposure/brightness/contrast/saturation/temperature/tint/levels, templates,
// _cc batch, before/after compare). Replaces the removed ColorCompensationWnd. The left-rail 이미지보정 button (shown when
// 클로리움 설정 > 이미지보정 is on) opens the window on the photo selected in G2; a corrected copy is saved next to it as
// *_cc and the original is never changed. Never throws into G2.
internal static class CloriumImageAdjust
{
	// Same-name siblings tried for an unsupported selection (RAW shot with JPEG), in this order.
	private static readonly string[] SiblingExtensions = { ".jpg", ".jpeg", ".tif", ".tiff", ".png", ".bmp" };

	// Replaced by tests; the defaults show a message box and the modal window.
	internal static Action<string> ShowMessage = text => MessageBox.Show(text, "이미지보정", MessageBoxButton.OK, MessageBoxImage.Information);

	internal static Action<string, Window> ShowWindow = (file, owner) => new G2ColorAdjustWnd(file) { Owner = owner }.ShowDialog();

	public static bool Enabled => ServiceProvider.Settings?.UseImageAdjust == true;

	// The photo the window opens for the selection: a supported photo itself, else a same-name JPEG/TIFF/PNG/BMP next
	// to it (RAW+JPEG capture), else null.
	public static string ResolveTarget(string selected)
	{
		if (string.IsNullOrWhiteSpace(selected)) return null;
		try
		{
			string full = Path.GetFullPath(selected.Trim().Trim('"'));
			if (!File.Exists(full)) return null;
			if (G2ImageAdjust.IsSupportedFile(full)) return full;
			foreach (string ext in SiblingExtensions)
			{
				string sibling = Path.ChangeExtension(full, ext);
				if (File.Exists(sibling)) return sibling;
			}
			return null;
		}
		catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is IOException)
		{
			return null;
		}
	}

	// Rail button. Returns the photo the window was opened on (null = disabled, nothing usable selected or failed;
	// the operator was told why).
	public static string Open(string selected, Window owner)
	{
		if (!Enabled) return null;
		if (string.IsNullOrWhiteSpace(selected) || !File.Exists(selected))
		{
			ShowMessage("이미지보정할 사진을 먼저 선택해 주세요.");
			return null;
		}
		string target = ResolveTarget(selected);
		if (target == null)
		{
			ShowMessage("JPEG, PNG, TIFF, BMP 사진만 이미지보정할 수 있습니다.\n\nRAW 파일은 같은 이름의 JPEG나 TIFF가 옆에 있으면 그 사진을 엽니다.");
			return null;
		}
		try
		{
			Log.Debug("ImageAdjust : open " + target);
			ShowWindow(target, owner);
			return target;
		}
		catch (Exception ex)
		{
			Log.Error("ImageAdjust : unable to open", ex);
			ShowMessage("이미지보정 창을 열지 못했습니다.\n\n" + ex.Message);
			return null;
		}
	}
}
