using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloriumAutoSizing;
public partial class Form4 : Form
{
    public class FilterSettings
    {
        public int Brightness { get; set; }
        public int Contrast { get; set; } = 100;
        public int Gamma { get; set; } = 100;
        public int Sharpness { get; set; } = 100;
        public int Saturation { get; set; } = 100;
        public int Hue { get; set; }
        public int GrayScale { get; set; }

        public FilterSettings Clone()
        {
            return new FilterSettings
            {
                Brightness = Brightness,
                Contrast = Contrast,
                Gamma = Gamma,
                Sharpness = Sharpness,
                Saturation = Saturation,
                Hue = Hue,
                GrayScale = GrayScale
            };
        }
    }

    public class HistoryManager
    {
        private Stack<FilterSettings> undo = new Stack<FilterSettings>();
        private Stack<FilterSettings> redo = new Stack<FilterSettings>();
        public void Push(FilterSettings current)
        {
            undo.Push(current.Clone());
            redo.Clear();
        }

        public FilterSettings Undo()
        {
            if (undo.Count == 0)
            {
                return null;
            }

            if (undo.Count > 1)
            {
                redo.Push(undo.Pop());
                return undo.Peek();
            }

            if (undo.Count == 1)
            {
                redo.Push(undo.Pop());
                FilterSettings current = new FilterSettings
                {
                    Brightness = 0,
                    Contrast = 100,
                    Gamma = 100,
                    Sharpness = 100,
                    Saturation = 100,
                    Hue = 0,
                    GrayScale = 0
                };
                Push(current);
            }

            return undo.Peek();
        }

        public FilterSettings Redo()
        {
            if (redo.Count > 0)
            {
                undo.Push(redo.Pop());
                return undo.Peek();
            }

            return undo.Peek();
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }
    }

    private F_Measure mainForm;
    private Bitmap originalImage;
    private FilterSettings filters = new FilterSettings();
    private readonly HistoryManager history = new HistoryManager();
    private bool suppressEvent;
    private CancellationTokenSource cts;
    public Form4(F_Measure form1)
    {
        InitializeComponent();
        mainForm = form1;
    }

    private void Form4_Load(object sender, EventArgs e)
    {
        if (mainForm.ExposureImage != null)
        {
            originalImage = mainForm.ExposureImage.Clone() as Bitmap;
            filters = new FilterSettings();
            history.Clear();
            SetTrkBarValue(filters);
            SetNumericUpDownValue(filters);
            UpdateFilteredImage(filters);
        }
    }

    private FilterSettings ResetFilterSettings(FilterSettings current)
    {
        history.Clear();
        current = new FilterSettings
        {
            Brightness = 0,
            Contrast = 100,
            Gamma = 100,
            Sharpness = 100,
            Saturation = 100,
            Hue = 0,
            GrayScale = 0
        };
        return current;
    }

    private FilterSettings SetFilterSettingsFromTrkBar(FilterSettings current)
    {
        current = new FilterSettings
        {
            Brightness = trkBrightness.Value,
            Contrast = trkContrast.Value,
            Gamma = trkGamma.Value,
            Sharpness = trkSharpness.Value,
            Saturation = trkSaturation.Value,
            Hue = trkHue.Value,
            GrayScale = trkGrayscale.Value
        };
        return current;
    }

    private FilterSettings SetFilterSettingsFromNumericUpDown(FilterSettings current)
    {
        current = new FilterSettings
        {
            Brightness = (int)numBrightness.Value,
            Contrast = (int)numContrast.Value,
            Gamma = (int)numGamma.Value,
            Sharpness = (int)numSharpness.Value,
            Saturation = (int)numSaturation.Value,
            Hue = (int)numHue.Value,
            GrayScale = (int)numGrayscale.Value
        };
        return current;
    }

    private void SetTrkBarValue(FilterSettings current)
    {
        trkBrightness.Value = current.Brightness;
        trkContrast.Value = current.Contrast;
        trkGamma.Value = current.Gamma;
        trkSharpness.Value = current.Sharpness;
        trkSaturation.Value = current.Saturation;
        trkHue.Value = current.Hue;
        trkGrayscale.Value = current.GrayScale;
    }

    private void SetNumericUpDownValue(FilterSettings current)
    {
        suppressEvent = true;
        numBrightness.Value = current.Brightness;
        numContrast.Value = current.Contrast;
        numGamma.Value = current.Gamma;
        numSharpness.Value = current.Sharpness;
        numSaturation.Value = current.Saturation;
        numHue.Value = current.Hue;
        numGrayscale.Value = current.GrayScale;
        suppressEvent = false;
    }

