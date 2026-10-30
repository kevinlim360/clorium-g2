using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Translation;

public static class TranslationManager
{
	private static Dictionary<string, string> _translations;

	private static readonly string _path;

	private static Dictionary<string, string> TranslatedStrings;

	private static AsyncObservableCollection<TranslationLangDesc> _availableLangs;

	public static AsyncObservableCollection<TranslationLangDesc> AvailableLangs
	{
		get
		{
			if (_availableLangs == null)
			{
				_availableLangs = new AsyncObservableCollection<TranslationLangDesc>();
			}
			return _availableLangs;
		}
		set
		{
			_availableLangs = value;
		}
	}

	public static Dictionary<string, string> Strings
	{
		get
		{
			if (_translations == null)
			{
				_translations = new Dictionary<string, string>();
				Type transType = typeof(TranslationStrings);
				FieldInfo[] fields = transType.GetFields(BindingFlags.Static | BindingFlags.Public);
				FieldInfo[] array = fields;
				foreach (FieldInfo field2 in array)
				{
					_translations.Add(field2.Name, field2.GetValue(transType).ToString());
				}
			}
			return _translations;
		}
	}

	static TranslationManager()
	{
		_path = string.Empty;
		TranslatedStrings = new Dictionary<string, string>();
		AvailableLangs = new AsyncObservableCollection<TranslationLangDesc>();
		try
		{
			_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string[] files = Directory.GetFiles(Path.Combine(_path, "Languages"));
			string[] array = files;
			foreach (string file in array)
			{
				try
				{
					CultureInfo cult = CultureInfo.GetCultureInfo(Path.GetFileNameWithoutExtension(file).Replace('_', '-'));
					AvailableLangs.Add(new TranslationLangDesc
					{
						Value = Path.GetFileNameWithoutExtension(file),
						Name = cult.EnglishName + " - " + cult.NativeName
					});
				}
				catch (Exception exception)
				{
					Log.Error("Error loading language", exception);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static int LoadLanguage(string lang_code)
	{
		XmlDocument doc = new XmlDocument();
		TranslatedStrings = new Dictionary<string, string>();
		string langPath = "";
		try
		{
			langPath = Path.Combine(_path, "Languages", lang_code.Replace('-', '_') + ".xml");
			doc.Load(langPath);
		}
		catch (Exception)
		{
			if (lang_code == "en-US")
			{
				return 0;
			}
			return LoadLanguage("en-US");
		}
		foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
		{
			if (stringEntry.NodeType == XmlNodeType.Element)
			{
				try
				{
					TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("name").Value, stringEntry.InnerText);
				}
				catch (Exception)
				{
				}
			}
		}
		Type TransType = typeof(TranslationStrings);
		FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Static | BindingFlags.Public);
		FieldInfo[] array = fieldInfos;
		foreach (FieldInfo fi in array)
		{
			if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
			{
				TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[1] { TranslatedStrings[fi.Name] });
			}
		}
		return TranslatedStrings.Count;
	}

	public static string GetTranslation(string key)
	{
		if (Strings.ContainsKey(key))
		{
			return Strings[key];
		}
		if (TranslatedStrings.ContainsKey(key))
		{
			return TranslatedStrings[key];
		}
		return key;
	}

	public static bool HaveTranslation(string key)
	{
		return Strings.ContainsKey(key) || TranslatedStrings.ContainsKey(key);
	}
}
