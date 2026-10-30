using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PortableDeviceLib;

namespace CameraControl.Devices.Classes;

public class PropertyValue<T> : BaseFieldClass
{
	public delegate void ValueChangedEventHandler(object sender, string key, T val);

	private Dictionary<string, T> _valuesDictionary;

	private Dictionary<string, string> _replaceValues;

	private AsyncObservableCollection<T> _numericValues = new AsyncObservableCollection<T>();

	private AsyncObservableCollection<string> _values = new AsyncObservableCollection<string>();

	private bool _notifyValuChange = true;

	private readonly object _syncRoot = new object();

	private uint _code;

	private string _name;

	private string _value;

	private T _numericValue;

	private bool _isEnabled;

	private bool _haveError;

	private bool _available;

	public string Tag { get; set; }

	public uint Code
	{
		get
		{
			return _code;
		}
		set
		{
			_code = value;
			NotifyPropertyChanged("Code");
		}
	}

	public bool Available
	{
		get
		{
			return _available;
		}
		set
		{
			_available = value;
			NotifyPropertyChanged("Available");
		}
	}

	public bool HaveError
	{
		get
		{
			return _haveError;
		}
		set
		{
			_haveError = value;
			NotifyPropertyChanged("HaveError");
			NotifyPropertyChanged("ErrorColor");
		}
	}

	public string ErrorColor => HaveError ? "Red" : "Transparent";

	public Type SubType { get; set; }

