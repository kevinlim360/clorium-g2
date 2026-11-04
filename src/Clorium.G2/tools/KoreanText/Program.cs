// G2 한글 문구 목록: export lists every Korean message/term the product shows (C# string literals, XAML attribute values and
// text, the Korean language file), with its place; apply puts edited wording back at exactly those places.
//
//   KoreanText export <projectsRoot> <out-folder> [--lang <ko.xml>]
//       writes 한글문구_위치.csv (one row per place) and 한글문구_고유.csv (one row per distinct wording)
//   KoreanText apply <projectsRoot> <out-folder> [--lang <ko.xml>] [--dry-run]
//       reads the two CSVs back: 새 문구 of a place wins, else 새 문구 of its wording; writes the files and 반영결과.txt
//
// Comments are not listed. Text is shown as written in the source (C# escapes such as \n and {0} stay), and a change that
// drops or adds a placeholder ({0}, {name}, \n, %s) is refused. CSVs are UTF-8 with BOM; a CSV saved by Excel as ANSI (CP949)
// is read too.
using System.Text;
using System.Text.RegularExpressions;

static class Program
{
	sealed class Entry
	{
		public string Id, File, Kind, Context, Text, Line;
		public int LineNo, Offset; // Offset: start of Text in the file (chars)
		public string Wording => Text;
	}

	static readonly Regex Hangul = new Regex("[\uAC00-\uD7A3\u3131-\u318E]");
	static readonly Encoding Utf8Bom = new UTF8Encoding(true);
	const string LocationsCsv = "한글문구_위치.csv", UniqueCsv = "한글문구_고유.csv", ReportTxt = "반영결과.txt";

