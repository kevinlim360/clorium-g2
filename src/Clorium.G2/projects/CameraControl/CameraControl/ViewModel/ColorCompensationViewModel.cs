using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraControl.ViewModel;

internal class ColorCompensationViewModel : INotifyPropertyChanged
{
	public FilterSettings filters = new FilterSettings();

	public readonly HistoryManager history = new HistoryManager();

	private string imagename;

	public WriteableBitmap OrgsourceImage;

	private WriteableBitmap _sourceimage;

	private bool _showbrightness = true;

	private bool _showcontrast = true;

	private bool _showgamma;

	private bool _showsharpness;

	private bool _showsaturation;

	private bool _showhue;

	private const string FilePath = "D:\\Clorium Data\\Clorium_ColorCompensation.json";

	public WriteableBitmap SourceImage
	{
		get
		{
			return _sourceimage;
		}
		set
		{
			_sourceimage = value;
			OnPropertyChanged("SourceImage");
		}
	}

	public bool ShowBrightness
	{
		get
		{
			return _showbrightness;
		}
		set
		{
			_showbrightness = true;
			OnPropertyChanged("ShowBrightness");
		}
	}

	public bool ShowContrast
	{
		get
		{
			return _showcontrast;
		}
		set
		{
			_showcontrast = true;
			OnPropertyChanged("ShowContrast");
		}
	}

	public bool ShowGamma
	{
		get
		{
			return _showgamma;
		}
		set
		{
			_showgamma = value;
			OnPropertyChanged("ShowGamma");
		}
	}

	public bool ShowSharpness
	{
		get
		{
			return _showsharpness;
		}
		set
		{
			_showsharpness = value;
			OnPropertyChanged("ShowSharpness");
		}
	}

	public bool ShowSaturation
	{
		get
		{
			return _showsaturation;
		}
		set
		{
			_showsaturation = value;
			OnPropertyChanged("ShowSaturation");
		}
	}

