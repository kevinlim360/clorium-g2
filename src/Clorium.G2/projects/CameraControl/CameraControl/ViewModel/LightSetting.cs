namespace CameraControl.ViewModel;

public class LightSetting
{
	public string Name { get; set; } = string.Empty;

	public string Brightness { get; set; } = string.Empty;

	public string CCT { get; set; } = string.Empty;

	public LightSetting(string name)
	{
		Name = name;
	}

	public LightSetting()
	{
	}
}
