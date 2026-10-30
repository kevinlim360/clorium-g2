using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class IniFile : IEnumerable<KeyValuePair<string, IniSection>>, IEnumerable, IDictionary<string, IniSection>, ICollection<KeyValuePair<string, IniSection>>
{
	private class CaseInsensitiveStringComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			return string.Compare(x, y, ignoreCase: true) == 0;
		}

		public int GetHashCode(string obj)
		{
			return obj.ToLowerInvariant().GetHashCode();
		}
	}

	private Dictionary<string, IniSection> sections;

	public IEqualityComparer<string> StringComparer;

	public bool SaveEmptySections;

	public static IEqualityComparer<string> DefaultComparer = new CaseInsensitiveStringComparer();

	public ICollection<string> Keys => sections.Keys;

	public ICollection<IniSection> Values => sections.Values;

	public int Count => sections.Count;

	bool ICollection<KeyValuePair<string, IniSection>>.IsReadOnly => ((ICollection<KeyValuePair<string, IniSection>>)sections).IsReadOnly;

	public IniSection this[string section]
	{
		get
		{
			if (sections.TryGetValue(section, out var s))
			{
				return s;
			}
			s = new IniSection(StringComparer);
			sections[section] = s;
			return s;
		}
		set
		{
			IniSection v = value;
			if (v.Comparer != StringComparer)
			{
				v = new IniSection(v, StringComparer);
			}
			sections[section] = v;
		}
	}

	public IniFile()
		: this(DefaultComparer)
	{
	}

	public IniFile(IEqualityComparer<string> stringComparer)
	{
		StringComparer = stringComparer;
		sections = new Dictionary<string, IniSection>(StringComparer);
	}

	public void Save(string path, FileMode mode = FileMode.Create)
	{
		using FileStream stream = new FileStream(path, mode, FileAccess.Write);
		Save(stream);
	}

	public void Save(Stream stream)
	{
		using StreamWriter writer = new StreamWriter(stream);
		Save(writer);
	}

	public void Save(StreamWriter writer)
	{
		foreach (KeyValuePair<string, IniSection> section in sections)
		{
			if (section.Value.Count <= 0 && !SaveEmptySections)
			{
				continue;
			}
			writer.WriteLine($"[{section.Key.Trim()}]");
			foreach (KeyValuePair<string, IniValue> kvp in section.Value)
			{
				writer.WriteLine($"{kvp.Key}={kvp.Value}");
			}
			writer.WriteLine("");
		}
	}

	public void Load(string path, bool ordered = false)
	{
		using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
		Load(stream, ordered);
	}

	public void Load(Stream stream, bool ordered = false)
	{
		using StreamReader reader = new StreamReader(stream);
		Load(reader, ordered);
	}

	public void Load(StreamReader reader, bool ordered = false)
	{
		IniSection section = null;
		while (!reader.EndOfStream)
		{
			string line = reader.ReadLine();
			if (line == null)
			{
				continue;
			}
			string trimStart = line.TrimStart();
			if (trimStart.Length <= 0)
			{
				continue;
			}
			string key;
			IniValue val;
			if (trimStart[0] == '[')
			{
				int sectionEnd = trimStart.IndexOf(']');
				if (sectionEnd > 0)
				{
					string sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
					section = new IniSection(StringComparer)
					{
						Ordered = ordered
					};
					sections[sectionName] = section;
				}
			}
			else if (section != null && trimStart[0] != ';' && LoadValue(line, out key, out val))
			{
				section[key] = val;
			}
		}
	}

	private bool LoadValue(string line, out string key, out IniValue val)
	{
		int assignIndex = line.IndexOf('=');
		if (assignIndex <= 0)
		{
			key = null;
			val = null;
			return false;
		}
		key = line.Substring(0, assignIndex).Trim();
		string value = line.Substring(assignIndex + 1);
		val = new IniValue(value);
		return true;
	}

	public bool ContainsSection(string section)
	{
		return sections.ContainsKey(section);
	}

	public bool TryGetSection(string section, out IniSection result)
	{
		return sections.TryGetValue(section, out result);
	}

	bool IDictionary<string, IniSection>.TryGetValue(string key, out IniSection value)
	{
		return TryGetSection(key, out value);
	}

	public bool Remove(string section)
	{
		return sections.Remove(section);
	}

	public IniSection Add(string section, Dictionary<string, IniValue> values, bool ordered = false)
	{
		return Add(section, new IniSection(values, StringComparer)
		{
			Ordered = ordered
		});
	}

	public IniSection Add(string section, IniSection value)
	{
		if (value.Comparer != StringComparer)
		{
			value = new IniSection(value, StringComparer);
		}
		sections.Add(section, value);
		return value;
	}

	public IniSection Add(string section, bool ordered = false)
	{
		IniSection value = new IniSection(StringComparer)
		{
			Ordered = ordered
		};
		sections.Add(section, value);
		return value;
	}

	void IDictionary<string, IniSection>.Add(string key, IniSection value)
	{
		Add(key, value);
	}

	bool IDictionary<string, IniSection>.ContainsKey(string key)
	{
		return ContainsSection(key);
	}

	void ICollection<KeyValuePair<string, IniSection>>.Add(KeyValuePair<string, IniSection> item)
	{
		((ICollection<KeyValuePair<string, IniSection>>)sections).Add(item);
	}

	public void Clear()
	{
		sections.Clear();
	}

	bool ICollection<KeyValuePair<string, IniSection>>.Contains(KeyValuePair<string, IniSection> item)
	{
		return ((ICollection<KeyValuePair<string, IniSection>>)sections).Contains(item);
	}

	void ICollection<KeyValuePair<string, IniSection>>.CopyTo(KeyValuePair<string, IniSection>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<string, IniSection>>)sections).CopyTo(array, arrayIndex);
	}

	bool ICollection<KeyValuePair<string, IniSection>>.Remove(KeyValuePair<string, IniSection> item)
	{
		return ((ICollection<KeyValuePair<string, IniSection>>)sections).Remove(item);
	}

	public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator()
	{
		return sections.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public string GetContents()
	{
		using MemoryStream stream = new MemoryStream();
		Save(stream);
		stream.Flush();
		StringBuilder builder = new StringBuilder(Encoding.UTF8.GetString(stream.ToArray()));
		return builder.ToString();
	}
}
