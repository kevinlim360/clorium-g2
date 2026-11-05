// 사용자 메시지 (오류 · 안내 · 확인 문구) in Korean and English.
//
//   KoreanText messages-export <projectsRoot> <out.xlsx> [--suggest <suggestions.tsv>] [--candidates <out.tsv>]
//       lists every user message string in the C# source (message boxes, errors, status and screen text; not logs, not
//       keys/paths/comparisons) as an Excel workbook: one row per wording (시트 "메시지") and one per place (시트 "위치").
//       --suggest merges proposed wording (tab separated: 현재 문구 \t 대상 O/X \t 새 한글 \t English \t 메모).
//   KoreanText messages-apply <projectsRoot> <reviewed.xlsx> --messages <Data/Messages.xml> [--dry-run]
//       puts every 대상 O wording back at its places: the literal becomes UiText.T("M0001", "새 한글") (an interpolated
//       string becomes string.Format(UiText.T(..., "…{0}…"), expr)), and Data/Messages.xml gets the Korean and English
//       text that the program shows for the selected language. Applying again only updates the wording.
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

static partial class Program
{
	const string Call = "global::CameraControl.Devices.UiText.T";

	sealed class Message
	{
		public string Id, Template, Target = "O", Ko, En, Note = "", Kinds;
		public List<Place> Places = new List<Place>();
	}

	sealed class Place
	{
		public Entry Entry;
		public string Template, ConvertedId, Blocked;
		public List<string> Holes = new List<string>();
		public string Pid;
	}

	static readonly Regex ConvertedBefore = new Regex(@"UiText\.T\(\s*""(M\d{4,}(?:_\d+)?)""\s*,\s*$");

