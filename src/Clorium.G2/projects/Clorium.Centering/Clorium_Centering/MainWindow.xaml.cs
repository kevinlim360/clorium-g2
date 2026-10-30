using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clorium_Centering.Properties;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Clorium_Centering;
public partial class MainWindow : Window, IComponentConnector, IStyleConnector
{
    public struct ObjectInfo
    {
        public Point Centroid;
        public double Width;
        public double Height;
        public Int32Rect CalcArea;
        public int[] Labels;
        public int MaxLabel;
        public int AbsMinX;
        public int AbsMaxY;
        public int AbsMinY;
        public int AbsMaxX;
    }

    private List<string> _imagePaths = new List<string>();
    private List<string> _correctedPaths = new List<string>();
    private Color _fillColor = Colors.Transparent;
    private DispatcherTimer _originalPlayTimer;
    private DispatcherTimer _resultPlayTimer;
    private int _blackThreshold = 200;
    private double _cropRatio = 0.7;
    private string _workFolder = "";
    private CancellationTokenSource _cts;
    public MainWindow()
    {
        InitializeComponent();
        Topmost = true;
        GetServerFolder();
        InitTimer();
    }

    private void GetServerFolder()
    {
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if (commandLineArgs.Length != 0)
        {
            string.Join("\n", commandLineArgs);
        }

        if (commandLineArgs.Length > 1)
        {
            string text = commandLineArgs[1].Replace("\"", "").Trim();
            if (Directory.Exists(text))
            {
                _workFolder = text;
            }
        }

        if (commandLineArgs.Length > 2 && bool.TryParse(commandLineArgs[2], out var result) && result)
        {
            _imagePaths = RefreshList(_workFolder, ListBoxImages);
            if (_imagePaths != null && _imagePaths.Count != 0)
            {
                LoadPreviewImage(_imagePaths[ListBoxImages.SelectedIndex]);
                ListBoxCorrected.Items.Clear();
                ProgressBarCorrection.Value = 0.0;
                BtnCorrectAll_Click(this, null);
            }
        }
    }

    private void InitTimer()
    {
        _originalPlayTimer = new DispatcherTimer();
        _originalPlayTimer.Tick += OriginalPlayTimer_Tick;
        _resultPlayTimer = new DispatcherTimer();
        _resultPlayTimer.Tick += PlayTimer_Tick;
    }

    private void OriginalPlayTimer_Tick(object sender, EventArgs e)
    {
        if (ListBoxImages.Items.Count != 0)
        {
            int num = ListBoxImages.SelectedIndex + 1;
            if (num >= ListBoxImages.Items.Count)
            {
                num = 0;
            }

            ListBoxImages.SelectedIndex = num;
        }
    }

    private void PlayTimer_Tick(object sender, EventArgs e)
    {
        if (ListBoxCorrected.Items.Count != 0)
        {
            int num = ListBoxCorrected.SelectedIndex + 1;
            if (num >= ListBoxCorrected.Items.Count)
            {
                num = 0;
            }

            ListBoxCorrected.SelectedIndex = num;
        }
    }

