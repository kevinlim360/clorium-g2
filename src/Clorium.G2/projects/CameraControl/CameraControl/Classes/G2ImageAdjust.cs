using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ImageMagick;

namespace CameraControl.Classes
{
	/// <summary>
	/// G2 색보정: 채널별 레벨(검정점·흰점·감마). 0–255 기준.
	/// </summary>
	public sealed class G2Levels
	{
		public int Black { get; set; }

		public int White { get; set; } = 255;

		public double Gamma { get; set; } = 1.0;

		public bool IsIdentity => Black == 0 && White == 255 && Math.Abs(Gamma - 1.0) < 1e-9;

		public G2Levels Clone() => new G2Levels { Black = Black, White = White, Gamma = Gamma };

		public void Reset()
		{
			Black = 0;
			White = 255;
			Gamma = 1.0;
		}

		internal void Normalize()
		{
			Black = Math.Max(0, Math.Min(254, Black));
			White = Math.Max(Black + 1, Math.Min(255, White));
			Gamma = Math.Max(0.1, Math.Min(10.0, double.IsNaN(Gamma) ? 1.0 : Gamma));
		}
	}

	/// <summary>
	/// G2 색보정 설정값. 모든 값이 기본값이면 픽셀을 바꾸지 않는다.
	/// </summary>
	public sealed class G2AdjustSettings
	{
		/// <summary>노출 보정(EV). 선형광 기준 2^EV 배. −3 … +3</summary>
		public double Exposure { get; set; }

		/// <summary>명도(중간톤 밝기). −100 … +100</summary>
		public double Brightness { get; set; }

		/// <summary>대비. −100 … +100</summary>
		public double Contrast { get; set; }

		/// <summary>채도. −100(흑백) … +100(2배)</summary>
		public double Saturation { get; set; }

		/// <summary>색온도. −100(차갑게) … +100(따뜻하게)</summary>
		public double Temperature { get; set; }

		/// <summary>색조(틴트). −100(녹색) … +100(마젠타)</summary>
		public double Tint { get; set; }

		public G2Levels Master { get; set; } = new G2Levels();

		public G2Levels Red { get; set; } = new G2Levels();

		public G2Levels Green { get; set; } = new G2Levels();

		public G2Levels Blue { get; set; } = new G2Levels();

		public bool IsIdentity => IsToneIdentity && Math.Abs(Saturation) < 1e-9;

		/// <summary>밝기·색 곡선(LUT)이 항등인지.</summary>
		public bool IsToneIdentity =>
			Math.Abs(Exposure) < 1e-9 && Math.Abs(Brightness) < 1e-9 && Math.Abs(Contrast) < 1e-9 &&
			Math.Abs(Temperature) < 1e-9 && Math.Abs(Tint) < 1e-9 &&
			Master.IsIdentity && Red.IsIdentity && Green.IsIdentity && Blue.IsIdentity;

		/// <summary>채널마다 다르게 바뀌는지(흑백 원본도 컬러로 저장해야 하는지).</summary>
		public bool ChangesColorBalance =>
			Math.Abs(Temperature) > 1e-9 || Math.Abs(Tint) > 1e-9 || !Red.IsIdentity || !Green.IsIdentity || !Blue.IsIdentity;

		public G2AdjustSettings Clone() => new G2AdjustSettings
		{
			Exposure = Exposure,
			Brightness = Brightness,
			Contrast = Contrast,
			Saturation = Saturation,
			Temperature = Temperature,
			Tint = Tint,
			Master = Master.Clone(),
			Red = Red.Clone(),
			Green = Green.Clone(),
			Blue = Blue.Clone()
		};

		/// <summary>값이 같은지(슬라이더 단위의 미세 오차 무시).</summary>
		public bool SameAs(G2AdjustSettings o)
		{
			if (o == null) return false;
			bool Near(double x, double y) => Math.Abs(x - y) < 1e-6;
			bool Lv(G2Levels x, G2Levels y) => x.Black == y.Black && x.White == y.White && Math.Abs(x.Gamma - y.Gamma) < 1e-6;
			return Near(Exposure, o.Exposure) && Near(Brightness, o.Brightness) && Near(Contrast, o.Contrast) && Near(Saturation, o.Saturation) &&
				Near(Temperature, o.Temperature) && Near(Tint, o.Tint) && Lv(Master, o.Master) && Lv(Red, o.Red) && Lv(Green, o.Green) && Lv(Blue, o.Blue);
		}

