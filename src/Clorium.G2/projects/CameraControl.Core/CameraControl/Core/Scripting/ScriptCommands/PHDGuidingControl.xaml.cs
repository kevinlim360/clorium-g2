using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Core.Scripting.ScriptCommands;

public partial class PHDGuidingControl : UserControl, IComponentConnector
{
	public PHDGuidingControl(PHDGuiding guiding)
	{
		InitializeComponent();
		cmb_move.Items.Add("Move 1");
		cmb_move.Items.Add("Move 2");
		cmb_move.Items.Add("Move 3");
		cmb_move.Items.Add("Move 4");
		cmb_move.Items.Add("Move 5");
		DataContext = guiding;
	}
}
