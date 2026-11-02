using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;

namespace CameraControl.windows;

// G2 UI: 클로리움 설정 — 설정프리셋 A/B/C and settings search.
public partial class SettingsWnd
{
	// Settings saved into a preset slot. Identity/password fields and server switches
	// (which need their start/stop handlers) are deliberately left out.
	private static readonly string[] G2PresetProperties =
	{
			"APIHost",
			"AlarmSoundIndex",
			"AlarmWaitTime",
			"AutoPreview",
			"AutoPreviewJpgOnly",
			"Autorotate",
			"BGTargetColor",
			"Barcode_DeviceName",
			"CanonFocusStepWait",
			"DetectionType",
			"DiskSize",
			"DontLoadThumbnails",
			"DownloadFolder",
			"EasyLiveViewControl",
			"ElapsedDownloadFolder",
			"ExternalDeviceWaitForCapture",
			"ExternalDeviceWaitForFocus",
			"ExternalViewerArgs",
			"ExternalViewerPath",
			"FullScreenInSecondaryMonitor",
			"LargeFocusStep",
			"LargeFocusStepCanon",
			"LightPowerSaveTime",
			"LiveViewFreezeTimeOut",
			"LoadFullRespreview",
			"MediumFocusStep",
			"MediumFocusStepCanon",
			"MetaScale",
			"ModeBarcodeCapture",
			"ModeBarcodeSession",
			"ModeBgJpeg",
			"ModeBgPng",
			"ModeLighting",
			"MotionBlockSize",
			"Preview",
			"PreviewSeconds",
			"ResolutionChange",
			"ResolutionPixelHeight",
			"ResolutionPixelWidth",
			"RotateIndex",
			"SessionFileKeepDays",
			"ShowAPICommunication",
			"ShowDebugTest",
			"ShowFrontBack",
			"ShowFullscreenControls",
			"ShowLiveView",
			"ShowPreset",
			"ShowRemoveBg",
			"ShowStatistics",
			"ShowThumbUpDown",
			"SmalFocusStep",
			"SmallFocusStepCanon",
			"SoundTime",
			"StartMinimized",
			"StartupWithWindows",
			"UseAPICommunication",
			"UseAestheticImprovement",
			"UseAlarm",
			"UseAlwaysBackup",
			"UseAutoSizing",
			"UseBarcodeScanner",
			"UseCloriumLight",
			"UseDatabase",
			"UseDebugLog",
			"UseDefectCapture",
			"UseExternalViewer",
			"UseLEDDimmer",
			"UseLargeText",
			"UseMonstarDeck",
			"UseParallelTransfer",
			"UsePlaySound",
			"UseSuperfocus",
			"WebCam_DeviceName"
	};

	private static string G2PresetFile => Path.Combine(Settings.DataFolder, "G2SettingsPresets.xml");

	private sealed class G2PresetStore
	{
		public string Selected { get; set; } = "";
		public Dictionary<string, Dictionary<string, string>> Slots { get; set; } = new Dictionary<string, Dictionary<string, string>>();
	}

	private G2PresetStore _g2Presets = new G2PresetStore();

	private void G2Settings_ContentRendered(object sender, EventArgs e)
	{
		_g2Presets = G2LoadPresets();
		G2RefreshSlots(null);
	}

