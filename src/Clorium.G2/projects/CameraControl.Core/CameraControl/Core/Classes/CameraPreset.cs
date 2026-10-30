using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Classes;

public class CameraPreset : BaseFieldClass
{
	public string Name { get; set; }

	public AsyncObservableCollection<ValuePair> Values { get; set; }

	public string FileName => Path.Combine(Settings.PresetFolder, Name + ".xml");

	public CameraPreset()
	{
		Values = new AsyncObservableCollection<ValuePair>();
	}

	public void Get(ICameraDevice camera)
	{
		Add(GetFrom(camera.Mode, "Mode"));
		Add(GetFrom(camera.CompressionSetting, "CompressionSetting"));
		Add(GetFrom(camera.ExposureCompensation, "ExposureCompensation"));
		Add(GetFrom(camera.ExposureMeteringMode, "ExposureMeteringMode"));
		Add(GetFrom(camera.FNumber, "FNumber"));
		Add(GetFrom(camera.IsoNumber, "IsoNumber"));
		Add(GetFrom(camera.ShutterSpeed, "ShutterSpeed"));
		Add(GetFrom(camera.WhiteBalance, "WhiteBalance"));
		Add(GetFrom(camera.ColorTemperature, "ColorTemperature"));
		Add(GetFrom(camera.FocusMode, "FocusMode"));
		Add(GetFrom(camera.LiveViewImageZoomRatio, "LiveViewImageZoomRatio"));
		Add(new ValuePair
		{
			Name = "CaptureInSdRam",
			Value = camera.CaptureInSdRam.ToString()
		});
		CameraProperty property = camera.LoadProperties();
		Add(new ValuePair
		{
			Name = "NoDownload",
			Value = property.NoDownload.ToString()
		});
		if (camera.AdvancedProperties == null)
		{
			return;
		}
		foreach (PropertyValue<long> propertyValue in camera.AdvancedProperties)
		{
			Add(GetFrom(propertyValue, propertyValue.Name));
		}
	}

