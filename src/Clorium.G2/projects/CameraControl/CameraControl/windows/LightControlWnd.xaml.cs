using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class LightControlWnd : Window, IComponentConnector, IStyleConnector
{
	private static LightControlWnd _instance;

	private bool skipSliderEvent = false;

	public static LightControlWnd Instance
	{
		get
		{
			if (_instance == null)
			{
				MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
				_instance = new LightControlWnd(mainWindow);
				_instance.Owner = mainWindow;
				_instance.Closed += (object s, EventArgs e) =>
				{
					_instance = null;
				};
			}
			return _instance;
		}
	}

	public LightControlWnd(MainWindow owner)
	{
		InitializeComponent();
		Owner = owner;
		if (!DesignerProperties.GetIsInDesignMode(this))
		{
			LightControlModel vm = new LightControlModel(owner);
			DataContext = vm;
		}
		Loaded += Window_Loaded;
		AllLightOn();
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		if (DataContext is IDisposable disposable)
		{
			disposable.Dispose();
		}
		DataContext = null;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		sliderCCT1.Minimum = ServiceProvider.Settings.LightTemperatureMin;
		sliderCCT1.Maximum = ServiceProvider.Settings.LightTemperatureMax;
		sliderCCT2.Minimum = ServiceProvider.Settings.LightTemperatureMin;
		sliderCCT2.Maximum = ServiceProvider.Settings.LightTemperatureMax;
		sliderCCT3.Minimum = ServiceProvider.Settings.LightTemperatureMin;
		sliderCCT3.Maximum = ServiceProvider.Settings.LightTemperatureMax;
		sliderCCT4.Minimum = ServiceProvider.Settings.LightTemperatureMin;
		sliderCCT4.Maximum = ServiceProvider.Settings.LightTemperatureMax;
		sliderBrightness1.ValueChanged += Slider_ValueChanged;
		sliderBrightness2.ValueChanged += Slider_ValueChanged;
		sliderBrightness3.ValueChanged += Slider_ValueChanged;
		sliderBrightness4.ValueChanged += Slider_ValueChanged;
		sliderCCT1.ValueChanged += Slider_ValueChanged;
		sliderCCT2.ValueChanged += Slider_ValueChanged;
		sliderCCT3.ValueChanged += Slider_ValueChanged;
		sliderCCT4.ValueChanged += Slider_ValueChanged;
	}

	public void AllLightOn()
	{
		if (!ServiceProvider.Branding.ControlLighting || !(DataContext is LightControlModel vm))
		{
			return;
		}
		string presetName = ServiceProvider.Settings.LightPresetName;
		if (!string.IsNullOrEmpty(presetName) && !(presetName == "None"))
		{
			Preset targetPreset = vm.Presets.FirstOrDefault((Preset p) => p.PresetName == presetName);
			if (targetPreset != null)
			{
				skipSliderEvent = true;
				sliderBrightness1.Value = int.Parse(targetPreset.Lights[0].Brightness);
				sliderCCT1.Value = int.Parse(targetPreset.Lights[0].CCT);
				SimulateBrightnessPreview(brightnessPreview1, (byte)sliderBrightness1.Value);
				SimulateColorPreview(colorPreview1, (int)sliderCCT1.Value);
				sliderBrightness2.Value = int.Parse(targetPreset.Lights[1].Brightness);
				sliderCCT2.Value = int.Parse(targetPreset.Lights[1].CCT);
				SimulateBrightnessPreview(brightnessPreview2, (byte)sliderBrightness2.Value);
				SimulateColorPreview(colorPreview2, (int)sliderCCT2.Value);
				sliderBrightness3.Value = int.Parse(targetPreset.Lights[2].Brightness);
				sliderCCT3.Value = int.Parse(targetPreset.Lights[2].CCT);
				SimulateBrightnessPreview(brightnessPreview3, (byte)sliderBrightness3.Value);
				SimulateColorPreview(colorPreview3, (int)sliderCCT3.Value);
				sliderBrightness4.Value = int.Parse(targetPreset.Lights[3].Brightness);
				sliderCCT4.Value = int.Parse(targetPreset.Lights[3].CCT);
				SimulateBrightnessPreview(brightnessPreview4, (byte)sliderBrightness4.Value);
				SimulateColorPreview(colorPreview4, (int)sliderCCT4.Value);
				skipSliderEvent = false;
				vm.LightOn(targetPreset);
			}
		}
	}

	// Recovery inference: ignore programmatic slider updates, then mirror the user-selected brightness or color into the light model and preview.
	private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (!skipSliderEvent && DataContext is LightControlModel vm)
		{
			if (sender == sliderBrightness1)
			{
				byte brightness = (byte)sliderBrightness1.Value;
				vm.Light1.Brightness = brightness.ToString();
				SimulateBrightnessPreview(brightnessPreview1, brightness);
			}
			else if (sender == sliderBrightness2)
			{
				byte brightness2 = (byte)sliderBrightness2.Value;
				vm.Light2.Brightness = brightness2.ToString();
				SimulateBrightnessPreview(brightnessPreview2, brightness2);
			}
			else if (sender == sliderBrightness3)
			{
				byte brightness3 = (byte)sliderBrightness3.Value;
				vm.Light3.Brightness = brightness3.ToString();
				SimulateBrightnessPreview(brightnessPreview3, brightness3);
			}
			else if (sender == sliderBrightness4)
			{
				byte brightness4 = (byte)sliderBrightness4.Value;
				vm.Light4.Brightness = brightness4.ToString();
				SimulateBrightnessPreview(brightnessPreview4, brightness4);
			}
			else if (sender == sliderCCT1)
			{
				int cct = (int)sliderCCT1.Value;
				vm.Light1.CCT = cct.ToString();
				SimulateColorPreview(colorPreview1, cct);
			}
			else if (sender == sliderCCT2)
			{
				int cct2 = (int)sliderCCT2.Value;
				vm.Light2.CCT = cct2.ToString();
				SimulateColorPreview(colorPreview2, cct2);
			}
			else if (sender == sliderCCT3)
			{
				int cct3 = (int)sliderCCT3.Value;
				vm.Light3.CCT = cct3.ToString();
				SimulateColorPreview(colorPreview3, cct3);
			}
			else if (sender == sliderCCT4)
			{
				int cct4 = (int)sliderCCT4.Value;
				vm.Light4.CCT = cct4.ToString();
				SimulateColorPreview(colorPreview4, cct4);
			}
			Preset newPreset = new Preset
			{
				PresetName = vm.DefaultLightPresetName,
				Lights = new List<LightSetting>
				{
					new LightSetting
					{
						Name = vm.Light1.Name,
						Brightness = vm.Light1.Brightness,
						CCT = vm.Light1.CCT
					},
					new LightSetting
					{
						Name = vm.Light2.Name,
						Brightness = vm.Light2.Brightness,
						CCT = vm.Light2.CCT
					},
					new LightSetting
					{
						Name = vm.Light3.Name,
						Brightness = vm.Light3.Brightness,
						CCT = vm.Light3.CCT
					},
					new LightSetting
					{
						Name = vm.Light4.Name,
						Brightness = vm.Light4.Brightness,
						CCT = vm.Light4.CCT
					}
				}
			};
			vm.ArtNet.Send(newPreset);
		}
	}

	private void SimulateBrightnessPreview(Border previewTarget, byte brightness)
	{
		byte alpha = 128;
		double intensity = (double)(int)brightness / 255.0;
		byte baseColor = (byte)(30.0 + 225.0 * intensity);
		previewTarget.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor, baseColor, baseColor));
	}

	private void SimulateColorPreview(Border previewTarget, int Kelvin_cct)
	{
		if (DataContext is LightControlModel vm)
		{
			byte cct = vm.ArtNet.KelvinToDmx(Kelvin_cct);
			byte a = 128;
			byte r = (byte)(255 - cct);
			byte g = (byte)(200 - Math.Abs(cct - 128));
			byte b = cct;
			previewTarget.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
		}
	}

	private void Btn_RemovePreset_Click(object sender, RoutedEventArgs e)
	{
		if (DataContext is LightControlModel { SelectedPreset: not null } vm && !(vm.SelectedPreset.PresetName == "Default"))
		{
			if (vm.DefaultLightPresetName == vm.SelectedPreset.PresetName)
			{
				vm.DefaultLightPresetName = "None";
				ServiceProvider.Settings.LightPresetName = vm.DefaultLightPresetName;
			}
			vm.Presets.Remove(vm.SelectedPreset);
			vm.SelectedPreset = null;
			PresetManager.SavePresets(vm.Presets);
		}
	}

	private void Btn_ApplyPreset_Click(object sender, RoutedEventArgs e)
	{
		if (DataContext is LightControlModel { SelectedPreset: not null } vm)
		{
			vm.DefaultLightPresetName = vm.SelectedPreset.PresetName;
			ServiceProvider.Settings.LightPresetName = vm.DefaultLightPresetName;
			AllLightOn();
		}
	}

	private void Btn_Copy_Click(object sender, RoutedEventArgs e)
	{
		skipSliderEvent = true;
		Slider slider = sliderBrightness2;
		Slider slider2 = sliderBrightness3;
		double num = (sliderBrightness4.Value = sliderBrightness1.Value);
		double value2 = (slider2.Value = num);
		slider.Value = value2;
		Slider slider3 = sliderCCT2;
		Slider slider4 = sliderCCT3;
		num = (sliderCCT4.Value = sliderCCT1.Value);
		value2 = (slider4.Value = num);
		slider3.Value = value2;
		skipSliderEvent = false;
		if (DataContext is LightControlModel { Light2: var light, Light3: var light2, Light4: var light3 } vm)
		{
			string text = (light3.Brightness = vm.Light1.Brightness);
			string brightness2 = (light2.Brightness = text);
			light.Brightness = brightness2;
			LightSetting light4 = vm.Light2;
			LightSetting light5 = vm.Light3;
			text = (vm.Light4.CCT = vm.Light1.CCT);
			brightness2 = (light5.CCT = text);
			light4.CCT = brightness2;
		}
		SimulateBrightnessPreview(brightnessPreview2, (byte)sliderBrightness2.Value);
		SimulateColorPreview(colorPreview2, (int)sliderCCT2.Value);
		SimulateBrightnessPreview(brightnessPreview3, (byte)sliderBrightness3.Value);
		SimulateColorPreview(colorPreview3, (int)sliderCCT3.Value);
		SimulateBrightnessPreview(brightnessPreview4, (byte)sliderBrightness4.Value);
		SimulateColorPreview(colorPreview4, (int)sliderCCT4.Value);
	}

	private void Btn_LightOn_Click(object sender, RoutedEventArgs e)
	{
		AllLightOn();
	}

	private void Btn_LightOff_Click(object sender, RoutedEventArgs e)
	{
		AllLightOff();
	}

	private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DataContext is LightControlModel { SelectedPreset: not null } vm)
		{
			vm.NewPreset = new Preset
			{
				PresetName = vm.SelectedPreset.PresetName
			};
		}
	}

	private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		Btn_ApplyPreset_Click(sender, e);
	}

	public void AllLightOff()
	{
		if (ServiceProvider.Branding.ControlLighting && DataContext is LightControlModel vm)
		{
			vm.LightOff();
		}
	}
}
