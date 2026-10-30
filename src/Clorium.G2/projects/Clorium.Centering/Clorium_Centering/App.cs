using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace Clorium_Centering;

public class App : Application
{
	private static Mutex _mutex;

	protected override void OnStartup(StartupEventArgs e)
	{
		_mutex = new Mutex(initiallyOwned: true, "Clorium_Centering_Yjh", out var createdNew);
		if (!createdNew)
		{
			MessageBox.Show("이미 프로그램이 실행 중입니다.");
			Application.Current.Shutdown();
		}
		else
		{
			base.OnStartup(e);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		// Restore the embedded-dependency loader before any UI is created.
		Costura.AssemblyLoader.Attach(true);
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