    private async void TrackBar_Scroll(object sender, EventArgs e)
    {
        if (await PreventAccumulativeEvent())
        {
            filters = SetFilterSettingsFromTrkBar(filters);
            SetNumericUpDownValue(filters);
            UpdateFilteredImage(filters);
        }
    }

    private void trk_MouseUp(object sender, MouseEventArgs e)
    {
        history.Push(filters);
    }

    private async void numericUpDown_ValueChanged(object sender, EventArgs e)
    {
        if (!suppressEvent && await PreventAccumulativeEvent())
        {
            filters = SetFilterSettingsFromNumericUpDown(filters);
            SetTrkBarValue(filters);
            UpdateFilteredImage(filters);
            history.Push(filters);
        }
    }

    // 유지보수 설명(복구 후 추가): 새 조작이 오면 이전 지연을 취소하고 100ms 뒤 마지막 조작만 처리한다. 취소/Dispose 순서와 async 이벤트 흐름을 보존하며 동시 UI 실행은 검증하지 않았다.
    private async Task<bool> PreventAccumulativeEvent()
    {
        CancellationTokenSource cancellationTokenSource = cts;
        cts = new CancellationTokenSource();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        CancellationToken token = cts.Token;
        try
        {
            await Task.Delay(100, token);
            if (token.IsCancellationRequested)
            {
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            return false;
        }

        return true;
    }

    private void Redo_Click(object sender, EventArgs e)
    {
        filters = history.Redo();
        SetTrkBarValue(filters);
        SetNumericUpDownValue(filters);
        UpdateFilteredImage(filters);
    }

    private void Undo_Click(object sender, EventArgs e)
    {
        filters = history.Undo();
        if (filters != null)
        {
            SetTrkBarValue(filters);
            SetNumericUpDownValue(filters);
            UpdateFilteredImage(filters);
        }
    }

    private void Reset_Click(object sender, EventArgs e)
    {
        filters = ResetFilterSettings(filters);
        SetTrkBarValue(filters);
        SetNumericUpDownValue(filters);
        UpdateFilteredImage(filters);
    }

    private void UpdateFilteredImage(FilterSettings current)
    {
        pictureBox.Image?.Dispose();
        pictureBox.Image = ProcessImage((Bitmap)originalImage.Clone(), filters);
        mainForm.pictureBox1.Image = pictureBox.Image;
    }

    // 유지보수 설명(복구 후 추가): 32bpp ARGB LockBits 버퍼를 밝기/대비→감마→선명도→채도→색상 순으로 처리한다. 바이트는 B,G,R,A 순서이며 알파 채널은 원본처럼 보존한다. GrayScale 설정은 이 처리 경로에서 적용되지 않는다. 원본 stride/버퍼 크기 가정도 유지한다.
    private Bitmap ProcessImage(Bitmap image, FilterSettings filters)
    {
        int width = image.Width;
        int height = image.Height;
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData bitmapData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData bitmapData2 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        int pixelBufferByteCount = bitmapData.Stride * height;
        byte[] sourcePixels = new byte[pixelBufferByteCount];
        byte[] filteredPixels = new byte[pixelBufferByteCount];
        Marshal.Copy(bitmapData.Scan0, sourcePixels, 0, pixelBufferByteCount);
        ApplyBrightnessContrast(sourcePixels, filteredPixels, filters);
        ApplyGammaCorrection(filteredPixels, filters);
        ApplySharpness(filteredPixels, filters, image.Width, image.Height);
        ApplySaturation(filteredPixels, filters);
        ApplyHueRotation(filteredPixels, filters);
        Marshal.Copy(filteredPixels, 0, bitmapData2.Scan0, pixelBufferByteCount);
        image.UnlockBits(bitmapData);
        bitmap.UnlockBits(bitmapData2);
        return bitmap;
    }

    // 유지보수 설명(복구 후 추가): RGB 바이트를 0~1로 환산해 대비 배수와 밝기 오프셋을 적용하고 잘라낸다. 각 계수의 기준은 UI 값/100이며 알파는 복사한다.
    private void ApplyBrightnessContrast(byte[] srcBuffer, byte[] dstBuffer, FilterSettings filters)
    {
        float brightnessOffset = (float)filters.Brightness / 100f;
        float contrastFactor = (float)filters.Contrast / 100f;
        for (int i = 0; i < srcBuffer.Length; i += 4)
        {
            float normalizedRed = (float)(int)srcBuffer[i + 2] / 255f;
            float normalizedGreen = (float)(int)srcBuffer[i + 1] / 255f;
            float normalizedBlue = (float)(int)srcBuffer[i] / 255f;
            normalizedRed = Clamp((normalizedRed - 0.5f) * contrastFactor + 0.5f + brightnessOffset, 0f, 1f);
            normalizedGreen = Clamp((normalizedGreen - 0.5f) * contrastFactor + 0.5f + brightnessOffset, 0f, 1f);
            normalizedBlue = Clamp((normalizedBlue - 0.5f) * contrastFactor + 0.5f + brightnessOffset, 0f, 1f);
            dstBuffer[i + 2] = (byte)(normalizedRed * 255f);
            dstBuffer[i + 1] = (byte)(normalizedGreen * 255f);
            dstBuffer[i] = (byte)(normalizedBlue * 255f);
            dstBuffer[i + 3] = srcBuffer[i + 3];
        }
    }

    // 유지보수 설명(복구 후 추가): UI 감마 값/100의 역수를 사용해 256개 밝기 대응표를 만든다. RGB 채널에만 적용하고 UI/입력 계수의 원본 범위를 임의 변경하지 않는다.
    private void ApplyGammaCorrection(byte[] dstBuffer, FilterSettings filters)
    {
        float gammaFactor = (float)filters.Gamma / 100f;
        byte[] gammaLookup = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            gammaLookup[i] = (byte)Math.Min(255, (int)(Math.Pow((double)i / 255.0, 1.0 / (double)gammaFactor) * 255.0 + 0.5));
        }

        for (int j = 0; j < dstBuffer.Length; j += 4)
        {
            dstBuffer[j] = gammaLookup[dstBuffer[j]];
            dstBuffer[j + 1] = gammaLookup[dstBuffer[j + 1]];
            dstBuffer[j + 2] = gammaLookup[dstBuffer[j + 2]];
        }
    }

