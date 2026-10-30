using System;
using System.Collections.Generic;
using System.Timers;

namespace CameraControl.Devices.Classes;

public class DelayedDelegate
{
	private static Timer runDelegates;

	private static Dictionary<Action, DateTime> delayedDelegates;

	static DelayedDelegate()
	{
		delayedDelegates = new Dictionary<Action, DateTime>();
		runDelegates = new Timer();
		runDelegates.Interval = 250.0;
		runDelegates.Elapsed += RunDelegates;
		runDelegates.Enabled = true;
		runDelegates.Start();
	}

	public static void Add(Action method, int delay)
	{
		delayedDelegates.Add(method, DateTime.Now + TimeSpan.FromMilliseconds((double)delay));
	}

	private static void RunDelegates(object sender, EventArgs e)
	{
		List<Action> list = new List<Action>();
		foreach (Action key in delayedDelegates.Keys)
		{
			if (DateTime.Now >= delayedDelegates[key])
			{
				key();
				list.Add(key);
			}
		}
		foreach (Action item in list)
		{
			delayedDelegates.Remove(item);
		}
	}
}
