using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraControl.Core.Wpf;

public static class WriteableBitmapExtensionsLocal
{
	public unsafe static void Blit(this WriteableBitmap bmp, Rect destRect, WriteableBitmap source, Rect sourceRect, Color color, WriteableBitmapExtensions.BlendMode blendMode)
	{
		if (color.A == 0)
		{
			return;
		}
		bool flag1 = source.Format == PixelFormats.Pbgra32 || source.Format == PixelFormats.Prgba64 || source.Format == PixelFormats.Prgba128Float;
		int num1 = (int)destRect.Width;
		int num2 = (int)destRect.Height;
		using BitmapContext bitmapContext1 = source.GetBitmapContext(ReadWriteMode.ReadOnly);
		using BitmapContext bitmapContext2 = bmp.GetBitmapContext();
		int width1 = bitmapContext1.Width;
		int width2 = bitmapContext2.Width;
		int height = bitmapContext2.Height;
		Rect rect = new Rect(0.0, 0.0, width2, height);
		rect.Intersect(destRect);
		if (rect.IsEmpty)
		{
			return;
		}
		int* pixels1 = bitmapContext1.Pixels;
		int* pixels2 = bitmapContext2.Pixels;
		int length = bitmapContext1.Length;
		int num3 = (int)destRect.X;
		int num4 = (int)destRect.Y;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = color.A;
		int num10 = color.R;
		int num11 = color.G;
		int num12 = color.B;
		bool flag2 = color != Colors.White;
		int num13 = (int)sourceRect.Width;
		double num14 = sourceRect.Width / destRect.Width;
		double num15 = sourceRect.Height / destRect.Height;
		int num16 = (int)sourceRect.X;
		int num17 = (int)sourceRect.Y;
		int num18 = -1;
		int num19 = -1;
		double num20 = num17;
		int num21 = num4;
		for (int index1 = 0; index1 < num2; index1++)
		{
			if (num21 >= 0 && num21 < height)
			{
				double num22 = num16;
				int index2 = num3 + num21 * width2;
				int num23 = num3;
				int num24 = *pixels1;
				if (blendMode == WriteableBitmapExtensions.BlendMode.None && !flag2)
				{
					int num25 = (int)num22 + (int)num20 * width1;
					int num26 = ((num23 < 0) ? (-num23) : 0);
					int num27 = num23 + num26;
					int num28 = width1 - num26;
					int num29 = ((num27 + num28 < width2) ? num28 : (width2 - num27));
					if (num29 > num13)
					{
						num29 = num13;
					}
					if (num29 > num1)
					{
						num29 = num1;
					}
					BitmapContext.BlockCopy(bitmapContext1, (num25 + num26) * 4, bitmapContext2, (index2 + num26) * 4, num29 * 4);
				}
				else
				{
					for (int i = 0; i < num1; i++)
					{
						if (num23 >= 0 && num23 < width2)
						{
							if ((int)num22 != num18 || (int)num20 != num19)
							{
								int index4 = (int)num22 + (int)num20 * width1;
								if (index4 >= 0 && index4 < length)
								{
									num24 = pixels1[index4];
									num8 = (num24 >> 24) & 0xFF;
									num5 = (num24 >> 16) & 0xFF;
									num6 = (num24 >> 8) & 0xFF;
									num7 = num24 & 0xFF;
									if (flag2 && num8 != 0)
									{
										num8 = num8 * num9 * 32897 >> 23;
										num5 = (num5 * num10 * 32897 >> 23) * num9 * 32897 >> 23;
										num6 = (num6 * num11 * 32897 >> 23) * num9 * 32897 >> 23;
										num7 = (num7 * num12 * 32897 >> 23) * num9 * 32897 >> 23;
										num24 = (num8 << 24) | (num5 << 16) | (num6 << 8) | num7;
									}
								}
								else
								{
									num8 = 0;
								}
							}
							switch (blendMode)
							{
							case WriteableBitmapExtensions.BlendMode.None:
								pixels2[index2] = num24;
								break;
							case WriteableBitmapExtensions.BlendMode.ColorKeying:
								num5 = (num24 >> 16) & 0xFF;
								num6 = (num24 >> 8) & 0xFF;
								num7 = num24 & 0xFF;
								if (num5 != color.R || num6 != color.G || num7 != color.B)
								{
									pixels2[index2] = num24;
								}
								break;
							case WriteableBitmapExtensions.BlendMode.Mask:
							{
								int num45 = pixels2[index2];
								int num46 = (num45 >> 24) & 0xFF;
								int num47 = (num45 >> 16) & 0xFF;
								int num48 = (num45 >> 8) & 0xFF;
								int num49 = num45 & 0xFF;
								int num50 = (num46 * num8 * 32897 >> 23 << 24) | (num47 * num8 * 32897 >> 23 << 16) | (num48 * num8 * 32897 >> 23 << 8) | (num49 * num8 * 32897 >> 23);
								pixels2[index2] = num50;
								break;
							}
							default:
							{
								if (num8 <= 0)
								{
									break;
								}
								int num30 = pixels2[index2];
								int num31 = (num30 >> 24) & 0xFF;
								if ((num8 == 255 || num31 == 0) && blendMode != WriteableBitmapExtensions.BlendMode.Additive && blendMode != WriteableBitmapExtensions.BlendMode.Subtractive && blendMode != WriteableBitmapExtensions.BlendMode.Multiply)
								{
									pixels2[index2] = num24;
									break;
								}
								int num32 = (num30 >> 16) & 0xFF;
								int num33 = (num30 >> 8) & 0xFF;
								int num34 = num30 & 0xFF;
								switch (blendMode)
								{
								case WriteableBitmapExtensions.BlendMode.Alpha:
								{
									int num44 = 255 - num8;
									num30 = ((!flag1) ? (((num31 & 0xFF) << 24) | (((num5 * num8 + num44 * num32 >> 8) & 0xFF) << 16) | (((num6 * num8 + num44 * num33 >> 8) & 0xFF) << 8) | ((num7 * num8 + num44 * num34 >> 8) & 0xFF)) : (((num31 & 0xFF) << 24) | ((((num5 << 8) + num44 * num32 >> 8) & 0xFF) << 16) | ((((num6 << 8) + num44 * num33 >> 8) & 0xFF) << 8) | (((num7 << 8) + num44 * num34 >> 8) & 0xFF)));
									break;
								}
								case WriteableBitmapExtensions.BlendMode.Additive:
								{
									int num43 = ((255 <= num8 + num31) ? 255 : (num8 + num31));
									num30 = (num43 << 24) | (((num43 <= num5 + num32) ? num43 : (num5 + num32)) << 16) | (((num43 <= num6 + num33) ? num43 : (num6 + num33)) << 8) | ((num43 <= num7 + num34) ? num43 : (num7 + num34));
									break;
								}
								case WriteableBitmapExtensions.BlendMode.Subtractive:
									num30 = (num31 << 24) | (((num5 < num32) ? (num5 - num32) : 0) << 16) | (((num6 < num33) ? (num6 - num33) : 0) << 8) | ((num7 < num34) ? (num7 - num34) : 0);
									break;
								case WriteableBitmapExtensions.BlendMode.Multiply:
								{
									int num35 = num8 * num31 + 128;
									int num36 = num5 * num32 + 128;
									int num37 = num6 * num33 + 128;
									int num38 = num7 * num34 + 128;
									int num39 = (num35 >> 8) + num35 >> 8;
									int num40 = (num36 >> 8) + num36 >> 8;
									int num41 = (num37 >> 8) + num37 >> 8;
									int num42 = (num38 >> 8) + num38 >> 8;
									num30 = (num39 << 24) | (((num39 <= num40) ? num39 : num40) << 16) | (((num39 <= num41) ? num39 : num41) << 8) | ((num39 <= num42) ? num39 : num42);
									break;
								}
								}
								pixels2[index2] = num30;
								break;
							}
							}
						}
						num23++;
						index2++;
						num22 += num14;
					}
				}
			}
			num20 += num15;
			num21++;
		}
	}

