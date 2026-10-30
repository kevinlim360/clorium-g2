namespace CameraControl.ViewModel;

public class FilterSettings
{
	public int Brightness { get; set; } = 0;

	public int Contrast { get; set; } = 100;

	public int Gamma { get; set; } = 100;

	public int Sharpness { get; set; } = 100;

	public int Saturation { get; set; } = 100;

	public int Hue { get; set; } = 0;

	public FilterSettings Clone()
	{
		return new FilterSettings
		{
			Brightness = Brightness,
			Contrast = Contrast,
			Gamma = Gamma,
			Sharpness = Sharpness,
			Saturation = Saturation,
			Hue = Hue
		};
	}
}
