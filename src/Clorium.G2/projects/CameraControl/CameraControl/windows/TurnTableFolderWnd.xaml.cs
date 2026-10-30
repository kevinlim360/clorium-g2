using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class TurnTableFolderWnd : Window, IComponentConnector
{
	public class FolderInform
	{
		public string PreFixFolder;

		public string PreFixImage;

		public string FullFolder;

		public string Level;

		public string ShotAmount;

		public int DelayTime;
	}

	public FolderInform folderInform = new FolderInform();

	private int folderCount;

	public TurnTableFolderWnd()
	{
		InitializeComponent();
		Loaded += TurnTableFolderWnd_Loaded;
	}

	private void TurnTableFolderWnd_Loaded(object sender, RoutedEventArgs e)
	{
		if (Owner != null)
		{
			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = Owner.Left + (Owner.Width - ActualWidth) / 2.0;
			Top = Owner.Top + (Owner.Height - ActualHeight) / 2.0;
		}
	}

	public void init()
	{
		((TurnTableFolderViewModel)DataContext).PrefixImage = "Photo";
		((TurnTableFolderViewModel)DataContext).PrefixFolder = folderInform.PreFixFolder;
		((TurnTableFolderViewModel)DataContext).FullName = GetFolderName_CreatFolder();
		((TurnTableFolderViewModel)DataContext).DelayTimeIndex = GetDelayTimeIndex(folderInform.DelayTime);
	}

	private int GetDelayTimeIndex(int delayTime)
	{
		int index = 0;
		switch (delayTime)
		{
		case 500:
			index = 0;
			break;
		case 1000:
			index = 1;
			break;
		case 1500:
			index = 2;
			break;
		case 2000:
			index = 3;
			break;
		}
		return index;
	}

	private void tb_PrefixFolder_TextChanged(object sender, TextChangedEventArgs e)
	{
		DispFullFolderName();
	}

	private void DispFullFolderName()
	{
		string fileName = Path.GetFileName(ServiceProvider.Settings.DefaultSession.Folder);
		string barCode = fileName.Replace(ServiceProvider.Settings.CloriumID, "");
		((TurnTableFolderViewModel)DataContext).FullName = barCode + "_" + ((TurnTableFolderViewModel)DataContext).PrefixFolder + "_" + folderInform.Level + "L_" + folderInform.ShotAmount + "P_" + folderCount.ToString("00");
	}

	private string GetFolderName_CreatFolder()
	{
		string fileName = Path.GetFileName(ServiceProvider.Settings.DefaultSession.Folder);
		string barCode = fileName.Replace(ServiceProvider.Settings.CloriumID, "");
		string folderName = "";
		for (int i = 0; i < 100; i++)
		{
			folderName = ((i != 0) ? (barCode + "_" + ((TurnTableFolderViewModel)DataContext).PrefixFolder + "_" + folderInform.Level + "L_" + folderInform.ShotAmount + "P___" + i.ToString("0")) : (barCode + "_" + ((TurnTableFolderViewModel)DataContext).PrefixFolder + "_" + folderInform.Level + "L_" + folderInform.ShotAmount + "P"));
			string createNewFolder = ServiceProvider.Settings.DefaultSession.Folder + "\\" + folderName;
			DirectoryInfo folder = new DirectoryInfo(createNewFolder);
			if (!folder.Exists)
			{
				folderCount = i;
				break;
			}
		}
		return folderName;
	}

	private void btnStart_Click(object sender, RoutedEventArgs e)
	{
		folderInform.PreFixImage = ((TurnTableFolderViewModel)DataContext).PrefixImage;
		folderInform.FullFolder = ((TurnTableFolderViewModel)DataContext).FullName;
		folderInform.PreFixFolder = ((TurnTableFolderViewModel)DataContext).PrefixFolder;
		switch (((TurnTableFolderViewModel)DataContext).DelayTimeIndex)
		{
		case 0:
			folderInform.DelayTime = 500;
			break;
		case 1:
			folderInform.DelayTime = 1000;
			break;
		case 2:
			folderInform.DelayTime = 1500;
			break;
		case 3:
			folderInform.DelayTime = 2000;
			break;
		}
		string createNewFolder = ServiceProvider.Settings.DefaultSession.Folder + "\\" + folderInform.FullFolder;
		DirectoryInfo di = new DirectoryInfo(createNewFolder);
		if (!di.Exists)
		{
			di.Create();
		}
		DialogResult = true;
	}

	private void btnCancel_Click(object sender, RoutedEventArgs e)
	{
		folderInform.PreFixFolder = ((TurnTableFolderViewModel)DataContext).PrefixFolder;
		DialogResult = false;
	}
}
