using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Core.Scripting.ScriptCommands;

public partial class WaitCommandControl : UserControl, IComponentConnector
{
	public WaitCommandControl(WaitCommand waitCommand)
	{
		InitializeComponent();
		DataContext = waitCommand;
	}
}
