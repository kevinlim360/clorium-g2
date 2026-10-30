using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CameraControl.ViewModel;

public static class PresetManager
{
	private const string FilePath = "D:\\Clorium Data\\Clorium_LightPresets.json";

	public static void SavePresets(IEnumerable<Preset> presets)
	{
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};
		string json = JsonSerializer.Serialize(presets, options);
		File.WriteAllText("D:\\Clorium Data\\Clorium_LightPresets.json", json);
	}

	public static List<Preset> LoadPresets()
	{
		if (!File.Exists("D:\\Clorium Data\\Clorium_LightPresets.json"))
		{
			return new List<Preset>();
		}
		string json = File.ReadAllText("D:\\Clorium Data\\Clorium_LightPresets.json");
		return JsonSerializer.Deserialize<List<Preset>>(json) ?? new List<Preset>();
	}
}