	public unsafe static void FillRectangleDeBlend(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
	{
		using BitmapContext context = bmp.GetBitmapContext();
		int w = bmp.PixelWidth;
		int h = bmp.PixelHeight;
		int* pixels = context.Pixels;
		if (x1 < 0)
		{
			x1 = 0;
		}
		if (y1 < 0)
		{
			y1 = 0;
		}
		if (x2 < 0)
		{
			x2 = 0;
		}
		if (y2 < 0)
		{
			y2 = 0;
		}
		if (x1 >= w)
		{
			x1 = w - 1;
		}
		if (y1 >= h)
		{
			y1 = h - 1;
		}
		if (x2 >= w)
		{
			x2 = w - 1;
		}
		if (y2 >= h)
		{
			y2 = h - 1;
		}
		for (int i = y1; i <= y2; i++)
		{
			for (int j = i * w + x1; j < i * w + x2; j++)
			{
				byte oneOverAlpha = (byte)(255 - color.A);
				int c = pixels[j];
				int r = ((byte)(c >> 16) << 8) / oneOverAlpha;
				int g = ((byte)(c >> 8) << 8) / oneOverAlpha;
				int b = ((byte)c << 8) / oneOverAlpha;
				pixels[j] = -16777216 | (r << 16) | (g << 8) | b;
			}
		}
	}

	public unsafe static void FillRectangle2(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
	{
		using BitmapContext context = bmp.GetBitmapContext();
		int w = bmp.PixelWidth;
		int h = bmp.PixelHeight;
		int* pixels = context.Pixels;
		if (x1 < 0)
		{
			x1 = 0;
		}
		if (y1 < 0)
		{
			y1 = 0;
		}
		if (x2 < 0)
		{
			x2 = 0;
		}
		if (y2 < 0)
		{
			y2 = 0;
		}
		if (x1 >= w)
		{
			x1 = w - 1;
		}
		if (y1 >= h)
		{
			y1 = h - 1;
		}
		if (x2 >= w)
		{
			x2 = w - 1;
		}
		if (y2 >= h)
		{
			y2 = h - 1;
		}
		for (int i = y1; i <= y2; i++)
		{
			for (int j = i * w + x1; j <= i * w + x2; j++)
			{
				byte oneOverAlpha = (byte)(255 - color.A);
				int c = pixels[j];
				int r = (byte)(c >> 16) * oneOverAlpha >> 8;
				int g = (byte)(c >> 8) * oneOverAlpha >> 8;
				int b = (byte)c * oneOverAlpha >> 8;
				pixels[j] = -16777216 | (r << 16) | (g << 8) | b;
			}
		}
	}
}
