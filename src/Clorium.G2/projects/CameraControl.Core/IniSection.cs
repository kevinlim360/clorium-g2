using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IniSection : IEnumerable<KeyValuePair<string, IniValue>>, IEnumerable, IDictionary<string, IniValue>, ICollection<KeyValuePair<string, IniValue>>
{
	private Dictionary<string, IniValue> values;

	private List<string> orderedKeys;

	public IniValue this[int index]
	{
		get
		{
			if (!Ordered)
			{
				throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
			}
			if (index < 0 || index >= orderedKeys.Count)
			{
				throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
			}
			return values[orderedKeys[index]];
		}
		set
		{
			if (!Ordered)
			{
				throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
			}
			if (index < 0 || index >= orderedKeys.Count)
			{
				throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
			}
			string key = orderedKeys[index];
			values[key] = value;
		}
	}

	public bool Ordered
	{
		get
		{
			return orderedKeys != null;
		}
		set
		{
			if (Ordered != value)
			{
				orderedKeys = (value ? new List<string>(values.Keys) : null);
			}
		}
	}

	public ICollection<string> Keys
	{
		get
		{
			ICollection<string> result;
			if (!Ordered)
			{
				ICollection<string> keys = values.Keys;
				result = keys;
			}
			else
			{
				ICollection<string> keys = orderedKeys;
				result = keys;
			}
			return result;
		}
	}

	public ICollection<IniValue> Values => values.Values;

	public int Count => values.Count;

	bool ICollection<KeyValuePair<string, IniValue>>.IsReadOnly => ((ICollection<KeyValuePair<string, IniValue>>)values).IsReadOnly;

	public IEqualityComparer<string> Comparer => values.Comparer;

	public IniValue this[string name]
	{
		get
		{
			if (values.TryGetValue(name, out var val))
			{
				return val;
			}
			return IniValue.Default;
		}
		set
		{
			if (Ordered && !orderedKeys.Contains(name, Comparer))
			{
				orderedKeys.Add(name);
			}
			values[name] = value;
		}
	}

	public int IndexOf(string key)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call IndexOf(string) on IniSection: section was not ordered.");
		}
		return IndexOf(key, 0, orderedKeys.Count);
	}

	public int IndexOf(string key, int index)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call IndexOf(string, int) on IniSection: section was not ordered.");
		}
		return IndexOf(key, index, orderedKeys.Count - index);
	}

	public int IndexOf(string key, int index, int count)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call IndexOf(string, int, int) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		if (count < 0)
		{
			throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
		}
		if (index + count > orderedKeys.Count)
		{
			throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
		}
		int end = index + count;
		for (int i = index; i < end; i++)
		{
			if (Comparer.Equals(orderedKeys[i], key))
			{
				return i;
			}
		}
		return -1;
	}

	public int LastIndexOf(string key)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call LastIndexOf(string) on IniSection: section was not ordered.");
		}
		return LastIndexOf(key, 0, orderedKeys.Count);
	}

	public int LastIndexOf(string key, int index)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call LastIndexOf(string, int) on IniSection: section was not ordered.");
		}
		return LastIndexOf(key, index, orderedKeys.Count - index);
	}

	public int LastIndexOf(string key, int index, int count)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call LastIndexOf(string, int, int) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		if (count < 0)
		{
			throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
		}
		if (index + count > orderedKeys.Count)
		{
			throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
		}
		int end = index + count;
		for (int i = end - 1; i >= index; i--)
		{
			if (Comparer.Equals(orderedKeys[i], key))
			{
				return i;
			}
		}
		return -1;
	}

	public void Insert(int index, string key, IniValue value)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call Insert(int, string, IniValue) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		values.Add(key, value);
		orderedKeys.Insert(index, key);
	}

	public void InsertRange(int index, IEnumerable<KeyValuePair<string, IniValue>> collection)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call InsertRange(int, IEnumerable<KeyValuePair<string, IniValue>>) on IniSection: section was not ordered.");
		}
		if (collection == null)
		{
			throw new ArgumentNullException("Value cannot be null." + Environment.NewLine + "Parameter name: collection");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		foreach (KeyValuePair<string, IniValue> kvp in collection)
		{
			Insert(index, kvp.Key, kvp.Value);
			index++;
		}
	}

	public void RemoveAt(int index)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call RemoveAt(int) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		string key = orderedKeys[index];
		orderedKeys.RemoveAt(index);
		values.Remove(key);
	}

	public void RemoveRange(int index, int count)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call RemoveRange(int, int) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		if (count < 0)
		{
			throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
		}
		if (index + count > orderedKeys.Count)
		{
			throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
		}
		for (int i = 0; i < count; i++)
		{
			RemoveAt(index);
		}
	}

	public void Reverse()
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call Reverse() on IniSection: section was not ordered.");
		}
		orderedKeys.Reverse();
	}

	public void Reverse(int index, int count)
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call Reverse(int, int) on IniSection: section was not ordered.");
		}
		if (index < 0 || index > orderedKeys.Count)
		{
			throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
		}
		if (count < 0)
		{
			throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
		}
		if (index + count > orderedKeys.Count)
		{
			throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
		}
		orderedKeys.Reverse(index, count);
	}

	public ICollection<IniValue> GetOrderedValues()
	{
		if (!Ordered)
		{
			throw new InvalidOperationException("Cannot call GetOrderedValues() on IniSection: section was not ordered.");
		}
		List<IniValue> list = new List<IniValue>();
		for (int i = 0; i < orderedKeys.Count; i++)
		{
			list.Add(values[orderedKeys[i]]);
		}
		return list;
	}

	public IniSection()
		: this(IniFile.DefaultComparer)
	{
	}

	public IniSection(IEqualityComparer<string> stringComparer)
	{
		values = new Dictionary<string, IniValue>(stringComparer);
	}

	public IniSection(Dictionary<string, IniValue> values)
		: this(values, IniFile.DefaultComparer)
	{
	}

	public IniSection(Dictionary<string, IniValue> values, IEqualityComparer<string> stringComparer)
	{
		this.values = new Dictionary<string, IniValue>(values, stringComparer);
	}

	public IniSection(IniSection values)
		: this(values, IniFile.DefaultComparer)
	{
	}

	public IniSection(IniSection values, IEqualityComparer<string> stringComparer)
	{
		this.values = new Dictionary<string, IniValue>(values.values, stringComparer);
	}

	public void Add(string key, IniValue value)
	{
		values.Add(key, value);
		if (Ordered)
		{
			orderedKeys.Add(key);
		}
	}

	public bool ContainsKey(string key)
	{
		return values.ContainsKey(key);
	}

	public bool Remove(string key)
	{
		bool ret = values.Remove(key);
		if (Ordered & ret)
		{
			for (int i = 0; i < orderedKeys.Count; i++)
			{
				if (Comparer.Equals(orderedKeys[i], key))
				{
					orderedKeys.RemoveAt(i);
					break;
				}
			}
		}
		return ret;
	}

	public bool TryGetValue(string key, out IniValue value)
	{
		return values.TryGetValue(key, out value);
	}

	void ICollection<KeyValuePair<string, IniValue>>.Add(KeyValuePair<string, IniValue> item)
	{
		((ICollection<KeyValuePair<string, IniValue>>)values).Add(item);
		if (Ordered)
		{
			orderedKeys.Add(item.Key);
		}
	}

	public void Clear()
	{
		values.Clear();
		if (Ordered)
		{
			orderedKeys.Clear();
		}
	}

	bool ICollection<KeyValuePair<string, IniValue>>.Contains(KeyValuePair<string, IniValue> item)
	{
		return ((ICollection<KeyValuePair<string, IniValue>>)values).Contains(item);
	}

	void ICollection<KeyValuePair<string, IniValue>>.CopyTo(KeyValuePair<string, IniValue>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<string, IniValue>>)values).CopyTo(array, arrayIndex);
	}

	bool ICollection<KeyValuePair<string, IniValue>>.Remove(KeyValuePair<string, IniValue> item)
	{
		bool ret = ((ICollection<KeyValuePair<string, IniValue>>)values).Remove(item);
		if (Ordered & ret)
		{
			for (int i = 0; i < orderedKeys.Count; i++)
			{
				if (Comparer.Equals(orderedKeys[i], item.Key))
				{
					orderedKeys.RemoveAt(i);
					break;
				}
			}
		}
		return ret;
	}

	public IEnumerator<KeyValuePair<string, IniValue>> GetEnumerator()
	{
		if (Ordered)
		{
			return GetOrderedEnumerator();
		}
		return values.GetEnumerator();
	}

	private IEnumerator<KeyValuePair<string, IniValue>> GetOrderedEnumerator()
	{
		for (int i = 0; i < orderedKeys.Count; i++)
		{
			yield return new KeyValuePair<string, IniValue>(orderedKeys[i], values[orderedKeys[i]]);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public static implicit operator IniSection(Dictionary<string, IniValue> dict)
	{
		return new IniSection(dict);
	}

	public static explicit operator Dictionary<string, IniValue>(IniSection section)
	{
		return section.values;
	}
}
