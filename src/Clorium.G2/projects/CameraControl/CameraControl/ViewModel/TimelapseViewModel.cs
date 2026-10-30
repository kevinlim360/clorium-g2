using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Quartz;
using Quartz.Impl;

namespace CameraControl.ViewModel;

public class TimelapseViewModel : ViewModelBase
{
	private System.Timers.Timer _timer = new System.Timers.Timer(1000.0);

	private DateTime _lastTime;

	private bool _isRunning;

	private bool _isActive;

	private DateTime _lastCaptureTime = DateTime.Now;

	private DateTime _timeLapseStartTime = DateTime.Now;

	private DateTime _firstLapseStartTime = DateTime.Now;

	private bool _fullSpeed;

	private BracketingViewModel _bracketingViewModel = null;

	private ITrigger trigger;

	private IScheduler sched;

	public TimeLapseSettings TimeLapseSettings { get; set; }

	public bool StartNow
	{
		get
		{
			return TimeLapseSettings.StartNow;
		}
		set
		{
			TimeLapseSettings.StartNow = value;
			RaisePropertyChanged(() => StartNow);
			RaisePropertyChanged(() => DateVisibility);
			RaisePropertyChanged(() => TimeVisibility);
			RaisePropertyChanged(() => DayVisibility);
			RaisePropertyChanged(() => StopAtEnable);
		}
	}

	public bool StartAt
	{
		get
		{
			return TimeLapseSettings.StartAt;
		}
		set
		{
			TimeLapseSettings.StartAt = value;
			RaisePropertyChanged(() => StartAt);
			RaisePropertyChanged(() => DateVisibility);
			RaisePropertyChanged(() => TimeVisibility);
			RaisePropertyChanged(() => DayVisibility);
			RaisePropertyChanged(() => StopAtEnable);
		}
	}

	public bool StartIn
	{
		get
		{
			return TimeLapseSettings.StartIn;
		}
		set
		{
			TimeLapseSettings.StartIn = value;
			RaisePropertyChanged(() => DateVisibility);
			RaisePropertyChanged(() => TimeVisibility);
			RaisePropertyChanged(() => StartIn);
			RaisePropertyChanged(() => DayVisibility);
			RaisePropertyChanged(() => StopAtEnable);
		}
	}

	public bool StartDaily
	{
		get
		{
			return TimeLapseSettings.StartDaily;
		}
		set
		{
			TimeLapseSettings.StartDaily = value;
			StopAtPhotos = false;
			StopAt = false;
			StopIn = true;
			RaisePropertyChanged(() => StartDaily);
			RaisePropertyChanged(() => DateVisibility);
			RaisePropertyChanged(() => TimeVisibility);
			RaisePropertyChanged(() => DayVisibility);
			RaisePropertyChanged(() => StopAtEnable);
		}
	}

	public DateTime StartDate
	{
		get
		{
			return TimeLapseSettings.StartDate;
		}
		set
		{
			DateTime d = TimeLapseSettings.StartDate;
			TimeLapseSettings timeLapseSettings = TimeLapseSettings;
			DateTime startDate = (TimeLapseSettings.StartDate = new DateTime(value.Year, value.Month, value.Day, d.Hour, d.Minute, d.Second));
			timeLapseSettings.StartDate = startDate;
			RaisePropertyChanged(() => StartDate);
		}
	}

	public TimeSpan StartTime
	{
		get
		{
			return new TimeSpan(TimeLapseSettings.StartDate.Hour, TimeLapseSettings.StartDate.Minute, TimeLapseSettings.StartDate.Second);
		}
		set
		{
			DateTime d = TimeLapseSettings.StartDate;
			TimeLapseSettings.StartDate = new DateTime(d.Year, d.Month, d.Day, value.Hours, value.Minutes, value.Seconds);
			RaisePropertyChanged(() => StartTime);
		}
	}

	public int StartHour
	{
		get
		{
			return TimeLapseSettings.StartDate.Hour;
		}
		set
		{
			DateTime d = TimeLapseSettings.StartDate;
			TimeLapseSettings.StartDate = new DateTime(d.Year, d.Month, d.Day, value, d.Minute, d.Second);
			RaisePropertyChanged(() => StartHour);
		}
	}