	// File format (XML, no extra dependency):
	// <G2SettingsPresets selected="A"><slot key="A"><v n="UseDefectCapture">True</v>...</slot>...</G2SettingsPresets>
	private static G2PresetStore G2LoadPresets()
	{
		var store = new G2PresetStore();
		try
		{
			if (File.Exists(G2PresetFile))
			{
				XElement root = XDocument.Load(G2PresetFile).Root;
				store.Selected = (string)root?.Attribute("selected") ?? "";
				foreach (XElement slot in root?.Elements("slot") ?? Enumerable.Empty<XElement>())
				{
					string key = (string)slot.Attribute("key");
					if (string.IsNullOrEmpty(key))
					{
						continue;
					}
					var map = new Dictionary<string, string>();
					foreach (XElement v in slot.Elements("v"))
					{
						string n = (string)v.Attribute("n");
						if (!string.IsNullOrEmpty(n))
						{
							map[n] = (string)v.Attribute("null") == "1" ? null : v.Value;
						}
					}
					store.Slots[key] = map;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("G2 settings presets: read failed", ex);
		}
		return store;
	}

	private void G2SavePresets()
	{
		var root = new XElement("G2SettingsPresets", new XAttribute("selected", _g2Presets.Selected ?? ""));
		foreach (KeyValuePair<string, Dictionary<string, string>> slot in _g2Presets.Slots)
		{
			var x = new XElement("slot", new XAttribute("key", slot.Key));
			foreach (KeyValuePair<string, string> kv in slot.Value)
			{
				x.Add(kv.Value == null ? new XElement("v", new XAttribute("n", kv.Key), new XAttribute("null", "1")) : new XElement("v", new XAttribute("n", kv.Key), kv.Value));
			}
			root.Add(x);
		}
		string tmp = G2PresetFile + ".tmp";
		new XDocument(root).Save(tmp);
		if (File.Exists(G2PresetFile))
		{
			File.Replace(tmp, G2PresetFile, null);
		}
		else
		{
			File.Move(tmp, G2PresetFile);
		}
	}

	private static Dictionary<string, string> G2Snapshot()
	{
		Settings s = ServiceProvider.Settings;
		var map = new Dictionary<string, string>();
		foreach (string name in G2PresetProperties)
		{
			PropertyInfo p = typeof(Settings).GetProperty(name);
			if (p == null || !p.CanRead || !p.CanWrite)
			{
				continue;
			}
			object v = p.GetValue(s);
			map[name] = v == null ? null : TypeDescriptor.GetConverter(p.PropertyType).ConvertToInvariantString(v);
		}
		return map;
	}

	private static int G2Apply(Dictionary<string, string> map)
	{
		Settings s = ServiceProvider.Settings;
		int n = 0;
		foreach (KeyValuePair<string, string> kv in map)
		{
			if (!G2PresetProperties.Contains(kv.Key))
			{
				continue;
			}
			PropertyInfo p = typeof(Settings).GetProperty(kv.Key);
			if (p == null || !p.CanWrite)
			{
				continue;
			}
			try
			{
				object v = kv.Value == null ? null : TypeDescriptor.GetConverter(p.PropertyType).ConvertFromInvariantString(kv.Value);
				if (!Equals(p.GetValue(s), v))
				{
					p.SetValue(s, v);
					n++;
				}
			}
			catch (Exception ex)
			{
				Log.Error("G2 settings presets: cannot apply " + kv.Key, ex);
			}
		}
		return n;
	}

	private static bool G2Same(Dictionary<string, string> map)
	{
		Dictionary<string, string> now = G2Snapshot();
		return map != null && now.All(kv => map.TryGetValue(kv.Key, out string t) && string.Equals(t, kv.Value, StringComparison.Ordinal));
	}

	private void G2RefreshSlots(string message)
	{
		foreach (string key in new[] { "A", "B", "C" })
		{
			var radio = FindName("G2Slot" + key) as RadioButton;
			var state = FindName("G2SlotState" + key) as TextBlock;
			var badge = FindName("G2SlotKey" + key) as Border;
			bool has = _g2Presets.Slots.TryGetValue(key, out Dictionary<string, string> map) && map != null;
			bool selected = _g2Presets.Selected == key;
			if (radio != null)
			{
				radio.IsChecked = selected;
			}
			if (state != null)
			{
				state.Text = !has ? "비어 있음" : (selected ? (G2Same(map) ? "선택됨 · 저장값과 같음" : "선택됨 · 바뀐 값 있음") : "저장됨");
				state.Foreground = new SolidColorBrush(!has ? Color.FromRgb(0x56, 0x60, 0x6C) : (selected ? Color.FromRgb(0x1F, 0x5F, 0xD6) : Color.FromRgb(0x3A, 0x46, 0x54)));
			}
			if (badge != null)
			{
				badge.Background = new SolidColorBrush(selected ? Color.FromRgb(0x1F, 0x5F, 0xD6) : (has ? Color.FromRgb(0x8F, 0xA8, 0xD6) : Color.FromRgb(0xC3, 0xC8, 0xCF)));
			}
		}
		if (message != null && G2Status != null)
		{
			G2Status.Text = message;
		}
	}

	private void G2PresetLoad_Click(object sender, RoutedEventArgs e)
	{
		string key = (sender as FrameworkElement)?.Tag as string;
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		_g2Presets.Selected = key;
		string msg;
		if (_g2Presets.Slots.TryGetValue(key, out Dictionary<string, string> map) && map != null)
		{
			int n = G2Apply(map);
			msg = "설정 " + key + "로 바꿨습니다 (" + n + "개 항목). 확정하려면 저장을 누르세요.";
		}
		else
		{
			msg = "설정 " + key + "은 비어 있습니다. 아래 '" + key + " 저장하기'로 지금 설정을 저장하세요.";
		}
		try
		{
			G2SavePresets();
		}
		catch (Exception ex)
		{
			Log.Error("G2 settings presets: save failed", ex);
		}
		G2RefreshSlots(msg);
	}

	private void G2PresetSave_Click(object sender, RoutedEventArgs e)
	{
		string key = (sender as FrameworkElement)?.Tag as string;
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		_g2Presets.Slots[key] = G2Snapshot();
		_g2Presets.Selected = key;
		try
		{
			G2SavePresets();
			G2RefreshSlots("지금 설정을 설정 " + key + "에 저장했습니다.");
		}
		catch (Exception ex)
		{
			Log.Error("G2 settings presets: save failed", ex);
			G2RefreshSlots("설정 " + key + " 저장에 실패했습니다: " + ex.Message);
		}
	}

	// ---- search: show only the rows/cards whose text matches, across all categories ----
	private void G2Search_TextChanged(object sender, TextChangedEventArgs e)
	{
		string q = (G2Search.Text ?? "").Trim();
		G2SearchHint.Visibility = q.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
		TabItem first = null;
		foreach (object o in G2Tabs.Items)
		{
			if (!(o is TabItem tab))
			{
				continue;
			}
			int hits = G2Filter(tab.Content as DependencyObject, q, false);
			if (q.Length == 0 || hits > 0)
			{
				tab.ClearValue(UIElement.VisibilityProperty);
			}
			else
			{
				tab.Visibility = Visibility.Collapsed;
			}
			if (hits > 0 && first == null)
			{
				first = tab;
			}
		}
		if (q.Length > 0)
		{
			if (first != null)
			{
				G2Tabs.SelectedItem = first;
				G2Status.Text = "'" + q + "' 검색 결과";
			}
			else
			{
				G2Status.Text = "'" + q + "'와 맞는 설정이 없습니다.";
			}
		}
		else
		{
			G2Status.Text = "";
		}
	}

	// Rows and cards carry their searchable text in Tag. A card that matches shows whole;
	// otherwise only its matching rows show. Returns the number of visible matches.
	private static int G2Filter(DependencyObject node, string q, bool parentMatched)
	{
		if (node == null)
		{
			return 0;
		}
		int hits = 0;
		foreach (object child in LogicalTreeHelper.GetChildren(node))
		{
			if (!(child is FrameworkElement fe))
			{
				continue;
			}
			string tag = fe.Tag as string;
			bool searchable = fe is Border && tag != null && (fe.Style == fe.TryFindResource("G2SetRow") || fe.Style == fe.TryFindResource("G2Card") || tag == "설정프리셋");
			if (searchable)
			{
				bool self = q.Length == 0 || parentMatched || tag.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
				int inner = G2Filter(fe, q, self);
				bool show = q.Length == 0 || self || inner > 0;
				// Rows hidden by product branding keep their own Visibility binding untouched.
				if (!BindingOperations.IsDataBound(fe, UIElement.VisibilityProperty))
				{
					if (show)
					{
						fe.ClearValue(UIElement.VisibilityProperty);
					}
					else
					{
						fe.Visibility = Visibility.Collapsed;
					}
				}
				if (q.Length > 0 && show)
				{
					hits += Math.Max(1, inner);
				}
			}
			else
			{
				hits += G2Filter(fe, q, parentMatched);
			}
		}
		return hits;
	}
}