	static int Messages(string[] args)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		if (args.Length < 3) { Console.Error.WriteLine("usage: KoreanText messages-export|messages-apply <projectsRoot> <file.xlsx> ..."); return 2; }
		string root = Path.GetFullPath(args[1]), xlsx = Path.GetFullPath(args[2]);
		string Opt(string name) { int i = Array.IndexOf(args, name); return i > 0 && i + 1 < args.Length ? Path.GetFullPath(args[i + 1]) : null; }
		var places = CollectPlaces(root);
		if (args[0] == "messages-export") return ExportMessages(places, xlsx, Opt("--suggest"), Opt("--candidates"));
		if (args[0] == "messages-apply")
		{
			string messages = Opt("--messages");
			if (messages == null) { Console.Error.WriteLine("--messages <Data/Messages.xml> is required"); return 2; }
			return ApplyMessages(places, root, xlsx, messages, args.Contains("--dry-run"));
		}
		Console.Error.WriteLine("unknown command " + args[0]);
		return 2;
	}

	// ---------------- which strings are user messages ----------------

	static List<Place> CollectPlaces(string root)
	{
		var result = new List<Place>();
		var texts = new Dictionary<string, string>();
		foreach (Entry e in Collect(root, null))
		{
			if (e.Kind != "C# 문자열" || e.Raw) continue;
			if (e.Context == "로그") continue;
			if (e.File.StartsWith("Clorium.Centering/", StringComparison.Ordinal)) continue; // separate program without UiText
			if (!texts.TryGetValue(e.File, out string text)) texts[e.File] = text = ReadText(Path.Combine(root, e.File), out _);
			int ls = text.LastIndexOf('\n', Math.Max(0, e.LitStart - 1)) + 1;
			string before = text.Substring(ls, e.LitStart - ls);
			if (e.Context.StartsWith("비교") && !Regex.IsMatch(before, @"[?:]\s*$") && !ConvertedBefore.IsMatch(before)) continue;
			var p = new Place { Entry = e };
			Match m = ConvertedBefore.Match(before);
			if (m.Success) p.ConvertedId = m.Groups[1].Value;
			else if (Regex.IsMatch(before, @"\bconst\b") || before.TrimStart().StartsWith("[") || Regex.IsMatch(before, @"\bcase\b") || Regex.IsMatch(before, @"\bnameof\b"))
				p.Blocked = "상수·특성·case 문자열은 자동으로 바꿀 수 없습니다";
			else if (e.Interp && e.Verbatim) p.Blocked = "@$ 문자열은 자동으로 바꿀 수 없습니다";
			if (!TryTemplate(e, p)) p.Blocked ??= "보간 문자열을 해석하지 못했습니다";
			result.Add(p);
		}
		return result;
	}

	// Regular-literal source form of the wording; interpolation holes become {0}, {1:fmt} with their expressions kept.
	static bool TryTemplate(Entry e, Place p)
	{
		string t = e.Text;
		if (e.Verbatim && !e.Interp)
		{
			p.Template = t.Replace("\\", "\\\\").Replace("\"\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
			return true;
		}
		if (!e.Interp) { p.Template = t; return true; }
		var sb = new StringBuilder();
		for (int i = 0; i < t.Length; i++)
		{
			char c = t[i];
			if (c == '\\' && i + 1 < t.Length) { sb.Append(c).Append(t[i + 1]); i++; continue; }
			if ((c == '{' || c == '}') && i + 1 < t.Length && t[i + 1] == c) { sb.Append(c).Append(c); i++; continue; }
			if (c == '}') return false;
			if (c != '{') { sb.Append(c); continue; }
			int end = SkipHole(t, i);
			if (end > t.Length || t[end - 1] != '}') return false;
			string inner = t.Substring(i + 1, end - i - 2);
			int split = TopLevelSplit(inner);
			string expr = (split < 0 ? inner : inner.Substring(0, split)).Trim(), rest = split < 0 ? "" : inner.Substring(split);
			if (expr.Length == 0) return false;
			sb.Append('{').Append(p.Holes.Count).Append(rest).Append('}');
			p.Holes.Add(expr);
			i = end - 1;
		}
		p.Template = sb.ToString();
		return true;
	}

	// First ',' (alignment) or ':' (format) outside brackets and strings.
	static int TopLevelSplit(string s)
	{
		int depth = 0;
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c == '"') { i++; while (i < s.Length && s[i] != '"') { if (s[i] == '\\') i++; i++; } continue; }
			if (c == '\'') { i++; while (i < s.Length && s[i] != '\'') { if (s[i] == '\\') i++; i++; } continue; }
			if (c == '(' || c == '[' || c == '{') depth++;
			else if (c == ')' || c == ']' || c == '}') depth--;
			else if (depth == 0 && (c == ',' || c == ':')) return i;
			else if (depth == 0 && c == '?') return -2 - i; // unparenthesised ?: is not allowed in a hole; treat as no split
		}
		return -1;
	}

	static List<Message> Group(List<Place> places)
	{
		var messages = new List<Message>();
		var byTemplate = new Dictionary<string, Message>(StringComparer.Ordinal);
		int next = 1 + places.Where(p => p.ConvertedId != null).Select(p => int.Parse(p.ConvertedId.Substring(1).Split('_')[0])).DefaultIfEmpty(0).Max();
		// Converted ids first, so their numbers stay.
		foreach (Place p in places.Where(p => p.Template != null).OrderBy(p => p.ConvertedId == null))
		{
			string key = p.ConvertedId ?? p.Template;
			if (p.ConvertedId == null && byTemplate.TryGetValue(p.Template, out Message same)) { same.Places.Add(p); continue; }
			if (!byTemplate.TryGetValue(key, out Message m))
			{
				m = new Message { Id = p.ConvertedId ?? "M" + (next++).ToString("0000"), Template = p.Template };
				byTemplate[key] = m;
				if (p.ConvertedId != null && !byTemplate.ContainsKey(p.Template)) byTemplate[p.Template] = m;
				messages.Add(m);
			}
			m.Places.Add(p);
		}
		foreach (Message m in messages)
		{
			m.Kinds = string.Join(", ", m.Places.Select(x => x.Entry.Context).Distinct());
			if (m.Places.All(x => x.Blocked != null)) { m.Target = "X"; m.Note = m.Places[0].Blocked; }
		}
		int n = 0;
		foreach (Place p in places.Where(p => p.Template != null).OrderBy(p => p.Entry.File, StringComparer.Ordinal).ThenBy(p => p.Entry.Offset)) p.Pid = "P" + (++n).ToString("0000");
		return messages.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
	}

	// ---------------- export ----------------

	static int ExportMessages(List<Place> places, string xlsx, string suggest, string candidates)
	{
		List<Message> messages = Group(places);
		var suggestions = new Dictionary<string, string[]>(StringComparer.Ordinal);
		if (suggest != null)
			foreach (string line in File.ReadAllLines(suggest, Encoding.UTF8))
			{
				string[] c = line.Split('\t');
				if (c.Length >= 4 && c[0].Length > 0) suggestions[c[0]] = c;
			}
		int suggested = 0;
		foreach (Message m in messages)
		{
			if (suggestions.TryGetValue(m.Template, out string[] s))
			{
				suggested++;
				if (m.Target == "O" && s[1].Trim().ToUpperInvariant() == "X") m.Target = "X";
				m.Ko = s[2]; m.En = s[3];
				if (s.Length > 4 && s[4].Trim().Length > 0) m.Note = (m.Note.Length > 0 ? m.Note + " / " : "") + s[4].Trim();
			}
			m.Ko ??= m.Template;
			m.En ??= "";
		}
		if (candidates != null)
		{
			var sb = new StringBuilder();
			sb.AppendLine("현재 문구\t종류\t사용 위치\t원본 줄");
			foreach (Message m in messages)
			{
				Place f = m.Places[0];
				sb.AppendLine(string.Join("\t", m.Template, m.Kinds, f.Entry.File + ":" + f.Entry.LineNo, f.Entry.Line.Replace("\t", " ")));
			}
			File.WriteAllText(candidates, sb.ToString(), Utf8Bom);
		}
		var usage = new List<string[]>
		{
			new[] { "사용자 메시지 한글·영어 문구표" },
			new[] { "" },
			new[] { "1. ‘메시지’ 시트의 노란 칸(대상, 새 한글, English)만 고칩니다. 다른 칸은 바꾸지 않습니다." },
			new[] { "2. 대상: O = 이 문구를 언어 설정에 맞춰 바꿉니다. X = 소스 그대로 둡니다(경로·키·내부 값 등)." },
			new[] { "3. {0}, {1:0.0} 같은 자리표시는 실제 값이 들어가는 곳입니다. 한글과 영어 모두 같은 자리표시를 그대로 남겨 주세요." },
			new[] { "4. \\n은 줄바꿈입니다. 칸 안에서 Alt+Enter로 줄을 바꿔도 \\n으로 반영됩니다." },
			new[] { "5. 같은 문구가 여러 곳에서 쓰이면 한 줄로 묶었습니다. 위치는 ‘위치’ 시트에 있습니다. 한 곳만 뜻이 다르면(예: 버튼 ‘중지’와 상태 ‘중지’) ‘위치’ 시트의 노란 칸 ‘이곳만 새 한글 / 이곳만 English’에 적습니다." },
			new[] { "6. 언어 설정이 한국어이면 새 한글, 그 밖의 언어이면 English를 표시합니다. English가 비어 있으면 한글을 표시합니다." },
			new[] { "7. 수정한 파일을 돌려주시면 소스의 제자리에 반영하고 Data\\Messages.xml을 만든 뒤 빌드·검사합니다." },
			new[] { "" },
			new[] { $"메시지 {messages.Count}개 · 위치 {messages.Sum(m => m.Places.Count)}곳 · 생성 {DateTime.Now:yyyy-MM-dd HH:mm}" },
		};
		var rows = new List<string[]> { new[] { "문구ID", "대상", "종류", "현재 문구", "새 한글", "English", "위치 수", "첫 위치", "메모" } };
		foreach (Message m in messages)
		{
			Place f = m.Places[0];
			rows.Add(new[] { m.Id, m.Target, m.Kinds, m.Template, m.Ko, m.En, m.Places.Count.ToString(), f.Entry.File + ":" + f.Entry.LineNo, m.Note });
		}
		var placeRows = new List<string[]> { new[] { "위치ID", "문구ID", "파일", "줄", "현재 문구", "원본 줄", "비고", "이곳만 새 한글", "이곳만 English" } };
		foreach (var p in messages.SelectMany(m => m.Places.Select(p => (m, p))).OrderBy(t => t.p.Pid, StringComparer.Ordinal))
			placeRows.Add(new[] { p.p.Pid, p.m.Id, p.p.Entry.File, p.p.Entry.LineNo.ToString(), p.p.Template, p.p.Entry.Line, p.p.Blocked ?? (p.p.ConvertedId != null ? "반영됨" : ""), "", "" });
		Xlsx.Write(xlsx, new[]
		{
			new Xlsx.Sheet { Name = "사용방법", Rows = usage, Widths = new[] { 120.0 } },
			new Xlsx.Sheet { Name = "메시지", Rows = rows, Widths = new[] { 9.0, 6, 14, 48, 48, 48, 7, 34, 26 }, Editable = new[] { 1, 4, 5 }, Header = true, ListColumn = 1 },
			new Xlsx.Sheet { Name = "위치", Rows = placeRows, Widths = new[] { 8.0, 9, 46, 6, 40, 60, 16, 36, 36 }, Editable = new[] { 7, 8 }, Header = true },
		});
		Console.WriteLine($"messages {messages.Count} (target O {messages.Count(m => m.Target == "O")}, suggested {suggested}), places {messages.Sum(m => m.Places.Count)} -> {xlsx}");
		return 0;
	}

	// ---------------- apply ----------------

	static readonly Regex FormatHole = new Regex(@"(?<!\{)\{(\d+)([^{}]*)\}");

	static int ApplyMessages(List<Place> places, string root, string xlsx, string messagesXml, bool dryRun)
	{
		var sheets = Xlsx.Read(xlsx);
		if (!sheets.TryGetValue("메시지", out var rows) || !sheets.TryGetValue("위치", out var placeRows)) throw new InvalidDataException("‘메시지’, ‘위치’ 시트가 필요합니다.");
		var head = rows[0]; var phead = placeRows[0];
		string Cell(List<string> h, List<string> r, string name) { int i = h.IndexOf(name); return i >= 0 && i < r.Count ? r[i] ?? "" : ""; }
		var report = new StringBuilder();
		var byId = new Dictionary<string, (string target, string template, string ko, string en)>(StringComparer.Ordinal);
		int refused = 0;
		foreach (var r in rows.Skip(1))
		{
			string id = Cell(head, r, "문구ID").Trim();
			if (id.Length == 0) continue;
			string target = Cell(head, r, "대상").Trim().ToUpperInvariant(), template = Cell(head, r, "현재 문구");
			string ko = Normalize(Cell(head, r, "새 한글")), en = Normalize(Cell(head, r, "English"));
			if (target != "O") continue;
			if (ko.Length == 0) ko = template;
			string problem = CheckWording(template, ko, "새 한글") ?? (en.Length > 0 ? CheckWording(template, en, "English") : null);
			if (problem != null) { refused++; report.AppendLine($"건너뜀 {id} — {problem}"); continue; }
			byId[id] = (target, template, ko, en);
		}
		var changes = new List<(Place p, string id, string replacement, int start, int length)>();
		int missing = 0;
		var used = new HashSet<Place>();
		var ordinal = new Dictionary<string, int>();
		var overrides = new Dictionary<string, (string target, string template, string ko, string en)>();
		foreach (var r in placeRows.Skip(1))
		{
			string id = Cell(phead, r, "문구ID").Trim(), file = Cell(phead, r, "파일"), template = Cell(phead, r, "현재 문구");
			if (!byId.TryGetValue(id, out var w)) continue;
			ordinal[id] = ordinal.TryGetValue(id, out int n) ? n + 1 : 1;
			string baseId = id;
			string oKo = Normalize(Cell(phead, r, "이곳만 새 한글")), oEn = Normalize(Cell(phead, r, "이곳만 English"));
			if (oKo.Length > 0 || oEn.Length > 0)
			{
				var own = (w.target, w.template, oKo.Length > 0 ? oKo : w.ko, oEn.Length > 0 ? oEn : w.en);
				string bad = CheckWording(template, own.Item3, "이곳만 새 한글") ?? (own.Item4.Length > 0 ? CheckWording(template, own.Item4, "이곳만 English") : null);
				if (bad != null) { refused++; report.AppendLine($"건너뜀 {id} {file} — {bad}"); continue; }
				id = baseId + "_" + ordinal[baseId];
				w = own;
				overrides[id] = own;
			}
			int.TryParse(Cell(phead, r, "줄"), out int line);
			// Already converted places are found by their id (their wording may have changed since the list was made), the
			// others by wording; a place is used once, so two equal strings on one line both change.
			Place p = places.Where(x => x.Entry.File == file && x.Blocked == null && !used.Contains(x)
					&& (x.ConvertedId == id || x.ConvertedId == baseId || (x.ConvertedId == null && x.Template == template)))
				.OrderBy(x => x.ConvertedId == id ? 0 : x.ConvertedId == baseId ? 1 : 2).ThenBy(x => Math.Abs(x.Entry.LineNo - line)).ThenBy(x => x.Entry.Offset).FirstOrDefault();
			if (p == null) { missing++; report.AppendLine($"건너뜀 {id} {file}:{line} — 지금 소스에서 '{template}'를 찾지 못함"); continue; }
			used.Add(p);
			string literal = "\"" + w.ko + "\"";
			if (p.ConvertedId == id) changes.Add((p, id, w.ko, p.Entry.Offset, p.Entry.Text.Length));
			else if (p.ConvertedId != null)
			{
				// converted under the shared id, now with its own wording: rename the id and the fallback
				changes.Add((p, id, "\"" + id + "\", \"" + w.ko, p.Entry.Offset - (p.ConvertedId.Length + 5), p.Entry.Text.Length + p.ConvertedId.Length + 5));
			}
			else
			{
				string call = Call + "(\"" + id + "\", " + literal + ")";
				if (p.Holes.Count > 0) call = "string.Format(" + call + ", " + string.Join(", ", p.Holes) + ")";
				changes.Add((p, id, call, p.Entry.LitStart, p.Entry.LitEnd - p.Entry.LitStart));
			}
		}
		foreach (var fileChanges in changes.GroupBy(c => c.p.Entry.File))
		{
			string path = Path.Combine(root, fileChanges.Key);
			string text = ReadText(path, out Encoding enc);
			foreach (var c in fileChanges.OrderByDescending(c => c.start))
			{
				text = text.Remove(c.start, c.length).Insert(c.start, c.replacement);
				report.AppendLine($"반영 {c.id} {c.p.Entry.File}:{c.p.Entry.LineNo}");
			}
			if (!dryRun) File.WriteAllText(path, text, enc);
		}
		// Data/Messages.xml: existing entries are kept, reviewed ones replaced.
		var table = new SortedDictionary<string, (string ko, string en)>(StringComparer.Ordinal);
		if (File.Exists(messagesXml))
			foreach (XElement m in XDocument.Load(messagesXml).Root.Elements("m"))
				table[(string)m.Attribute("id")] = ((string)m.Element("ko") ?? "", (string)m.Element("en") ?? "");
		foreach (var kv in byId.Concat(overrides)) table[kv.Key] = (Unescape(kv.Value.ko), Unescape(kv.Value.en));
		var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
			new XComment(" G2 사용자 메시지 (UiText). tools/KoreanText messages-apply가 검토된 엑셀에서 만듭니다. "),
			new XElement("messages", table.Select(kv => new XElement("m", new XAttribute("id", kv.Key), new XElement("ko", kv.Value.ko), new XElement("en", kv.Value.en)))));
		if (!dryRun)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(messagesXml));
			using var writer = new StreamWriter(messagesXml, false, Utf8Bom);
			doc.Save(writer);
		}
		report.Insert(0, $"{(dryRun ? "[미리보기] " : "")}메시지 {byId.Count}개 · 반영 {changes.Count}곳 · 문구 거부 {refused} · 위치 못 찾음 {missing}{Environment.NewLine}{Environment.NewLine}");
		string reportPath = Path.Combine(Path.GetDirectoryName(xlsx), "메시지_반영결과.txt");
		File.WriteAllText(reportPath, report.ToString(), Utf8Bom);
		Console.WriteLine($"messages {byId.Count}, places {changes.Count}, refused {refused}, missing {missing}{(dryRun ? " (dry run)" : "")} -> {reportPath}");
		return 0;
	}

	// Excel line breaks -> \n; a bare quote is escaped for the C# literal.
	static string Normalize(string s)
	{
		s = (s ?? "").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
		return Regex.Replace(s, "(?<!\\\\)\"", "\\\"");
	}

	static string CheckWording(string template, string wanted, string column)
	{
		var a = FormatHole.Matches(template).Select(m => "{" + m.Groups[1].Value + m.Groups[2].Value + "}").OrderBy(x => x).ToList();
		var b = FormatHole.Matches(wanted).Select(m => "{" + m.Groups[1].Value + m.Groups[2].Value + "}").OrderBy(x => x).ToList();
		if (!a.SequenceEqual(b)) return $"{column}의 자리표시({string.Join(" ", b)})가 원래 문구({string.Join(" ", a)})와 다름";
		try { Unescape(wanted); } catch (FormatException ex) { return column + ": " + ex.Message; }
		if (Regex.IsMatch(wanted, @"(?<!\\)(\\\\)*\\$")) return column + "가 \\로 끝납니다";
		return null;
	}

	static string Unescape(string s)
	{
		var sb = new StringBuilder();
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c != '\\') { sb.Append(c); continue; }
			if (++i >= s.Length) throw new FormatException("문구 끝의 \\");
			char e = s[i];
			switch (e)
			{
				case 'n': sb.Append('\n'); break;
				case 'r': sb.Append('\r'); break;
				case 't': sb.Append('\t'); break;
				case '0': sb.Append('\0'); break;
				case 'a': sb.Append('\a'); break;
				case 'b': sb.Append('\b'); break;
				case 'f': sb.Append('\f'); break;
				case 'v': sb.Append('\v'); break;
				case '\\': case '"': case '\'': sb.Append(e); break;
				case 'u':
					if (i + 4 >= s.Length) throw new FormatException("\\u 뒤 16진수 4자리 필요");
					sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
				case 'U':
					sb.Append(char.ConvertFromUtf32(Convert.ToInt32(s.Substring(i + 1, 8), 16))); i += 8; break;
				case 'x':
					int j = i + 1; while (j < s.Length && j - i <= 4 && Uri.IsHexDigit(s[j])) j++;
					if (j == i + 1) throw new FormatException("\\x 뒤 16진수 필요");
					sb.Append((char)Convert.ToInt32(s.Substring(i + 1, j - i - 1), 16)); i = j - 1; break;
				default: throw new FormatException("알 수 없는 이스케이프 \\" + e);
			}
		}
		return sb.ToString();
	}
}