	public int StartMinute
	{
		get
		{
			return TimeLapseSettings.StartDate.Minute;
		}
		set
		{
			DateTime d = TimeLapseSettings.StartDate;
			TimeLapseSettings.StartDate = new DateTime(d.Year, d.Month, d.Day, d.Hour, value, d.Second);
			RaisePropertyChanged(() => StartMinute);
		}
	}

	public int StartSecond
	{
		get
		{
			return TimeLapseSettings.StartDate.Second;
		}
		set
		{
			DateTime d = TimeLapseSettings.StartDate;
			TimeLapseSettings.StartDate = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, value);
			RaisePropertyChanged(() => StartSecond);
		}
	}

	public bool StartDay0
	{
		get
		{
			return TimeLapseSettings.StartDay0;
		}
		set
		{
			TimeLapseSettings.StartDay0 = value;
			RaisePropertyChanged(() => StartDay0);
		}
	}

	public bool StartDay1
	{
		get
		{
			return TimeLapseSettings.StartDay1;
		}
		set
		{
			TimeLapseSettings.StartDay1 = value;
			RaisePropertyChanged(() => StartDay1);
		}
	}

	public bool StartDay2
	{
		get
		{
			return TimeLapseSettings.StartDay2;
		}
		set
		{
			TimeLapseSettings.StartDay2 = value;
			RaisePropertyChanged(() => StartDay2);
		}
	}

	public bool StartDay3
	{
		get
		{
			return TimeLapseSettings.StartDay3;
		}
		set
		{
			TimeLapseSettings.StartDay3 = value;
			RaisePropertyChanged(() => StartDay3);
		}
	}

	public bool StartDay4
	{
		get
		{
			return TimeLapseSettings.StartDay4;
		}
		set
		{
			TimeLapseSettings.StartDay4 = value;
			RaisePropertyChanged(() => StartDay4);
		}
	}

	public bool StartDay5
	{
		get
		{
			return TimeLapseSettings.StartDay5;
		}
		set
		{
			TimeLapseSettings.StartDay5 = value;
			RaisePropertyChanged(() => StartDay5);
		}
	}

	public bool StartDay6
	{
		get
		{
			return TimeLapseSettings.StartDay6;
		}
		set
		{
			TimeLapseSettings.StartDay6 = value;
			RaisePropertyChanged(() => StartDay6);
		}
	}

	public bool StopAtPhotos
	{
		get
		{
			return TimeLapseSettings.StopAtPhotos;
		}
		set
		{
			TimeLapseSettings.StopAtPhotos = value;
			RaisePropertyChanged(() => StopAtPhotos);
			RaisePropertyChanged(() => StopDateVisibility);
			RaisePropertyChanged(() => StopTimeVisibility);
			RaisePropertyChanged(() => StopCountVisibility);
		}
	}

	public bool StopIn
	{
		get
		{
			return TimeLapseSettings.StopIn;
		}
		set
		{
			TimeLapseSettings.StopIn = value;
			RaisePropertyChanged(() => StopIn);
			RaisePropertyChanged(() => StopDateVisibility);
			RaisePropertyChanged(() => StopTimeVisibility);
			RaisePropertyChanged(() => StopCountVisibility);
		}
	}

	public bool StopAt
	{
		get
		{
			return TimeLapseSettings.StopAt;
		}
		set
		{
			TimeLapseSettings.StopAt = value;
			RaisePropertyChanged(() => StopAt);
			RaisePropertyChanged(() => StopDateVisibility);
			RaisePropertyChanged(() => StopTimeVisibility);
			RaisePropertyChanged(() => StopCountVisibility);
		}
	}

	public int StopCaptureCount
	{
		get
		{
			return TimeLapseSettings.StopCaptureCount;
		}
		set
		{
			TimeLapseSettings.StopCaptureCount = value;
			RaisePropertyChanged(() => StopCaptureCount);
		}
	}

	public bool StopAtEnable => !StartDaily;

	public DateTime StopDate
	{
		get
		{
			return TimeLapseSettings.StopDate;
		}
		set
		{
			DateTime d = TimeLapseSettings.StopDate;
			TimeLapseSettings timeLapseSettings = TimeLapseSettings;
			DateTime stopDate = (TimeLapseSettings.StopDate = new DateTime(value.Year, value.Month, value.Day, d.Hour, d.Minute, d.Second));
			timeLapseSettings.StopDate = stopDate;
			RaisePropertyChanged(() => StopDate);
		}
	}

	public TimeSpan StopTime
	{
		get
		{
			return new TimeSpan(TimeLapseSettings.StopDate.Hour, TimeLapseSettings.StopDate.Minute, TimeLapseSettings.StopDate.Second);
		}
		set
		{
			DateTime d = TimeLapseSettings.StopDate;
			TimeLapseSettings.StopDate = new DateTime(d.Year, d.Month, d.Day, value.Hours, value.Minutes, value.Seconds);
			RaisePropertyChanged(() => StopTime);
		}
	}

	public int StopHour
	{
		get
		{
			return TimeLapseSettings.StopDate.Hour;
		}
		set
		{
			DateTime d = TimeLapseSettings.StopDate;
			TimeLapseSettings.StopDate = new DateTime(d.Year, d.Month, d.Day, value, d.Minute, d.Second);
			RaisePropertyChanged(() => StopHour);
		}
	}

	public int StopMinute
	{
		get
		{
			return TimeLapseSettings.StopDate.Minute;
		}
		set
		{
			DateTime d = TimeLapseSettings.StopDate;
			TimeLapseSettings.StopDate = new DateTime(d.Year, d.Month, d.Day, d.Hour, value, d.Second);
			RaisePropertyChanged(() => StopMinute);
		}
	}

	public int StopSecond
	{
		get
		{
			return TimeLapseSettings.StopDate.Second;
		}
		set
		{
			DateTime d = TimeLapseSettings.StopDate;
			TimeLapseSettings.StopDate = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, value);
			RaisePropertyChanged(() => StopSecond);
		}
	}

	public int TimeBetweenShots
	{
		get
		{
			return TimeLapseSettings.TimeBetweenShots;
		}
		set
		{
			TimeLapseSettings.TimeBetweenShots = value;
			RaisePropertyChanged(() => TimeBetweenShots);
		}
	}

	public bool Capture
	{
		get
		{
			return TimeLapseSettings.Capture;
		}
		set
		{
			TimeLapseSettings.Capture = value;
			RaisePropertyChanged(() => Capture);
			RaisePropertyChanged(() => ScriptVisibility);
			RaisePropertyChanged(() => FullSpeed);
			RaisePropertyChanged(() => NotFullSpeed);
		}
	}

	public bool CaptureAll
	{
		get
		{
			return TimeLapseSettings.CaptureAll;
		}
		set
		{
			TimeLapseSettings.CaptureAll = value;
			RaisePropertyChanged(() => CaptureAll);
			RaisePropertyChanged(() => ScriptVisibility);
			RaisePropertyChanged(() => FullSpeed);
			RaisePropertyChanged(() => NotFullSpeed);
		}
	}

	public bool CaptureScript
	{
		get
		{
			return TimeLapseSettings.CaptureScript;
		}
		set
		{
			TimeLapseSettings.CaptureScript = value;
			RaisePropertyChanged(() => CaptureScript);
			RaisePropertyChanged(() => ScriptVisibility);
			RaisePropertyChanged(() => FullSpeed);
			RaisePropertyChanged(() => NotFullSpeed);
		}
	}

	public bool Bracketing
	{
		get
		{
			return TimeLapseSettings.Bracketing;
		}
		set
		{
			TimeLapseSettings.Bracketing = value;
		}
	}

	public string ScriptFile
	{
		get
		{
			return TimeLapseSettings.ScriptFile;
		}
		set
		{
			TimeLapseSettings.ScriptFile = value;
			RaisePropertyChanged(() => ScriptFile);
		}
	}

	public Visibility DateVisibility => (!StartAt) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility TimeVisibility => (!StartIn && !StartAt && !StartDaily) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility DayVisibility => (!StartDaily) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility StopDateVisibility => (!StopAt) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility StopTimeVisibility => (!StopIn && !StopAt) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility StopCountVisibility => (!StopAtPhotos) ? Visibility.Collapsed : Visibility.Visible;

	public Visibility ScriptVisibility => (!CaptureScript) ? Visibility.Collapsed : Visibility.Visible;

	public bool IsFree => !_isRunning;

	public bool FullSpeed
	{
		get
		{
			return _fullSpeed && Capture;
		}
		set
		{
			_fullSpeed = value;
			RaisePropertyChanged(() => FullSpeed);
			RaisePropertyChanged(() => NotFullSpeed);
		}
	}

	public bool NotFullSpeed => !FullSpeed;

	public bool IsRunning
	{
		get
		{
			return _isRunning;
		}
		set
		{
			_isRunning = value;
			RaisePropertyChanged(() => IsRunning);
			RaisePropertyChanged(() => StartText);
			RaisePropertyChanged(() => StatusText);
			RaisePropertyChanged(() => IsFree);
		}
	}

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			_isActive = value;
			RaisePropertyChanged(() => IsActive);
			RaisePropertyChanged(() => StatusText);
		}
	}

	public string StartText => (!IsRunning) ? TranslationStrings.ButtonStartTimeLapse : TranslationStrings.ButtonStopTimeLapse;

	public string StatusText
	{
		get
		{
			if (trigger != null)
			{
				DateTimeOffset? nextFireTimeUtc = sched.GetTrigger(trigger.Key)?.Result?.GetNextFireTimeUtc();
				if (nextFireTimeUtc.HasValue)
				{
					return "Next capture time " + nextFireTimeUtc.Value.ToLocalTime().ToString();
				}
			}
			return "????";
		}
	}

	public RelayCommand StartCommand { get; set; }

	public RelayCommand BrowsCommand { get; set; }

	public TimelapseViewModel()
	{
		StartCommand = new RelayCommand(Start);
		BrowsCommand = new RelayCommand(Browse);
		if (IsInDesignMode)
		{
			TimeLapseSettings = new TimeLapseSettings();
			TimeLapseSettings.StartNow = false;
			TimeLapseSettings.StartDaily = true;
		}
		else
		{
			TimeLapseSettings = ServiceProvider.Settings.DefaultSession.TimeLapseSettings;
			ServiceProvider.WindowsManager.Event += WindowsManager_Event;
		}
		_timer.AutoReset = true;
		_timer.Elapsed += _timer_Elapsed;
	}

	private void WindowsManager_Event(string cmd, object o)
	{
		switch (cmd)
		{
		case "TimeLapse_Start":
			StartL();
			break;
		case "TimeLapse_Stop":
			StopL();
			break;
		case "All_Close":
			sched?.Shutdown();
			break;
		}
	}

	public void Browse()
	{
		Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
		dlg.Filter = "Tcl Script file(*.tcl)|*.tcl|Script file(*.dccscript)|*.dccscript|All files|*.*";
		if (dlg.ShowDialog() == true)
		{
			ScriptFile = dlg.FileName;
		}
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		Task<ITrigger> tempTrigger = sched.GetTrigger(trigger.Key);
		if (tempTrigger == null)
		{
			StopL();
		}
		RaisePropertyChanged(() => StatusText);
	}

	public void Start()
	{
		try
		{
			if (!IsRunning)
			{
				StartL();
			}
			else
			{
				StopL();
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error starting timelapse", ex);
			System.Windows.Forms.MessageBox.Show("Error starting timelapse " + ex.Message);
		}
	}

	private void StartL()
	{
		if (IsRunning)
		{
			return;
		}
		try
		{
			ISchedulerFactory schedFact = new StdSchedulerFactory();
			if (sched == null)
			{
				sched = schedFact.GetScheduler().Result;
			}
			IJobDetail job = JobBuilder.Create<SimpleJob>().WithIdentity("job1", "group1").Build();
			TriggerBuilder triggerB = TriggerBuilder.Create().WithIdentity("trigger1", "group1");
			if (StartIn)
			{
				triggerB = triggerB.StartAt(DateBuilder.FutureDate(StartHour * 60 * 60 + StartMinute * 60 + StartSecond, IntervalUnit.Second));
			}
			if (StartAt)
			{
				triggerB = triggerB.StartAt(DateBuilder.DateOf(StartHour, StartMinute, StartSecond, StartDate.Day, StartDate.Month, StartDate.Year));
			}
			if (StartDaily)
			{
				ISet<DayOfWeek> days = new HashSet<DayOfWeek>();
				if (StartDay0)
				{
					days.Add(DayOfWeek.Sunday);
				}
				if (StartDay1)
				{
					days.Add(DayOfWeek.Monday);
				}
				if (StartDay2)
				{
					days.Add(DayOfWeek.Tuesday);
				}
				if (StartDay3)
				{
					days.Add(DayOfWeek.Wednesday);
				}
				if (StartDay4)
				{
					days.Add(DayOfWeek.Thursday);
				}
				if (StartDay5)
				{
					days.Add(DayOfWeek.Friday);
				}
				if (StartDay6)
				{
					days.Add(DayOfWeek.Saturday);
				}
				triggerB = triggerB.WithDailyTimeIntervalSchedule((DailyTimeIntervalScheduleBuilder x) =>
				{
					x.WithIntervalInSeconds(TimeBetweenShots).WithMisfireHandlingInstructionFireAndProceed().StartingDailyAt(new TimeOfDay(StartHour, StartMinute, StartSecond))
						.EndingDailyAt(new TimeOfDay(StopHour, StopMinute, StopSecond))
						.OnDaysOfTheWeek(new ReadOnlyCollection<DayOfWeek>(days.ToList()));
				});
			}
			else
			{
				if (StopAtPhotos)
				{
					triggerB = triggerB.WithSimpleSchedule((SimpleScheduleBuilder x) =>
					{
						x.WithIntervalInSeconds(TimeBetweenShots).WithRepeatCount(StopCaptureCount).WithMisfireHandlingInstructionNowWithExistingCount();
					});
				}
				else
				{
					triggerB = ((!StopIn) ? triggerB.WithSimpleSchedule((SimpleScheduleBuilder x) =>
					{
						x.WithIntervalInSeconds(TimeBetweenShots).WithMisfireHandlingInstructionNowWithExistingCount().RepeatForever();
					}) : triggerB.WithSimpleSchedule((SimpleScheduleBuilder x) =>
					{
						x.WithIntervalInSeconds(TimeBetweenShots).WithMisfireHandlingInstructionNowWithExistingCount().WithRepeatCount((StopHour * 60 * 60 + StopMinute * 60 + StopSecond) / TimeBetweenShots);
					}));
				}
				if (StopAt)
				{
					triggerB = triggerB.EndAt(DateBuilder.DateOf(StopHour, StopMinute, StopSecond, StopDate.Day, StopDate.Month, StopDate.Year));
				}
			}
			trigger = triggerB.Build();
			sched.ScheduleJob(job, trigger);
			sched.Start();
			if (_bracketingViewModel != null)
			{
				_bracketingViewModel.Stop();
			}
			_bracketingViewModel = null;
			IsRunning = true;
			_timeLapseStartTime = DateTime.Now;
			_lastCaptureTime = DateTime.Now;
			Log.Debug("Timelapse start");
			_timer.Interval = 1000.0;
			_timer.Start();
			_lastTime = DateTime.Now;
			TimeLapseSettings.Started = true;
			ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show("Unable to start timelapse " + ex.Message);
			Log.Debug("Unable to start timelapse ", ex);
		}
	}

	private void StopL()
	{
		if (IsRunning)
		{
			sched.Standby();
			sched.Clear();
			Log.Debug("Timelapse stop");
			IsActive = false;
			IsRunning = false;
			_timer.Stop();
			TimeLapseSettings.Started = false;
			ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
		}
	}
}
