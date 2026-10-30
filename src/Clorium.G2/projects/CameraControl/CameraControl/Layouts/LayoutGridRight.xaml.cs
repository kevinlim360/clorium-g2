using System.Windows.Markup;
using CameraControl.ViewModel;

namespace CameraControl.Layouts;

public partial class LayoutGridRight : LayoutBase, IComponentConnector
{
	public LayoutGridRight()
	{
		InitializeComponent();
		ImageLIst = ImageLIstBox;
		ZoomAndPanControl = zoomAndPanControl;
		if (ZoomAndPanControl != null)
		{
			LayoutViewModel = (LayoutViewModel)ZoomAndPanControl.DataContext;
			MediaElement = VideoControl;
			content = Image;
			InitServices();
		}
	}
}
