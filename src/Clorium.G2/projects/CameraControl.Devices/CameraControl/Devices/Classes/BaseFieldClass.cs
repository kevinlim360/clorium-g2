using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace CameraControl.Devices.Classes;

public class BaseFieldClass : INotifyPropertyChanged
{
	private Hashtable props = null;

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public virtual void BeginEdit()
	{
		PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
		if (props != null)
		{
			props.Clear();
		}
		else
		{
			props = new Hashtable(properties.Length - 1);
		}
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			if (!(null != propertyInfo.GetSetMethod()))
			{
				continue;
			}
			try
			{
				object value = propertyInfo.GetValue(this, null);
				if (!props.ContainsKey(propertyInfo.Name))
				{
					props.Add(propertyInfo.Name, value);
				}
			}
			catch
			{
			}
		}
	}

	public virtual void EndEdit()
	{
		props = null;
	}

	public virtual void CancelEdit()
	{
		if (props == null)
		{
			return;
		}
		PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < properties.Length; i++)
		{
			if (null != properties[i].GetSetMethod())
			{
				object value = props[properties[i].Name];
				properties[i].SetValue(this, value, null);
				NotifyPropertyChanged(properties[i].Name);
			}
		}
		props = null;
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}
