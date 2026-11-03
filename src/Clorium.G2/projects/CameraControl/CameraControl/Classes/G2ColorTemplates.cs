using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace CameraControl.Classes
{
	/// <summary>G2 색보정 템플릿: 이름 + 보정값.</summary>
	public sealed class G2ColorTemplate
	{
		public string Name { get; set; } = "";

		public G2AdjustSettings Settings { get; set; } = new G2AdjustSettings();

		public DateTime Updated { get; set; }

		/// <summary>목록에 보여 줄 요약.</summary>
		[XmlIgnore]
		public string Summary => Settings == null ? "" : Settings.Describe();

		public G2ColorTemplate Clone() => new G2ColorTemplate { Name = Name, Settings = (Settings ?? new G2AdjustSettings()).Clone(), Updated = Updated };

		public override string ToString() => Name;
	}

	[XmlRoot("G2ColorTemplates")]
	public sealed class G2ColorTemplateFile
	{
		[XmlAttribute("version")]
		public int Version { get; set; } = 1;

		[XmlElement("Template")]
		public List<G2ColorTemplate> Templates { get; set; } = new List<G2ColorTemplate>();
	}

	/// <summary>
	/// 템플릿 저장소(XML 한 파일). 저장은 임시 파일에 쓴 뒤 교체하고, 읽을 수 없는 파일은 지우지 않고 .bad-시각 으로 옮겨 둔다.
	/// </summary>
	public sealed class G2ColorTemplateStore
	{
		public const int MaxNameLength = 40;

		private readonly List<G2ColorTemplate> _items = new List<G2ColorTemplate>();

		public G2ColorTemplateStore(string path)
		{
			FilePath = path ?? throw new ArgumentNullException(nameof(path));
		}

		/// <summary>%APPDATA%\Clorium\G2\color-templates.xml</summary>
		public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clorium", "G2", "color-templates.xml");

		public string FilePath { get; }

		/// <summary>불러오기 중 손상 파일을 옮긴 경우 그 경로.</summary>
		public string QuarantinedFile { get; private set; }

		public IReadOnlyList<G2ColorTemplate> Items => _items;

		public G2ColorTemplate Find(string name) =>
			_items.FirstOrDefault(t => string.Equals(t.Name, (name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

		public void Load()
		{
			_items.Clear();
			QuarantinedFile = null;
			if (!File.Exists(FilePath))
			{
				return;
			}
			try
			{
				using (FileStream s = File.OpenRead(FilePath))
				{
					var file = (G2ColorTemplateFile)new XmlSerializer(typeof(G2ColorTemplateFile)).Deserialize(s);
					foreach (G2ColorTemplate t in file?.Templates ?? new List<G2ColorTemplate>())
					{
						if (t == null || string.IsNullOrWhiteSpace(t.Name) || Find(t.Name) != null)
						{
							continue;
						}
						t.Name = t.Name.Trim();
						t.Settings = t.Settings ?? new G2AdjustSettings();
						t.Settings.Normalize();
						_items.Add(t);
					}
				}
			}
			catch (Exception)
			{
				QuarantinedFile = FilePath + ".bad-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
				try { File.Move(FilePath, QuarantinedFile); } catch (Exception) { QuarantinedFile = null; }
				_items.Clear();
			}
		}

		public void Save()
		{
			string dir = Path.GetDirectoryName(Path.GetFullPath(FilePath));
			Directory.CreateDirectory(dir);
			string temp = Path.Combine(dir, ".~tpl-" + Guid.NewGuid().ToString("N") + ".xml");
			try
			{
				var file = new G2ColorTemplateFile { Templates = _items.Select(t => t.Clone()).ToList() };
				using (FileStream s = File.Create(temp))
				{
					new XmlSerializer(typeof(G2ColorTemplateFile)).Serialize(s, file);
				}
				if (File.Exists(FilePath))
				{
					// The file just written may still be held for a moment (virus scan, indexer, the previous save):
					// File.Replace then fails with "unable to remove the file to be replaced". Retry briefly.
					for (int attempt = 0; ; attempt++)
					{
						try
						{
							File.Replace(temp, FilePath, null);
							break;
						}
						catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) && attempt < 9)
						{
							Thread.Sleep(50 * (attempt + 1));
						}
					}
				}
				else
				{
					File.Move(temp, FilePath);
				}
			}
			finally
			{
				if (File.Exists(temp))
				{
					try { File.Delete(temp); } catch (Exception) { }
				}
			}
		}

		public static string ValidateName(string name)
		{
			string n = (name ?? "").Trim();
			if (n.Length == 0) throw new ArgumentException("템플릿 이름을 입력해 주세요.");
			if (n.Length > MaxNameLength) throw new ArgumentException("템플릿 이름은 " + MaxNameLength + "자 이하로 입력해 주세요.");
			return n;
		}

		/// <summary>새 템플릿 추가 후 저장. 같은 이름(대소문자 무시)이 있으면 거부.</summary>
		public G2ColorTemplate Add(string name, G2AdjustSettings settings)
		{
			string n = ValidateName(name);
			if (Find(n) != null) throw new InvalidOperationException("같은 이름의 템플릿이 이미 있습니다: " + n);
			var t = new G2ColorTemplate { Name = n, Settings = Normalized(settings), Updated = DateTime.Now };
			_items.Add(t);
			Save();
			return t;
		}

		/// <summary>기존 템플릿의 보정값을 바꾼 뒤 저장.</summary>
		public G2ColorTemplate Update(string name, G2AdjustSettings settings)
		{
			G2ColorTemplate t = Find(name) ?? throw new InvalidOperationException("템플릿을 찾을 수 없습니다: " + name);
			t.Settings = Normalized(settings);
			t.Updated = DateTime.Now;
			Save();
			return t;
		}

		public G2ColorTemplate Rename(string name, string newName)
		{
			G2ColorTemplate t = Find(name) ?? throw new InvalidOperationException("템플릿을 찾을 수 없습니다: " + name);
			string n = ValidateName(newName);
			G2ColorTemplate other = Find(n);
			if (other != null && !ReferenceEquals(other, t)) throw new InvalidOperationException("같은 이름의 템플릿이 이미 있습니다: " + n);
			t.Name = n;
			t.Updated = DateTime.Now;
			Save();
			return t;
		}

		public bool Remove(string name)
		{
			G2ColorTemplate t = Find(name);
			if (t == null) return false;
			_items.Remove(t);
			Save();
			return true;
		}

		private static G2AdjustSettings Normalized(G2AdjustSettings s)
		{
			G2AdjustSettings c = (s ?? new G2AdjustSettings()).Clone();
			c.Normalize();
			return c;
		}
	}

	public sealed class G2BatchProgress
	{
		public int Index { get; set; }

		public int Total { get; set; }

		public string File { get; set; }
	}

	public sealed class G2BatchResult
	{
		public string SourceFolder { get; set; }

		public string OutputFolder { get; set; }

		public string LogFile { get; set; }

		public int Total { get; set; }

		public int Saved { get; set; }

		public bool Cancelled { get; set; }

		public List<string> Outputs { get; } = new List<string>();

		public List<string> Errors { get; } = new List<string>();
	}

	/// <summary>폴더 한 개(하위 폴더 제외)의 모든 사진에 같은 보정값을 적용한다. 결과는 폴더\_cc\ 에 새 파일로 저장, 원본은 그대로.</summary>
	public static class G2ColorBatch
	{
		public const string OutputFolderName = "_cc";

		/// <summary>처리 대상: 지원 형식, 숨김/임시 파일 제외, 이름순.</summary>
		public static List<string> FindImages(string folder)
		{
			if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("폴더를 찾을 수 없습니다: " + folder);
			return Directory.GetFiles(folder)
				.Where(f => G2ImageAdjust.IsSupportedFile(f))
				.Where(f => !Path.GetFileName(f).StartsWith(".~", StringComparison.Ordinal))
				.Where(f => (File.GetAttributes(f) & (FileAttributes.Hidden | FileAttributes.System)) == 0)
				.OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public static G2BatchResult Run(string folder, G2AdjustSettings settings, string templateName, IProgress<G2BatchProgress> progress, CancellationToken token)
		{
			var result = new G2BatchResult { SourceFolder = Path.GetFullPath(folder) };
			List<string> files = FindImages(folder);
			result.Total = files.Count;
			result.OutputFolder = Path.Combine(result.SourceFolder, OutputFolderName);
			if (files.Count == 0)
			{
				return result;
			}
			Directory.CreateDirectory(result.OutputFolder);
			G2AdjustSettings s = settings.Clone();
			var log = new StringBuilder();
			DateTime started = DateTime.Now;
			log.AppendLine("Clorium G2 이미지보정 일괄 적용");
			log.AppendLine("시작: " + started.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
			log.AppendLine("템플릿: " + (templateName ?? "(이름 없음)"));
			log.AppendLine("보정값: " + s.Describe());
			log.AppendLine("원본 폴더: " + result.SourceFolder);
			log.AppendLine("저장 폴더: " + result.OutputFolder);
			log.AppendLine("엔진: " + G2ImageAdjust.EngineVersion);
			log.AppendLine();
			for (int i = 0; i < files.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					result.Cancelled = true;
					log.AppendLine("중지됨: " + (files.Count - i) + "장 처리하지 않음");
					break;
				}
				string src = files[i];
				progress?.Report(new G2BatchProgress { Index = i, Total = files.Count, File = Path.GetFileName(src) });
				try
				{
					string dest = G2ImageAdjust.NextAdjustedName(src, result.OutputFolder);
					G2ImageAdjust.SaveAdjusted(src, dest, s, false);
					result.Saved++;
					result.Outputs.Add(dest);
					log.AppendLine("완료\t" + Path.GetFileName(src) + "\t→ " + Path.GetFileName(dest));
				}
				catch (Exception ex)
				{
					string msg = Path.GetFileName(src) + ": " + ex.GetBaseException().Message;
					result.Errors.Add(msg);
					log.AppendLine("실패\t" + msg);
				}
			}
			progress?.Report(new G2BatchProgress { Index = files.Count, Total = files.Count, File = null });
			log.AppendLine();
			log.AppendLine("결과: 전체 " + result.Total + " · 저장 " + result.Saved + " · 실패 " + result.Errors.Count + (result.Cancelled ? " · 중지" : ""));
			log.AppendLine("소요: " + (DateTime.Now - started).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "초");
			try
			{
				string name = "보정기록_" + started.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
				string logFile = Path.Combine(result.OutputFolder, name + ".txt");
				for (int n = 1; File.Exists(logFile); n++)
				{
					logFile = Path.Combine(result.OutputFolder, name + " (" + n + ").txt");
				}
				File.WriteAllText(logFile, log.ToString(), new UTF8Encoding(true));
				result.LogFile = logFile;
			}
			catch (Exception)
			{
			}
			return result;
		}
	}
}
