using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Core.Scripting.ScriptCommands;

public partial class BulbCaptureControl : UserControl, IComponentConnector
{
	public BulbCapture BulbCapture { get; set; }

	public BulbCaptureControl(BulbCapture capture)
	{
		BulbCapture = capture;
		InitializeComponent();
		DataContext = capture;
	}
}
