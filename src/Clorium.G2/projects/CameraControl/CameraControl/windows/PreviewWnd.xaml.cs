using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraControl.windows;

public partial class PreviewWnd : Window, IComponentConnector
{
	public PreviewWnd()
	{
		InitializeComponent();
	}

	private void Image_MouseMove(object sender, MouseEventArgs e)
	{
		Point point = e.GetPosition(this);
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96.0, 96.0, PixelFormats.Default);
		renderTargetBitmap.Render(this);
		if (point.X <= (double)renderTargetBitmap.PixelWidth && point.Y <= (double)renderTargetBitmap.PixelHeight)
		{
			CroppedBitmap croppedBitmap = new CroppedBitmap(renderTargetBitmap, new Int32Rect((int)point.X, (int)point.Y, 1, 1));
			byte[] pixels = new byte[4];
			croppedBitmap.CopyPixels(pixels, 4, 0);
			Color SelectedColor = Color.FromArgb(byte.MaxValue, pixels[2], pixels[1], pixels[0]);
			TextBox.Text = "#" + SelectedColor.ToString().Substring(3);
			Label.Background = new SolidColorBrush(SelectedColor);
		}
	}

	private void Image_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (TextBox.Text != null)
			{
				Clipboard.SetText(TextBox.Text);
			}
		}
		catch (Exception)
		{
		}
	}
}