	static int Main(string[] args)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		if (args.Length < 3 || (args[0] != "export" && args[0] != "apply"))
		{
			Console.Error.WriteLine("usage: KoreanText export|apply <projectsRoot> <folder> [--lang <ko.xml>] [--dry-run]");
			return 2;
		}
		string root = Path.GetFullPath(args[1]), folder = Path.GetFullPath(args[2]);
		int li = Array.IndexOf(args, "--lang");
		string lang = li > 0 && li + 1 < args.Length ? Path.GetFullPath(args[li + 1]) : null;
		List<Entry> entries = Collect(root, lang);
		return args[0] == "export" ? Export(entries, folder) : Apply(entries, root, lang, folder, args.Contains("--dry-run"));
	}

	// ---------------- collect ----------------

	static List<Entry> Collect(string root, string lang)
	{
		var list = new List<Entry>();
		var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
			.Where(f => (f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
				&& !Regex.IsMatch(f, @"[\\/](bin|obj)[\\/]"))
			.OrderBy(f => f, StringComparer.Ordinal);
		foreach (string file in files)
		{
			string text = ReadText(file, out _);
			string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
			if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) CSharp(text, rel, list);
			else Xaml(text, rel, list);
		}
		if (lang != null && File.Exists(lang)) LanguageFile(ReadText(lang, out _), Path.GetRelativePath(root, lang).Replace('\\', '/'), list);
		for (int i = 0; i < list.Count; i++) list[i].Id = "K" + (i + 1).ToString("00000");
		return list;
	}

	static void Add(List<Entry> list, string text, string rel, int offset, int length, string kind, string context)
	{
		string value = text.Substring(offset, length);
		if (!Hangul.IsMatch(value)) return;
		int lineNo = 1 + CountNewlines(text, offset);
		int ls = text.LastIndexOf('\n', Math.Max(0, offset - 1)) + 1, le = text.IndexOf('\n', offset);
		if (le < 0) le = text.Length;
		string line = text.Substring(ls, le - ls).Trim();
		list.Add(new Entry { File = rel, Kind = kind, Context = context, Text = value, LineNo = lineNo, Offset = offset, Line = line.Length > 200 ? line.Substring(0, 200) + "…" : line });
	}

	static int CountNewlines(string s, int end)
	{
		int n = 0;
		for (int i = 0; i < end; i++) if (s[i] == '\n') n++;
		return n;
	}

	// C# lexer: skips comments and char literals; records the content of regular, verbatim, interpolated and raw strings.
	static void CSharp(string s, string rel, List<Entry> list)
	{
		int i = 0;
		while (i < s.Length)
		{
			char c = s[i];
			if (c == '/' && i + 1 < s.Length && s[i + 1] == '/') { i = s.IndexOf('\n', i); if (i < 0) return; continue; }
			if (c == '/' && i + 1 < s.Length && s[i + 1] == '*') { i = s.IndexOf("*/", i + 2, StringComparison.Ordinal); if (i < 0) return; i += 2; continue; }
			if (c == '\'') { i++; while (i < s.Length && s[i] != '\'') { if (s[i] == '\\') i++; i++; } i++; continue; }
			int p = i; bool verbatim = false, interp = false;
			while (p < s.Length && (s[p] == '@' || s[p] == '$')) { if (s[p] == '@') verbatim = true; else interp = true; p++; }
			if (p < s.Length && s[p] == '"' && (p == i || IsPrefixStart(s, i)))
			{
				if (p + 2 < s.Length && s[p + 1] == '"' && s[p + 2] == '"')
				{
					int q = p; while (q < s.Length && s[q] == '"') q++;
					int quotes = q - p, end = s.IndexOf(new string('"', quotes), q, StringComparison.Ordinal);
					if (end < 0) return;
					Add(list, s, rel, q, end - q, "C# 문자열", Context(s, i));
					i = end + quotes; continue;
				}
				int start = p + 1, j = start;
				while (j < s.Length)
				{
					if (verbatim) { if (s[j] == '"') { if (j + 1 < s.Length && s[j + 1] == '"') { j += 2; continue; } break; } }
					else { if (s[j] == '\\') { j += 2; continue; } if (s[j] == '"' || s[j] == '\n') break; }
					if (interp && s[j] == '{' && j + 1 < s.Length && s[j + 1] != '{') j = SkipHole(s, j);
					else j++;
				}
				Add(list, s, rel, start, j - start, "C# 문자열", Context(s, i));
				i = j + 1; continue;
			}
			i++;
		}
	}

	static bool IsPrefixStart(string s, int i) => i == 0 || !(char.IsLetterOrDigit(s[i - 1]) || s[i - 1] == '_');

	// {expr} inside an interpolated string, including nested strings.
	static int SkipHole(string s, int j)
	{
		int depth = 0;
		for (; j < s.Length; j++)
		{
			if (s[j] == '{') depth++;
			else if (s[j] == '}') { depth--; if (depth == 0) return j + 1; }
			else if (s[j] == '"') { j++; while (j < s.Length && s[j] != '"') { if (s[j] == '\\') j++; j++; } }
		}
		return j;
	}

	// What the string is used for, from the code in front of it on the same statement.
	static string Context(string s, int at)
	{
		int ls = s.LastIndexOf('\n', Math.Max(0, at - 1)) + 1;
		string before = s.Substring(ls, at - ls);
		if (Regex.IsMatch(before, @"\bLog\.(Debug|Error|Info)\b")) return "로그";
		if (Regex.IsMatch(before, @"MessageBox\.Show|ShowError|ShowMessage|Notify|DisplayMessage")) return "메시지 상자";
		if (Regex.IsMatch(before, @"\bthrow\b|Exception\(")) return "오류";
		if (Regex.IsMatch(before, @"\.(Text|Content|Title|Header|ToolTip)\s*=")) return "화면 문구";
		if (Regex.IsMatch(before, @"\b(case|==|!=|Contains|StartsWith|EndsWith|Equals)\b")) return "비교/키 (바꾸면 동작이 달라질 수 있음)";
		return "문자열";
	}

	static readonly Regex XmlComment = new Regex("<!--.*?-->", RegexOptions.Singleline);
	static readonly Regex XamlAttribute = new Regex("([A-Za-z_][\\w.:]*)\\s*=\\s*\"([^\"]*)\"");
	static readonly Regex XamlTextNode = new Regex(">([^<>]+)<");

	static void Xaml(string s, string rel, List<Entry> list)
	{
		string masked = XmlComment.Replace(s, m => new string(' ', m.Length));
		foreach (Match m in XamlAttribute.Matches(masked))
		{
			string name = m.Groups[1].Value;
			string kind = name == "Tag" ? "XAML 검색어(Tag)" : "XAML " + name;
			Add(list, s, rel, m.Groups[2].Index, m.Groups[2].Length, kind, name == "Tag" ? "설정 검색용 단어" : "화면 문구");
		}
		foreach (Match m in XamlTextNode.Matches(masked))
		{
			string inner = m.Groups[1].Value;
			if (inner.Trim().Length == 0) continue;
			int lead = inner.Length - inner.TrimStart().Length;
			Add(list, s, rel, m.Groups[1].Index + lead, inner.Trim().Length, "XAML 본문", "화면 문구");
		}
	}

	static readonly Regex LangString = new Regex("<string\\s+name=\"([^\"]+)\"\\s*>(.*?)</string>", RegexOptions.Singleline);

	static void LanguageFile(string s, string rel, List<Entry> list)
	{
		string masked = XmlComment.Replace(s, m => new string(' ', m.Length));
		foreach (Match m in LangString.Matches(masked))
			Add(list, s, rel, m.Groups[2].Index, m.Groups[2].Length, "언어 파일", m.Groups[1].Value);
	}

	// ---------------- export ----------------

	static int Export(List<Entry> entries, string folder)
	{
		Directory.CreateDirectory(folder);
		var groups = entries.GroupBy(e => e.Wording).OrderBy(g => g.First().Id).ToList();
		var wid = new Dictionary<string, string>();
		for (int i = 0; i < groups.Count; i++) wid[groups[i].Key] = "W" + (i + 1).ToString("0000");
		var loc = new StringBuilder();
		loc.AppendLine(Csv("ID", "문구ID", "파일", "줄", "종류", "용도/키", "현재 문구", "새 문구", "같은 문구 수", "원본 줄"));
		foreach (Entry e in entries)
			loc.AppendLine(Csv(e.Id, wid[e.Wording], e.File, e.LineNo.ToString(), e.Kind, e.Context, e.Text, "", groups.First(g => g.Key == e.Wording).Count().ToString(), e.Line));
		var uni = new StringBuilder();
		uni.AppendLine(Csv("문구ID", "현재 문구", "새 문구", "사용 횟수", "종류", "첫 위치"));
		foreach (var g in groups)
		{
			Entry f = g.First();
			uni.AppendLine(Csv(wid[g.Key], g.Key, "", g.Count().ToString(), string.Join(", ", g.Select(x => x.Kind).Distinct()), f.File + ":" + f.LineNo));
		}
		File.WriteAllText(Path.Combine(folder, LocationsCsv), loc.ToString(), Utf8Bom);
		File.WriteAllText(Path.Combine(folder, UniqueCsv), uni.ToString(), Utf8Bom);
		Console.WriteLine($"places {entries.Count}, distinct wordings {groups.Count}");
		foreach (var k in entries.GroupBy(e => e.Kind).OrderByDescending(g => g.Count())) Console.WriteLine($"  {k.Key}: {k.Count()}");
		return 0;
	}

	static string Csv(params string[] cells) => string.Join(",", cells.Select(c => "\"" + (c ?? "").Replace("\"", "\"\"") + "\""));

	// ---------------- apply ----------------

	static int Apply(List<Entry> entries, string root, string lang, string folder, bool dryRun)
	{
		var locRows = ReadCsv(Path.Combine(folder, LocationsCsv));
		var uniRows = File.Exists(Path.Combine(folder, UniqueCsv)) ? ReadCsv(Path.Combine(folder, UniqueCsv)) : new List<Dictionary<string, string>>();
		var byWording = uniRows.Where(r => Get(r, "새 문구").Length > 0).ToDictionary(r => Get(r, "현재 문구"), r => Get(r, "새 문구"));
		var report = new StringBuilder();
		var changes = new List<(Entry e, string newText)>();
		int skipped = 0;
		// Rows are matched to the current source by file + wording (+ nearest line), so earlier edits elsewhere do not break it.
		foreach (var row in locRows)
		{
			string file = Get(row, "파일"), old = Get(row, "현재 문구"), wanted = Get(row, "새 문구");
			if (wanted.Length == 0 && !byWording.TryGetValue(old, out wanted)) continue;
			if (wanted == old) continue;
			int.TryParse(Get(row, "줄"), out int line);
			Entry e = entries.Where(x => x.File == file && x.Text == old).OrderBy(x => Math.Abs(x.LineNo - line)).FirstOrDefault();
			if (e == null) { skipped++; report.AppendLine($"건너뜀 {Get(row, "ID")} {file}:{line} — 지금 소스에서 '{old}'를 찾지 못함 (이미 바뀌었거나 줄이 이동)"); continue; }
			if (changes.Any(c => c.e == e)) continue;
			string problem = Check(e, old, wanted);
			if (problem != null) { skipped++; report.AppendLine($"건너뜀 {Get(row, "ID")} {file}:{e.LineNo} — {problem}"); continue; }
			changes.Add((e, Encode(e, wanted)));
		}
		foreach (var fileChanges in changes.GroupBy(c => c.e.File))
		{
			string path = Path.GetFullPath(Path.Combine(root, fileChanges.Key));
			string text = ReadText(path, out Encoding enc);
			foreach (var (e, newText) in fileChanges.OrderByDescending(c => c.e.Offset))
			{
				if (text.Substring(e.Offset, e.Text.Length) != e.Text) throw new InvalidOperationException("source changed while applying: " + e.File);
				text = text.Remove(e.Offset, e.Text.Length).Insert(e.Offset, newText);
				report.AppendLine($"반영 {e.File}:{e.LineNo}  '{e.Text}'  →  '{newText}'");
			}
			if (!dryRun) File.WriteAllText(path, text, enc);
		}
		report.Insert(0, $"{(dryRun ? "[미리보기] " : "")}반영 {changes.Count}곳, 건너뜀 {skipped}곳{Environment.NewLine}{Environment.NewLine}");
		File.WriteAllText(Path.Combine(folder, ReportTxt), report.ToString(), Utf8Bom);
		Console.WriteLine($"applied {changes.Count}, skipped {skipped}{(dryRun ? " (dry run)" : "")} -> {Path.Combine(folder, ReportTxt)}");
		return 0;
	}

	static readonly Regex Placeholder = new Regex(@"\{[^{}]*\}|\\[nrt""\\]|%[sd]|&#\d+;|&\w+;");

	static string Check(Entry e, string old, string wanted)
	{
		var a = Placeholder.Matches(old).Select(m => m.Value).OrderBy(x => x).ToList();
		var b = Placeholder.Matches(wanted).Select(m => m.Value).OrderBy(x => x).ToList();
		if (!a.SequenceEqual(b)) return $"자리표시({string.Join(" ", a)})가 새 문구({string.Join(" ", b)})와 다름";
		if (e.Kind == "C# 문자열" && wanted.Contains('\n')) return "C# 문자열에는 줄바꿈 대신 \\n을 써 주세요";
		return null;
	}

	// Characters that would end the string or break the markup are escaped for the kind of place.
	static string Encode(Entry e, string wanted)
	{
		if (e.Kind == "C# 문자열")
			return Regex.Replace(wanted, "(?<!\\\\)\"", "\\\"");
		string x = Regex.Replace(wanted, "&(?!#?\\w+;)", "&amp;").Replace("<", "&lt;");
		return e.Kind.StartsWith("XAML") && !e.Kind.Contains("본문") ? x.Replace("\"", "&quot;") : x;
	}

	static string Get(Dictionary<string, string> r, string k) => r.TryGetValue(k, out string v) ? v ?? "" : "";

	static List<Dictionary<string, string>> ReadCsv(string path)
	{
		byte[] bytes = File.ReadAllBytes(path);
		string text;
		try { text = new UTF8Encoding(false, true).GetString(bytes); }
		catch (DecoderFallbackException) { text = Encoding.GetEncoding(949).GetString(bytes); }
		text = text.TrimStart('\uFEFF');
		var rows = new List<List<string>>();
		var cur = new List<string>(); var cell = new StringBuilder(); bool quoted = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (quoted)
			{
				if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else quoted = false; }
				else cell.Append(c);
			}
			else if (c == '"') quoted = true;
			else if (c == ',') { cur.Add(cell.ToString()); cell.Clear(); }
			else if (c == '\n' || c == '\r') { if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; cur.Add(cell.ToString()); cell.Clear(); if (cur.Count > 1 || cur[0].Length > 0) rows.Add(cur); cur = new List<string>(); }
			else cell.Append(c);
		}
		if (cell.Length > 0 || cur.Count > 0) { cur.Add(cell.ToString()); rows.Add(cur); }
		var head = rows[0];
		return rows.Skip(1).Select(r => head.Select((h, k) => (h, v: k < r.Count ? r[k] : "")).ToDictionary(t => t.h, t => t.v)).ToList();
	}

	static string ReadText(string path, out Encoding enc)
	{
		byte[] b = File.ReadAllBytes(path);
		bool bom = b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
		enc = new UTF8Encoding(bom);
		return new UTF8Encoding(false).GetString(b, bom ? 3 : 0, b.Length - (bom ? 3 : 0));
	}
}
