using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;
using MahApps.Metro.Controls;

namespace CameraControl.windows;

public partial class TurnTableWnd : Window, IWindow, IComponentConnector, IStyleConnector
{
	public class UserAngle
	{
		private bool _userAngleSelected;

		public string UserAngleName { get; set; }

		public bool UserAngleSelected
		{
			get
			{
				return _userAngleSelected;
			}
			set
			{
				if (value != _userAngleSelected)
				{
					_userAngleSelected = value;
				}
			}
		}
	}

	public static ObservableCollection<UserAngle> CaptureUserAngleList = new ObservableCollection<UserAngle>();

	public static ObservableCollection<UserAngle> CaptureUserAngleCheckedList = new ObservableCollection<UserAngle>();

	private DateTime RecodeStartTime;

	private Timer RecodingTimer = new Timer(500.0);

	private long elapsedTicks = 0L;

	public TurnTableWnd()
	{
		InitializeComponent();
		CaptureUserAngle.ItemsSource = CaptureUserAngleList;
		ListBoxUserAngleChecked.ItemsSource = CaptureUserAngleCheckedList;
		RecodingTimer.Elapsed += RecodingTimer_Elapsed;
		if (!DesignerProperties.GetIsInDesignMode(this))
		{
			((TurnTableViewModel)(DataContext = new TurnTableViewModel())).HostWindow = this;
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "TurnTableWnd_Show":
			if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
			{
				MessageBox.Show("⚠\ufe0f 카메라가 연결되지 않았습니다. 참고하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			else
			{
				((TurnTableViewModel)DataContext).SetCameraDevice(ServiceProvider.DeviceManager.SelectedCameraDevice);
				((TurnTableViewModel)DataContext).StartLiveView();
			}
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
				((TurnTableViewModel)DataContext).ReadMDP("D:\\Clorium Data\\Clorium_MDPData.txt");
				((TurnTableViewModel)DataContext).USBConnect();
			});
			break;
		case "TurnTableWnd_Hide":
			((TurnTableViewModel)DataContext).StopLiveView();
			((TurnTableViewModel)DataContext).USBDisconnect();
			Dispatcher.Invoke(Hide);
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		Vel.Visibility = Visibility.Hidden;
		Acc.Visibility = Visibility.Hidden;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("TurnTableWnd_Hide");
		}
	}

	public void RemoteLaserOn()
	{
		((TurnTableViewModel)DataContext).LaserOn();
	}

	public void RemoteLaserOff()
	{
		((TurnTableViewModel)DataContext).LaserOff();
	}

	public void RemoteMoveOneCycle()
	{
		((TurnTableViewModel)DataContext).MoveOneCycle();
	}

	public void RemoteMoveStop()
	{
		MouseUpClick(this, null);
	}

	public void RemoteMoveContinueCW()
	{
		CW_MouseDownClick(this, null);
	}

	public void RemoteMoveContinueCCW()
	{
		CCW_MouseDownClick(this, null);
	}

	public void RemoteMoveStepCW()
	{
		((TurnTableViewModel)DataContext).MoveStepCW();
	}

	public void RemoteMoveStepCCW()
	{
		((TurnTableViewModel)DataContext).MoveStepCCW();
	}

	private void CW_MouseDownClick(object sender, MouseButtonEventArgs e)
	{
		if (DataContext != null)
		{
			((TurnTableViewModel)DataContext).OnCWMouseDownClick(sender, e);
		}
	}

	private void CCW_MouseDownClick(object sender, MouseButtonEventArgs e)
	{
		if (DataContext != null)
		{
			((TurnTableViewModel)DataContext).OnCCWMouseDownClick(sender, e);
		}
	}

	private void MouseUpClick(object sender, MouseButtonEventArgs e)
	{
		if (DataContext != null)
		{
			((TurnTableViewModel)DataContext).OnMouseUpClick(sender, e);
		}
	}