		public G2Levels Channel(int index)
		{
			switch (index)
			{
				case 1: return Red;
				case 2: return Green;
				case 3: return Blue;
				default: return Master;
			}
		}

		internal void Normalize()
		{
			Exposure = Clamp(Exposure, -3, 3);
			Brightness = Clamp(Brightness, -100, 100);
			Contrast = Clamp(Contrast, -100, 100);
			Saturation = Clamp(Saturation, -100, 100);
			Temperature = Clamp(Temperature, -100, 100);
			Tint = Clamp(Tint, -100, 100);
			Master.Normalize();
			Red.Normalize();
			Green.Normalize();
			Blue.Normalize();
		}

		private static double Clamp(double v, double lo, double hi) => double.IsNaN(v) ? 0 : Math.Max(lo, Math.Min(hi, v));

		public string Describe()
		{
			var parts = new List<string>();
			CultureInfo c = CultureInfo.InvariantCulture;
			if (Math.Abs(Exposure) > 1e-9) parts.Add("노출 " + Exposure.ToString("+0.00;-0.00", c) + "EV");
			if (Math.Abs(Brightness) > 1e-9) parts.Add("명도 " + Brightness.ToString("+0;-0", c));
			if (Math.Abs(Contrast) > 1e-9) parts.Add("대비 " + Contrast.ToString("+0;-0", c));
			if (Math.Abs(Saturation) > 1e-9) parts.Add("채도 " + Saturation.ToString("+0;-0", c));
			if (Math.Abs(Temperature) > 1e-9) parts.Add("색온도 " + Temperature.ToString("+0;-0", c));
			if (Math.Abs(Tint) > 1e-9) parts.Add("색조 " + Tint.ToString("+0;-0", c));
			string[] names = { "전체", "R", "G", "B" };
			for (int i = 0; i < 4; i++)
			{
				G2Levels l = Channel(i);
				if (!l.IsIdentity) parts.Add(names[i] + " 레벨 " + l.Black + "/" + l.Gamma.ToString("0.00", c) + "/" + l.White);
			}
			return parts.Count == 0 ? "보정 없음" : string.Join(", ", parts);
		}
	}

	/// <summary>
	/// G2 색보정 엔진. 디코딩·색 프로필·EXIF·인코딩과 픽셀 연산은 ImageMagick(Magick.NET, Apache-2.0)으로 처리한다.
	/// 밝기/색 곡선은 실수 정밀도로 계산한 256단계 컬러 LUT를 ImageMagick CLUT로 한 번에 적용하고(8비트 반복 반올림 누적 없음),
	/// 채도는 Rec.709 휘도를 유지하는 ImageMagick 색 행렬로 적용한다. 미리보기와 원본 저장은 같은 함수를 쓴다.
	/// 원본 파일은 수정하지 않는다.
	/// </summary>
	public static class G2ImageAdjust
	{
		public const double LumaR = 0.2126;

		public const double LumaG = 0.7152;

		public const double LumaB = 0.0722;

		private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp"
		};

		public static bool IsSupportedFile(string path) =>
			!string.IsNullOrEmpty(path) && SupportedExtensions.Contains(Path.GetExtension(path));

		public static string EngineVersion
		{
			get
			{
				try
				{
					return "Magick.NET " + MagickNET.Version + " / " + MagickNET.ImageMagickVersion;
				}
				catch (Exception)
				{
					return "Magick.NET";
				}
			}
		}

		// ---------- 곡선 계산 (실수) ----------

