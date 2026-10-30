using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.ViewModel;

namespace CameraControl.Layouts;

public partial class LayoutNormal : LayoutBase, IComponentConnector, IStyleConnector
{
	private static class WindowsPhotoViewer
	{
		private const string FilePath32 = "c:\\program files (x86)\\Windows Photo Viewer\\PhotoViewer.dll";

		private const string FilePath64 = "c:\\program files\\Windows Photo Viewer\\PhotoViewer.dll";

		[DllImport("c:\\program files (x86)\\Windows Photo Viewer\\PhotoViewer.dll", CharSet = CharSet.Unicode, EntryPoint = "ImageView_FullscreenW")]
		private static extern void ImageView_Fullscreen32(IntPtr unknown1, IntPtr unknown2, string path, int unknown3);

		[DllImport("c:\\program files\\Windows Photo Viewer\\PhotoViewer.dll", CharSet = CharSet.Unicode, EntryPoint = "ImageView_FullscreenW")]
		private static extern void ImageView_Fullscreen64(IntPtr unknown1, IntPtr unknown2, string path, int unknown3);

		public static bool ShowImage(FileInfo imageFile)
		{
			if (IntPtr.Size == 8 && File.Exists("c:\\program files\\Windows Photo Viewer\\PhotoViewer.dll") && imageFile.Exists)
			{
				ImageView_Fullscreen64(IntPtr.Zero, IntPtr.Zero, imageFile.FullName, 0);
				return true;
			}
			if (IntPtr.Size == 4 && File.Exists("c:\\program files (x86)\\Windows Photo Viewer\\PhotoViewer.dll") && imageFile.Exists)
			{
				ImageView_Fullscreen32(IntPtr.Zero, IntPtr.Zero, imageFile.FullName, 0);
				return true;
			}
			return false;
		}
	}

	public LayoutNormal()
	{
		InitializeComponent();
		ImageLIst = ImageLIstBox;
		ZoomAndPanControl = zoomAndPanControl;
		if (ZoomAndPanControl != null)
		{
			LayoutViewModel = (LayoutViewModel)ZoomAndPanControl.DataContext;
			MediaElement = VideoControl;
			content = Image;
			InitServices();
		}
	}

	private void DoubleClickHandler(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("ViewExternal");
	}

	private void button1_Click(object sender, EventArgs e)
	{
		if (!WindowsPhotoViewer.ShowImage(new FileInfo("\\\\test.jpg")))
		{
			Console.WriteLine("Failed to show image");
		}
	}
}
