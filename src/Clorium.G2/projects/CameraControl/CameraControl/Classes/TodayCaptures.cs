using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CameraControl.Classes;

// Camera icon tooltip: today's cuts and sessions counted from the session folders on disk, so photos taken
// before a restart, superfocus frames and sessions made from any screen are all included.
// A cut is a camera file (JPEG/RAW) written today anywhere under a session folder; results such as
// Superfocus.png or background-removed PNGs are not cuts. A session counts when it holds at least one of today's cuts.
public static class TodayCaptures
{
	public static readonly string[] CameraExtensions = { ".jpg", ".jpeg", ".cr2", ".cr3", ".crw", ".nef", ".nrw", ".arw", ".raf", ".orf", ".rw2", ".pef", ".dng", ".heif", ".heic" };

	public static (int Cuts, int Sessions) Count(IEnumerable<string> sessionFolders, DateTime today)
	{
		DateTime start = today.Date;
		DateTime end = start.AddDays(1);
		var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int sessions = 0;
		foreach (string folder in sessionFolders.Where(f => !string.IsNullOrWhiteSpace(f)).Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			bool sessionHasToday = false;
			foreach (string file in CameraFiles(folder))
			{
				DateTime written;
				try
				{
					written = File.GetLastWriteTime(file);
				}
				catch (Exception)
				{
					continue;
				}
				if (written < start || written >= end) continue;
				sessionHasToday = true;
				seenFiles.Add(file);
			}
			// a session folder nested inside another one still counts as its own session, but its files only once
			if (sessionHasToday) sessions++;
		}
		return (seenFiles.Count, sessions);
	}

	private static string Normalize(string folder)
	{
		try
		{
			return Path.GetFullPath(folder).TrimEnd('\\', '/');
		}
		catch (Exception)
		{
			return folder;
		}
	}

	private static IEnumerable<string> CameraFiles(string folder)
	{
		if (!Directory.Exists(folder)) yield break;
		var pending = new Stack<string>();
		pending.Push(folder);
		while (pending.Count > 0)
		{
			string dir = pending.Pop();
			string[] files, dirs;
			try
			{
				files = Directory.GetFiles(dir);
				dirs = Directory.GetDirectories(dir);
			}
			catch (Exception)
			{
				continue;
			}
			foreach (string f in files)
			{
				if (CameraExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())) yield return f;
			}
			foreach (string d in dirs) pending.Push(d);
		}
	}
}
