using System;
using System.Collections.Generic;

namespace CameraControl.ViewModel;

public class Preset
{
	public string PresetName { get; set; } = string.Empty;

	public List<LightSetting> Lights { get; set; } = new List<LightSetting>
	{
		new LightSetting(),
		new LightSetting(),
		new LightSetting(),
		new LightSetting()
	};

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
