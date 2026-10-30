using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Scripting;
using CameraControl.Core.TclScripting;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.Win32;

namespace CameraControl.windows;

public partial class ScriptWnd : Window, IWindow, IToolPlugin, IComponentConnector
{
	private readonly TclScriptManager _manager = new TclScriptManager();

	private CompletionWindow completionWindow;

	public string Id => "{04F1DD8E-3E4E-497D-80A9-125ABC76DA7E}";

	public string ScriptFileName { get; set; }

	public void Init()
	{
	}

	public ScriptWnd()
	{
		InitializeComponent();
		textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
		textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
		NewScript();
	}

	private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
	{
		if (e.Text == "<")
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
			IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
			foreach (IScriptCommand command in ServiceProvider.ScriptManager.AvaiableCommands)
			{
				data.Add(new MyCompletionData(command.DefaultValue, command.Description, command.Name.ToLower()));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (e.Text == ".")
		{
			string word = textEditor.GetWordBeforeDot();
			if (word == "{session" || word == "session")
			{
				IList<PropertyInfo> props = new List<PropertyInfo>(typeof(PhotoSession).GetProperties());
				completionWindow = new CompletionWindow(textEditor.TextArea);
				completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
				IList<ICompletionData> data2 = completionWindow.CompletionList.CompletionData;
				foreach (PropertyInfo prop in props)
				{
					if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool))
					{
						data2.Add(new MyCompletionData(prop.Name.ToLower(), "", prop.Name.ToLower()));
					}
				}
				completionWindow.Show();
				completionWindow.Closed += delegate
				{
					completionWindow = null;
				};
			}
			if (word == "{camera" && ServiceProvider.DeviceManager.SelectedCameraDevice != null)
			{
				completionWindow = new CompletionWindow(textEditor.TextArea);
				completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
				IList<ICompletionData> data3 = completionWindow.CompletionList.CompletionData;
				CameraPreset preset = new CameraPreset();
				preset.Get(ServiceProvider.DeviceManager.SelectedCameraDevice);
				foreach (ValuePair value in preset.Values)
				{
					data3.Add(new MyCompletionData(value.Name.Replace(" ", "").ToLower(), "Current value :" + value.Value, value.Name.Replace(" ", "").ToLower()));
				}
				completionWindow.Show();
				completionWindow.Closed += delegate
				{
					completionWindow = null;
				};
			}
		}
		if (e.Text == " ")
		{
			string line = textEditor.GetLine();
			if (line.StartsWith("setcamera"))
			{
				if (!line.Contains("property") && !line.Contains("value"))
				{
					completionWindow = new CompletionWindow(textEditor.TextArea);
					completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
					IList<ICompletionData> data4 = completionWindow.CompletionList.CompletionData;
					data4.Add(new MyCompletionData("property", "", "property"));
					completionWindow.Show();
					completionWindow.Closed += delegate
					{
						completionWindow = null;
					};
				}
				if (line.Contains("property") && !line.Contains("value"))
				{
					completionWindow = new CompletionWindow(textEditor.TextArea);
					completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
					IList<ICompletionData> data5 = completionWindow.CompletionList.CompletionData;
					data5.Add(new MyCompletionData("value", "", "value"));
					completionWindow.Show();
					completionWindow.Closed += delegate
					{
						completionWindow = null;
					};
				}
			}
		}
		if (!(e.Text == "=") || ServiceProvider.DeviceManager.SelectedCameraDevice == null)
		{
			return;
		}
		string line2 = textEditor.GetLine();
		string word2 = textEditor.GetWordBeforeDot();
		if (!line2.StartsWith("setcamera"))
		{
			return;
		}
		if (word2 == "property")
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
			IList<ICompletionData> data6 = completionWindow.CompletionList.CompletionData;
			data6.Add(new MyCompletionData("\"aperture\"", "", "aperture"));
			data6.Add(new MyCompletionData("\"iso\"", "", "iso"));
			data6.Add(new MyCompletionData("\"shutter\"", "", "shutter"));
			data6.Add(new MyCompletionData("\"ec\"", "Exposure Compensation", "ec"));
			data6.Add(new MyCompletionData("\"wb\"", "White Balance", "wb"));
			data6.Add(new MyCompletionData("\"cs\"", "Compression Setting", "cs"));
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (!(word2 == "value"))
		{
			return;
		}
		if (line2.Contains("property=\"aperture\"") && ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber != null)
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			completionWindow.CompletionList.ListBox.Foreground = new SolidColorBrush(Colors.Black);
			IList<ICompletionData> data7 = completionWindow.CompletionList.CompletionData;
			foreach (string value2 in ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber.Values)
			{
				data7.Add(new MyCompletionData("\"" + value2 + "\"", value2, value2));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (line2.Contains("property=\"iso\"") && ServiceProvider.DeviceManager.SelectedCameraDevice.IsoNumber != null)
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			IList<ICompletionData> data8 = completionWindow.CompletionList.CompletionData;
			foreach (string value3 in ServiceProvider.DeviceManager.SelectedCameraDevice.IsoNumber.Values)
			{
				data8.Add(new MyCompletionData("\"" + value3 + "\"", value3, value3));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (line2.Contains("property=\"shutter\"") && ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed != null)
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			IList<ICompletionData> data9 = completionWindow.CompletionList.CompletionData;
			foreach (string value4 in ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed.Values)
			{
				data9.Add(new MyCompletionData("\"" + value4 + "\"", value4, value4));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (line2.Contains("property=\"ec\"") && ServiceProvider.DeviceManager.SelectedCameraDevice.ExposureCompensation != null)
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			IList<ICompletionData> data10 = completionWindow.CompletionList.CompletionData;
			foreach (string value5 in ServiceProvider.DeviceManager.SelectedCameraDevice.ExposureCompensation.Values)
			{
				data10.Add(new MyCompletionData("\"" + value5 + "\"", value5, value5));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (line2.Contains("property=\"wb\"") && ServiceProvider.DeviceManager.SelectedCameraDevice.WhiteBalance != null)
		{
			completionWindow = new CompletionWindow(textEditor.TextArea);
			IList<ICompletionData> data11 = completionWindow.CompletionList.CompletionData;
			foreach (string value6 in ServiceProvider.DeviceManager.SelectedCameraDevice.WhiteBalance.Values)
			{
				data11.Add(new MyCompletionData("\"" + value6 + "\"", value6, value6));
			}
			completionWindow.Show();
			completionWindow.Closed += delegate
			{
				completionWindow = null;
			};
		}
		if (!line2.Contains("property=\"cs\"") || ServiceProvider.DeviceManager.SelectedCameraDevice.CompressionSetting == null)
		{
			return;
		}
		completionWindow = new CompletionWindow(textEditor.TextArea);
		IList<ICompletionData> data12 = completionWindow.CompletionList.CompletionData;
		foreach (string value7 in ServiceProvider.DeviceManager.SelectedCameraDevice.CompressionSetting.Values)
		{
			data12.Add(new MyCompletionData("\"" + value7 + "\"", value7, value7));
		}
		completionWindow.Show();
		completionWindow.Closed += delegate
		{
			completionWindow = null;
		};
	}

	private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length > 0 && completionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
		{
			completionWindow.CompletionList.RequestInsertion(e);
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "ScriptWnd_Show":
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				ServiceProvider.ScriptManager.OutPutMessageReceived += ScriptManager_OutPutMessageReceived;
				_manager.Output += manager_Output;
				Focus();
			});
			break;
		case "ScriptWnd_Hide":
			ServiceProvider.ScriptManager.OutPutMessageReceived -= ScriptManager_OutPutMessageReceived;
			_manager.Output -= manager_Output;
			Hide();
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				ServiceProvider.ScriptManager.OutPutMessageReceived -= ScriptManager_OutPutMessageReceived;
				Hide();
				Close();
			});
			break;
		}
	}

	private void ScriptManager_OutPutMessageReceived(object sender, MessageEventArgs e)
	{
		AddTclOutput(e.Message);
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("ScriptWnd_Hide");
		}
	}

	public bool Execute()
	{
		ServiceProvider.WindowsManager.ExecuteCommand("ScriptWnd_Show");
		return true;
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
		dlg.Filter = "Tcl Script file(*.tcl)|*.tcl|Script file(*.dccscript)|*.dccscript|All files|*.*";
		if (dlg.ShowDialog() == true)
		{
			try
			{
				ScriptFileName = dlg.FileName;
				LoadScriptFile();
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show("Error loading script file" + ex.Message);
				Log.Error("Error loading script file", ex);
			}
		}
	}

	public void LoadScriptFile()
	{
		if (Path.GetExtension(ScriptFileName) == ".tcl")
		{
			textEditorTcl.Load(ScriptFileName);
			TabControl.SelectedItem = TclTabItem;
		}
		else
		{
			textEditor.Load(ScriptFileName);
			TabControl.SelectedItem = XmTabItem;
		}
	}

	public void SaveScriptFile()
	{
		if (IsXmlActive())
		{
			textEditor.Save(ScriptFileName);
		}
		else
		{
			textEditorTcl.Save(ScriptFileName);
		}
	}

	private void mnu_save_as_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
		dlg.Filter = (IsXmlActive() ? "Script file(*.dccscript)|*.dccscript|All files|*.*" : "Tcl Script file(*.tcl)|*.tcl|All files|*.*");
		if (dlg.ShowDialog() == true)
		{
			try
			{
				ScriptFileName = dlg.FileName;
				SaveScriptFile();
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show("Error saving script file" + ex.Message);
				Log.Error("Error saving script file", ex);
			}
		}
	}

	private void mnu_save_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(ScriptFileName) || !File.Exists(ScriptFileName))
		{
			mnu_save_as_Click(null, null);
		}
		else
		{
			SaveScriptFile();
		}
	}

	private void mnu_verify_Click(object sender, RoutedEventArgs e)
	{
		if (IsXmlActive())
		{
			mnu_save_Click(null, null);
			ScriptObject scriptObject = null;
			try
			{
				lst_output.Items.Clear();
				scriptObject = ServiceProvider.ScriptManager.Load(ScriptFileName);
			}
			catch (Exception ex)
			{
				AddOutput("Loading error :" + ex.Message);
			}
			AddOutput(ServiceProvider.ScriptManager.Verify(scriptObject) ? "Verification done " : "Verification fail ");
		}
	}

	private void NewScript()
	{
		textEditor.Text = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<dccscript> \n   <commands>\n     \n   </commands>\n</dccscript>";
		ScriptFileName = null;
	}

	public void AddOutput(string msg)
	{
		Dispatcher.BeginInvoke((Action)(() =>
		{
			lst_output.Items.Add(msg);
			lst_output.ScrollIntoView(lst_output.Items[lst_output.Items.Count - 1]);
		}));
	}

	public void AddTclOutput(string msg)
	{
		Dispatcher.Invoke(() =>
		{
			lst_outputTcl.Items.Add(msg);
			lst_outputTcl.ScrollIntoView(lst_outputTcl.Items[lst_outputTcl.Items.Count - 1]);
		});
	}

	private void mnu_run_Click(object sender, RoutedEventArgs e)
	{
		if (IsXmlActive())
		{
			mnu_save_Click(null, null);
			ScriptObject scriptObject = null;
			try
			{
				lst_output.Items.Clear();
				scriptObject = ServiceProvider.ScriptManager.Load(ScriptFileName);
				scriptObject.CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
			}
			catch (Exception ex)
			{
				AddOutput("Loading error :" + ex.Message);
				return;
			}
			if (ServiceProvider.ScriptManager.Verify(scriptObject))
			{
				ServiceProvider.ScriptManager.Execute(scriptObject);
			}
			else
			{
				AddOutput("Error in script. Running aborted ! ");
			}
			return;
		}
		try
		{
			lst_outputTcl.Items.Clear();
			_manager.Execute(textEditorTcl.Text);
		}
		catch (Exception ex2)
		{
			AddOutput("Error in script. Running aborted ! " + ex2.Message);
		}
	}

	private bool IsXmlActive()
	{
		return TabControl.SelectedItem == XmTabItem;
	}

	private void manager_Output(string message, bool newline)
	{
		Dispatcher.Invoke(() =>
		{
			lst_outputTcl.Items.Add(message);
			lst_outputTcl.SelectedItem = message;
			lst_outputTcl.ScrollIntoView(message);
		});
	}

	private void mnu_stop_Click(object sender, RoutedEventArgs e)
	{
		if (IsXmlActive())
		{
			ServiceProvider.ScriptManager.Stop();
		}
		else
		{
			_manager.Stop();
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			CommandLineProcessor processor = new CommandLineProcessor();
			object resp = processor.Pharse(TextBoxCmd.Text.Split(' '));
			if (resp is IEnumerable<string> list)
			{
				TextBlockError.Text = "";
				foreach (string o in list)
				{
					System.Windows.Controls.TextBox textBlockError = TextBlockError;
					textBlockError.Text = textBlockError.Text + o + "\n";
				}
			}
			else if (resp != null)
			{
				TextBlockError.Text = resp.ToString();
			}
			lst_cmd.Items.Add(TextBoxCmd.Text);
		}
		catch (Exception ex)
		{
			TextBlockError.Text = ex.Message;
		}
	}

	private void lst_cmd_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (lst_cmd.SelectedItem != null)
		{
			TextBoxCmd.Text = lst_cmd.SelectedItem.ToString();
		}
	}

	private void TextBoxCmd_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			Button_Click(null, null);
		}
	}

	private void Label_MouseDown(object sender, MouseButtonEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
	}
}
