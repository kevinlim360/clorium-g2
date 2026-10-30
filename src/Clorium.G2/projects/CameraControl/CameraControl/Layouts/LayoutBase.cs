using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Classes.Queue;
using CameraControl.Devices;
using CameraControl.ViewModel;
using CameraControl.windows;
using Microsoft.VisualBasic.FileIO;
using ZoomAndPan;

namespace CameraControl.Layouts;

public class LayoutBase : UserControl
{
	protected bool prevZoomRectSet = false;

	private readonly BackgroundWorker _worker = new BackgroundWorker();

	private FileItem _selectedItem = null;

	private bool Flag_IsMovie = false;

	public ListBox ImageLIst { get; set; }

	public ZoomAndPanControl ZoomAndPanControl { get; set; }

	public UIElement content { get; set; }

	public MediaElement MediaElement { get; set; }

	public LayoutViewModel LayoutViewModel { get; set; }

	public LayoutBase()
	{
		LayoutViewModel = new LayoutViewModel();
		_worker.DoWork += worker_DoWork;
		_worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
	}

	public void InitServices()
	{
		ServiceProvider.Settings.PropertyChanged += Settings_PropertyChanged;
		ServiceProvider.WindowsManager.Event += Trigger_Event;
		ImageLIst.SelectionChanged += ImageLIst_SelectionChanged;
		if (ServiceProvider.Settings.SelectedBitmap != null && ServiceProvider.Settings.SelectedBitmap.FileItem != null)
		{
			ImageLIst.SelectedItem = ServiceProvider.Settings.SelectedBitmap.FileItem;
			ImageLIst.ScrollIntoView(ImageLIst.SelectedItem);
		}
		else if (ServiceProvider.Settings.DefaultSession.Files.Count > 0)
		{
			ImageLIst.SelectedIndex = 0;
		}
		if (ZoomAndPanControl != null)
		{
			ZoomAndPanControl.ContentOffsetXChanged += ZoomAndPanControl_ContentScaleChanged;
			ZoomAndPanControl.ContentOffsetYChanged += ZoomAndPanControl_ContentScaleChanged;
		}
	}

	public void UnInit()
	{
		if (ZoomAndPanControl != null)
		{
			ZoomAndPanControl.ContentOffsetXChanged -= ZoomAndPanControl_ContentScaleChanged;
			ZoomAndPanControl.ContentOffsetYChanged -= ZoomAndPanControl_ContentScaleChanged;
		}
		_worker.DoWork -= worker_DoWork;
		_worker.RunWorkerCompleted -= _worker_RunWorkerCompleted;
		ServiceProvider.Settings.PropertyChanged -= Settings_PropertyChanged;
		ServiceProvider.WindowsManager.Event -= Trigger_Event;
		ImageLIst.SelectionChanged -= ImageLIst_SelectionChanged;
	}