		public static double SrgbToLinear(double v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

		public static double LinearToSrgb(double v) => v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;

		/// <summary>색온도/틴트 채널 이득(선형광). 휘도 가중 평균이 1이 되도록 정규화한다.</summary>
		public static double[] WhiteBalanceGains(double temperature, double tint)
		{
			double t = temperature / 100.0;
			double m = tint / 100.0;
			double r = (1 + 0.22 * t) * (1 + 0.06 * m);
			double g = 1 - 0.14 * m;
			double b = (1 - 0.22 * t) * (1 + 0.06 * m);
			double l = LumaR * r + LumaG * g + LumaB * b;
			return new[] { r / l, g / l, b / l };
		}

		/// <summary>대비 곡선. k = −1 … +1. 0.5(중간 회색)는 고정, 0과 1도 고정.</summary>
		public static double ContrastCurve(double y, double k)
		{
			if (Math.Abs(k) < 1e-9)
			{
				return y;
			}
			if (k < 0)
			{
				return 0.5 + (y - 0.5) * (1 + 0.5 * k);
			}
			double a = 7.0 * k;
			double lo = Logistic(-0.5 * a);
			double hi = Logistic(0.5 * a);
			return (Logistic(a * (y - 0.5)) - lo) / (hi - lo);
		}

		private static double Logistic(double x) => 1.0 / (1.0 + Math.Exp(-x));

		public static double LevelsCurve(double y, G2Levels l)
		{
			if (l.IsIdentity)
			{
				return y;
			}
			double b = l.Black / 255.0;
			double w = Math.Max(l.White, l.Black + 1) / 255.0;
			double v = (y - b) / (w - b);
			v = Math.Max(0, Math.Min(1, v));
			return Math.Pow(v, 1.0 / l.Gamma);
		}

		/// <summary>채널 c(0=R,1=G,2=B)의 입력 x(0–1) → 출력(0–1).</summary>
		public static double ChannelCurve(double x, int c, G2AdjustSettings s, double[] gains)
		{
			double y = x;
			if (Math.Abs(s.Exposure) > 1e-9 || Math.Abs(gains[c] - 1) > 1e-12)
			{
				double lin = SrgbToLinear(x) * Math.Pow(2, s.Exposure) * gains[c];
				y = LinearToSrgb(Math.Max(0, Math.Min(1, lin)));
			}
			if (Math.Abs(s.Brightness) > 1e-9)
			{
				y = Math.Pow(Math.Max(0, Math.Min(1, y)), Math.Pow(2, -s.Brightness / 100.0));
			}
			y = ContrastCurve(Math.Max(0, Math.Min(1, y)), s.Contrast / 100.0);
			y = LevelsCurve(Math.Max(0, Math.Min(1, y)), s.Master);
			y = LevelsCurve(y, s.Channel(c + 1));
			return Math.Max(0, Math.Min(1, y));
		}

		/// <summary>256×1 RGB LUT (r,g,b 순서 바이트 768개).</summary>
		public static byte[] BuildLut(G2AdjustSettings s)
		{
			double[] gains = WhiteBalanceGains(s.Temperature, s.Tint);
			var lut = new byte[256 * 3];
			for (int i = 0; i < 256; i++)
			{
				for (int c = 0; c < 3; c++)
				{
					lut[i * 3 + c] = (byte)Math.Round(ChannelCurve(i / 255.0, c, s, gains) * 255.0);
				}
			}
			return lut;
		}

		/// <summary>채도 행렬(3×3, 행 우선). f = 1 + 채도/100.</summary>
		public static double[] SaturationMatrix(double saturation)
		{
			double f = 1 + saturation / 100.0;
			double r = LumaR * (1 - f), g = LumaG * (1 - f), b = LumaB * (1 - f);
			return new[]
			{
				r + f, g, b,
				r, g + f, b,
				r, g, b + f
			};
		}

		// ---------- ImageMagick 적용 ----------

		/// <summary>이미지에 보정을 적용한다(제자리). 미리보기 프록시와 원본 해상도에 같은 경로를 쓴다.</summary>
		public static void Apply(IMagickImage<byte> image, G2AdjustSettings settings)
		{
			if (image == null) throw new ArgumentNullException(nameof(image));
			G2AdjustSettings s = settings.Clone();
			s.Normalize();
			if (s.IsIdentity)
			{
				return;
			}
			EnsureEditable(image, s);
			bool hadAlpha = image.HasAlpha;
			if (!s.IsToneIdentity)
			{
				using (var lut = new MagickImage())
				{
					lut.ReadPixels(BuildLut(s), new PixelReadSettings(256, 1, StorageType.Char, PixelMapping.RGB));
					lut.HasAlpha = false;
					image.Clut(lut, PixelInterpolateMethod.Bilinear, Channels.RGB);
				}
			}
			if (Math.Abs(s.Saturation) > 1e-9)
			{
				image.ColorMatrix(new MagickColorMatrix(3, SaturationMatrix(s.Saturation)));
			}
			if (!hadAlpha && image.HasAlpha)
			{
				// never introduce transparency into an opaque photo
				image.HasAlpha = false;
			}
		}

		private static void EnsureEditable(IMagickImage<byte> image, G2AdjustSettings s)
		{
			if (image.ColorSpace == ColorSpace.CMYK)
			{
				throw new NotSupportedException("CMYK 이미지는 색보정을 지원하지 않습니다. RGB 사진을 사용해 주세요.");
			}
			bool gray = image.ColorType == ColorType.Grayscale || image.ColorType == ColorType.GrayscaleAlpha || image.ColorType == ColorType.Bilevel;
			if (gray && (s.ChangesColorBalance || s.Saturation > 1e-9))
			{
				image.ColorType = image.HasAlpha ? ColorType.TrueColorAlpha : ColorType.TrueColor;
			}
			else if (image.ColorType == ColorType.Palette || image.ColorType == ColorType.PaletteAlpha || image.ColorType == ColorType.PaletteBilevelAlpha)
			{
				image.ColorType = image.HasAlpha ? ColorType.TrueColorAlpha : ColorType.TrueColor;
			}
		}

		/// <summary>원본을 읽어 방향을 바로잡는다. 호출자가 Dispose 한다.</summary>
		public static MagickImage Load(string path)
		{
			if (!File.Exists(path)) throw new FileNotFoundException("사진 파일을 찾을 수 없습니다.", path);
			if (!IsSupportedFile(path)) throw new NotSupportedException("JPEG, PNG, TIFF, BMP 사진만 색보정할 수 있습니다. RAW 파일은 지원하지 않습니다.");
			var image = new MagickImage(path);
			try
			{
				if (image.ColorSpace == ColorSpace.CMYK)
				{
					throw new NotSupportedException("CMYK 이미지는 색보정을 지원하지 않습니다. RGB 사진을 사용해 주세요.");
				}
				image.AutoOrient();
				return image;
			}
			catch
			{
				image.Dispose();
				throw;
			}
		}

		/// <summary>긴 변이 maxEdge 이하인 미리보기 사본. 원본보다 크게 늘리지 않는다.</summary>
		public static IMagickImage<byte> CreatePreview(IMagickImage<byte> original, int maxEdge)
		{
			IMagickImage<byte> preview = original.Clone();
			uint edge = (uint)Math.Max(64, maxEdge);
			if (preview.Width > edge || preview.Height > edge)
			{
				preview.Resize(edge, edge);
			}
			return preview;
		}

		/// <summary>BGRA 32비트 픽셀(미리보기 표시용).</summary>
		public static byte[] ToBgra(IMagickImage<byte> image) => ToBgra(image, false);

		/// <summary>
		/// BGRA 32비트 픽셀. 투명도가 없는 이미지는 BGR로 내보낸 뒤 알파 255를 채운다.
		/// (ImageMagick의 BGRA 내보내기는 알파가 없는 이미지에 알파 채널을 켜는 부작용이 있어, 그 이미지를 계속 쓰면 알파 값이 불안정해진다.)
		/// forceOpaque이면 원본에 투명도가 없을 때와 같이 항상 불투명으로 만든다.
		/// </summary>
		public static byte[] ToBgra(IMagickImage<byte> image, bool forceOpaque)
		{
			if (!forceOpaque && image.HasAlpha)
			{
				using (IMagickImage<byte> copy = image.Clone())
				{
					return copy.ToByteArray(MagickFormat.Bgra);
				}
			}
			byte[] bgr = image.ToByteArray(MagickFormat.Bgr);
			var bgra = new byte[bgr.Length / 3 * 4];
			for (int i = 0, j = 0; i + 2 < bgr.Length; i += 3, j += 4)
			{
				bgra[j] = bgr[i];
				bgra[j + 1] = bgr[i + 1];
				bgra[j + 2] = bgr[i + 2];
				bgra[j + 3] = 255;
			}
			return bgra;
		}

		/// <summary>원본 해상도로 보정해 새 파일에 저장한다. 원본은 바꾸지 않는다. 같은 폴더의 임시 파일에 쓴 뒤 이름을 바꾼다.</summary>
		public static void SaveAdjusted(string source, string destination, G2AdjustSettings settings, bool overwrite)
		{
			if (string.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination));
			if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("원본과 다른 파일 이름을 지정해 주세요. 원본은 덮어쓰지 않습니다.");
			}
			if (!IsSupportedFile(destination)) throw new NotSupportedException("JPEG, PNG, TIFF, BMP 형식으로만 저장할 수 있습니다.");
			if (File.Exists(destination) && !overwrite) throw new IOException("같은 이름의 파일이 이미 있습니다: " + Path.GetFileName(destination));
			string folder = Path.GetDirectoryName(Path.GetFullPath(destination));
			string temp = Path.Combine(folder, ".~cc-" + Guid.NewGuid().ToString("N") + Path.GetExtension(destination));
			try
			{
				using (MagickImage image = Load(source))
				{
					Apply(image, settings);
					string ext = Path.GetExtension(destination).ToLowerInvariant();
					if ((ext == ".jpg" || ext == ".jpeg") && image.Quality == 0)
					{
						image.Quality = 95;
					}
					image.Write(temp);
				}
				if (File.Exists(destination))
				{
					File.Delete(destination);
				}
				File.Move(temp, destination);
			}
			finally
			{
				if (File.Exists(temp))
				{
					try { File.Delete(temp); } catch (Exception) { }
				}
			}
		}

		/// <summary>새 이름: (targetFolder 또는 원본 폴더)\이름_cc.확장자, 있으면 이름_cc (1).확장자 …</summary>
		public static string NextAdjustedName(string source, string targetFolder = null, string suffix = "_cc")
		{
			string folder = string.IsNullOrEmpty(targetFolder) ? Path.GetDirectoryName(Path.GetFullPath(source)) : Path.GetFullPath(targetFolder);
			string name = Path.GetFileNameWithoutExtension(source) + suffix;
			string ext = Path.GetExtension(source);
			string candidate = Path.Combine(folder, name + ext);
			for (int i = 1; File.Exists(candidate); i++)
			{
				candidate = Path.Combine(folder, name + " (" + i + ")" + ext);
			}
			return candidate;
		}

		/// <summary>BGRA 픽셀의 R,G,B,휘도 히스토그램(각 256칸)과 0/255 클리핑 비율.</summary>
		public static G2Histogram Histogram(byte[] bgra)
		{
			var h = new G2Histogram();
			long n = bgra.Length / 4;
			long lowClip = 0, highClip = 0;
			for (long i = 0; i < bgra.Length - 3; i += 4)
			{
				byte b = bgra[i], g = bgra[i + 1], r = bgra[i + 2];
				h.Red[r]++;
				h.Green[g]++;
				h.Blue[b]++;
				int y = (int)Math.Round(LumaR * r + LumaG * g + LumaB * b);
				h.Luma[Math.Min(255, y)]++;
				if (r == 0 && g == 0 && b == 0) lowClip++;
				if (r == 255 || g == 255 || b == 255) highClip++;
			}
			h.Pixels = n;
			h.ShadowClipPercent = n == 0 ? 0 : 100.0 * lowClip / n;
			h.HighlightClipPercent = n == 0 ? 0 : 100.0 * highClip / n;
			return h;
		}
	}

	public sealed class G2Histogram
	{
		public long[] Red { get; } = new long[256];

		public long[] Green { get; } = new long[256];

		public long[] Blue { get; } = new long[256];

		public long[] Luma { get; } = new long[256];

		public long Pixels { get; set; }

		public double ShadowClipPercent { get; set; }

		public double HighlightClipPercent { get; set; }
	}
}