	private void PosRadioButton_Click(object sender, RoutedEventArgs e)
	{
		RadioButton button = (RadioButton)sender;
		switch (button.Content.ToString())
		{
		case "  1단":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).Camera_Level = 0;
			}
			break;
		case "  2단":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).Camera_Level = 1;
			}
			break;
		case "  3단":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).Camera_Level = 2;
			}
			break;
		case "  4단":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).Camera_Level = 3;
			}
			break;
		case "  5단":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).Camera_Level = 4;
			}
			break;
		}
	}

	private void SpeedRadioButton_Click(object sender, RoutedEventArgs e)
	{
		RadioButton button = (RadioButton)sender;
		switch (button.Content.ToString())
		{
		case "고속":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).TurnTableSpeed = 0;
			}
			break;
		case "중속":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).TurnTableSpeed = 1;
			}
			break;
		case "저속":
			if (DataContext != null)
			{
				((TurnTableViewModel)DataContext).TurnTableSpeed = 2;
			}
			break;
		}
		((TurnTableViewModel)DataContext).SetTurnTableSpeed(((TurnTableViewModel)DataContext).TurnTableSpeed);
		Vel.SelectedItem = null;
		Vel.Text = string.Empty;
		Acc.SelectedItem = null;
		Acc.Text = string.Empty;
	}

	private void UserAcc_Click(object sender, RoutedEventArgs e)
	{
		if (UserAcc.IsChecked == true)
		{
			Vel.Visibility = Visibility.Visible;
			Acc.Visibility = Visibility.Visible;
		}
		else
		{
			Vel.Visibility = Visibility.Hidden;
			Acc.Visibility = Visibility.Hidden;
		}
	}

	private void Vel_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		object selectedItem = Vel.SelectedItem;
		if (selectedItem != null)
		{
			((TurnTableViewModel)DataContext).SetVelocity(((TurnTableViewModel)DataContext).sAxis, GetVelocityPulse(selectedItem.ToString()));
			((TurnTableViewModel)DataContext).TurnTableSpeed = 5;
			((TurnTableViewModel)DataContext).Speed_High = false;
			((TurnTableViewModel)DataContext).Speed_Mid = false;
			((TurnTableViewModel)DataContext).Speed_Low = false;
		}
	}

	private void Acc_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		object selectedItem = Acc.SelectedItem;
		if (selectedItem != null)
		{
			((TurnTableViewModel)DataContext).SetAcceleration(((TurnTableViewModel)DataContext).sAxis, GetAccelerationPulse(selectedItem.ToString()));
		}
	}

	private string GetVelocityPulse(string item)
	{
		return item switch
		{
			"10%" => "1000", 
			"20%" => "2000", 
			"30%" => "3000", 
			"40%" => "4000", 
			"50%" => "5000", 
			"60%" => "6000", 
			"70%" => "7000", 
			"80%" => "8000", 
			"90%" => "9000", 
			_ => "6000", 
		};
	}

	private string GetAccelerationPulse(string item)
	{
		return item switch
		{
			"10%" => "10000", 
			"20%" => "20000", 
			"30%" => "30000", 
			"40%" => "40000", 
			"50%" => "50000", 
			"60%" => "60000", 
			"70%" => "70000", 
			"80%" => "80000", 
			"90%" => "90000", 
			_ => "60000", 
		};
	}

	private void CaptureCountChanged_Click(object sender, SelectionChangedEventArgs e)
	{
		ChangeUserAngleList(allSelect: false);
		if (CaptureUserAngle != null)
		{
			CaptureUserAngle.SelectedIndex = 0;
			switch (((TurnTableViewModel)DataContext).CaptureCount_SelectedIndex)
			{
			case 0:
				((TurnTableViewModel)DataContext).maxShots = 2;
				break;
			case 1:
				((TurnTableViewModel)DataContext).maxShots = 4;
				break;
			case 2:
				((TurnTableViewModel)DataContext).maxShots = 8;
				break;
			case 3:
				((TurnTableViewModel)DataContext).maxShots = 12;
				break;
			case 4:
				((TurnTableViewModel)DataContext).maxShots = 18;
				break;
			case 5:
				((TurnTableViewModel)DataContext).maxShots = 24;
				break;
			case 6:
				((TurnTableViewModel)DataContext).maxShots = 36;
				break;
			case 7:
				((TurnTableViewModel)DataContext).maxShots = 48;
				break;
			case 8:
				((TurnTableViewModel)DataContext).maxShots = 60;
				break;
			case 9:
				((TurnTableViewModel)DataContext).maxShots = 72;
				break;
			}
		}
	}

	private void All_Checked(object sender, RoutedEventArgs e)
	{
		ChangeUserAngleList(allSelect: true);
		CaptureUserAngle.SelectedIndex = 0;
	}

	private void All_Unchecked(object sender, RoutedEventArgs e)
	{
		ChangeUserAngleList(allSelect: false);
		CaptureUserAngle.SelectedIndex = 0;
	}

	private void ChangeUserAngleList(bool allSelect)
	{
		try
		{
			CaptureUserAngleList.Clear();
			CaptureUserAngleCheckedList.Clear();
			int count = 0;
			switch (((TurnTableViewModel)DataContext).CaptureCount_SelectedIndex)
			{
			case 0:
				count = 2;
				break;
			case 1:
				count = 4;
				break;
			case 2:
				count = 8;
				break;
			case 3:
				count = 12;
				break;
			case 4:
				count = 18;
				break;
			case 5:
				count = 24;
				break;
			case 6:
				count = 36;
				break;
			case 7:
				count = 48;
				break;
			case 8:
				count = 60;
				break;
			case 9:
				count = 72;
				break;
			}
			string name = "";
			double angle = 0.0;
			double step = 360.0 / (double)count;
			for (int i = 0; i < count; i++)
			{
				angle = step * (double)i;
				int no = i + 1;
				name = "[" + no + "]   " + angle.ToString("F0") + "도";
				if (allSelect)
				{
					CaptureUserAngleList.Add(new UserAngle
					{
						UserAngleName = name,
						UserAngleSelected = true
					});
					CaptureUserAngleCheckedList.Add(CaptureUserAngleList[i]);
				}
				else if (i == 0)
				{
					CaptureUserAngleList.Add(new UserAngle
					{
						UserAngleName = name,
						UserAngleSelected = true
					});
					CaptureUserAngleCheckedList.Add(CaptureUserAngleList[0]);
				}
				else
				{
					CaptureUserAngleList.Add(new UserAngle
					{
						UserAngleName = name,
						UserAngleSelected = false
					});
				}
			}
			((TurnTableViewModel)DataContext).UserAngleSelectedCount = CaptureUserAngleCheckedList.Count;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void CheckBox_Checked(object sender, RoutedEventArgs e)
	{
		UpdateCheckBoxStates();
	}

	private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
	{
		CheckBox checkbox = sender as CheckBox;
		if (checkbox?.DataContext is UserAngle item && item == CaptureUserAngleList[0])
		{
			item.UserAngleSelected = true;
			checkbox.IsChecked = true;
		}
		else
		{
			UpdateCheckBoxStates();
		}
	}

	private void UpdateCheckBoxStates()
	{
		CaptureUserAngleCheckedList.Clear();
		foreach (UserAngle m in CaptureUserAngleList)
		{
			if (m.UserAngleSelected)
			{
				CaptureUserAngleCheckedList.Add(m);
			}
		}
		((TurnTableViewModel)DataContext).UserAngleSelectedCount = CaptureUserAngleCheckedList.Count;
	}

	private void RecodingTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		elapsedTicks = DateTime.Now.Ticks - RecodeStartTime.Ticks;
		TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
		this.Invoke(() =>
		{
			ElapsedDisp.Content = elapsedSpan.Minutes.ToString("00") + ":" + elapsedSpan.Seconds.ToString("00");
		});
	}

	private void RecBotton_Checked(object sender, RoutedEventArgs e)
	{
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;
		if (!LiveViewManager.Active)
		{
			((TurnTableViewModel)DataContext).Record_Running = false;
			MessageBox.Show("⚠\ufe0f 먼저 Live 화면을 실행하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		else if (DataContext != null)
		{
			ElapsedTitle.Foreground = Brushes.Red;
			ElapsedDisp.Content = "00:00";
			RecodeStartTime = DateTime.Now;
			RecodingTimer.Start();
			((TurnTableViewModel)DataContext).RecordMovie_Start();
		}
	}

	private void RecBotton_Unchecked(object sender, RoutedEventArgs e)
	{
		if (DataContext != null)
		{
			ElapsedTitle.Foreground = Brushes.Black;
			((TurnTableViewModel)DataContext).Record_Running = false;
			((TurnTableViewModel)DataContext).TurnTableStop();
			((TurnTableViewModel)DataContext).RecordMovie_Stop();
			RecodingTimer.Stop();
		}
	}
}