	public bool DisableIfWrongValue { get; set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			NotifyPropertyChanged("Name");
		}
	}

	public string Value
	{
		get
		{
			if (_value != null && _replaceValues != null && _replaceValues.Count > 0)
			{
				using IEnumerator<KeyValuePair<string, string>> enumerator = _replaceValues.Where((KeyValuePair<string, string> replaceValue) => replaceValue.Value == _value).GetEnumerator();
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Key;
				}
			}
			return _value;
		}
		set
		{
			_value = value;
			if (_value != null)
			{
				if (_replaceValues != null && _replaceValues.Count > 0 && _replaceValues.ContainsKey(_value))
				{
					_value = _replaceValues[_value];
				}
				HaveError = false;
				if (ValueChanged != null && _notifyValuChange && _valuesDictionary.ContainsKey(_value))
				{
					OnValueChanged(this, _value, _valuesDictionary[_value]);
				}
			}
			NotifyPropertyChanged("Value");
		}
	}

	public T NumericValue
	{
		get
		{
			return _numericValue;
		}
		set
		{
			_numericValue = value;
			NotifyPropertyChanged("NumericValue");
		}
	}

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
			NotifyPropertyChanged("IsEnabled");
		}
	}

	public AsyncObservableCollection<string> Values => _values;

	public AsyncObservableCollection<T> NumericValues => _numericValues;

	public event ValueChangedEventHandler ValueChanged;

	public void NextValue()
	{
		lock (_syncRoot)
		{
			if (Values == null || Values.Count == 0 || !IsEnabled)
			{
				return;
			}
			int num = Values.IndexOf(Value);
			if (num >= 0)
			{
				num++;
				if (num < Values.Count)
				{
					Value = Values[num];
				}
			}
		}
	}

	public void PrevValue()
	{
		lock (_syncRoot)
		{
			if (Values != null && Values.Count != 0 && IsEnabled)
			{
				int num = Values.IndexOf(Value);
				num--;
				if (num >= 0 && num < Values.Count)
				{
					Value = Values[num];
				}
			}
		}
	}

	public void OnValueChanged(object sender, string key, T val)
	{
		Thread thread = new Thread(OnValueChangedThread);
		thread.Name = "SetProperty thread " + Name;
		thread.Start(new object[3] { sender, key, val });
		thread.Join(200);
	}

	public void OnValueChangedThread(object obj)
	{
		lock (_syncRoot)
		{
			object[] array = obj as object[];
			int num = 5;
			bool flag;
			do
			{
				flag = false;
				try
				{
					object sender = array[0];
					string key = array[1] as string;
					T val = (T)array[2];
					ValueChanged(sender, key, val);
				}
				catch (DeviceException ex)
				{
					if ((ex.ErrorCode == 2147942570u || ex.ErrorCode == 8217) && num > 0)
					{
						num--;
						flag = true;
						Thread.Sleep(100);
					}
				}
			}
			while (flag);
		}
	}

	public PropertyValue()
	{
		_valuesDictionary = new Dictionary<string, T>();
		_replaceValues = new Dictionary<string, string>();
		DisableIfWrongValue = false;
		IsEnabled = true;
		Available = true;
	}

	public void SetValue(T o, bool notifyValuChange)
	{
		_notifyValuChange = notifyValuChange;
		SetValue(o);
		_notifyValuChange = true;
	}

	public void SetValue(T o)
	{
		lock (_syncRoot)
		{
			NumericValue = o;
			foreach (KeyValuePair<string, T> item in _valuesDictionary)
			{
				if (EqualityComparer<T>.Default.Equals(item.Value, o))
				{
					Value = item.Key;
					break;
				}
			}
		}
	}

	public void Filter(List<T> values)
	{
		Dictionary<string, T> dictionary = new Dictionary<string, T>(_valuesDictionary);
		_valuesDictionary.Clear();
		foreach (KeyValuePair<string, T> item in dictionary)
		{
			if (values.Contains(item.Value))
			{
				_valuesDictionary.Add(item.Key, item.Value);
			}
		}
		_values = new AsyncObservableCollection<string>(_valuesDictionary.Keys);
		_numericValues = new AsyncObservableCollection<T>(_valuesDictionary.Values);
		NotifyPropertyChanged("Values");
	}

	public void SetValue()
	{
		Value = Value;
	}

	public void SetValue(string o, bool notifyValuChange = true)
	{
		lock (_syncRoot)
		{
			foreach (KeyValuePair<string, T> item in _valuesDictionary)
			{
				if (item.Key == o)
				{
					_notifyValuChange = notifyValuChange;
					Value = item.Key;
					_notifyValuChange = true;
					break;
				}
			}
		}
	}

	public void SetValue(byte[] ba, bool notifyValuChange)
	{
		_notifyValuChange = notifyValuChange;
		SetValue(ba);
		_notifyValuChange = true;
	}

	public void SetValue(MTPDataResponse data, bool notifyValuChange)
	{
		_notifyValuChange = notifyValuChange;
		SetValue(data.Data);
		_notifyValuChange = true;
	}

	public void SetValue(byte[] ba)
	{
		if (ba == null || ba.Length < 1)
		{
			return;
		}
		if (typeof(T) == typeof(int))
		{
			int num = ((ba.Length == 1) ? ba[0] : BitConverter.ToInt16(ba, 0));
			SetValue((T)(object)num);
		}
		if (typeof(T) == typeof(uint))
		{
			uint num2 = BitConverter.ToUInt16(ba, 0);
			SetValue((T)(object)num2);
		}
		if (typeof(T) == typeof(long) && SubType == typeof(sbyte))
		{
			long num3 = Convert.ToSByte(ba[0]);
			SetValue((T)(object)num3);
			return;
		}
		if (typeof(T) == typeof(long) && ba.Length == 1)
		{
			long num4 = ba[0];
			SetValue((T)(object)num4);
			return;
		}
		if (typeof(T) == typeof(long) && ba.Length == 2 && SubType == typeof(ushort))
		{
			long num5 = BitConverter.ToUInt16(ba, 0);
			SetValue((T)(object)num5);
			return;
		}
		if (typeof(T) == typeof(long) && ba.Length == 2)
		{
			long num6 = BitConverter.ToInt16(ba, 0);
			SetValue((T)(object)num6);
			return;
		}
		if (typeof(T) == typeof(long) && ba.Length == 4)
		{
			long num7 = BitConverter.ToUInt32(ba, 0);
			SetValue((T)(object)num7);
			return;
		}
		if (typeof(T) == typeof(long))
		{
			long num8 = BitConverter.ToInt64(ba, 0);
			SetValue((T)(object)num8);
		}
		if (typeof(T) == typeof(uint))
		{
			uint num9 = BitConverter.ToUInt16(ba, 0);
			SetValue((T)(object)num9);
		}
	}

	public void AddValues(string key, T value)
	{
		if (!_valuesDictionary.ContainsKey(key))
		{
			_valuesDictionary.Add(key, value);
		}
	}

	public void ReloadValues()
	{
		_values = new AsyncObservableCollection<string>(_valuesDictionary.Keys);
		_numericValues = new AsyncObservableCollection<T>(_valuesDictionary.Values);
		NotifyPropertyChanged("Values");
	}

	public void GetValues()
	{
		lock (_syncRoot)
		{
			string value = Value;
			_values.Clear();
			NotifyPropertyChanged("Values");
			_values = new AsyncObservableCollection<string>(_valuesDictionary.Keys);
			if (_replaceValues != null && _replaceValues.Count > 0)
			{
				for (int i = 0; i < _values.Count; i++)
				{
					if (!_replaceValues.ContainsValue(_values[i]))
					{
						continue;
					}
					foreach (KeyValuePair<string, string> replaceValue in _replaceValues)
					{
						if (replaceValue.Value == _values[i])
						{
							_values[i] = replaceValue.Key;
						}
					}
				}
			}
			_numericValues = new AsyncObservableCollection<T>(_valuesDictionary.Values);
			NotifyPropertyChanged("Values");
			Value = value;
		}
	}

	public void ReplaceValue(string oldVal, string newVal)
	{
		lock (_syncRoot)
		{
			if (!_replaceValues.ContainsKey(newVal))
			{
				_replaceValues.Add(newVal, oldVal);
			}
		}
	}

	public void Clear(bool notify = true)
	{
		_valuesDictionary.Clear();
		_values.Clear();
		_numericValues.Clear();
		if (notify)
		{
			NotifyPropertyChanged("Values");
		}
	}
}
