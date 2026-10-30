using System;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Panels;

public partial class SelectionControl : UserControl, IComponentConnector
{
	public RelayCommand<object> SelectAllCommand { get; private set; }

	public RelayCommand<object> SelectLiked { get; private set; }

	public RelayCommand<object> SelectUnLiked { get; private set; }

	public RelayCommand<object> SelectNoneCommand { get; private set; }

	public RelayCommand<object> SelectInvertCommand { get; private set; }

	public RelayCommand<object> SelectSeries { get; private set; }

	public SelectionControl()
	{
		SelectAllCommand = new RelayCommand<object>(delegate
		{
			ServiceProvider.Settings.DefaultSession.SelectAll();
		});
		SelectNoneCommand = new RelayCommand<object>(delegate
		{
			ServiceProvider.Settings.DefaultSession.SelectNone();
		});
		SelectLiked = new RelayCommand<object>(delegate
		{
			ServiceProvider.Settings.DefaultSession.SelectLiked();
		});
		SelectUnLiked = new RelayCommand<object>(delegate
		{
			ServiceProvider.Settings.DefaultSession.SelectUnLiked();
		});
		SelectInvertCommand = new RelayCommand<object>(delegate
		{
			ServiceProvider.Settings.DefaultSession.SelectInver();
		});
		SelectSeries = new RelayCommand<object>(delegate
		{
			try
			{
				ServiceProvider.Settings.DefaultSession.SelectSameSeries(ServiceProvider.Settings.SelectedBitmap.FileItem.Series);
			}
			catch (Exception exception)
			{
				Log.Error("SelectSeries", exception);
			}
		});
		InitializeComponent();
	}
}
