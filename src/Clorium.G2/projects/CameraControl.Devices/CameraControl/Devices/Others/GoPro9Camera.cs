using System.Collections.Generic;
using CameraControl.Devices.Classes;
using CameraControl.Devices.GoPro;
using Newtonsoft.Json.Linq;

namespace CameraControl.Devices.Others;

public class GoPro9Camera : GoProBaseCamera
{
	private Dictionary<int, string> _subModes = new Dictionary<int, string>();

	private Dictionary<int, IEnumerable<int>> _subModesGroups = new Dictionary<int, IEnumerable<int>>();

	public PropertyValue<long> SubMode { get; set; }

	public override void Init(string address, JObject json, GoProBluetoothDevice bluetoothDevice)
	{
		_useOpenGoPro = true;
		base.BaseInit(address, json, bluetoothDevice);
		foreach (JToken item in (IEnumerable<JToken>)json["modes"])
		{
			_subModes.Add((int)item["id"], (string?)item["display_name"]);
		}
		foreach (JToken item2 in (IEnumerable<JToken>)json["ui_mode_groups"])
		{
			_subModesGroups.Add((int)item2["id"], item2["modes"].Values<int>());
		}
		Mode = new PropertyValue<long>
		{
			Tag = "-1",
			Available = true,
			IsEnabled = true
		};
		Mode.AddValues("Video", 1000L);
		Mode.AddValues("Photo", 1001L);
		Mode.AddValues("Timelapse", 1002L);
		Mode.ValueChanged += Mode_ValueChanged;
		Mode.ReloadValues();
		SubMode = new PropertyValue<long>
		{
			Tag = "144",
			Available = true,
			IsEnabled = false
		};
		SubMode.ValueChanged += Property_ValueChanged;
		AdvancedProperties.Insert(0, SubMode);
		ShutterSpeed = new PropertyValue<long>
		{
			Tag = "145",
			Available = true,
			IsEnabled = true
		};
		ShutterSpeed.ValueChanged += Property_ValueChanged;
		LoadJsonValues(ShutterSpeed, json);
		WhiteBalance = new PropertyValue<long>
		{
			Tag = "115",
			Available = true,
			IsEnabled = true
		};
		WhiteBalance.ValueChanged += Property_ValueChanged;
		LoadJsonValues(WhiteBalance, json);
		ExposureCompensation = new PropertyValue<long>
		{
			Tag = "118",
			Available = true,
			IsEnabled = true
		};
		ExposureCompensation.ValueChanged += Property_ValueChanged;
		LoadJsonValues(ExposureCompensation, json);
		AdvancedProperties.Add(LoadJsonValues("121", json));
		AdvancedProperties.Add(LoadJsonValues("122", json));
		AdvancedProperties.Add(LoadJsonValues("123", json));
		AdvancedProperties.Add(LoadJsonValues("59", json));
		AdvancedProperties.Add(LoadJsonValues("87", json));
		AdvancedProperties.Add(LoadJsonValues("88", json));
		AdvancedProperties.Add(LoadJsonValues("147", json));
		GetEvent();
	}

	private PropertyValue<long> LoadJsonValues(string tag, JObject json)
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Tag = tag,
			IsEnabled = true,
			Available = true
		};
		propertyValue.ValueChanged += Property_ValueChanged;
		LoadJsonValues(propertyValue, json);
		return propertyValue;
	}

	private void LoadJsonValues(PropertyValue<long> prop, JObject json)
	{
		prop.Clear();
		foreach (JToken item in (IEnumerable<JToken>)json["settings"])
		{
			if (!(((int)item["id"]).ToString() == prop.Tag))
			{
				continue;
			}
			prop.Name = (string?)item["display_name"];
			foreach (JToken item2 in (IEnumerable<JToken>)item["options"])
			{
				prop.AddValues((string?)item2["display_name"], (long)item2["id"]);
			}
		}
		prop.ReloadValues();
	}

	private void Property_ValueChanged(object sender, string key, long val)
	{
		GetJson($"/gopro/camera/setting?setting={((PropertyValue<long>)sender).Tag}&option={val}");
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		GetJson("/gopro/camera/presets/set_group?id=" + val);
	}

	public override void ReloadSubModes()
	{
		if (!_subModesGroups.ContainsKey((int)Mode.NumericValue))
		{
			return;
		}
		SubMode.Clear();
		foreach (int item in _subModesGroups[(int)Mode.NumericValue])
		{
			SubMode.AddValues(_subModes[item], item);
		}
		SubMode.ReloadValues();
	}

	public override void StartLiveView()
	{
		GetJson("/gopro/camera/stream/stop");
		JObject json = GetJson("/gopro/camera/stream/start");
	}

	public override void StopLiveView()
	{
		JObject json = GetJson("/gopro/camera/stream/stop");
	}
}
