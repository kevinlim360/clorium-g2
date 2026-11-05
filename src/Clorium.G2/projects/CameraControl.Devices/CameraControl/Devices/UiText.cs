using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CameraControl.Devices
{
	/// <summary>
	/// G2 user messages (errors, notices, prompts) in the selected language. Source code calls
	/// <c>UiText.T("M0001", "한글 문구")</c>; the Korean argument is the built-in wording and the fallback.
	/// Wording comes from Data\Messages.xml next to the program, which tools/KoreanText (messages-apply) writes from the
	/// reviewed Excel list:
	/// <code>&lt;messages&gt;&lt;m id="M0001"&gt;&lt;ko&gt;…&lt;/ko&gt;&lt;en&gt;…&lt;/en&gt;&lt;/m&gt;&lt;/messages&gt;</code>
	/// Korean is used when the program language (클로리움 설정 > 언어) starts with "ko", English otherwise.
	/// A missing file, id or English text falls back to the Korean argument. Never throws.
	/// </summary>
	public static class UiText
	{
		private sealed class Pair
		{
			public string Ko, En;
		}

		private static readonly object Gate = new object();
		private static Dictionary<string, Pair> _table;
		private static string _language = "ko";

		/// <summary>Language code as selected in the settings (ko, en_US, ...). Set by TranslationManager.LoadLanguage.</summary>
		public static string Language
		{
			get { return _language; }
			set { _language = string.IsNullOrWhiteSpace(value) ? "ko" : value.Trim(); }
		}

		public static bool IsKorean => _language.StartsWith("ko", StringComparison.OrdinalIgnoreCase);

		/// <summary>Messages file; tests may point it elsewhere. Changing it reloads.</summary>
		public static string FilePath
		{
			get { return _filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Messages.xml"); }
			set { lock (Gate) { _filePath = value; _table = null; } }
		}

		private static string _filePath;

		public static string T(string id, string korean)
		{
			Dictionary<string, Pair> table = Table();
			if (id != null && table.TryGetValue(id, out Pair pair))
			{
				string text = IsKorean ? pair.Ko : pair.En;
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
			return korean;
		}

		public static void Reload()
		{
			lock (Gate)
			{
				_table = null;
			}
		}

		private static Dictionary<string, Pair> Table()
		{
			Dictionary<string, Pair> table = _table;
			if (table != null)
			{
				return table;
			}
			lock (Gate)
			{
				if (_table != null)
				{
					return _table;
				}
				table = new Dictionary<string, Pair>(StringComparer.Ordinal);
				try
				{
					string path = FilePath;
					if (File.Exists(path))
					{
						XmlDocument doc = new XmlDocument();
						doc.Load(path);
						foreach (XmlNode node in doc.DocumentElement.ChildNodes)
						{
							XmlElement m = node as XmlElement;
							string id = m?.GetAttribute("id");
							if (string.IsNullOrEmpty(id))
							{
								continue;
							}
							table[id] = new Pair { Ko = m["ko"]?.InnerText, En = m["en"]?.InnerText };
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error("UiText : unable to read " + FilePath, ex);
				}
				_table = table;
				return table;
			}
		}
	}
}