    private void SliderSpeedOriginal_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_originalPlayTimer != null && _originalPlayTimer.IsEnabled)
        {
            _originalPlayTimer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
        }
    }

    private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_resultPlayTimer != null && _resultPlayTimer.IsEnabled)
        {
            _resultPlayTimer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
        }
    }

    private void BtnPlayOriginal_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxImages.Items.Count != 0)
        {
            _originalPlayTimer.Interval = TimeSpan.FromMilliseconds(SliderSpeedOriginal.Value);
            _originalPlayTimer.Start();
            BtnPlayOriginal.IsEnabled = false;
        }
    }

    private void BtnStopOriginal_Click(object sender, RoutedEventArgs e)
    {
        _originalPlayTimer.Stop();
        BtnPlayOriginal.IsEnabled = true;
    }

    private void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxCorrected.Items.Count != 0)
        {
            _resultPlayTimer.Interval = TimeSpan.FromMilliseconds(SliderSpeed.Value);
            _resultPlayTimer.Start();
            BtnPlay.IsEnabled = false;
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _resultPlayTimer.Stop();
        BtnPlay.IsEnabled = true;
    }

    private void BtnLoadImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dir = LoadImageFolder();
            if (string.IsNullOrEmpty(dir)) return;
            var paths = RefreshList(dir, ListBoxImages);
            if (paths == null) return;
            _imagePaths = paths;
            _correctedPaths.Clear();
            ListBoxCorrected.Items.Clear();
            ProgressBarCorrection.Value = 0.0;
            if (paths.Count == 0) ImgPreview.Source = null;
        }
        catch (Exception ex)
        {
            ReportImageLoadError(ex);
        }
    }

    private void BtnLoadFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dir = LoadImageFolder();
            if (string.IsNullOrEmpty(dir)) return;
            var paths = RefreshList(dir, ListBoxCorrected);
            if (paths != null) _correctedPaths = paths;
            if (paths != null && paths.Count == 0) ImgPreview.Source = null;
        }
        catch (Exception ex)
        {
            ReportImageLoadError(ex);
        }
    }

    private void ReportImageLoadError(Exception ex)
    {
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Clorium", "Logs", "Centering.log");
        string logHint;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.AppendAllText(logPath, DateTime.Now.ToString("s") + Environment.NewLine + ex + Environment.NewLine);
            logHint = "\n\n오류 기록: " + logPath;
        }
        catch
        {
            logHint = "\n\n오류 기록 파일을 저장하지 못했습니다.";
        }
        MessageBox.Show(this, "이미지 폴더 또는 미리보기를 열지 못했습니다.\n" + ex.Message + logHint,
            "Clorium Centering", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private string LoadImageFolder()
    {
        using CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "이미지 폴더를 선택하세요",
            InitialDirectory = (Directory.Exists(Settings.Default.LastPath) ? Settings.Default.LastPath : string.Empty)
        };
        if (_workFolder != "" && Directory.Exists(_workFolder))
        {
            commonOpenFileDialog.InitialDirectory = _workFolder;
        }

        string lastPath = Settings.Default.LastPath;
        if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
        {
            commonOpenFileDialog.InitialDirectory = lastPath;
        }

        if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            string fileName = commonOpenFileDialog.FileName;
            Settings.Default.LastPath = fileName;
            Settings.Default.Save();
            return fileName;
        }

        return null;
    }

    private List<string> RefreshList(string dir, ListBox targetListBox)
    {
        if (Directory.Exists(dir))
        {
            List<string> paths = Enumerable.OrderBy(Directory.GetFiles(dir, "*.jpg"), (string f) => f).ToList();
            Dispatcher.Invoke(() =>
            {
                // SelectionChanged fires synchronously: publish the new paths first.
                if (targetListBox == ListBoxImages) _imagePaths = paths;
                else if (targetListBox == ListBoxCorrected) _correctedPaths = paths;
                targetListBox.Items.Clear();
                foreach (string item in paths)
                {
                    targetListBox.Items.Add(Path.GetFileName(item));
                }

                targetListBox.SelectedIndex = paths.Count == 0 ? -1 : 0;
            });
            return paths;
        }

        return null;
    }

    private void LoadPreviewImage(string path)
    {
        try
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.DecodePixelWidth = 1200;
            bitmapImage.EndInit();
            ImgPreview.Source = bitmapImage;
        }
        catch (Exception ex)
        {
            ImgPreview.Source = null;
            ReportImageLoadError(ex);
        }
    }

    private void ListBoxImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBoxImages.SelectedIndex >= 0 && _imagePaths != null && ListBoxImages.SelectedIndex < _imagePaths.Count)
        {
            LoadPreviewImage(_imagePaths[ListBoxImages.SelectedIndex]);
        }
    }

    private void ListBoxCorrected_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBoxCorrected.SelectedIndex >= 0 && _correctedPaths != null && ListBoxCorrected.SelectedIndex < _correctedPaths.Count)
        {
            LoadPreviewImage(_correctedPaths[ListBoxCorrected.SelectedIndex]);
        }
    }

    private void TxtThreshold_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(TxtThreshold.Text, out var result))
        {
            _blackThreshold = result;
        }
    }

    private void BtnUpThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtThreshold.Text, out var result))
        {
            int num = result + 10;
            if (num > 255)
            {
                num = 255;
            }

            TxtThreshold.Text = num.ToString();
        }

        BtnPreviewThreshold_Click(sender, e);
    }

    private void BtnDownThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtThreshold.Text, out var result))
        {
            int num = result - 10;
            if (num < 0)
            {
                num = 0;
            }

            TxtThreshold.Text = num.ToString();
        }

        BtnPreviewThreshold_Click(sender, e);
    }

    private void TxtCropRatio_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (double.TryParse(TxtCropRatio.Text, out var result))
        {
            if (result > 1.0)
            {
                _cropRatio = result / 100.0;
            }
            else
            {
                _cropRatio = result;
            }
        }
    }

    private void BtnUpCrop_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtCropRatio.Text, out var result))
        {
            int num = result + 10;
            if (num > 100)
            {
                num = 100;
            }

            TxtCropRatio.Text = num.ToString();
        }
    }

    private void BtnDownCrop_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtCropRatio.Text, out var result))
        {
            int num = result - 10;
            if (num < 50)
            {
                num = 50;
            }

            TxtCropRatio.Text = num.ToString();
        }
    }

    // 선택한 영역 또는 전체 이미지에서 밝은 경계 연결 배경을 제외하고 최대 연결요소의 중심·경계를 구한다.
    // 좌표 결과는 이미지 픽셀 단위이며, 전경이 없을 때의 기본값도 원본 처리 그대로 유지한다.
    private ObjectInfo GetObjectInfo(WriteableBitmap bitmap, int threshold, bool preview, Int32Rect? regionOfInterest = null)
    {
        Int32Rect analysisArea = regionOfInterest ?? new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
        int[] labels = new int[analysisArea.Width * analysisArea.Height];
        MarkBackground(bitmap, labels, analysisArea, threshold, preview);
        int largestComponentLabel = FindLargestBlob(labels, analysisArea);
        CalculateObjectStats(labels, analysisArea, largestComponentLabel, out var centroid, out var boundingBox);
        return new ObjectInfo
        {
            Centroid = centroid,
            Width = boundingBox.Width,
            Height = boundingBox.Height,
            CalcArea = analysisArea,
            Labels = labels,
            MaxLabel = largestComponentLabel,
            AbsMinX = (int)boundingBox.Left,
            AbsMaxX = (int)boundingBox.Right,
            AbsMinY = (int)boundingBox.Top,
            AbsMaxY = (int)boundingBox.Bottom
        };
    }

    // 영역 경계에서 시작해 밝기 임계값 이상인 BGR 픽셀을 사방 연결로 탐색한다.
    // labels의 -1은 배경, 0은 미분류를 뜻한다. preview가 참이면 배경 픽셀을 검게/투명하게 바꾼다.
    // 픽셀 버퍼는 최소 BGR 3바이트 형식을 전제로 접근하며 예외 시 Unlock을 보장하는 finally는 원본에 없다.
    private unsafe void MarkBackground(WriteableBitmap bitmap, int[] labels, Int32Rect area, int threshold, bool preview)
    {
        _ = bitmap.PixelWidth;
        int rowStrideBytes = bitmap.BackBufferStride;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int[] backgroundQueue = new int[labels.Length];
        int queueReadIndex = 0;
        int queueWriteIndex = 0;
        bitmap.Lock();
        byte* pixelBuffer = (byte*)(void*)bitmap.BackBuffer;
        for (int boundaryX = 0; boundaryX < area.Width; boundaryX++)
        {
            AddBg(boundaryX, 0, backgroundQueue, ref queueWriteIndex, labels, pixelBuffer, area, rowStrideBytes, bytesPerPixel, threshold);
            AddBg(boundaryX, area.Height - 1, backgroundQueue, ref queueWriteIndex, labels, pixelBuffer, area, rowStrideBytes, bytesPerPixel, threshold);
        }

        for (int boundaryY = 0; boundaryY < area.Height; boundaryY++)
        {
            AddBg(0, boundaryY, backgroundQueue, ref queueWriteIndex, labels, pixelBuffer, area, rowStrideBytes, bytesPerPixel, threshold);
            AddBg(area.Width - 1, boundaryY, backgroundQueue, ref queueWriteIndex, labels, pixelBuffer, area, rowStrideBytes, bytesPerPixel, threshold);
        }

        int[] neighborXOffsets = new int[4]
        {
            1,
            -1,
            0,
            0
        };
        int[] neighborYOffsets = new int[4]
        {
            0,
            0,
            1,
            -1
        };
        while (queueReadIndex < queueWriteIndex)
        {
            int queuedPixelIndex = backgroundQueue[queueReadIndex++];
            int relativeX = queuedPixelIndex % area.Width;
            int relativeY = queuedPixelIndex / area.Width;
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                int neighborX = relativeX + neighborXOffsets[directionIndex];
                int neighborY = relativeY + neighborYOffsets[directionIndex];
                if (neighborX < 0 || neighborX >= area.Width || neighborY < 0 || neighborY >= area.Height)
                {
                    continue;
                }

                int neighborLabelIndex = neighborY * area.Width + neighborX;
                if (labels[neighborLabelIndex] == 0)
                {
                    byte* neighborPixel = pixelBuffer + (neighborY + area.Y) * rowStrideBytes + (neighborX + area.X) * bytesPerPixel;
                    if (0.299 * (double)(int)neighborPixel[2] + 0.587 * (double)(int)neighborPixel[1] + 0.114 * (double)(int)(*neighborPixel) >= (double)threshold)
                    {
                        labels[neighborLabelIndex] = -1;
                        backgroundQueue[queueWriteIndex++] = neighborLabelIndex;
                    }
                }
            }
        }

        if (preview)
        {
            Parallel.For(0, area.Height, (int ry) =>
            {
                for (int l = 0; l < area.Width; l++)
                {
                    if (labels[ry * area.Width + l] == -1)
                    {
                        byte* ptr2 = pixelBuffer + (ry + area.Y) * rowStrideBytes + (l + area.X) * bytesPerPixel;
                        *ptr2 = 0;
                        ptr2[1] = 0;
                        ptr2[2] = 0;
                        if (bytesPerPixel == 4)
                        {
                            ptr2[3] = 0;
                        }
                    }
                }
            });
        }

        bitmap.Unlock();
    }

    // 후보 픽셀의 BGR 가중 밝기를 검사하고 밝은 배경이면 labels=-1로 표시해 큐에 넣는다.
    // 상대 좌표의 범위 확인은 호출부에서 수행한다. stride는 바이트, area 위치와 relativeX/relativeY는 픽셀이다.
    private unsafe void AddBg(int relativeX, int relativeY, int[] backgroundQueue, ref int queueWriteIndex, int[] labels, byte* pixelBuffer, Int32Rect area, int rowStrideBytes, int bytesPerPixel, int brightnessThreshold)
    {
        int labelIndex = relativeY * area.Width + relativeX;
        if (labels[labelIndex] == 0)
        {
            byte* candidatePixel = pixelBuffer + (relativeY + area.Y) * rowStrideBytes + (relativeX + area.X) * bytesPerPixel;
            if (0.299 * (double)(int)candidatePixel[2] + 0.587 * (double)(int)candidatePixel[1] + 0.114 * (double)(int)(*candidatePixel) >= (double)brightnessThreshold)
            {
                labels[labelIndex] = -1;
                backgroundQueue[queueWriteIndex++] = labelIndex;
            }
        }
    }

    // 미분류 픽셀을 사방 연결요소로 나누어 양수 레이블을 부여하고 가장 큰 요소의 레이블을 반환한다.
    // 같은 크기면 먼저 찾은 요소를 유지한다. 배경(-1)은 탐색하지 않고 전경이 없으면 0을 반환한다.
    private int FindLargestBlob(int[] labels, Int32Rect area)
    {
        int componentLabel = 1;
        int largestComponentLabel = 0;
        int largestPixelCount = 0;
        int[] componentQueue = new int[labels.Length];
        for (int candidatePixelIndex = 0; candidatePixelIndex < labels.Length; candidatePixelIndex++)
        {
            if (labels[candidatePixelIndex] != 0)
            {
                continue;
            }

            int componentPixelCount = 0;
            int queueReadIndex = 0;
            int queueWriteIndex = 0;
            labels[candidatePixelIndex] = componentLabel;
            componentQueue[queueWriteIndex++] = candidatePixelIndex;
            while (queueReadIndex < queueWriteIndex)
            {
                int currentPixelIndex = componentQueue[queueReadIndex++];
                componentPixelCount++;
                int currentX = currentPixelIndex % area.Width;
                int currentY = currentPixelIndex / area.Width;
                for (int directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    int neighborX = currentX + directionIndex switch
                    {
                        1 => -1,
                        0 => 1,
                        _ => 0,
                    };
                    int neighborY = currentY + directionIndex switch
                    {
                        3 => -1,
                        2 => 1,
                        _ => 0,
                    };
                    if (neighborX >= 0 && neighborX < area.Width && neighborY >= 0 && neighborY < area.Height)
                    {
                        int neighborPixelIndex = neighborY * area.Width + neighborX;
                        if (labels[neighborPixelIndex] == 0)
                        {
                            labels[neighborPixelIndex] = componentLabel;
                            componentQueue[queueWriteIndex++] = neighborPixelIndex;
                        }
                    }
                }
            }

            if (componentPixelCount > largestPixelCount)
            {
                largestPixelCount = componentPixelCount;
                largestComponentLabel = componentLabel;
            }

            componentLabel++;
        }

        return largestComponentLabel;
    }

    // 지정 레이블 픽셀의 절대 좌표 평균과 경계를 병렬로 계산하고 행별 결과를 합친다.
    // 경계 폭/높이는 원본의 max-min 계산을 유지한다. 빈 결과는 영역 중심과 1x1 경계로 처리한다.
    private void CalculateObjectStats(int[] labels, Int32Rect area, int targetLabel, out Point centroid, out Rect boundingBox)
    {
        long sumX = 0L;
        long sumY = 0L;
        long count = 0L;
        int minX = area.Width;
        int maxX = 0;
        int minY = area.Height;
        int maxY = 0;
        object lockObj = new object ();
        Parallel.For(0, area.Height, () => (SX: 0L, SY: 0L, C: 0L, MiX: area.Width, MaX: 0, MiY: area.Height, MaY: 0), (int ry, ParallelLoopState state, (long SX, long SY, long C, int MiX, int MaX, int MiY, int MaY) local) =>
        {
            int rowLabelOffset = ry * area.Width;
            for (int relativeX = 0; relativeX < area.Width; relativeX++)
            {
                if (labels[rowLabelOffset + relativeX] == targetLabel)
                {
                    local.SX += relativeX + area.X;
                    local.SY += ry + area.Y;
                    local.C++;
                    if (relativeX < local.MiX)
                    {
                        local.MiX = relativeX;
                    }

                    if (relativeX > local.MaX)
                    {
                        local.MaX = relativeX;
                    }

                    if (ry < local.MiY)
                    {
                        local.MiY = ry;
                    }

                    if (ry > local.MaY)
                    {
                        local.MaY = ry;
                    }
                }
            }

            return local;
        }, ((long SX, long SY, long C, int MiX, int MaX, int MiY, int MaY) local) =>
        {
            Interlocked.Add(ref sumX, local.SX);
            Interlocked.Add(ref sumY, local.SY);
            Interlocked.Add(ref count, local.C);
            lock (lockObj)
            {
                if (local.MiX < minX)
                {
                    minX = local.MiX;
                }

                if (local.MaX > maxX)
                {
                    maxX = local.MaX;
                }

                if (local.MiY < minY)
                {
                    minY = local.MiY;
                }

                if (local.MaY > maxY)
                {
                    maxY = local.MaY;
                }
            }
        });
        if (count == 0L)
        {
            centroid = new Point((double)area.X + (double)area.Width / 2.0, (double)area.Y + (double)area.Height / 2.0);
            boundingBox = new Rect(area.X, area.Y, 1.0, 1.0);
        }
        else
        {
            centroid = new Point((double)sumX / (double)count, (double)sumY / (double)count);
            boundingBox = new Rect(minX + area.X, minY + area.Y, maxX - minX, maxY - minY);
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
    public static extern void ZeroMemory(IntPtr destination, int length);
    // 최대 연결요소에 속한 픽셀만 원본 좌표에 복사하고 나머지 출력 버퍼는 0으로 채운다.
    // 입력/출력 형식과 픽셀 바이트 수는 원본대로 유지하며, 고객 이미지에서의 투명도 결과는 별도 실행 확인이 필요하다.
    private unsafe BitmapSource ProcessTransparent(WriteableBitmap sourceBitmap, int[] labels, int maxLabel, Int32Rect area)
    {
        if (labels == null || maxLabel <= 0)
        {
            return sourceBitmap;
        }

        WriteableBitmap resultBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight, sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, null);
        int fullWidth = resultBitmap.PixelWidth;
        int fullHeight = resultBitmap.PixelHeight;
        int rowStrideBytes = resultBitmap.BackBufferStride;
        int bytesPerPixel = (resultBitmap.Format.BitsPerPixel + 7) / 8;
        int bufferByteCount = rowStrideBytes * fullHeight;
        resultBitmap.Lock();
        sourceBitmap.Lock();
        ZeroMemory(resultBitmap.BackBuffer, bufferByteCount);
        byte* sourcePixelBuffer = (byte*)(void*)sourceBitmap.BackBuffer;
        byte* destinationPixelBuffer = (byte*)(void*)resultBitmap.BackBuffer;
        Parallel.For(0, area.Height, (int ry) =>
        {
            int rowLabelOffset = ry * area.Width;
            int absoluteY = ry + area.Y;
            if (absoluteY >= 0 && absoluteY < fullHeight)
            {
                for (int i = 0; i < area.Width; i++)
                {
                    int labelIndex = rowLabelOffset + i;
                    if (labels[labelIndex] == maxLabel)
                    {
                        int absoluteX = i + area.X;
                        if (absoluteX >= 0 && absoluteX < fullWidth)
                        {
                            byte* ptr = sourcePixelBuffer + absoluteY * rowStrideBytes + absoluteX * bytesPerPixel;
                            byte* ptr2 = destinationPixelBuffer + absoluteY * rowStrideBytes + absoluteX * bytesPerPixel;
                            *ptr2 = *ptr;
                            ptr2[1] = ptr[1];
                            ptr2[2] = ptr[2];
                            if (bytesPerPixel == 4)
                            {
                                ptr2[3] = byte.MaxValue;
                            }
                        }
                    }
                }
            }
        });
        sourceBitmap.Unlock();
        resultBitmap.Unlock();
        resultBitmap.Freeze();
        return resultBitmap;
    }

    private WriteableBitmap ProcessCentroid(WriteableBitmap wb, Point centroid)
    {
        int pixelWidth = wb.PixelWidth;
        int pixelHeight = wb.PixelHeight;
        int num = (int)((double)pixelWidth / 2.0 - centroid.X);
        int num2 = (int)((double)pixelHeight / 2.0 - centroid.Y);
        WriteableBitmap writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight, wb.DpiX, wb.DpiY, wb.Format, null);
        int x = Math.Max(0, -num);
        int y = Math.Max(0, -num2);
        int x2 = Math.Max(0, num);
        int y2 = Math.Max(0, num2);
        int num3 = pixelWidth - Math.Abs(num);
        int num4 = pixelHeight - Math.Abs(num2);
        if (num3 > 0 && num4 > 0)
        {
            int num5 = pixelWidth * ((wb.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[num5 * pixelHeight];
            wb.CopyPixels(new Int32Rect(x, y, num3, num4), pixels, num5, 0);
            writeableBitmap.WritePixels(new Int32Rect(x2, y2, num3, num4), pixels, num5, 0);
        }

        return writeableBitmap;
    }

    private BitmapSource ProcessMagnification(BitmapSource inputBmp, ObjectInfo info, double targetHeight)
    {
        double num = targetHeight / info.Height;
        ScaleTransform transform = new ScaleTransform(num, num, inputBmp.Width / 2.0, inputBmp.Height / 2.0);
        DrawingVisual drawingVisual = new DrawingVisual();
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.PushTransform(transform);
            drawingContext.DrawImage(inputBmp, new Rect(0.0, 0.0, inputBmp.Width, inputBmp.Height));
        }

        RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)inputBmp.Width, (int)inputBmp.Height, 96.0, 96.0, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);
        return renderTargetBitmap;
    }

    private void BtnCorrectStop_Click(object sender, RoutedEventArgs e)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            BtnCorrectStop.IsEnabled = false;
            BtnCorrectStop.Content = "Stopping...";
            MessageBox.Show("작업이 중단되었습니다.");
        }
    }

    private async void BtnCorrectAll_Click(object sender, RoutedEventArgs e)
    {
        if (_imagePaths.Count == 0)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        Dictionary<string, bool> options = new Dictionary<string, bool>();
        Dispatcher.Invoke(() =>
        {
            options["Trans"] = ChkSaveTransparent.IsChecked == true;
            options["Center"] = ChkSaveCenter.IsChecked == true;
            options["Mag"] = ChkSaveMag.IsChecked == true;
            ProgressBarCorrection.Value = 0.0;
        });
        if (!options.Values.Any((bool v) => v))
        {
            return;
        }

        ProgressRing.Visibility = Visibility.Visible;
        BtnCorrectAll.IsEnabled = false;
        BtnCorrectStop.IsEnabled = true;
        BtnCorrectStop.Content = "STOP";
        await Task.Yield();
        string directoryName = Path.GetDirectoryName(_imagePaths[0]);
        Dictionary<string, string> dirs = new Dictionary<string, string>
        {
            {
                "Trans",
                Path.Combine(directoryName, "Transparent")
            },
            {
                "Center",
                Path.Combine(directoryName, "Center")
            },
            {
                "Mag",
                Path.Combine(directoryName, "Magnification")
            }
        };
        foreach (string item in dirs.Values.Where((string d, int i) => options.ElementAt(i).Value))
        {
            if (!Directory.Exists(item))
            {
                Directory.CreateDirectory(item);
            }
        }

        try
        {
            ObjectInfo[] informList = await AnalyzeAllImages(_imagePaths, _blackThreshold);
            int num = (int)Math.Round((double)_imagePaths.Count / 4.0);
            double targetHeight = informList[Math.Max(0, num - 1)].Height;
            await Task.Run(() =>
            {
                int i;
                for (i = 0; i < _imagePaths.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        ProcessAndSave(_imagePaths[i], informList[i], targetHeight, _blackThreshold, _cropRatio, dirs, options);
                    }
                    catch
                    {
                    }

                    Dispatcher.Invoke(() =>
                    {
                        LoadPreviewImage(_imagePaths[i]);
                        ProgressBarCorrection.Value = (double)(i + 1) / (double)_imagePaths.Count * 100.0;
                    });
                    if (i % 5 == 0)
                    {
                        CleanMemory();
                    }
                }
            }, _cts.Token);
            string dir;
            if (options["Mag"])
            {
                dir = dirs["Mag"];
            }
            else
            {
                dir = (options["Center"] ? dirs["Center"] : dirs["Trans"]);
            }

            _correctedPaths = RefreshList(dir, ListBoxCorrected);
            LoadPreviewImage(_correctedPaths[0]);
            ProgressRing.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("작업이 중단되었습니다.");
        }
        finally
        {
            ProgressRing.Visibility = Visibility.Collapsed;
            BtnCorrectAll.IsEnabled = true;
            BtnCorrectStop.IsEnabled = false;
            BtnCorrectStop.Content = "STOP";
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task<ObjectInfo[]> AnalyzeAllImages(List<string> imagePaths, int threshold)
    {
        ObjectInfo[] informList = new ObjectInfo[imagePaths.Count];
        await Task.Run(() =>
        {
            Parallel.For(0, imagePaths.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (int i) =>
            {
                WriteableBitmap thumbnailBitmap = GetThumbnailBitmap(imagePaths[i], 800);
                if (thumbnailBitmap != null)
                {
                    try
                    {
                        int imageWidth = GetImageWidth(imagePaths[i]);
                        int imageHeight = GetImageHeight(imagePaths[i]);
                        double num = (double)imageWidth / (double)thumbnailBitmap.PixelWidth;
                        ObjectInfo objectInfo = GetObjectInfo(thumbnailBitmap, threshold, preview: false);
                        objectInfo.CalcArea = CalculateSafetyROI(objectInfo, num, imageWidth, imageHeight);
                        objectInfo.Height *= num;
                        informList[i] = objectInfo;
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        thumbnailBitmap = null;
                    }
                }
            });
        });
        return informList;
    }

    private void ProcessAndSave(string imagePath, ObjectInfo analysisInfo, double targetHeight, int threshold, double currentRatio, Dictionary<string, string> dirs, Dictionary<string, bool> options)
    {
        BitmapMetadata metaInform = GetMetaInform(imagePath);
        WriteableBitmap fullResolutionBitmap = GetFullResolutionBitmap(imagePath);
        ObjectInfo objectInfo = GetObjectInfo(fullResolutionBitmap, threshold, preview: false, analysisInfo.CalcArea);
        if (options["Trans"])
        {
            BitmapSource source = ProcessTransparent(fullResolutionBitmap, objectInfo.Labels, objectInfo.MaxLabel, objectInfo.CalcArea);
            string savePath = Path.Combine(dirs["Trans"], Path.ChangeExtension(Path.GetFileName(imagePath), ".png"));
            SaveImageToFile(source, savePath, metaInform, "png");
        }

        if (options["Center"] || options["Mag"])
        {
            BitmapSource bitmapSource = ProcessCentroid(fullResolutionBitmap, objectInfo.Centroid);
            if (options["Center"])
            {
                BitmapSource source2 = ImageCrop(bitmapSource, fullResolutionBitmap.PixelWidth, fullResolutionBitmap.PixelHeight, currentRatio);
                SaveImageToFile(source2, Path.Combine(dirs["Center"], Path.GetFileName(imagePath)), metaInform);
            }

            if (options["Mag"])
            {
                BitmapSource source3 = ProcessMagnification(bitmapSource, objectInfo, targetHeight);
                BitmapSource source4 = ImageCrop(source3, fullResolutionBitmap.PixelWidth, fullResolutionBitmap.PixelHeight, currentRatio);
                SaveImageToFile(source4, Path.Combine(dirs["Mag"], Path.GetFileName(imagePath)), metaInform);
            }
        }
    }

    private Int32Rect CalculateSafetyROI(ObjectInfo rough, double scale, int imageWidth, int imageHeight)
    {
        double num = 0.2;
        int num2 = (int)(rough.Width * scale * num);
        int num3 = (int)(rough.Height * scale * num);
        int num4 = Math.Max(0, (int)(rough.Centroid.X * scale - rough.Width * scale / 2.0 - (double)num2));
        int num5 = Math.Max(0, (int)(rough.Centroid.Y * scale - rough.Height * scale / 2.0 - (double)num3));
        int width = Math.Min(imageWidth - num4, (int)(rough.Width * scale + (double)(num2 * 2)));
        int height = Math.Min(imageHeight - num5, (int)(rough.Height * scale + (double)(num3 * 2)));
        return new Int32Rect(num4, num5, width, height);
    }

    private WriteableBitmap GetFullResolutionBitmap(string path)
    {
        using FileStream bitmapStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0], PixelFormats.Bgra32, null, 0.0);
        formatConvertedBitmap.Freeze();
        return new WriteableBitmap(formatConvertedBitmap);
    }

    private WriteableBitmap GetThumbnailBitmap(string path, int width)
    {
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(path);
        bitmapImage.DecodePixelWidth = width;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap();
        formatConvertedBitmap.BeginInit();
        formatConvertedBitmap.Source = bitmapImage;
        formatConvertedBitmap.DestinationFormat = PixelFormats.Bgra32;
        formatConvertedBitmap.EndInit();
        return new WriteableBitmap(formatConvertedBitmap);
    }

    private int GetImageWidth(string path)
    {
        using FileStream bitmapStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        return BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0].PixelWidth;
    }

    private int GetImageHeight(string path)
    {
        using FileStream bitmapStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        return BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0].PixelHeight;
    }

    private void CleanMemory()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private BitmapMetadata GetMetaInform(string Pathfile)
    {
        try
        {
            BitmapMetadata result = null;
            if (File.Exists(Pathfile))
            {
                using FileStream bitmapStream = new FileStream(Pathfile, FileMode.Open, FileAccess.Read, FileShare.Read);
                BitmapDecoder bitmapDecoder = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (bitmapDecoder.Frames.Count > 0 && bitmapDecoder.Frames[0].Metadata != null)
                {
                    result = bitmapDecoder.Frames[0].Metadata.Clone() as BitmapMetadata;
                }
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private BitmapSource ImageCrop(BitmapSource source, int baseW, int baseH, double ratio)
    {
        int val = (int)((double)baseW * ratio);
        int val2 = (int)((double)baseH * ratio);
        int num = Math.Min(source.PixelWidth, val);
        int num2 = Math.Min(source.PixelHeight, val2);
        int val3 = (source.PixelWidth - num) / 2;
        int val4 = (source.PixelHeight - num2) / 2;
        val3 = Math.Max(0, val3);
        val4 = Math.Max(0, val4);
        return new CroppedBitmap(source, new Int32Rect(val3, val4, num, num2));
    }

    private void SaveImageToFile(BitmapSource source, string savePath, BitmapMetadata metadata, string format = "jpg")
    {
        try
        {
            BitmapEncoder bitmapEncoder = ((!(format.ToLower() == "png") && !savePath.ToLower().EndsWith(".png")) ? ((BitmapEncoder)new JpegBitmapEncoder
            {
                QualityLevel = 93
            }

            ) : ((BitmapEncoder)new PngBitmapEncoder()));
            BitmapMetadata metadata2 = null;
            try
            {
                if (metadata != null)
                {
                    metadata2 = metadata.Clone();
                }
            }
            catch
            {
                metadata2 = null;
            }

            bitmapEncoder.Frames.Add(BitmapFrame.Create(source, null, metadata2, null));
            string directoryName = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            bitmapEncoder.Save(stream);
        }
        catch (Exception)
        {
        }
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BtnPreviewThreshold_Click(sender, e);
    }

    private void BtnPreviewThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (_imagePaths == null || _imagePaths.Count == 0)
        {
            MessageBox.Show("먼저 이미지 폴더를 로드해 주세요.");
            return;
        }

        int num = ListBoxImages.SelectedIndex;
        if (num < 0)
        {
            num = 0;
        }

        try
        {
            string path = _imagePaths[num];
            WriteableBitmap thumbnailBitmap = GetThumbnailBitmap(path, 800);
            int imageWidth = (int)thumbnailBitmap.Width;
            int imageHeight = (int)thumbnailBitmap.Height;
            double scale = 1.0;
            ObjectInfo objectInfo = GetObjectInfo(thumbnailBitmap, _blackThreshold, preview: true);
            objectInfo.CalcArea = CalculateSafetyROI(objectInfo, scale, imageWidth, imageHeight);
            DrawDirectMarkers(thumbnailBitmap, objectInfo);
            ImgPreview.Source = thumbnailBitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show("미리보기 생성 중 오류 발생: " + ex.Message);
        }
    }

    private void DrawDirectMarkers(WriteableBitmap wb, ObjectInfo info)
    {
        wb.Lock();
        if (info.Labels != null)
        {
            DrawContour(wb, info.Labels, info.MaxLabel, info.CalcArea, Colors.Lime);
        }

        DrawRect(wb, info.CalcArea, Colors.Red);
        Int32Rect rect = new Int32Rect(info.AbsMinX, info.AbsMinY, info.AbsMaxX - info.AbsMinX + 1, info.AbsMaxY - info.AbsMinY + 1);
        DrawRect(wb, rect, Colors.Blue);
        DrawCross(wb, info.Centroid, Colors.Lime);
        wb.Unlock();
    }

    private unsafe void DrawContour(WriteableBitmap wb, int[] labels, int maxLabel, Int32Rect area, Color color)
    {
        if (labels == null || maxLabel <= 0)
        {
            return;
        }

        int fullWidth = wb.PixelWidth;
        int fullHeight = wb.PixelHeight;
        int stride = wb.BackBufferStride;
        int bpp = (wb.Format.BitsPerPixel + 7) / 8;
        byte* pBackBuffer = (byte*)(void*)wb.BackBuffer;
        byte b = color.B;
        byte g = color.G;
        byte r = color.R;
        byte a = color.A;
        Parallel.For(area.Y, area.Y + area.Height, (int py) =>
        {
            if (py >= 0 && py < fullHeight)
            {
                for (int i = area.X; i < area.X + area.Width; i++)
                {
                    if (i >= 0 && i < fullWidth)
                    {
                        int num = py * fullWidth + i;
                        if (labels[num] == maxLabel)
                        {
                            bool flag = false;
                            if (i == 0 || i == fullWidth - 1 || py == 0 || py == fullHeight - 1)
                            {
                                flag = true;
                            }
                            else if (labels[num - 1] != maxLabel || labels[num + 1] != maxLabel || labels[num - fullWidth] != maxLabel || labels[num + fullWidth] != maxLabel)
                            {
                                flag = true;
                            }

                            if (flag)
                            {
                                byte* ptr = pBackBuffer + py * stride + i * bpp;
                                *ptr = b;
                                ptr[1] = g;
                                ptr[2] = r;
                                if (bpp == 4)
                                {
                                    ptr[3] = a;
                                }
                            }
                        }
                    }
                }
            }
        });
    }

    private unsafe void DrawRect(WriteableBitmap wb, Int32Rect rect, Color color)
    {
        int backBufferStride = wb.BackBufferStride;
        int bpp = (wb.Format.BitsPerPixel + 7) / 8;
        byte* pBuffer = (byte*)(void*)wb.BackBuffer;
        for (int i = rect.X; i < rect.X + rect.Width; i++)
        {
            if (i >= 0 && i < wb.PixelWidth)
            {
                SetPixel(pBuffer, i, rect.Y, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
                SetPixel(pBuffer, i, rect.Y + rect.Height - 1, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
            }
        }

        for (int j = rect.Y; j < rect.Y + rect.Height; j++)
        {
            if (j >= 0 && j < wb.PixelHeight)
            {
                SetPixel(pBuffer, rect.X, j, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
                SetPixel(pBuffer, rect.X + rect.Width - 1, j, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
            }
        }
    }

    private unsafe void DrawCross(WriteableBitmap wb, Point center, Color color)
    {
        int backBufferStride = wb.BackBufferStride;
        int bpp = (wb.Format.BitsPerPixel + 7) / 8;
        byte* pBuffer = (byte*)(void*)wb.BackBuffer;
        int num = (int)center.X;
        int num2 = (int)center.Y;
        int num3 = 5;
        for (int i = -num3; i <= num3; i++)
        {
            SetPixel(pBuffer, num + i, num2, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
            SetPixel(pBuffer, num, num2 + i, backBufferStride, bpp, color, wb.PixelWidth, wb.PixelHeight);
        }
    }

    private unsafe void SetPixel(byte* pBuffer, int x, int y, int stride, int bpp, Color c, int imgW, int imgH)
    {
        if (x >= 0 && x < imgW && y >= 0 && y < imgH)
        {
            byte* ptr = pBuffer + y * stride + x * bpp;
            *ptr = c.B;
            ptr[1] = c.G;
            ptr[2] = c.R;
            if (bpp == 4)
            {
                ptr[3] = c.A;
            }
        }
    }
}