	public void Verify(ICameraDevice camera)
	{
		try
		{
			camera.Mode.HaveError = camera.Mode.Value != GetValue("Mode");
			camera.CompressionSetting.HaveError = camera.CompressionSetting.Value != GetValue("CompressionSetting");
			camera.ExposureCompensation.HaveError = camera.ExposureCompensation.Value != GetValue("ExposureCompensation");
			camera.ExposureMeteringMode.HaveError = camera.ExposureMeteringMode.Value != GetValue("ExposureMeteringMode");
			camera.FNumber.HaveError = camera.FNumber.Value != GetValue("FNumber");
			camera.IsoNumber.HaveError = camera.IsoNumber.Value != GetValue("IsoNumber");
			camera.ShutterSpeed.HaveError = camera.ShutterSpeed.Value != GetValue("ShutterSpeed");
			camera.WhiteBalance.HaveError = camera.WhiteBalance.Value != GetValue("WhiteBalance");
			camera.ColorTemperature.HaveError = camera.ColorTemperature.Value != GetValue("ColorTemperature");
			camera.FocusMode.HaveError = camera.FocusMode.Value != GetValue("FocusMode");
			if (camera.AdvancedProperties == null)
			{
				return;
			}
			foreach (PropertyValue<long> propertyValue in camera.AdvancedProperties)
			{
				propertyValue.HaveError = propertyValue.Value != GetValue(propertyValue.Name);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to verify the preset " + Name, exception);
		}
	}

	public void Set(ICameraDevice camera)
	{
		Log.Debug("Loading preset for " + camera.DisplayName);
		SetIntern(camera);
	}

	public void SetIntern(ICameraDevice camera)
	{
		camera.IsBusy = true;
		SetTo(camera.Mode, "Mode");
		SetTo(camera.CompressionSetting, "CompressionSetting");
		SetTo(camera.ExposureCompensation, "ExposureCompensation");
		SetTo(camera.ExposureMeteringMode, "ExposureMeteringMode");
		SetTo(camera.FNumber, "FNumber");
		SetTo(camera.IsoNumber, "IsoNumber");
		SetTo(camera.ShutterSpeed, "ShutterSpeed");
		SetTo(camera.WhiteBalance, "WhiteBalance");
		SetTo(camera.ColorTemperature, "ColorTemperature");
		SetTo(camera.FocusMode, "FocusMode");
		SetTo(camera.LiveViewImageZoomRatio, "LiveViewImageZoomRatio");
		CameraProperty property = camera.LoadProperties();
		if (!string.IsNullOrEmpty(GetValue("NoDownload")) && bool.TryParse(GetValue("NoDownload"), out var val))
		{
			property.NoDownload = val;
		}
		if (camera.AdvancedProperties != null)
		{
			foreach (PropertyValue<long> propertyValue in camera.AdvancedProperties)
			{
				SetTo(propertyValue, propertyValue.Name);
			}
		}
		camera.IsBusy = false;
		Verify(camera);
	}

	public void SetTo(PropertyValue<int> value, string name)
	{
		if (value == null)
		{
			return;
		}
		foreach (ValuePair valuePair in Values)
		{
			if (valuePair.Name == name && value.IsEnabled && !string.IsNullOrEmpty(valuePair.Value))
			{
				value.SetValue(valuePair.Value);
				return;
			}
		}
		Thread.Sleep(25);
	}

	public void SetTo(PropertyValue<uint> value, string name)
	{
		if (value == null)
		{
			return;
		}
		foreach (ValuePair valuePair in Values)
		{
			if (valuePair.Name == name && value.IsEnabled && value.Value != valuePair.Value)
			{
				value.SetValue(valuePair.Value);
				break;
			}
		}
	}

	public void SetTo(PropertyValue<long> value, string name)
	{
		if (value == null)
		{
			Log.Debug("Value is null ");
			return;
		}
		foreach (ValuePair valuePair in Values)
		{
			if (valuePair.Name == name && value.IsEnabled && value.Value != valuePair.Value)
			{
				if (value.Values.Count == 0)
				{
					Log.Debug("No value list " + value.Name);
				}
				value.SetValue(valuePair.Value);
				break;
			}
		}
	}

	private ValuePair GetFrom(PropertyValue<uint> value, string name)
	{
		if (value == null)
		{
			return null;
		}
		return new ValuePair
		{
			Name = name,
			IsDisabled = value.IsEnabled,
			Value = value.Value
		};
	}

	private ValuePair GetFrom(PropertyValue<int> value, string name)
	{
		if (value == null)
		{
			return null;
		}
		return new ValuePair
		{
			Name = name,
			IsDisabled = value.IsEnabled,
			Value = value.Value
		};
	}

	private ValuePair GetFrom(PropertyValue<long> value, string name)
	{
		if (value == null)
		{
			return null;
		}
		return new ValuePair
		{
			Name = name,
			IsDisabled = value.IsEnabled,
			Value = value.Value
		};
	}

	public void Add(ValuePair pairParam)
	{
		if (pairParam == null)
		{
			return;
		}
		ValuePair pair = new ValuePair
		{
			Name = pairParam.Name,
			IsDisabled = pairParam.IsDisabled,
			Value = ((pairParam.Value == null) ? null : pairParam.Value.Replace('\0', ' '))
		};
		foreach (ValuePair valuePair in Values)
		{
			if (pair.Name == valuePair.Name)
			{
				valuePair.Value = pair.Value;
				valuePair.IsDisabled = pair.IsDisabled;
				return;
			}
		}
		Values.Add(pair);
	}

	public string GetValue(string name)
	{
		return (from valuePair in Values
			where valuePair.Name == name
			select valuePair.Value).FirstOrDefault();
	}

	public void Save(string filename)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(CameraPreset));
		Stream writer = new FileStream(filename, FileMode.Create);
		serializer.Serialize(writer, this);
		writer.Close();
	}

	public static CameraPreset Load(string filename)
	{
		CameraPreset cameraPreset = new CameraPreset();
		if (File.Exists(filename))
		{
			XmlSerializer mySerializer = new XmlSerializer(typeof(CameraPreset));
			FileStream myFileStream = new FileStream(filename, FileMode.Open);
			cameraPreset = (CameraPreset)mySerializer.Deserialize(myFileStream);
			myFileStream.Close();
		}
		return cameraPreset;
	}

	public override string ToString()
	{
		return Name;
	}
}
