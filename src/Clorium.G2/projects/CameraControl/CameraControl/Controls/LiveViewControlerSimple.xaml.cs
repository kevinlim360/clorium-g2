using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Controls;

public partial class LiveViewControlerSimple : UserControl, IComponentConnector
{
	public LiveViewControlerSimple()
	{
		InitializeComponent();
	}
}
