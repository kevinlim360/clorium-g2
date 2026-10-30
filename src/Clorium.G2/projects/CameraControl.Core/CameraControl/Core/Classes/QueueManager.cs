using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

public class QueueManager
{
	private readonly BackgroundWorker _worker;

	private object _locker = new object();

	private BlockingCollection<IQueueItem> PriorityQueue { get; set; }

	private BlockingCollection<IQueueItem> Queue { get; set; }

	private BlockingCollection<IQueueItem> LowPriorityQueue { get; set; }

	public QueueManager()
	{
		PriorityQueue = new BlockingCollection<IQueueItem>();
		Queue = new BlockingCollection<IQueueItem>();
		LowPriorityQueue = new BlockingCollection<IQueueItem>();
		_worker = new BackgroundWorker();
		_worker.DoWork += _worker_DoWork;
	}

	private void _worker_DoWork(object sender, DoWorkEventArgs e)
	{
		while (true)
		{
			try
			{
				if (PriorityQueue.Count > 0 && PriorityQueue.TryTake(out var item))
				{
					item.Execute(this);
					continue;
				}
				if (Queue.Count > 0 && Queue.TryTake(out item))
				{
					item.Execute(this);
					continue;
				}
				if (LowPriorityQueue.Count > 0 && LowPriorityQueue.TryTake(out item))
				{
					item.Execute(this);
				}
				if (Queue.Count == 0 && PriorityQueue.Count == 0 && LowPriorityQueue.Count == 0)
				{
					Thread.Sleep(100);
				}
			}
			catch (Exception exception)
			{
				Log.Error("Queue manager error:", exception);
			}
		}
	}

	public void Clear()
	{
		while (PriorityQueue.Count > 0)
		{
			Queue.TryTake(out var _);
		}
		while (Queue.Count > 0)
		{
			Queue.TryTake(out var _);
		}
		while (LowPriorityQueue.Count > 0)
		{
			Queue.TryTake(out var _);
		}
	}

	public void AddWithPriority(IQueueItem item)
	{
		PriorityQueue.Add(item);
		if (!_worker.IsBusy)
		{
			_worker.RunWorkerAsync();
		}
	}

	public void AddWithLowPriority(IQueueItem item)
	{
		LowPriorityQueue.Add(item);
		if (!_worker.IsBusy)
		{
			_worker.RunWorkerAsync();
		}
	}

	public void Add(IQueueItem item)
	{
		Queue.Add(item);
		if (!_worker.IsBusy)
		{
			_worker.RunWorkerAsync();
		}
	}
}
