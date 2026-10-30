using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Classes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.windows;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

internal class TurnTableViewModel : LiveViewViewModel
{
	public enum SaveTypeEnum
	{
		SaveTo_Camera = 1,
		SaveTo_Host,
		SaveTo_Both
	}

	private readonly Arduino arduino;

	private readonly SerialPort serialPort1;

	private int RESOLUTION_MOTOR;

	private int GEARRATIO_MOTOR;

	private int GEARCOUNT_PULLEY;

	private int GEARCOUNT_TURNTABLE;

	private double RADIUS_PULLEY;

	private double RADIUS_TURNTABLE;

	private bool DrivingMethod = true;

	private double UNIT_ANGLE_TURNTABLE = 0.0;

	private int UNIT_CYCLE_TURNTABLE = 0;

	private string TURNTABLE_SPEED_LOW;

	private string TURNTABLE_SPEED_MID;

	private string TURNTABLE_SPEED_HIGH;

	private string TURNTABLE_ACCEL;

	private int ONECYCLE_TIME_LOW;

	private int ONECYCLE_TIME_MID;

	private int ONECYCLE_TIME_HIGH;

	private bool isRunning = false;

	private bool bIsMovePouse = false;

	private bool bIsMoveStop = false;

	private bool bIsBtnExcute = false;

	private bool bTargetPosUpdate = false;

	public string sAxis = "0";

	private string ContinuousSpeed = "4500";

	private string PreFixImage = "";

	private string FullFolder = "";

	private string PreFixFolder = "";

	private int DelayTime = 0;

	private bool _dirccw;

	private string _currentPos;

	private string _currentPosDisp;

	private string _tatgetPos;

	private string _tatgetPosDisp;

	private SolidColorBrush _connectStatus = System.Windows.Media.Brushes.Red;

	private int _turnTableCount;

	public int _userAngleSelectedCount;

	public int TurnTableSpeed;

	private bool _speed_high;

	private bool _speed_mid;

	private bool _speed_low;

	private int _camera_pos;

	private bool _pos_level1;

	private bool _pos_level2;

	private bool _pos_level3;

	private bool _pos_level4;

	private bool _pos_level5;

	private bool _record_running;

	private bool _pouseRun;

	private bool _waitProgressBar;

	private string _recodingtime;

	private string _folderPath;

	private bool _shownonstopbutton = true;

	private bool _processCentering = false;

	private int _captureCount_SelectedIndex;

	private IList<string> _captureCount_List = new List<string>();

	private IList<string> _vel_List = new List<string>();

	private IList<string> _accl_List = new List<string>();

	private readonly string CMD_MOTOR_EBABLE = "enable";

	private readonly string CMD_MOVE_ABS = "moveabs";

	private readonly string CMD_MOVE_CON = "movecon";

	private readonly string CMD_MOVE_STOP = "stop";

	private readonly string CMD_SET_MAXVEL = "setmaxvel";

	private readonly string CMD_SET_ACC = "setacc";

	private readonly string CMD_SET_HOME = "sethome";

	private readonly string CMD_GET_POS = "getpos";

	private readonly string CMD_OUTPUT_LASER = "laser";

	public int maxShots = 4;

	private bool bErrorTimeout = false;

	private bool MultiCapture = true;

	private readonly object _locker = new object();

	public RelayCommand LaserOnCommand { get; set; }

	public RelayCommand LaserOffCommand { get; set; }

	public RelayCommand CameraFocusCommand { get; set; }

	public RelayCommand CameraCaptureCommand { get; set; }

	public RelayCommand MoveTeachHomeCommand { get; set; }

	public RelayCommand MoveHomeCommand { get; set; }

	public RelayCommand MoveStepCWCommand { get; set; }

	public RelayCommand MoveStepCCWCommand { get; set; }

	public RelayCommand MoveOneCycleCommand { get; set; }

	public RelayCommand TurnTableStartCommand { get; set; }

	public RelayCommand TurnTableUserStartCommand { get; set; }

	public RelayCommand TurnTablePouseCommand { get; set; }

	public RelayCommand TurnTablePouseStartCommand { get; set; }

	public RelayCommand TurnTableStopCommand { get; set; }

	public RelayCommand TurnTableNonStopStartCommand { get; set; }

	public RelayCommand TurnTableCenteringCommand { get; set; }

	public ICommand CaptureCount_SelectionChangedCommand { get; set; }

	public bool DirCCW
	{
		get
		{
			return _dirccw;
		}
		set
		{
			_dirccw = value;
		}
	}

