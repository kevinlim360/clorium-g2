using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;

namespace CameraControl.Devices.Classes;

public class AsyncObservableCollection<T> : ObservableCollection<T>
{
	private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

	private static object _syncLock = new object();

	public override event NotifyCollectionChangedEventHandler CollectionChanged;

	public AsyncObservableCollection()
	{
		enableCollectionSynchronization(this, _syncLock);
	}

	public AsyncObservableCollection(IEnumerable<T> list)
		: base(list)
	{
		enableCollectionSynchronization(this, _syncLock);
	}

	private void RaiseCollectionChanged(object param)
	{
		try
		{
			base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
		}
		catch (Exception)
		{
		}
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		if (SynchronizationContext.Current == _synchronizationContext)
		{
			RaisePropertyChanged(e);
		}
		else if (_synchronizationContext != null)
		{
			_synchronizationContext.Post(RaisePropertyChanged, e);
		}
	}

	private void RaisePropertyChanged(object param)
	{
		base.OnPropertyChanged((PropertyChangedEventArgs)param);
	}

	protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		using (BlockReentrancy())
		{
			try
			{
				NotifyCollectionChangedEventHandler notifyCollectionChangedEventHandler = CollectionChanged;
				if (notifyCollectionChangedEventHandler == null)
				{
					return;
				}
				Dispatcher val = (from _003C_003Eh__TransparentIdentifier0 in notifyCollectionChangedEventHandler.GetInvocationList().Cast<NotifyCollectionChangedEventHandler>().Select((NotifyCollectionChangedEventHandler nh) =>
					{
						object? target = nh.Target;
						return new
						{
							nh = nh,
							dpo = (DispatcherObject)((target is DispatcherObject) ? target : null)
						};
					})
					where _003C_003Eh__TransparentIdentifier0.dpo != null
					select _003C_003Eh__TransparentIdentifier0.dpo.Dispatcher).FirstOrDefault();
				if (val != null && !val.CheckAccess())
				{
					val.Invoke((DispatcherPriority)8, (Delegate)(Action)(() =>
					{
						OnCollectionChanged(e);
					}));
					return;
				}
				Delegate[] invocationList = notifyCollectionChangedEventHandler.GetInvocationList();
				for (int num = 0; num < invocationList.Length; num++)
				{
					NotifyCollectionChangedEventHandler notifyCollectionChangedEventHandler2 = (NotifyCollectionChangedEventHandler)invocationList[num];
					notifyCollectionChangedEventHandler2(this, e);
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private static void enableCollectionSynchronization(IEnumerable collection, object lockObject)
	{
		MethodInfo method = typeof(BindingOperations).GetMethod("EnableCollectionSynchronization", new Type[2]
		{
			typeof(IEnumerable),
			typeof(object)
		});
		if (method != null)
		{
			method.Invoke(null, new object[2] { collection, lockObject });
		}
	}
}