	private void ImageLIst_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count <= 0)
		{
			return;
		}
		_selectedItem = e.AddedItems[0] as FileItem;
		if (!_worker.IsBusy)
		{
			if (e.AddedItems[0] is FileItem item)
			{
				ServiceProvider.Settings.SelectedBitmap.SetFileItem(item);
				_worker.RunWorkerAsync(false);
			}
			LayoutViewModel.SelectedPhotoIndex = ImageLIst.SelectedIndex + 1;
		}
	}

	private void worker_DoWork(object sender, DoWorkEventArgs e)
	{
		FileItem item = ServiceProvider.Settings.SelectedBitmap.FileItem;
		if (ServiceProvider.Settings.SelectedBitmap.FileItem == null)
		{
			return;
		}
		bool fullres = e.Argument is bool && (bool)e.Argument;
		if (!File.Exists(item.LargeThumb) && item.IsJpg && File.Exists(item.FileName))
		{
			try
			{
				PhotoUtils.WaitForFile(item.FileName);
				ServiceProvider.Settings.SelectedBitmap.DisplayImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.FileName, 1600, 0);
			}
			catch (Exception exception)
			{
				Log.Error("Unable to load fast preview", exception);
			}
		}
		ServiceProvider.Settings.ImageLoading = fullres || !ServiceProvider.Settings.SelectedBitmap.FileItem.IsLoaded;
		if (ServiceProvider.Settings.SelectedBitmap.FileItem.Loading)
		{
			while (ServiceProvider.Settings.SelectedBitmap.FileItem.Loading)
			{
				Thread.Sleep(10);
			}
		}
		else
		{
			BitmapLoader.Instance.GenerateCache(ServiceProvider.Settings.SelectedBitmap.FileItem);
		}
		if (!ServiceProvider.Settings.SelectedBitmap.FileItem.HaveHistogramReady())
		{
			ServiceProvider.QueueManager.Add(new QueueItemFileItem
			{
				FileItem = ServiceProvider.Settings.SelectedBitmap.FileItem,
				Generate = QueueType.Histogram
			});
		}
		ServiceProvider.Settings.SelectedBitmap.DisplayImage = BitmapLoader.Instance.LoadImage(ServiceProvider.Settings.SelectedBitmap.FileItem, fullres);
		BitmapLoader.Instance.SetData(ServiceProvider.Settings.SelectedBitmap, ServiceProvider.Settings.SelectedBitmap.FileItem);
		if (ServiceProvider.Settings.UseHighlight)
		{
			BitmapLoader.Highlight(ServiceProvider.Settings.SelectedBitmap, ServiceProvider.Settings.HighlightUnderExp, ServiceProvider.Settings.HighlightOverExp);
		}
		ServiceProvider.Settings.SelectedBitmap.FullResLoaded = fullres;
		ServiceProvider.Settings.ImageLoading = false;
		Dispatcher.BeginInvoke(new Action(OnImageLoaded));
	}

	public virtual void OnImageLoaded()
	{
		if (LayoutViewModel.ZoomIndex == 0 && ZoomAndPanControl != null)
		{
			ZoomAndPanControl.ScaleToFit();
		}
		else if (ZoomAndPanControl != null)
		{
			ZoomToFocus();
		}
		if (ServiceProvider.Branding.ShowPreviewPanel)
		{
			GeneratePreview();
			LayoutViewModel.DoZoom();
		}
		ServiceProvider.Settings.SelectedBitmap.Notify();
	}

	private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if (_selectedItem != ServiceProvider.Settings.SelectedBitmap.FileItem)
		{
			ServiceProvider.Settings.SelectedBitmap.FileItem = _selectedItem;
			if (ServiceProvider.Settings.LoadFullRespreview)
			{
				_worker.RunWorkerAsync(true);
			}
			else
			{
				_worker.RunWorkerAsync(_selectedItem);
			}
		}
		else if (LayoutViewModel.ZoomIndex > 0)
		{
			LoadFullRes();
		}
		else if (ZoomAndPanControl != null)
		{
			ZoomAndPanControl.ScaleToFit();
		}
	}

	private void ZoomAndPanControl_ContentScaleChanged(object sender, EventArgs e)
	{
	}

	private void DeleteItem(bool messageShow)
	{
		List<FileItem> filestodelete = new List<FileItem>();
		try
		{
			filestodelete.AddRange(ServiceProvider.Settings.DefaultSession.Files.Where((FileItem fileItem2) => fileItem2.IsChecked));
			if (ServiceProvider.Settings.SelectedBitmap != null && ServiceProvider.Settings.SelectedBitmap.FileItem != null && filestodelete.Count == 0)
			{
				filestodelete.Add(ServiceProvider.Settings.SelectedBitmap.FileItem);
			}
			if (filestodelete.Count == 0)
			{
				return;
			}
			int selectedindex = ImageLIst.Items.IndexOf(filestodelete[0]);
			bool delete = false;
			if (!((filestodelete.Count > 1) ? (MessageBox.Show("Multiple files are selected!! Do you really want to delete selected files?", "Delete files", MessageBoxButton.YesNo) == MessageBoxResult.Yes) : (!messageShow || MessageBox.Show("Do you really want to delete selected file ?", "Delete file", MessageBoxButton.YesNo) == MessageBoxResult.Yes)))
			{
				return;
			}
			foreach (FileItem fileItem in filestodelete)
			{
				if (ServiceProvider.Settings.SelectedBitmap != null && ServiceProvider.Settings.SelectedBitmap.FileItem != null && fileItem.FileName == ServiceProvider.Settings.SelectedBitmap.FileItem.FileName)
				{
					ServiceProvider.Settings.SelectedBitmap.DisplayImage = null;
				}
				if (File.Exists(fileItem.FileName))
				{
					FileSystem.DeleteFile(fileItem.FileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
				}
				fileItem.RemoveThumbs();
				ServiceProvider.Settings.DefaultSession.Files.Remove(fileItem);
			}
			if (selectedindex < ImageLIst.Items.Count)
			{
				ImageLIst.SelectedIndex = selectedindex + 1;
				ImageLIst.SelectedIndex = selectedindex - 1;
				if (ImageLIst.SelectedItem is FileItem item)
				{
					ImageLIst.ScrollIntoView(item);
				}
			}
			LayoutViewModel.SelectedPhotoIndex = ImageLIst.SelectedIndex + 1;
		}
		catch (Exception exception)
		{
			Log.Error("Error to delete file", exception);
		}
	}

	private void GeneratePreview()
	{
		try
		{
			WriteableBitmap bitmap = BitmapLoader.Instance.LoadSmallImage(ServiceProvider.Settings.SelectedBitmap.FileItem, 270);
			if (bitmap != null)
			{
				if (ServiceProvider.Settings.SelectedBitmap.FileItem.RotationAngle != 0)
				{
					bitmap.Rotate(ServiceProvider.Settings.SelectedBitmap.FileItem.RotationAngle);
				}
				bitmap.Freeze();
				ServiceProvider.Settings.SelectedBitmap.Preview = bitmap;
			}
		}
		catch (Exception)
		{
		}
	}

	public void LoadFullRes()
	{
		if (!_worker.IsBusy && !ServiceProvider.Settings.SelectedBitmap.FullResLoaded)
		{
			_worker.RunWorkerAsync(true);
		}
	}

	private void RefreshImage()
	{
		if (!_worker.IsBusy)
		{
			_worker.RunWorkerAsync(false);
		}
	}

	private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "DefaultSession")
		{
			Thread.Sleep(1000);
			Dispatcher.Invoke(() =>
			{
				ImageLIst.SelectedIndex = 0;
				if (ImageLIst.Items.Count == 0)
				{
					ServiceProvider.Settings.SelectedBitmap.DisplayImage = null;
					ServiceProvider.Settings.SelectedBitmap.Preview = null;
				}
			});
		}
		if (e.PropertyName == "HighlightOverExp" && !_worker.IsBusy)
		{
			_worker.RunWorkerAsync(false);
		}
		if (e.PropertyName == "HighlightUnderExp" && !_worker.IsBusy)
		{
			_worker.RunWorkerAsync(false);
		}
		if (e.PropertyName == "ShowFocusPoints" && !_worker.IsBusy)
		{
			_worker.RunWorkerAsync(false);
		}
		if (e.PropertyName == "FlipPreview" && !_worker.IsBusy)
		{
			_worker.RunWorkerAsync(false);
		}
	}

	private void Trigger_Event(string cmd, object o)
	{
		try
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				TriggerEvent(cmd, o);
			}));
		}
		catch (Exception)
		{
		}
	}

	private void TriggerEvent(string cmd, object o)
	{
		try
		{
			switch (cmd)
			{
			case "Next_Image":
				if (ImageLIst.SelectedIndex < ImageLIst.Items.Count - 1)
				{
					if (ImageLIst.SelectedItem is FileItem item7)
					{
						int ind2 = ImageLIst.Items.IndexOf(item7);
						ImageLIst.SelectedIndex = ind2 + 1;
					}
					if (ImageLIst.SelectedItem is FileItem item8)
					{
						ImageLIst.ScrollIntoView(item8);
					}
				}
				break;
			case "Prev_Image":
				if (ImageLIst.SelectedIndex > 0)
				{
					if (ImageLIst.SelectedItem is FileItem item3)
					{
						int ind = ImageLIst.Items.IndexOf(item3);
						ImageLIst.SelectedIndex = ind - 1;
					}
					if (ImageLIst.SelectedItem is FileItem item4)
					{
						ImageLIst.ScrollIntoView(item4);
					}
				}
				break;
			case "Like_Image":
				if (ImageLIst.SelectedItem != null)
				{
					FileItem item9 = null;
					item9 = ((o == null) ? (ImageLIst.SelectedItem as FileItem) : ServiceProvider.Settings.DefaultSession.GetByName(o as string));
					if (item9 != null)
					{
						item9.IsLiked = !item9.IsLiked;
					}
				}
				break;
			case "Unlike_Image":
				if (ImageLIst.SelectedItem != null)
				{
					FileItem item = null;
					item = ((o == null) ? (ImageLIst.SelectedItem as FileItem) : ServiceProvider.Settings.DefaultSession.GetByName(o as string));
					if (item != null)
					{
						item.IsUnLiked = !item.IsUnLiked;
					}
				}
				break;
			case "Del_Image":
				if (o == null)
				{
					DeleteItem(messageShow: true);
				}
				else
				{
					DeleteItem(messageShow: false);
				}
				break;
			case "SelectAll_Image":
				ServiceProvider.Settings.DefaultSession.SelectAll();
				break;
			case "Select_Image":
				if (o is FileItem fileItem)
				{
					ImageLIst.SelectedValue = fileItem;
					ImageLIst.ScrollIntoView(fileItem);
				}
				break;
			case "Refresh_Image":
				RefreshImage();
				break;
			case "Zoom_Image_Fit":
				ZoomAndPanControl?.ScaleToFit();
				if (ImageLIst.SelectedItem != null)
				{
					if (Flag_IsMovie)
					{
						LoadFullRes();
						Flag_IsMovie = false;
					}
					FileItem item6 = ImageLIst.SelectedItem as FileItem;
					string extension = Path.GetExtension(item6.FileName);
					if (extension.ToLower() == ".mp4" || extension.ToLower() == ".mov")
					{
						Flag_IsMovie = true;
					}
				}
				break;
			case "Zoom_Image_60":
				ZoomToFocus();
				LoadFullRes();
				ZoomAndPanControl.ZoomTo(0.6);
				break;
			case "Zoom_Image_100":
				ZoomToFocus();
				LoadFullRes();
				ZoomAndPanControl.ZoomTo(1.0);
				break;
			case "Zoom_Image_200":
				ZoomToFocus();
				LoadFullRes();
				ZoomAndPanControl.ZoomTo(2.0);
				break;
			case "RotateLeft":
				if (ImageLIst.SelectedItem is FileItem item5)
				{
					item5.Rotation--;
				}
				break;
			case "RotateRight":
				if (ImageLIst.SelectedItem is FileItem item2)
				{
					item2.Rotation++;
				}
				break;
			case "ViewExternal":
				OpenInExternalViewer();
				break;
			case "ViewExplorer":
				OpenInExplorer();
				break;
			case "ViewColorCompensation":
				OpenColorCompensation();
				break;
			case "RefreshDisplay":
				if (LayoutViewModel.ZoomIndex == 0)
				{
					ZoomAndPanControl.ScaleToFit();
				}
				break;
			}
			if (cmd.StartsWith("ZoomPoint") && ZoomAndPanControl != null && cmd.Contains("_"))
			{
				string[] vals = cmd.Split('_');
				if (vals.Count() > 2)
				{
					double.TryParse(vals[1], out var _);
					double.TryParse(vals[2], out var _);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to process event ", exception);
		}
	}

	private void OpenInExplorer()
	{
		if (ServiceProvider.Settings.SelectedBitmap == null || ServiceProvider.Settings.SelectedBitmap.FileItem == null)
		{
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "explorer";
			processStartInfo.UseShellExecute = true;
			processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
			processStartInfo.Arguments = $"/e,/select,\"{ServiceProvider.Settings.SelectedBitmap.FileItem.FileName}\"";
			Process.Start(processStartInfo);
		}
		catch (Exception exception)
		{
			Log.Error("Error to show file in explorer", exception);
		}
	}

	private void OpenInExternalViewer()
	{
		if (ServiceProvider.Settings.SelectedBitmap != null && ServiceProvider.Settings.SelectedBitmap.FileItem != null)
		{
			if (!string.IsNullOrWhiteSpace(ServiceProvider.Settings.ExternalViewer) && File.Exists(ServiceProvider.Settings.ExternalViewer))
			{
				PhotoUtils.Run(ServiceProvider.Settings.ExternalViewer, "\"" + ServiceProvider.Settings.SelectedBitmap.FileItem.FileName + "\"", ProcessWindowStyle.Maximized);
			}
			else
			{
				PhotoUtils.Run("\"" + ServiceProvider.Settings.SelectedBitmap.FileItem.FileName + "\"", "", ProcessWindowStyle.Maximized);
			}
		}
	}

	private void ZoomToFocus()
	{
		if (LayoutViewModel.ZoomToFocus && ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo != null && ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.FocusPoints.Count > 0)
		{
			ZoomAndPanControl.SnapTo(new Point(ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.FocusPoints[0].X + ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.FocusPoints[0].Width / 2.0, ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.FocusPoints[0].Y + ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.FocusPoints[0].Height / 2.0));
		}
	}

	protected void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		Point curContentMousePoint = e.GetPosition(content);
		if (e.Delta > 0)
		{
			ZoomIn(curContentMousePoint);
		}
		else if (e.Delta < 0)
		{
			ZoomOut(curContentMousePoint);
		}
	}

	private void ZoomOut(Point contentZoomCenter)
	{
	}

	private void ZoomIn(Point contentZoomCenter)
	{
	}

	public void Button_Click(object sender, RoutedEventArgs e)
	{
		MediaElement.Play();
	}

	public void Button_Click_1(object sender, RoutedEventArgs e)
	{
		MediaElement.Pause();
	}

	public void Button_Click_2(object sender, RoutedEventArgs e)
	{
		MediaElement.Stop();
	}

	private void OpenColorCompensation()
	{
		Log.Debug("OpenColorCompensation Click");
		ColorCompensationWnd wnd = new ColorCompensationWnd();
		wnd.pathfilename = ServiceProvider.Settings.SelectedBitmap.FileItem.FileName;
		if (wnd.ShowDialog() != true)
		{
		}
	}
}
