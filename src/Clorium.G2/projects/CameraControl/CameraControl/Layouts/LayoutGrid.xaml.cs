using System.Windows.Markup;
using CameraControl.ViewModel;

namespace CameraControl.Layouts;

public partial class LayoutGrid : LayoutBase, IComponentConnector
{
	public LayoutGrid()
	{
		InitializeComponent();
		ImageLIst = ImageLIstBox;
		LayoutViewModel = (LayoutViewModel)DataContext;
		InitServices();
	}
}