	public string CurrentPos
	{
		get
		{
			return _currentPos;
		}
		set
		{
			_currentPos = value;
			CurrentPosDisp = (double.Parse(_currentPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
		}
	}

	public string CurrentPosDisp
	{
		get
		{
			return _currentPosDisp;
		}
		set
		{
			_currentPosDisp = value;
			RaisePropertyChanged(() => CurrentPosDisp);
		}
	}

	public string TargetPos
	{
		get
		{
			return _tatgetPos;
		}
		set
		{
			_tatgetPos = value;
		}
	}

	public string TargetPosDisp
	{
		get
		{
			return _tatgetPosDisp;
		}
		set
		{
			_tatgetPosDisp = value;
			RaisePropertyChanged(() => TargetPosDisp);
		}
	}

	public SolidColorBrush ConnectStatus
	{
		get
		{
			return _connectStatus;
		}
		set
		{
			_connectStatus = value;
			RaisePropertyChanged(() => ConnectStatus);
		}
	}

	public int TurnTableCount
	{
		get
		{
			return _turnTableCount;
		}
		set
		{
			_turnTableCount = value;
			RaisePropertyChanged(() => TurnTableCount);
		}
	}

	public int UserAngleSelectedCount
	{
		get
		{
			return _userAngleSelectedCount;
		}
		set
		{
			_userAngleSelectedCount = value;
			RaisePropertyChanged(() => UserAngleSelectedCount);
		}
	}

	public bool Speed_High
	{
		get
		{
			return _speed_high;
		}
		set
		{
			_speed_high = value;
			RaisePropertyChanged(() => Speed_High);
		}
	}

	public bool Speed_Mid
	{
		get
		{
			return _speed_mid;
		}
		set
		{
			_speed_mid = value;
			RaisePropertyChanged(() => Speed_Mid);
		}
	}

	public bool Speed_Low
	{
		get
		{
			return _speed_low;
		}
		set
		{
			_speed_low = value;
			RaisePropertyChanged(() => Speed_Low);
		}
	}

	public int Camera_Level
	{
		get
		{
			return _camera_pos;
		}
		set
		{
			_camera_pos = value;
			RaisePropertyChanged(() => Camera_Level);
		}
	}

	public bool Pos_Level1
	{
		get
		{
			return _pos_level1;
		}
		set
		{
			_pos_level1 = value;
			RaisePropertyChanged(() => Pos_Level1);
		}
	}

	public bool Pos_Level2
	{
		get
		{
			return _pos_level2;
		}
		set
		{
			_pos_level2 = value;
			RaisePropertyChanged(() => Pos_Level2);
		}
	}

	public bool Pos_Level3
	{
		get
		{
			return _pos_level3;
		}
		set
		{
			_pos_level3 = value;
			RaisePropertyChanged(() => Pos_Level3);
		}
	}

	public bool Pos_Level4
	{
		get
		{
			return _pos_level4;
		}
		set
		{
			_pos_level4 = value;
			RaisePropertyChanged(() => Pos_Level4);
		}
	}

	public bool Pos_Level5
	{
		get
		{
			return _pos_level5;
		}
		set
		{
			_pos_level5 = value;
			RaisePropertyChanged(() => Pos_Level5);
		}
	}

	public bool Record_Running
	{
		get
		{
			return _record_running;
		}
		set
		{
			_record_running = value;
			RaisePropertyChanged(() => Record_Running);
		}
	}

	public bool PouseRun
	{
		get
		{
			return _pouseRun;
		}
		set
		{
			_pouseRun = value;
			RaisePropertyChanged(() => PouseRun);
		}
	}

	public bool WaitProgressBar
	{
		get
		{
			return _waitProgressBar;
		}
		set
		{
			_waitProgressBar = value;
			RaisePropertyChanged(() => WaitProgressBar);
		}
	}

	public string RecodingTime
	{
		get
		{
			return _recodingtime;
		}
		set
		{
			_recodingtime = value;
			RaisePropertyChanged(() => RecodingTime);
		}
	}

	public string FolderPath
	{
		get
		{
			return _folderPath;
		}
		set
		{
			_folderPath = value;
			RaisePropertyChanged(() => FolderPath);
		}
	}

	public bool ShowNonStopButton
	{
		get
		{
			return _shownonstopbutton;
		}
		set
		{
			_shownonstopbutton = value;
			RaisePropertyChanged(() => ShowNonStopButton);
		}
	}

	public bool ProcessCentering
	{
		get
		{
			return _processCentering;
		}
		set
		{
			_processCentering = value;
			RaisePropertyChanged(() => ProcessCentering);
		}
	}

	public int CaptureCount_SelectedIndex
	{
		get
		{
			return _captureCount_SelectedIndex;
		}
		set
		{
			_captureCount_SelectedIndex = value;
		}
	}

	public IList<string> CaptureCount_List
	{
		get
		{
			return _captureCount_List;
		}
		set
		{
			_captureCount_List = value;
			RaisePropertyChanged(() => CaptureCount_List);
		}
	}

	public IList<string> Vel_List
	{
		get
		{
			return _vel_List;
		}
		set
		{
			_vel_List = value;
			RaisePropertyChanged(() => Vel_List);
		}
	}

	public IList<string> Acc_List
	{
		get
		{
			return _accl_List;
		}
		set
		{
			_accl_List = value;
			RaisePropertyChanged(() => Acc_List);
		}
	}

	public Window HostWindow { get; set; }

	public void SetCameraDevice(ICameraDevice device)
	{
		CameraDevice = device;
	}

	public TurnTableViewModel()
	{
		LaserOnCommand = new RelayCommand(LaserOn);
		LaserOffCommand = new RelayCommand(LaserOff);
		CameraFocusCommand = new RelayCommand(CameraFocus);
		CameraCaptureCommand = new RelayCommand(CameraCaptureNoAf);
		MoveTeachHomeCommand = new RelayCommand(MoveTeachHome);
		MoveHomeCommand = new RelayCommand(MoveHomeThread);
		MoveStepCWCommand = new RelayCommand(MoveStepCW);
		MoveStepCCWCommand = new RelayCommand(MoveStepCCW);
		MoveOneCycleCommand = new RelayCommand(MoveOneCycle);
		TurnTableStartCommand = new RelayCommand(TurnTableStepStart);
		TurnTableNonStopStartCommand = new RelayCommand(TurnTableNonStopStart);
		TurnTableUserStartCommand = new RelayCommand(TurnTableUserStart);
		TurnTablePouseCommand = new RelayCommand(TurnTablePouse);
		TurnTablePouseStartCommand = new RelayCommand(TurnTablePouseStart);
		TurnTableStopCommand = new RelayCommand(TurnTableStop);
		TurnTableCenteringCommand = new RelayCommand(ProcessCenteringAndMagnification);
		CaptureCount_List.Add(" 2컷 (180도)");
		CaptureCount_List.Add(" 4컷 ( 90도)");
		CaptureCount_List.Add(" 8컷 ( 45도)");
		CaptureCount_List.Add("12컷 ( 30도)");
		CaptureCount_List.Add("18컷 ( 20도)");
		CaptureCount_List.Add("24컷 ( 15도)");
		CaptureCount_List.Add("36컷 ( 10도)");
		CaptureCount_List.Add("48컷 (7.5도)");
		CaptureCount_List.Add("60컷 (6.0도)");
		CaptureCount_List.Add("72컷 (5.0도)");
		Vel_List.Add("10%");
		Vel_List.Add("20%");
		Vel_List.Add("30%");
		Vel_List.Add("40%");
		Vel_List.Add("50%");
		Vel_List.Add("60%");
		Vel_List.Add("70%");
		Vel_List.Add("80%");
		Vel_List.Add("90%");
		Acc_List.Add("10%");
		Acc_List.Add("20%");
		Acc_List.Add("30%");
		Acc_List.Add("40%");
		Acc_List.Add("50%");
		Acc_List.Add("60%");
		Acc_List.Add("70%");
		Acc_List.Add("80%");
		Acc_List.Add("90%");
		Camera_Level = 0;
		Pos_Level1 = true;
		Record_Running = false;
		WaitProgressBar = false;
		PouseRun = false;
		UserAngleSelectedCount = 0;
		CaptureCount_SelectedIndex = 1;
		RecodingTime = "00:00";
		serialPort1 = new SerialPort();
		arduino = new Arduino(serialPort1, 115200, "VID_2341");
		arduino.ConnectStatusChanged += (System.Windows.Media.Brush status) =>
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				ConnectStatus = (SolidColorBrush)status;
			});
		};
		arduino.CurrentPosPluseChanged += (string pos) =>
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				CurrentPos = pos;
				if (bTargetPosUpdate)
				{
					TargetPos = CurrentPos;
					TargetPosDisp = (double.Parse(TargetPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
					bTargetPosUpdate = false;
				}
			});
		};
	}

	private void OnCaptureCount_SelectionChanged(object obj)
	{
		SelectionChangedEventArgs args = obj as SelectionChangedEventArgs;
		if (args != null)
		{
		}
	}

	public void USBConnect()
	{
		arduino.Init();
		if (!arduino.ConnectSerial())
		{
			MessageBox.Show("\ud83d\udea8 턴테이블 USB 드라이브를 찾을 수 없습니다.\ud83d\udea8\n\n⚠\ufe0f 턴테이블 USB 포트 확인 바랍니다.⚠\ufe0f", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		GetPos(sAxis);
		Thread.Sleep(100);
		MotorEnable(sAxis);
		Thread.Sleep(100);
		Stop(sAxis);
		SetHome(sAxis);
		SetTurnTableSpeed(TurnTableSpeed);
	}

	public void USBDisconnect()
	{
		arduino.Uninit();
	}

	private string MakePacket(string command, string axis, string param1, string param2)
	{
		string packet = command;
		if (!string.IsNullOrEmpty(axis))
		{
			packet = packet + " " + axis;
		}
		if (!string.IsNullOrEmpty(param1))
		{
			packet = packet + " " + param1;
		}
		if (!string.IsNullOrEmpty(param2))
		{
			packet = packet + " " + param2;
		}
		return packet + "\n";
	}

	// Recovery inference: send an absolute-position packet to the Arduino controller and optionally wait for the requested position.
	private bool MoveAbs(string axis, string targetpos, bool wait = true)
	{
		try
		{
			isRunning = true;
			TargetPos = targetpos;
			TargetPosDisp = (double.Parse(TargetPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
			string packet = MakePacket(CMD_MOVE_ABS, axis, targetpos, "");
			arduino.CommandSend(packet);
			if (wait)
			{
				WaitDone(targetpos);
			}
			return true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("⚠\ufe0f 절대 위치 이동 중 예외 발생: " + ex3.Message, "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
			return false;
		}
		finally
		{
			if (wait)
			{
				isRunning = false;
			}
		}
	}

	private bool MoveRel(string axis, string step, bool wait = true)
	{
		try
		{
			isRunning = true;
			int targetpos = int.Parse(CurrentPos) + (int)double.Parse(step);
			TargetPos = targetpos.ToString();
			TargetPosDisp = (double.Parse(TargetPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
			string packet = MakePacket(CMD_MOVE_ABS, axis, targetpos.ToString(), "");
			arduino.CommandSend(packet);
			if (wait)
			{
				WaitDone(targetpos.ToString());
			}
			return true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("⚠\ufe0f 상대 위치 이동 중 예외 발생: " + ex3.Message, "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
			return false;
		}
		finally
		{
			if (wait)
			{
				isRunning = false;
			}
		}
	}

	private void MoveHome(string axis)
	{
		try
		{
			bool dir_minus = false;
			isRunning = true;
			double currentPulse = double.Parse(CurrentPos);
			int remainderPulse = (int)(currentPulse % (double)UNIT_CYCLE_TURNTABLE);
			if (remainderPulse < 0)
			{
				dir_minus = true;
			}
			if (Math.Abs(remainderPulse) > UNIT_CYCLE_TURNTABLE / 2)
			{
				if (dir_minus)
				{
					MoveRel(axis, "-" + (UNIT_CYCLE_TURNTABLE + remainderPulse));
				}
				else
				{
					MoveRel(axis, (UNIT_CYCLE_TURNTABLE - remainderPulse).ToString());
				}
			}
			else if (dir_minus)
			{
				MoveRel(axis, Math.Abs(remainderPulse).ToString());
			}
			else
			{
				MoveRel(axis, "-" + remainderPulse);
			}
			TargetPos = "0";
			CurrentPos = "0";
			TargetPosDisp = "0도";
			CurrentPosDisp = "0도";
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("⚠\ufe0f 홈 위치로 이동 중 예외 발생: " + ex3.Message);
			});
			isRunning = false;
		}
	}

	private void WaitDone(string targetPos)
	{
		while (CurrentPos != targetPos)
		{
			GetPos(sAxis);
			if (!isRunning)
			{
				break;
			}
			Thread.Sleep(100);
		}
		isRunning = false;
	}

	private void WaitPouse()
	{
		while (bIsMovePouse)
		{
			Thread.Sleep(100);
		}
	}

	private void Stop(string axis)
	{
		string packet = MakePacket(CMD_MOVE_STOP, axis, "", "");
		arduino.CommandSend(packet);
		isRunning = false;
	}

	private void MoveContinuous(string axis, string velocity)
	{
		string packet = MakePacket(CMD_MOVE_CON, axis, velocity, "");
		arduino.CommandSend(packet);
	}

	public void SetVelocity(string axis, string vel)
	{
		string packet = MakePacket(CMD_SET_MAXVEL, axis, vel, "");
		arduino.CommandSend(packet);
	}

	public void SetAcceleration(string axis, string acc)
	{
		string packet = MakePacket(CMD_SET_ACC, axis, acc, "");
		arduino.CommandSend(packet);
	}

	private void SetHome(string axis)
	{
		string packet = MakePacket(CMD_SET_HOME, axis, "", "");
		arduino.CommandSend(packet);
		CurrentPos = "0";
		TargetPos = CurrentPos;
		CurrentPosDisp = (double.Parse(CurrentPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
		TargetPosDisp = (double.Parse(TargetPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
	}

	private void GetPos(string axis)
	{
		string packet = MakePacket(CMD_GET_POS, axis, "", "");
		arduino.CommandSend(packet);
	}

	private void MotorEnable(string axis)
	{
		string packet = MakePacket(CMD_MOTOR_EBABLE, axis, "", "");
		arduino.CommandSend(packet);
	}

	public void LaserOn()
	{
		string packet = MakePacket(CMD_OUTPUT_LASER, "1", "", "");
		arduino.CommandSend(packet);
	}

	public void LaserOff()
	{
		string packet = MakePacket(CMD_OUTPUT_LASER, "0", "", "");
		arduino.CommandSend(packet);
	}

	public void MoveTeachHome()
	{
		if (!isRunning)
		{
			SetHome(sAxis);
		}
	}

	public void MoveHomeThread()
	{
		if (!isRunning)
		{
			Thread exe = new Thread(() =>
			{
				MoveHome(sAxis);
			});
			exe.Start();
		}
	}

	public void MoveStepCW()
	{
		if (!isRunning)
		{
			string step = UNIT_ANGLE_TURNTABLE.ToString();
			Thread exe = new Thread(() =>
			{
				MoveRel(sAxis, step);
			});
			exe.Start();
		}
	}

	public void MoveStepCCW()
	{
		if (!isRunning)
		{
			string step = "-" + UNIT_ANGLE_TURNTABLE;
			Thread exe = new Thread(() =>
			{
				MoveRel(sAxis, step);
			});
			exe.Start();
		}
	}

	public void MoveOneCycle()
	{
		if (!isRunning)
		{
			string step = UNIT_CYCLE_TURNTABLE.ToString();
			Thread exe = new Thread(() =>
			{
				MoveRel(sAxis, step);
			});
			exe.Start();
		}
	}

	public void OnCWMouseDownClick(object sender, RoutedEventArgs e)
	{
		if (isRunning)
		{
			return;
		}
		if (TurnTableSpeed == 5)
		{
			MessageBox.Show("✅ 고속/중속/저속 선택해 주세요.");
			return;
		}
		switch (TurnTableSpeed)
		{
		case 0:
			ContinuousSpeed = TURNTABLE_SPEED_HIGH;
			break;
		case 1:
			ContinuousSpeed = TURNTABLE_SPEED_MID;
			break;
		case 2:
			ContinuousSpeed = TURNTABLE_SPEED_LOW;
			break;
		}
		MoveContinuous(sAxis, ContinuousSpeed);
	}

	public void OnCCWMouseDownClick(object sender, RoutedEventArgs e)
	{
		if (isRunning)
		{
			return;
		}
		if (TurnTableSpeed == 5)
		{
			MessageBox.Show("✅ 고속/중속/저속 선택해 주세요.");
			return;
		}
		switch (TurnTableSpeed)
		{
		case 0:
			ContinuousSpeed = "-" + TURNTABLE_SPEED_HIGH;
			break;
		case 1:
			ContinuousSpeed = "-" + TURNTABLE_SPEED_MID;
			break;
		case 2:
			ContinuousSpeed = "-" + TURNTABLE_SPEED_LOW;
			break;
		}
		MoveContinuous(sAxis, ContinuousSpeed);
	}

	public void OnMouseUpClick(object sender, RoutedEventArgs e)
	{
		Stop(sAxis);
		GetPos(sAxis);
		bTargetPosUpdate = true;
	}

	public void TurnTableGotoHome()
	{
		bIsMoveStop = true;
		bIsMovePouse = false;
		MoveHome(sAxis);
	}

	public void TurnTableStop()
	{
		bIsMoveStop = true;
		bIsMovePouse = false;
		Stop(sAxis);
	}

	public void TurnTablePouse()
	{
		bIsMovePouse = true;
		PouseRun = true;
	}

	public void TurnTablePouseStart()
	{
		bIsMovePouse = false;
		PouseRun = false;
	}

	public void SetTurnTableSpeed(int setSpeed)
	{
		switch (setSpeed)
		{
		case 0:
			SetVelocity(sAxis, TURNTABLE_SPEED_HIGH);
			Speed_High = true;
			break;
		case 1:
			SetVelocity(sAxis, TURNTABLE_SPEED_MID);
			Speed_Mid = true;
			break;
		case 2:
			SetVelocity(sAxis, TURNTABLE_SPEED_LOW);
			Speed_Low = true;
			break;
		}
		SetAcceleration(sAxis, TURNTABLE_ACCEL);
	}

	public void TurnTableStepStart()
	{
		if (!CheckTurnTableStatus() || bIsBtnExcute)
		{
			return;
		}
		bIsBtnExcute = true;
		Log.Debug("턴테이블 스텝촬영 실행");
		try
		{
			TurnTableFolderWnd turnTableFolderWnd = new TurnTableFolderWnd();
			turnTableFolderWnd.Owner = HostWindow;
			turnTableFolderWnd.folderInform.PreFixFolder = "Step";
			turnTableFolderWnd.folderInform.Level = (Camera_Level + 1).ToString("0");
			turnTableFolderWnd.folderInform.ShotAmount = maxShots.ToString("00");
			turnTableFolderWnd.folderInform.DelayTime = DelayTime;
			turnTableFolderWnd.init();
			bool IsOk = turnTableFolderWnd.ShowDialog().Value;
			PreFixFolder = turnTableFolderWnd.folderInform.PreFixFolder;
			FolderPath = ServiceProvider.Settings.DefaultSession.Folder + "\\" + turnTableFolderWnd.folderInform.FullFolder;
			if (IsOk)
			{
				PreFixImage = turnTableFolderWnd.folderInform.PreFixImage;
				FullFolder = turnTableFolderWnd.folderInform.FullFolder;
				DelayTime = turnTableFolderWnd.folderInform.DelayTime;
				Thread exe = new Thread(() =>
				{
					ThreadTurnTableStepStart();
				});
				exe.Start();
			}
			else
			{
				bIsBtnExcute = false;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error TurnTable Folder Dialog", ex);
			MessageBox.Show(ex.ToString());
		}
	}

	public void ThreadTurnTableStepStart()
	{
		switch (CaptureCount_SelectedIndex)
		{
		case 0:
			maxShots = 2;
			break;
		case 1:
			maxShots = 4;
			break;
		case 2:
			maxShots = 8;
			break;
		case 3:
			maxShots = 12;
			break;
		case 4:
			maxShots = 18;
			break;
		case 5:
			maxShots = 24;
			break;
		case 6:
			maxShots = 36;
			break;
		case 7:
			maxShots = 48;
			break;
		case 8:
			maxShots = 60;
			break;
		case 9:
			maxShots = 72;
			break;
		}
		List<string> absPosList = new List<string>();
		PositionListForCapture(ref absPosList, maxShots, DirCCW);
		CaptureStepMove_Start(absPosList, maxShots);
		if (ProcessCentering)
		{
			ProcessCenteringAndMagnification();
		}
	}

	private void PositionListForCapture(ref List<string> absposlist, int maxCount, bool dir)
	{
		for (int i = 0; i < maxCount + 1; i++)
		{
			double stepAngle = 360.0 / (double)maxCount;
			int absPosition = (int)(stepAngle * (double)i * UNIT_ANGLE_TURNTABLE);
			if (dir)
			{
				absPosition = -absPosition;
			}
			absposlist.Add(absPosition.ToString());
		}
	}

	public void TurnTableUserStart()
	{
		if (!CheckTurnTableStatus() || bIsBtnExcute)
		{
			return;
		}
		bIsBtnExcute = true;
		Log.Debug("턴테이블 지정각도 촬영 실행");
		try
		{
			TurnTableFolderWnd turnTableFolderWnd = new TurnTableFolderWnd();
			turnTableFolderWnd.Owner = HostWindow;
			turnTableFolderWnd.folderInform.PreFixFolder = "Angle";
			turnTableFolderWnd.folderInform.Level = (Camera_Level + 1).ToString("0");
			turnTableFolderWnd.folderInform.ShotAmount = maxShots.ToString("00");
			turnTableFolderWnd.folderInform.DelayTime = DelayTime;
			turnTableFolderWnd.init();
			bool IsOk = turnTableFolderWnd.ShowDialog().Value;
			PreFixFolder = turnTableFolderWnd.folderInform.PreFixFolder;
			FolderPath = ServiceProvider.Settings.DefaultSession.Folder + "\\" + turnTableFolderWnd.folderInform.FullFolder;
			if (IsOk)
			{
				PreFixImage = turnTableFolderWnd.folderInform.PreFixImage;
				FullFolder = turnTableFolderWnd.folderInform.FullFolder;
				DelayTime = turnTableFolderWnd.folderInform.DelayTime;
				Thread exe = new Thread(() =>
				{
					ThreadTurnTableUserStart();
				});
				exe.Start();
			}
			else
			{
				bIsBtnExcute = false;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error TurnTable Folder Dialog", ex);
			MessageBox.Show(ex.ToString());
		}
	}

	public void ThreadTurnTableUserStart()
	{
		int count = UserAngleSelectedCount;
		List<string> absPosList = new List<string>();
		SelectedUserAngleListForCapture(ref absPosList, count, DirCCW);
		CaptureStepMove_Start(absPosList, count);
	}

	private void SelectedUserAngleListForCapture(ref List<string> absposlist, int count, bool dir)
	{
		for (int i = 0; i < count; i++)
		{
			string userangleName = TurnTableWnd.CaptureUserAngleCheckedList[i].UserAngleName;
			if (userangleName != null)
			{
				int spos = userangleName.IndexOf(']');
				int epos = userangleName.IndexOf('도');
				int len = epos - spos;
				userangleName = userangleName.Substring(spos + 2, len - 2);
				int userAngle = Convert.ToInt32(userangleName);
				int absPosition = (int)((double)userAngle * UNIT_ANGLE_TURNTABLE);
				if (dir)
				{
					absPosition = -absPosition;
				}
				absposlist.Add(absPosition.ToString());
			}
		}
		int lastPosition = (int)(360.0 * UNIT_ANGLE_TURNTABLE);
		if (dir)
		{
			lastPosition = -lastPosition;
		}
		absposlist.Add(lastPosition.ToString());
	}

	private void CaptureStepMove_Start(List<string> absPos, int maxshots)
	{
		try
		{
			WaitProgressBar = true;
			bIsMoveStop = false;
			bIsMovePouse = false;
			PouseRun = false;
			bErrorTimeout = false;
			ServiceProvider.Settings.TurnTableShot = true;
			LaserOff();
			SetHome(sAxis);
			TurnTableCount = 0;
			for (int i = 0; i < maxshots + 1 && MoveAbs(sAxis, absPos[i]); i++)
			{
				if (!CaptureWait())
				{
					break;
				}
				if (maxshots - i == 0)
				{
					break;
				}
				Thread.Sleep(DelayTime);
				CaptureStart();
				TurnTableCount++;
				if (bIsMovePouse && i < maxshots)
				{
					WaitPouse();
				}
				if (bIsMoveStop)
				{
					Thread.Sleep(300);
					MoveHomeThread();
					Application.Current.Dispatcher.Invoke(() =>
					{
						MessageBox.Show("\ud83d\udce2 원점 위치로 이동중입니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
					});
					break;
				}
			}
			if (bErrorTimeout)
			{
				TurnTableGotoHome();
				Application.Current.Dispatcher.Invoke(() =>
				{
					Log.Error("이미지 다운로드 타임 아웃...");
					MessageBox.Show("⚠\ufe0f 이미지 다운로드 타임 아웃...", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				});
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception e = ex2;
			TurnTableGotoHome();
			Application.Current.Dispatcher.Invoke(() =>
			{
				Log.Error("일반/지정 촬영 : 에러 발생", e);
				MessageBox.Show(e.ToString(), "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
			});
		}
		finally
		{
			WaitProgressBar = false;
			bIsBtnExcute = false;
			ServiceProvider.Settings.TurnTableShot = false;
		}
	}

	public void TurnTableNonStopStart()
	{
		if (!CheckTurnTableStatus() || bIsBtnExcute)
		{
			return;
		}
		bIsBtnExcute = true;
		Log.Debug("턴테이블 논스톱 촬영 실행 [속도] " + TurnTableSpeed);
		try
		{
			TurnTableFolderWnd turnTableFolderWnd = new TurnTableFolderWnd();
			turnTableFolderWnd.Owner = HostWindow;
			turnTableFolderWnd.folderInform.PreFixFolder = "NonStop";
			turnTableFolderWnd.folderInform.Level = (Camera_Level + 1).ToString("0");
			turnTableFolderWnd.folderInform.ShotAmount = maxShots.ToString("00");
			turnTableFolderWnd.init();
			bool IsOk = turnTableFolderWnd.ShowDialog().Value;
			PreFixFolder = turnTableFolderWnd.folderInform.PreFixFolder;
			FolderPath = ServiceProvider.Settings.DefaultSession.Folder + "\\" + turnTableFolderWnd.folderInform.FullFolder;
			if (IsOk)
			{
				PreFixImage = turnTableFolderWnd.folderInform.PreFixImage;
				FullFolder = turnTableFolderWnd.folderInform.FullFolder;
				Thread exe = new Thread(() =>
				{
					ThreadTurnTableNonStopStart();
				});
				exe.Start();
			}
			else
			{
				bIsBtnExcute = false;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error TurnTable Folder Dialog", ex);
			MessageBox.Show(ex.ToString());
		}
	}

	private void ThreadTurnTableNonStopStart()
	{
		int MAX_shot = 0;
		switch (CaptureCount_SelectedIndex)
		{
		case 0:
			MAX_shot = 2;
			break;
		case 1:
			MAX_shot = 4;
			break;
		case 2:
			MAX_shot = 8;
			break;
		case 3:
			MAX_shot = 12;
			break;
		case 4:
			MAX_shot = 18;
			break;
		case 5:
			MAX_shot = 24;
			break;
		case 6:
			MAX_shot = 36;
			break;
		case 7:
			MAX_shot = 48;
			break;
		case 8:
			MAX_shot = 60;
			break;
		case 9:
			MAX_shot = 72;
			break;
		}
		GetTimeInterval(TurnTableSpeed, MAX_shot, out var interval);
		if (interval < 2500)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("⚠\ufe0f 턴테이블 속도 대비 촬영 빈도가 많아 촬영할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
			bIsBtnExcute = false;
			return;
		}
		CaptureContinuousMove_Start(MAX_shot, interval);
		if (ProcessCentering)
		{
			ProcessCenteringAndMagnification();
		}
	}

	private void GetTimeInterval(int setSpeed, int maxShot, out int interval)
	{
		int oneCycleTime = 0;
		switch (setSpeed)
		{
		case 0:
			oneCycleTime = ONECYCLE_TIME_HIGH;
			break;
		case 1:
			oneCycleTime = ONECYCLE_TIME_MID;
			break;
		case 2:
			oneCycleTime = ONECYCLE_TIME_LOW;
			break;
		}
		interval = oneCycleTime / maxShot;
	}

	private void GetAccInform(int speed, out int accdist, out int acctime)
	{
		string velocity = "";
		switch (speed)
		{
		case 0:
			velocity = TURNTABLE_SPEED_HIGH;
			break;
		case 1:
			velocity = TURNTABLE_SPEED_MID;
			break;
		case 2:
			velocity = TURNTABLE_SPEED_LOW;
			break;
		}
		double time = double.Parse(velocity) / double.Parse(TURNTABLE_ACCEL);
		acctime = (int)(time * 1000.0);
		accdist = (int)(double.Parse(TURNTABLE_ACCEL) * (time * time) / 2.0);
	}

	private void CaptureContinuousMove_Start(int max_shot, int interval)
	{
		try
		{
			WaitProgressBar = true;
			bIsMoveStop = false;
			PouseRun = false;
			bErrorTimeout = false;
			TurnTableCount = 0;
			ServiceProvider.Settings.TurnTableShot = true;
			CameraHelper.StopLiveAll();
			LaserOff();
			GetAccInform(TurnTableSpeed, out var accDist, out var accTime);
			accDist *= 2;
			interval -= accTime;
			Log.Debug("턴테이블 논스톱 촬영 주기 : " + interval + "msec");
			Log.Debug("가속  시간 : " + accTime + "msec");
			Log.Debug("가감속거리 : " + accDist + "pulse");
			SetVelocity(sAxis, "2000");
			SetAcceleration(sAxis, "10000");
			MoveRel(sAxis, "-" + accDist);
			SetTurnTableSpeed(TurnTableSpeed);
			Thread.Sleep(200);
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			long startTime = stopwatch.ElapsedMilliseconds;
			int targetpos = (int)(UNIT_ANGLE_TURNTABLE * 360.0) + accDist;
			TargetPos = targetpos.ToString();
			TargetPosDisp = (double.Parse(TargetPos) / UNIT_ANGLE_TURNTABLE).ToString("F1") + "도";
			SetHome(sAxis);
			MoveAbs(sAxis, targetpos.ToString(), wait: false);
			Thread.Sleep(accTime);
			CaptureStart();
			TurnTableCount++;
			while (true)
			{
				long nowTime = stopwatch.ElapsedMilliseconds;
				long diffTime = nowTime - startTime;
				if (diffTime >= interval)
				{
					startTime = stopwatch.ElapsedMilliseconds;
					Log.Debug("인터벌 진입 시간 : " + diffTime + "msec");
					if (!CaptureWait())
					{
						break;
					}
					CaptureStart();
					TurnTableCount++;
				}
				GetPos(sAxis);
				if (!bIsMoveStop && TurnTableCount < max_shot)
				{
					Thread.Sleep(10);
					continue;
				}
				if (bIsMoveStop)
				{
					TurnTableGotoHome();
					MoveRel(sAxis, accDist.ToString());
				}
				else
				{
					WaitDone(targetpos.ToString());
				}
				CaptureWait();
				stopwatch.Stop();
				long elapsed = stopwatch.ElapsedMilliseconds - accTime * 2;
				Console.WriteLine("360도 시간 : " + elapsed + "msec");
				string speed = "";
				if (TurnTableSpeed == 0)
				{
					speed = "[HIGH SPEED] ";
				}
				if (TurnTableSpeed == 1)
				{
					speed = "[MID  SPEED] ";
				}
				if (TurnTableSpeed == 2)
				{
					speed = "[LOW  SPEED] ";
				}
				WriteMemo("D:\\Clorium Data\\Clorium_MDPMemo.txt", speed + elapsed + "(msec)");
				break;
			}
			if (bErrorTimeout)
			{
				TurnTableGotoHome();
				Application.Current.Dispatcher.Invoke(() =>
				{
					Log.Error("논스톱 : 이미지 다운로드 타임 아웃...");
					MessageBox.Show("⚠\ufe0f 이미지 다운로드 타임 아웃...", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				});
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception e = ex2;
			TurnTableGotoHome();
			Application.Current.Dispatcher.Invoke(() =>
			{
				Log.Error("논스톱 : 에러 발생", e);
				MessageBox.Show(e.ToString(), "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
			});
		}
		finally
		{
			WaitProgressBar = false;
			bIsBtnExcute = false;
			ServiceProvider.Settings.TurnTableShot = false;
			ServiceProvider.DeviceManager.SelectedCameraDevice.StartLiveView();
		}
	}

	private void CaptureStart()
	{
		string filename = PreFixImage + "-L" + (Camera_Level + 1).ToString("0") + "-" + (TurnTableCount + 1).ToString("00");
		ServiceProvider.Settings.TurnTableShot_FileName = filename;
		ServiceProvider.Settings.TurnTableShot_PathName = FullFolder;
		if (MultiCapture)
		{
			CameraHelper.CaptureNoAfAll(0);
		}
		else
		{
			ServiceProvider.DeviceManager.SelectedCameraDevice.CapturePhotoNoAf();
		}
	}

	private bool CaptureWait()
	{
		if (MultiCapture)
		{
			bErrorTimeout = CameraHelper.WaitForCameraAll(5000);
		}
		else
		{
			int timer = 0;
			while (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
			{
				if (timer++ > 200)
				{
					bErrorTimeout = true;
					break;
				}
				Thread.Sleep(50);
			}
		}
		if (bErrorTimeout)
		{
			return false;
		}
		return true;
	}

	private void ProcessCenteringAndMagnification()
	{
		Log.Debug("TurnTable Image Centering Click");
		Process[] myProc = Process.GetProcessesByName("Clorium Centering");
		if (myProc.Length >= 1)
		{
			return;
		}
		FullFolder = "F:\\1. Code Source\\2. Clorium - ImageCentering\\Sample Images";
		if (FullFolder == "")
		{
			return;
		}
		string targetFolderPath = FullFolder;
		string processname = "C:\\Program Files (x86)\\Clorium\\Clorium Centering.exe";
		if (!File.Exists(processname))
		{
			MessageBox.Show("실행 파일을 찾을 수 없습니다:\n" + processname);
			return;
		}
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = processname,
			Arguments = $"\"{targetFolderPath}\" {ProcessCentering}",
			UseShellExecute = true,
			WorkingDirectory = Path.GetDirectoryName(processname)
		};
		try
		{
			Process.Start(startInfo);
		}
		catch (Exception ex)
		{
			Log.Error("프로세스 실행 실패: " + ex.Message);
		}
	}

	public void RecordMovie_Start()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		if (CheckTurnTableStatus() && !bIsBtnExcute)
		{
			Log.Debug("턴테이블 동영상 촬영");
			bIsBtnExcute = true;
			Thread exe = new Thread(() =>
			{
				ThreadRecordMovie_Start();
			});
			exe.Start();
		}
	}

	private void ThreadRecordMovie_Start()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		bIsMoveStop = false;
		Record_Running = true;
		FolderPath = ServiceProvider.Settings.DefaultSession.Folder + "\\";
		int backwardStep = -(int)(UNIT_ANGLE_TURNTABLE * 15.0);
		int targetPosition = (int)(UNIT_ANGLE_TURNTABLE * 360.0) + backwardStep;
		SetTurnTableSpeed(0);
		MoveRel(sAxis, backwardStep.ToString());
		Thread.Sleep(500);
		RecordMovie_Turntable();
		MoveRel(sAxis, targetPosition.ToString());
		Record_Running = false;
		bIsBtnExcute = false;
	}

	public void RecordMovie_Stop()
	{
		StopRecordMovie_Turntable();
		bIsBtnExcute = false;
	}

	public void CameraFocus()
	{
		try
		{
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
			{
				ServiceProvider.DeviceManager.SelectedCameraDevice.AutoFocus();
			}
		}
		catch (Exception ex)
		{
			Log.Error("Unable to Focus", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	public void CameraCaptureNoAf()
	{
		try
		{
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
			{
				ServiceProvider.DeviceManager.SelectedCameraDevice.CapturePhotoNoAf();
			}
		}
		catch (Exception ex)
		{
			Log.Error("Unable to Capture NoAf", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	public override void GetLiveImage()
	{
		lock (_locker)
		{
			try
			{
				LiveViewData = LiveViewManager.GetLiveViewImage(CameraDevice);
			}
			catch (Exception)
			{
				return;
			}
			if (LiveViewData == null || LiveViewData.ImageData == null)
			{
				return;
			}
			switch (getLiveViewRotation())
			{
			case 0:
				Rotation = 0;
				break;
			case 1:
				Rotation = 90;
				break;
			case 2:
				Rotation = 180;
				break;
			case 3:
				Rotation = 270;
				break;
			case 4:
				Rotation = LiveViewData.Rotation;
				break;
			}
			MemoryStream stream = new MemoryStream(LiveViewData.ImageData, LiveViewData.ImageDataPosition, LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition);
			using Bitmap bmp = new Bitmap(stream);
			Bitmap res = bmp;
			WriteableBitmap _bitmap = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(res));
			_bitmap.Freeze();
			Bitmap = _bitmap;
		}
	}

	private bool CheckTurnTableStatus()
	{
		if (!arduino.bIsConnected)
		{
			MessageBox.Show("\ud83d\udea8 턴테이블 연결 확인하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
		{
			MessageBoxResult result = MessageBox.Show("\ud83d\udea8 연결된 카메라가 없습니다. 계속하시겠습니까?", "알림", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
			if (result == MessageBoxResult.Yes)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public void ReadMDP(string file)
	{
		if (File.Exists(file))
		{
			try
			{
				IniFile ini = new IniFile();
				ini.Load(file);
				RESOLUTION_MOTOR = Convert.ToInt32(ini["MDP Data"]["Motor_Resolution"].ToString());
				GEARRATIO_MOTOR = Convert.ToInt32(ini["MDP Data"]["Motor_GearRatio"].ToString());
				DrivingMethod = Convert.ToBoolean(ini["MDP Data"]["Driving_Method"].ToString());
				GEARCOUNT_PULLEY = Convert.ToInt32(ini["MDP Data"]["Pulley_GearCount"].ToString());
				GEARCOUNT_TURNTABLE = Convert.ToInt32(ini["MDP Data"]["TurnTable_GearCount"].ToString());
				RADIUS_PULLEY = Convert.ToDouble(ini["MDP Data"]["Pulley_Radius"].ToString());
				RADIUS_TURNTABLE = Convert.ToDouble(ini["MDP Data"]["TurnTable_Radius"].ToString());
				TURNTABLE_SPEED_LOW = ini["MDP Data"]["Turntable_Speed_Low"].ToString().Trim();
				TURNTABLE_SPEED_MID = ini["MDP Data"]["Turntable_Speed_Mid"].ToString().Trim();
				TURNTABLE_SPEED_HIGH = ini["MDP Data"]["Turntable_Speed_High"].ToString().Trim();
				TURNTABLE_ACCEL = ini["MDP Data"]["TurnTable_Acceleration"].ToString().Trim();
				ONECYCLE_TIME_HIGH = Convert.ToInt32(ini["MDP Data"]["HighSpeed_CycleTime"].ToString());
				ONECYCLE_TIME_MID = Convert.ToInt32(ini["MDP Data"]["MidSpeed_CycleTime"].ToString());
				ONECYCLE_TIME_LOW = Convert.ToInt32(ini["MDP Data"]["LowSpeed_CycleTime"].ToString());
				TurnTableSpeed = Convert.ToInt32(ini["MDP Data"]["TurnTable_Speed"].ToString());
				ShowNonStopButton = Convert.ToBoolean(ini["MDP Data"]["TurnTable_ShowNonStopButton"].ToString());
				MultiCapture = Convert.ToBoolean(ini["MDP Data"]["TurnTable_MultiCapture"].ToString());
				if (DrivingMethod)
				{
					UNIT_CYCLE_TURNTABLE = RESOLUTION_MOTOR * GEARRATIO_MOTOR / GEARCOUNT_PULLEY * GEARCOUNT_TURNTABLE;
				}
				else
				{
					UNIT_CYCLE_TURNTABLE = (int)((double)(RESOLUTION_MOTOR * GEARRATIO_MOTOR) * (RADIUS_TURNTABLE / RADIUS_PULLEY));
				}
				UNIT_ANGLE_TURNTABLE = (double)UNIT_CYCLE_TURNTABLE / 360.0;
				return;
			}
			catch (Exception)
			{
				MessageBox.Show("\ud83d\udea8 Clorium_MDPData.txt 파일의 값을 확인하세요.", "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
		}
		WriteMDP(file);
		MessageBox.Show("\ud83d\udea8 MDP 파일이 없어 Default 값을 로드했습니다.\n\n\ud83d\udea8 Clorium_MDPData.txt에서 수정하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	private void WriteMDP(string file)
	{
		IniFile ini = new IniFile();
		RESOLUTION_MOTOR = 3200;
		GEARRATIO_MOTOR = 10;
		GEARCOUNT_PULLEY = 16;
		GEARCOUNT_TURNTABLE = 55;
		RADIUS_PULLEY = 10.0;
		RADIUS_TURNTABLE = 500.0;
		TURNTABLE_SPEED_LOW = "3000";
		TURNTABLE_SPEED_MID = "6000";
		TURNTABLE_SPEED_HIGH = "9000";
		TURNTABLE_ACCEL = "20000";
		TurnTableSpeed = 0;
		ShowNonStopButton = true;
		MultiCapture = true;
		ONECYCLE_TIME_LOW = 40000;
		ONECYCLE_TIME_MID = 26000;
		ONECYCLE_TIME_HIGH = 13000;
		if (DrivingMethod)
		{
			UNIT_CYCLE_TURNTABLE = RESOLUTION_MOTOR * GEARRATIO_MOTOR / GEARCOUNT_PULLEY * GEARCOUNT_TURNTABLE;
		}
		else
		{
			UNIT_CYCLE_TURNTABLE = (int)((double)(RESOLUTION_MOTOR * GEARRATIO_MOTOR) * (RADIUS_TURNTABLE / RADIUS_PULLEY));
		}
		UNIT_ANGLE_TURNTABLE = (double)UNIT_CYCLE_TURNTABLE / 360.0;
		ini["MDP Data"]["Motor_Resolution"] = RESOLUTION_MOTOR.ToString();
		ini["MDP Data"]["Motor_GearRatio"] = GEARRATIO_MOTOR.ToString();
		ini["MDP Data"]["Driving_Method"] = DrivingMethod.ToString();
		ini["MDP Data"]["Pulley_GearCount"] = GEARCOUNT_PULLEY.ToString();
		ini["MDP Data"]["TurnTable_GearCount"] = GEARCOUNT_TURNTABLE.ToString();
		ini["MDP Data"]["Pulley_Radius"] = RADIUS_PULLEY.ToString();
		ini["MDP Data"]["TurnTable_Radius"] = RADIUS_TURNTABLE.ToString();
		ini["MDP Data"]["Turntable_Speed_Low"] = TURNTABLE_SPEED_LOW;
		ini["MDP Data"]["Turntable_Speed_Mid"] = TURNTABLE_SPEED_MID;
		ini["MDP Data"]["Turntable_Speed_High"] = TURNTABLE_SPEED_HIGH;
		ini["MDP Data"]["TurnTable_Acceleration"] = TURNTABLE_ACCEL;
		ini["MDP Data"]["TurnTable_Speed"] = TurnTableSpeed.ToString();
		ini["MDP Data"]["TurnTable_ShowNonStopButton"] = ShowNonStopButton.ToString();
		ini["MDP Data"]["TurnTable_MultiCapture"] = MultiCapture.ToString();
		ini["MDP Data"]["LowSpeed_CycleTime"] = ONECYCLE_TIME_LOW.ToString();
		ini["MDP Data"]["MidSpeed_CycleTime"] = ONECYCLE_TIME_MID.ToString();
		ini["MDP Data"]["HighSpeed_CycleTime"] = ONECYCLE_TIME_HIGH.ToString();
		ini.Save(file);
	}

	private void WriteMemo(string file, string message)
	{
		System.IO.FileInfo fi = new System.IO.FileInfo(file);
		if (!fi.Exists)
		{
			fi.Create().Close();
		}
		try
		{
			using StreamWriter sw = File.AppendText(file);
			string temp = $"[{DateTime.Now}] {message}";
			sw.WriteLine(temp);
			sw.Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void SetSavetoCameraStream(SaveTypeEnum dest)
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
			if (dest == SaveTypeEnum.SaveTo_Host && !ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam)
			{
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = true;
			}
			if (dest == SaveTypeEnum.SaveTo_Camera)
			{
				property.NoDownload = true;
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
			}
			if (dest == SaveTypeEnum.SaveTo_Both)
			{
				property.NoDownload = false;
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
			}
			property.CaptureInSdRam = ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam;
		}
	}

	public void DownloadAllImagesFromCamera(ICameraDevice cameraDevice)
	{
		if (!cameraDevice.IsBusy)
		{
		}
	}
}
