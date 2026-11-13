using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

// G2 촬영음: the sounds in <G2 folder>\Data\ShutterSounds named "NN_이름.wav|mp3" (01_아이폰_셔터음.mp3 ...). The setting
// Settings.ShutterSound keeps the number NN (default 1). WAV and MP3 both play through WPF's MediaPlayer on the UI thread;
// without the folder the original Data\takephoto.wav is used.
public static class ShutterSounds
{
	public sealed class Sound
	{
		public int Number { get; set; }

		public string Name { get; set; }

		public string Path { get; set; }

		public string Label => Number + ". " + Name;
	}

	private static readonly string[] Extensions = { ".wav", ".mp3" };

	private static List<Sound> _all;

	// Players still sounding; held here so the garbage collector does not stop one mid-sound.
	private static readonly HashSet<MediaPlayer> _playing = new HashSet<MediaPlayer>();

	// Replaced by tests: the folder is next to the running program.
	internal static string Folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? AppDomain.CurrentDomain.BaseDirectory) ?? "", "Data", "ShutterSounds");

	public static IReadOnlyList<Sound> All => _all ?? (_all = Scan(Folder));

	internal static List<Sound> Scan(string folder)
	{
		List<Sound> list = new List<Sound>();
		if (!Directory.Exists(folder)) return list;
		foreach (string file in Directory.GetFiles(folder))
		{
			Match m = Regex.Match(System.IO.Path.GetFileName(file), "^(\\d+)_(.+)\\.(wav|mp3)$", RegexOptions.IgnoreCase);
			if (!m.Success || list.Any(s => s.Number == int.Parse(m.Groups[1].Value))) continue;
			list.Add(new Sound { Number = int.Parse(m.Groups[1].Value), Name = m.Groups[2].Value.Replace('_', ' '), Path = file });
		}
		return list.OrderBy(s => s.Number).ToList();
	}

	// The sound for a setting value: that number, else the first one, else null.
	public static Sound Find(int number) => All.FirstOrDefault(s => s.Number == number) ?? All.FirstOrDefault();

	public static void Play(int number)
	{
		Sound sound = Find(number);
		if (sound == null)
		{
			PlayLegacy();
			return;
		}
		// A new player per shot: rewinding one shared player (Stop, Position = 0, Play) right after the previous sound
		// sometimes played nothing, so quick F/B/D/촬영 presses lost their sound now and then.
		Dispatch(() =>
		{
			MediaPlayer player = new MediaPlayer { Volume = 1 };
			_playing.Add(player);
			player.MediaEnded += (s, e) => Release(player);
			player.MediaFailed += (s, e) =>
			{
				Log.Debug("ShutterSound failed : " + sound.Path + " " + e.ErrorException?.Message);
				Release(player);
			};
			player.Open(new Uri(sound.Path));
			player.Play();
		});
	}

	private static void Release(MediaPlayer player)
	{
		_playing.Remove(player);
		player.Close();
	}

	private static void Dispatch(Action action)
	{
		Action safe = () =>
		{
			try { action(); }
			catch (Exception ex) { Log.Debug("ShutterSound : " + ex.Message); }
		};
		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher == null) safe();
		else if (dispatcher.CheckAccess()) safe();
		else dispatcher.BeginInvoke(safe);
	}

	private static void PlayLegacy()
	{
		try
		{
			string basedir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (basedir != null)
			{
				new SoundPlayer(System.IO.Path.Combine(basedir, "Data", "takephoto.wav")).Play();
			}
		}
		catch (Exception message)
		{
			Log.Debug(message);
		}
	}
}