	public bool ShowHue
	{
		get
		{
			return _showhue;
		}
		set
		{
			_showhue = value;
			OnPropertyChanged("ShowHue");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public ColorCompensationViewModel(string path)
	{
		imagename = path;
		Init();
	}

	private void Init()
	{
		SourceImage = new WriteableBitmap(LoadBitmapSourceWithOrientation(imagename));
		OrgsourceImage = SourceImage.Clone();
		OrgsourceImage.Freeze();
		filters = new FilterSettings();
		history.Clear();
	}

	private BitmapSource LoadBitmapSourceWithOrientation(string jpgPath)
	{
		using FileStream fs = new FileStream(jpgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		BitmapDecoder decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
		BitmapFrame frame = decoder.Frames[0];
		if (frame.Metadata is BitmapMetadata metadata && metadata.ContainsQuery("System.Photo.Orientation"))
		{
			object orientation = metadata.GetQuery("System.Photo.Orientation");
			if (orientation != null)
			{
				Rotation rotation = Rotation.Rotate0;
				switch (ushort.Parse(orientation.ToString()))
				{
				case 3:
					rotation = Rotation.Rotate180;
					break;
				case 6:
					rotation = Rotation.Rotate90;
					break;
				case 8:
					rotation = Rotation.Rotate270;
					break;
				}
				if (rotation != Rotation.Rotate0)
				{
					TransformedBitmap transformed = new TransformedBitmap();
					transformed.BeginInit();
					transformed.Source = frame;
					transformed.Transform = new RotateTransform((int)rotation * 90);
					return new TransformedBitmap(frame, new RotateTransform(rotation switch
					{
						Rotation.Rotate180 => 180, 
						Rotation.Rotate90 => 90, 
						_ => 270, 
					}));
				}
			}
		}
		return frame;
	}

	public void SaveImage(string filename)
	{
		if (SourceImage == null || string.IsNullOrEmpty(filename))
		{
			return;
		}
		try
		{
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.QualityLevel = 95;
			encoder.Frames.Add(BitmapFrame.Create(SourceImage));
			using FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
			encoder.Save(stream);
		}
		catch (Exception ex)
		{
			MessageBox.Show("저장 중 오류 발생: " + ex.Message);
		}
	}

	public byte[] ApplyFiltersToBuffer(byte[] srcBuffer, FilterSettings filters, int width, int height)
	{
		int stride = width * 4;
		byte[] dstBuffer = new byte[stride * height];
		ApplyBrightnessContrast(srcBuffer, dstBuffer, filters);
		if (ShowGamma)
		{
			ApplyGammaCorrection(dstBuffer, filters);
		}
		if (ShowSharpness)
		{
			ApplySharpness(dstBuffer, filters, width, height);
		}
		if (ShowSaturation)
		{
			ApplySaturation(dstBuffer, filters);
		}
		if (ShowHue)
		{
			ApplyHueRotation(dstBuffer, filters);
		}
		return dstBuffer;
	}

	private void ApplyBrightnessContrast(byte[] srcBuffer, byte[] dstBuffer, FilterSettings filters)
	{
		float brightness = (float)filters.Brightness / 100f;
		float contrast = (float)filters.Contrast / 100f;
		int totalPixels = srcBuffer.Length / 4;
		Parallel.For(0, totalPixels, (int i) =>
		{
			int num = i * 4;
			float num2 = (float)(int)srcBuffer[num + 2] / 255f;
			float num3 = (float)(int)srcBuffer[num + 1] / 255f;
			float num4 = (float)(int)srcBuffer[num] / 255f;
			num2 = Clamp((num2 - 0.5f) * contrast + 0.5f + brightness, 0f, 1f);
			num3 = Clamp((num3 - 0.5f) * contrast + 0.5f + brightness, 0f, 1f);
			num4 = Clamp((num4 - 0.5f) * contrast + 0.5f + brightness, 0f, 1f);
			dstBuffer[num + 2] = (byte)(num2 * 255f);
			dstBuffer[num + 1] = (byte)(num3 * 255f);
			dstBuffer[num] = (byte)(num4 * 255f);
			dstBuffer[num + 3] = srcBuffer[num + 3];
		});
	}

	private void ApplyGammaCorrection(byte[] dstBuffer, FilterSettings filters)
	{
		float gamma = (float)filters.Gamma / 100f;
		byte[] gammaLUT = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			gammaLUT[i] = (byte)Math.Min(255, (int)(Math.Pow((double)i / 255.0, 1.0 / (double)gamma) * 255.0 + 0.5));
		}
		Parallel.For(0, dstBuffer.Length / 4, (int num2) =>
		{
			int num = num2 * 4;
			dstBuffer[num] = gammaLUT[dstBuffer[num]];
			dstBuffer[num + 1] = gammaLUT[dstBuffer[num + 1]];
			dstBuffer[num + 2] = gammaLUT[dstBuffer[num + 2]];
		});
	}

	private void ApplySharpness(byte[] dstBuffer, FilterSettings filters, int width, int height)
	{
		if (filters.Sharpness == 100)
		{
			return;
		}
		double factor = (double)filters.Sharpness / 100.0;
		double[,] sharpnessFilter = new double[3, 3]
		{
			{
				-1.0 * factor,
				-1.0 * factor,
				-1.0 * factor
			},
			{
				-1.0 * factor,
				8.0 * factor + 1.0,
				-1.0 * factor
			},
			{
				-1.0 * factor,
				-1.0 * factor,
				-1.0 * factor
			}
		};
		byte[] tempBuffer = (byte[])dstBuffer.Clone();
		int stride = width * 4;
		Parallel.For(1, height - 1, (int y) =>
		{
			for (int i = 1; i < width - 1; i++)
			{
				int num = y * stride + i * 4;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				for (int j = 0; j < 3; j++)
				{
					for (int k = 0; k < 3; k++)
					{
						int num5 = i - 1 + k;
						int num6 = y - 1 + j;
						int num7 = num6 * stride + num5 * 4;
						num2 += (double)(int)tempBuffer[num7 + 2] * sharpnessFilter[k, j];
						num3 += (double)(int)tempBuffer[num7 + 1] * sharpnessFilter[k, j];
						num4 += (double)(int)tempBuffer[num7] * sharpnessFilter[k, j];
					}
				}
				dstBuffer[num + 2] = (byte)Clamp((float)num2, 0f, 255f);
				dstBuffer[num + 1] = (byte)Clamp((float)num3, 0f, 255f);
				dstBuffer[num] = (byte)Clamp((float)num4, 0f, 255f);
			}
		});
	}

	private void ApplySaturation(byte[] dstBuffer, FilterSettings filters)
	{
		float saturationFactor = Clamp((float)filters.Saturation / 100f, 0f, 2f);
		int pixelCount = dstBuffer.Length / 4;
		Parallel.For(0, pixelCount, (int i) =>
		{
			int num = i * 4;
			float r = (float)(int)dstBuffer[num + 2] / 255f;
			float g = (float)(int)dstBuffer[num + 1] / 255f;
			float b = (float)(int)dstBuffer[num] / 255f;
			RGBToHSV(r, g, b, out var h, out var s, out var v);
			s = Clamp(s * saturationFactor, 0f, 1f);
			HSVToRGB(h, s, v, out var r2, out var g2, out var b2);
			dstBuffer[num + 2] = (byte)Clamp(r2 * 255f, 0f, 255f);
			dstBuffer[num + 1] = (byte)Clamp(g2 * 255f, 0f, 255f);
			dstBuffer[num] = (byte)Clamp(b2 * 255f, 0f, 255f);
		});
	}

	private void ApplyHueRotation(byte[] dstBuffer, FilterSettings filters)
	{
		int hueShift = filters.Hue;
		int pixelCount = dstBuffer.Length / 4;
		Parallel.For(0, pixelCount, (int i) =>
		{
			int num = i * 4;
			float r = (float)(int)dstBuffer[num + 2] / 255f;
			float g = (float)(int)dstBuffer[num + 1] / 255f;
			float b = (float)(int)dstBuffer[num] / 255f;
			RGBToHSV(r, g, b, out var h, out var s, out var v);
			h = (h + (float)hueShift) % 360f;
			if (h < 0f)
			{
				h += 360f;
			}
			HSVToRGB(h, s, v, out var r2, out var g2, out var b2);
			dstBuffer[num + 2] = (byte)Clamp(r2 * 255f, 0f, 255f);
			dstBuffer[num + 1] = (byte)Clamp(g2 * 255f, 0f, 255f);
			dstBuffer[num] = (byte)Clamp(b2 * 255f, 0f, 255f);
		});
	}

	private float Clamp(float value, float min, float max)
	{
		float result;
		if (value < min)
		{
			result = min;
		}
		else
		{
			result = ((value > max) ? max : value);
		}
		return result;
	}

	private void RGBToHSV(float r, float g, float b, out float h, out float s, out float v)
	{
		float max = Math.Max(r, Math.Max(g, b));
		float min = Math.Min(r, Math.Min(g, b));
		float delta = max - min;
		v = max;
		if (max == 0f)
		{
			s = 0f;
			h = 0f;
			return;
		}
		s = delta / max;
		if (delta == 0f)
		{
			h = 0f;
		}
		else if (max == r)
		{
			h = (60f * ((g - b) / delta) + 360f) % 360f;
		}
		else if (max == g)
		{
			h = (60f * ((b - r) / delta) + 120f) % 360f;
		}
		else
		{
			h = (60f * ((r - g) / delta) + 240f) % 360f;
		}
	}

	private void HSVToRGB(float h, float s, float v, out float r, out float g, out float b)
	{
		float c = v * s;
		float x = c * (1f - Math.Abs(h / 60f % 2f - 1f));
		float m = v - c;
		if (0f <= h && h < 60f)
		{
			r = c;
			g = x;
			b = 0f;
		}
		else if (60f <= h && h < 120f)
		{
			r = x;
			g = c;
			b = 0f;
		}
		else if (120f <= h && h < 180f)
		{
			r = 0f;
			g = c;
			b = x;
		}
		else if (180f <= h && h < 240f)
		{
			r = 0f;
			g = x;
			b = c;
		}
		else if (240f <= h && h < 300f)
		{
			r = x;
			g = 0f;
			b = c;
		}
		else
		{
			r = c;
			g = 0f;
			b = x;
		}
		r += m;
		g += m;
		b += m;
	}

	public void LoadCheckCondition()
	{
		if (!File.Exists("D:\\Clorium Data\\Clorium_ColorCompensation.json"))
		{
			SetDefaultValues();
			SaveCheckCondition();
			return;
		}
		try
		{
			string jsonString = File.ReadAllText("D:\\Clorium Data\\Clorium_ColorCompensation.json");
			FilterCondition condition = JsonSerializer.Deserialize<FilterCondition>(jsonString);
			if (condition != null)
			{
				ShowBrightness = condition.ShowBrightness;
				ShowContrast = condition.ShowContrast;
				ShowGamma = condition.ShowGamma;
				ShowSharpness = condition.ShowSharpness;
				ShowSaturation = condition.ShowSaturation;
				ShowHue = condition.ShowHue;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
	}

	public void SaveCheckCondition()
	{
		try
		{
			FilterCondition condition = new FilterCondition
			{
				ShowBrightness = ShowBrightness,
				ShowContrast = ShowContrast,
				ShowGamma = ShowGamma,
				ShowSharpness = ShowSharpness,
				ShowSaturation = ShowSaturation,
				ShowHue = ShowHue
			};
			string directory = Path.GetDirectoryName("D:\\Clorium Data\\Clorium_ColorCompensation.json");
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			string jsonString = JsonSerializer.Serialize(condition, options);
			File.WriteAllText("D:\\Clorium Data\\Clorium_ColorCompensation.json", jsonString);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
	}

	private void SetDefaultValues()
	{
		ShowBrightness = true;
		ShowContrast = true;
		ShowGamma = false;
		ShowSharpness = false;
		ShowSaturation = false;
		ShowHue = false;
	}
}