    // 유지보수 설명(복구 후 추가): 복제 버퍼에서 이웃을 읽어 3x3 커널 합을 출력 RGB에 쓴다. 가장자리와 알파는 유지하고 Sharpness 100은 원본대로 처리를 생략한다. 행 위치는 폭*4 바이트 가정을 사용한다.
    private void ApplySharpness(byte[] dstBuffer, FilterSettings filters, int width, int height)
    {
        if (filters.Sharpness == 100)
        {
            return;
        }

        double sharpnessFactor = (double)filters.Sharpness / 100.0;
        double[, ] sharpnessKernel = new double[3, 3]
        {
            {
                -1.0 * sharpnessFactor,
                -1.0 * sharpnessFactor,
                -1.0 * sharpnessFactor
            },
            {
                -1.0 * sharpnessFactor,
                8.0 * sharpnessFactor + 1.0,
                -1.0 * sharpnessFactor
            },
            {
                -1.0 * sharpnessFactor,
                -1.0 * sharpnessFactor,
                -1.0 * sharpnessFactor
            }
        };
        byte[] sourcePixels = (byte[])dstBuffer.Clone();
        int rowStrideBytes = width * 4;
        for (int i = 1; i < height - 1; i++)
        {
            for (int j = 1; j < width - 1; j++)
            {
                int destinationPixelOffset = i * rowStrideBytes + j * 4;
                double redSum = 0.0;
                double greenSum = 0.0;
                double blueSum = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        int neighborX = j - 1 + l;
                        int sourcePixelOffset = (i - 1 + k) * rowStrideBytes + neighborX * 4;
                        redSum += (double)(int)sourcePixels[sourcePixelOffset + 2] * sharpnessKernel[l, k];
                        greenSum += (double)(int)sourcePixels[sourcePixelOffset + 1] * sharpnessKernel[l, k];
                        blueSum += (double)(int)sourcePixels[sourcePixelOffset] * sharpnessKernel[l, k];
                    }
                }

                dstBuffer[destinationPixelOffset + 2] = (byte)Clamp((float)redSum, 0f, 255f);
                dstBuffer[destinationPixelOffset + 1] = (byte)Clamp((float)greenSum, 0f, 255f);
                dstBuffer[destinationPixelOffset] = (byte)Clamp((float)blueSum, 0f, 255f);
            }
        }
    }

    // 유지보수 설명(복구 후 추가): 0~1 RGB를 HSV로 바꾸고 채도에 0~2 계수를 곱해 다시 RGB로 돌린다. 색상과 밝기는 해당 단계에서 유지한다.
    private void ApplySaturation(byte[] dstBuffer, FilterSettings filters)
    {
        float saturationFactor = Clamp((float)filters.Saturation / 100f, 0f, 2f);
        for (int i = 0; i < dstBuffer.Length; i += 4)
        {
            float r = (float)(int)dstBuffer[i + 2] / 255f;
            float g = (float)(int)dstBuffer[i + 1] / 255f;
            float b = (float)(int)dstBuffer[i] / 255f;
            RGBToHSV(r, g, b, out var h, out var s, out var v);
            s = Clamp(s * saturationFactor, 0f, 1f);
            HSVToRGB(h, s, v, out var r2, out var g2, out var b2);
            dstBuffer[i + 2] = (byte)Clamp(r2 * 255f, 0f, 255f);
            dstBuffer[i + 1] = (byte)Clamp(g2 * 255f, 0f, 255f);
            dstBuffer[i] = (byte)Clamp(b2 * 255f, 0f, 255f);
        }
    }

    // 유지보수 설명(복구 후 추가): HSV 색상에 UI Hue 값을 더하고 원본의 360 나머지 연산으로 회전한다. 각도 단위는 도이며 음수 나머지의 기존 처리를 별도 보정하지 않는다.
    private void ApplyHueRotation(byte[] dstBuffer, FilterSettings filters)
    {
        int hue = filters.Hue;
        for (int i = 0; i < dstBuffer.Length; i += 4)
        {
            float r = (float)(int)dstBuffer[i + 2] / 255f;
            float g = (float)(int)dstBuffer[i + 1] / 255f;
            float b = (float)(int)dstBuffer[i] / 255f;
            RGBToHSV(r, g, b, out var h, out var s, out var v);
            h = (h + (float)hue) % 360f;
            HSVToRGB(h, s, v, out var r2, out var g2, out var b2);
            dstBuffer[i + 2] = (byte)Clamp(r2 * 255f, 0f, 255f);
            dstBuffer[i + 1] = (byte)Clamp(g2 * 255f, 0f, 255f);
            dstBuffer[i] = (byte)Clamp(b2 * 255f, 0f, 255f);
        }
    }

    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    // 유지보수 설명(복구 후 추가): 0~1 RGB에서 최대/최소 채널 차이를 구해 색상(도), 채도/밝기를 반환한다. 검정과 무채색의 원본 분기를 유지한다.
    private void RGBToHSV(float r, float g, float b, out float h, out float s, out float v)
    {
        float maxChannel = Math.Max(r, Math.Max(g, b));
        float minChannel = Math.Min(r, Math.Min(g, b));
        float channelRange = maxChannel - minChannel;
        v = maxChannel;
        if (maxChannel == 0f)
        {
            s = 0f;
            h = 0f;
            return;
        }

        s = channelRange / maxChannel;
        if (channelRange == 0f)
        {
            h = 0f;
        }
        else if (maxChannel == r)
        {
            h = (60f * ((g - b) / channelRange) + 360f) % 360f;
        }
        else if (maxChannel == g)
        {
            h = (60f * ((b - r) / channelRange) + 120f) % 360f;
        }
        else
        {
            h = (60f * ((r - g) / channelRange) + 240f) % 360f;
        }
    }

    // 유지보수 설명(복구 후 추가): 색상 60도 구간별 색도 배분과 공통 밝기 오프셋으로 0~1 RGB를 반환한다. 호출부의 바이트 범위 제한과 연결된다.
    private void HSVToRGB(float h, float s, float v, out float r, out float g, out float b)
    {
        float chroma = v * s;
        float secondaryChroma = chroma * (1f - Math.Abs(h / 60f % 2f - 1f));
        float valueOffset = v - chroma;
        if (0f <= h && h < 60f)
        {
            r = chroma;
            g = secondaryChroma;
            b = 0f;
        }
        else if (60f <= h && h < 120f)
        {
            r = secondaryChroma;
            g = chroma;
            b = 0f;
        }
        else if (120f <= h && h < 180f)
        {
            r = 0f;
            g = chroma;
            b = secondaryChroma;
        }
        else if (180f <= h && h < 240f)
        {
            r = 0f;
            g = secondaryChroma;
            b = chroma;
        }
        else if (240f <= h && h < 300f)
        {
            r = secondaryChroma;
            g = 0f;
            b = chroma;
        }
        else
        {
            r = chroma;
            g = 0f;
            b = secondaryChroma;
        }

        r += valueOffset;
        g += valueOffset;
        b += valueOffset;
    }
}
