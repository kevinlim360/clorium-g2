using System.Windows;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class EditTagWnd : Window, IComponentConnector
{
	private TagItem _tagItem;

	public EditTagWnd(TagItem item)
	{
		_tagItem = item;
		InitializeComponent();
		_tagItem.BeginEdit();
		DataContext = _tagItem;
	}

	private void btn_save_Click(object sender, RoutedEventArgs e)
	{
		_tagItem.EndEdit();
		DialogResult = true;
	}

	private void btn_cancel_Click(object sender, RoutedEventArgs e)
	{
		_tagItem.CancelEdit();
		DialogResult = false;
	}
}
