using System.Windows.Media;

namespace CameraControl.Core.Wpf;

public class ColorViewModel
{
	public Color Color { get; set; }

	public Brush Brush => new SolidColorBrush(Color);

	public string Name { get; set; }
}