// Minimal .xlsx writer/reader (no external package, so the round trip works on any PC with the .NET SDK).
static class Xlsx
{
	public sealed class Sheet
	{
		public string Name;
		public List<string[]> Rows;
		public double[] Widths = new double[0];
		public int[] Editable = new int[0];
		public bool Header;
		public int ListColumn = -1;
	}

	static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
	static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
	static readonly XNamespace PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

	static string Col(int i) { string s = ""; for (i++; i > 0; i = (i - 1) / 26) s = (char)('A' + (i - 1) % 26) + s; return s; }

	static string Clean(string s) => new string((s ?? "").Where(c => c == '\t' || c == '\n' || c == '\r' || c >= 0x20 && c != 0xFFFE && c != 0xFFFF).ToArray());

	public static void Write(string path, IList<Sheet> sheets)
	{
		if (File.Exists(path)) File.Delete(path);
		using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
		void Put(string name, string xml) { var e = zip.CreateEntry(name); using var w = new StreamWriter(e.Open(), new UTF8Encoding(false)); w.Write(xml); }
		var ct = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
		for (int i = 0; i < sheets.Count; i++) ct.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
		Put("[Content_Types].xml", ct.Append("</Types>").ToString());
		Put("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
		var wb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
		var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
		var names = new StringBuilder();
		for (int i = 0; i < sheets.Count; i++)
		{
			wb.Append($"<sheet name=\"{SecurityElement.Escape(sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
			rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
			if (sheets[i].Header && sheets[i].Rows.Count > 1)
				names.Append($"<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"{i}\" hidden=\"1\">'{sheets[i].Name}'!$A$1:${Col(sheets[i].Rows[0].Length - 1)}${sheets[i].Rows.Count}</definedName>");
		}
		wb.Append("</sheets>");
		if (names.Length > 0) wb.Append("<definedNames>").Append(names).Append("</definedNames>");
		Put("xl/workbook.xml", wb.Append("</workbook>").ToString());
		Put("xl/_rels/workbook.xml.rels", rels.Append($"<Relationship Id=\"rId{sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>").ToString());
		// 0 normal, 1 header, 2 editable (yellow), 3 title
		Put("xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"
			+ "<fonts count=\"3\"><font><sz val=\"10\"/><name val=\"맑은 고딕\"/><family val=\"3\"/></font><font><b/><sz val=\"10\"/><name val=\"맑은 고딕\"/><family val=\"3\"/></font><font><b/><sz val=\"14\"/><name val=\"맑은 고딕\"/><family val=\"3\"/></font></fonts>"
			+ "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9E1F2\"/><bgColor indexed=\"64\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFF2CC\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>"
			+ "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border><border><left style=\"thin\"><color rgb=\"FFBFBFBF\"/></left><right style=\"thin\"><color rgb=\"FFBFBFBF\"/></right><top style=\"thin\"><color rgb=\"FFBFBFBF\"/></top><bottom style=\"thin\"><color rgb=\"FFBFBFBF\"/></bottom><diagonal/></border></borders>"
			+ "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"
			+ "<cellXfs count=\"4\"><xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>"
			+ "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>"
			+ "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>"
			+ "<xf numFmtId=\"49\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\"/></cellXfs>"
			+ "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
		for (int i = 0; i < sheets.Count; i++)
		{
			Sheet s = sheets[i];
			var x = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
			x.Append(s.Header ? "<sheetViews><sheetView workbookViewId=\"0\"" + (i == 1 ? " tabSelected=\"1\"" : "") + "><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" : "<sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"0\"/></sheetViews>");
			if (s.Widths.Length > 0)
			{
				x.Append("<cols>");
				for (int c = 0; c < s.Widths.Length; c++) x.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{s.Widths[c].ToString(System.Globalization.CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
				x.Append("</cols>");
			}
			x.Append("<sheetData>");
			for (int r = 0; r < s.Rows.Count; r++)
			{
				x.Append($"<row r=\"{r + 1}\">");
				for (int c = 0; c < s.Rows[r].Length; c++)
				{
					int style = !s.Header ? (r == 0 ? 3 : -1) : r == 0 ? 1 : s.Editable.Contains(c) ? 2 : 0;
					string v = Clean(s.Rows[r][c]);
					x.Append($"<c r=\"{Col(c)}{r + 1}\" t=\"inlineStr\"{(style >= 0 ? $" s=\"{style}\"" : "")}><is><t xml:space=\"preserve\">{SecurityElement.Escape(v)}</t></is></c>");
				}
				x.Append("</row>");
			}
			x.Append("</sheetData>");
			if (s.Header && s.Rows.Count > 1) x.Append($"<autoFilter ref=\"A1:{Col(s.Rows[0].Length - 1)}{s.Rows.Count}\"/>");
			if (s.ListColumn >= 0 && s.Rows.Count > 1)
				x.Append($"<dataValidations count=\"1\"><dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" errorTitle=\"대상\" error=\"O 또는 X를 고르세요.\" sqref=\"{Col(s.ListColumn)}2:{Col(s.ListColumn)}{s.Rows.Count}\"><formula1>\"O,X\"</formula1></dataValidation></dataValidations>");
			x.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/></worksheet>");
			Put($"xl/worksheets/sheet{i + 1}.xml", x.ToString());
		}
	}

	// Sheet name -> rows of cell text (as Excel saved them: shared strings, inline strings or numbers).
	public static Dictionary<string, List<List<string>>> Read(string path)
	{
		using var zip = ZipFile.OpenRead(path);
		XDocument Load(string name) { var e = zip.GetEntry(name) ?? throw new InvalidDataException(name + " 없음"); using var s = e.Open(); return XDocument.Load(s); }
		var shared = new List<string>();
		if (zip.GetEntry("xl/sharedStrings.xml") != null)
			foreach (XElement si in Load("xl/sharedStrings.xml").Root.Elements(Main + "si")) shared.Add(Text(si));
		var rels = Load("xl/_rels/workbook.xml.rels").Root.Elements(PkgRel + "Relationship").ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target"));
		var result = new Dictionary<string, List<List<string>>>();
		foreach (XElement sheet in Load("xl/workbook.xml").Root.Element(Main + "sheets").Elements(Main + "sheet"))
		{
			string target = rels[(string)sheet.Attribute(Rel + "id")].TrimStart('/');
			if (!target.StartsWith("xl/")) target = "xl/" + target;
			var rows = new List<List<string>>();
			foreach (XElement row in Load(target).Root.Element(Main + "sheetData").Elements(Main + "row"))
			{
				var cells = new List<string>();
				foreach (XElement c in row.Elements(Main + "c"))
				{
					string r = (string)c.Attribute("r");
					int col = 0; foreach (char ch in r.TakeWhile(char.IsLetter)) col = col * 26 + (ch - 'A' + 1);
					col--;
					string t = (string)c.Attribute("t"), v = (string)c.Element(Main + "v");
					string value = t == "s" ? shared[int.Parse(v)] : t == "inlineStr" ? Text(c.Element(Main + "is")) : v ?? "";
					while (cells.Count < col) cells.Add("");
					cells.Add(value);
				}
				int index = (int?)row.Attribute("r") ?? rows.Count + 1;
				while (rows.Count < index - 1) rows.Add(new List<string>());
				rows.Add(cells);
			}
			result[(string)sheet.Attribute("name")] = rows;
		}
		return result;
	}

	// Plain or rich text; phonetic runs (rPh) are not part of the value.
	static string Text(XElement e) => e == null ? "" : string.Concat(e.Elements().Where(x => x.Name != Main + "rPh").SelectMany(x => x.Name == Main + "t" ? new[] { x } : x.Elements(Main + "t")).Select(t => t.Value));
}
