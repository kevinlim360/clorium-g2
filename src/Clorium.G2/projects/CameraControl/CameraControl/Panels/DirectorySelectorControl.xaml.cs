using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace CameraControl.Panels;

public partial class DirectorySelectorControl : UserControl, INotifyPropertyChanged, IComponentConnector
{
	public static readonly DependencyProperty SelectedPathProperty = DependencyProperty.Register("SelectedPath", typeof(string), typeof(DirectorySelectorControl), new PropertyMetadata("", PropertyChangedCallback));

	public string SelectedPath
	{
		get
		{
			return GetValue(SelectedPathProperty) as string;
		}
		set
		{
			SetValue(SelectedPathProperty, value);
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
	{
		if (dependencyPropertyChangedEventArgs.OldValue != dependencyPropertyChangedEventArgs.NewValue)
		{
			ExpandAndSelectItem(((DirectorySelectorControl)dependencyObject).trvStructure, (string)dependencyPropertyChangedEventArgs.NewValue);
		}
	}

	public DirectorySelectorControl()
	{
		InitializeComponent();
		Refresh();
	}

	private void Refresh()
	{
		trvStructure.Items.Clear();
		DriveInfo[] drives = DriveInfo.GetDrives();
		DriveInfo[] array = drives;
		foreach (DriveInfo driveInfo in array)
		{
			trvStructure.Items.Add(CreateTreeItem(driveInfo));
		}
	}

	private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
	{
		TreeViewItem item = e.Source as TreeViewItem;
		if (item.Items.Count != 1 || !(item.Items[0] is string))
		{
			return;
		}
		item.Items.Clear();
		DirectoryInfo expandedDir = null;
		if (item.Tag is DriveInfo)
		{
			expandedDir = (item.Tag as DriveInfo).RootDirectory;
		}
		if (item.Tag is DirectoryInfo)
		{
			expandedDir = item.Tag as DirectoryInfo;
		}
		try
		{
			DirectoryInfo[] directories = expandedDir.GetDirectories();
			foreach (DirectoryInfo subDir in directories)
			{
				item.Items.Add(CreateTreeItem(subDir));
			}
		}
		catch
		{
		}
	}

	private TreeViewItem CreateTreeItem(object o)
	{
		TreeViewItem item = new TreeViewItem();
		item.Header = o.ToString();
		item.Tag = o;
		item.Items.Add("Loading...");
		return item;
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void TrvStructure_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		TreeView tree = (TreeView)sender;
		TreeViewItem temp = (TreeViewItem)tree.SelectedItem;
		if (temp == null)
		{
			return;
		}
		string _selectedPath = "";
		string temp2 = "";
		string temp3 = "";
		while (true)
		{
			temp2 = temp.Header.ToString();
			if (temp2.Contains("\\"))
			{
				temp3 = "";
			}
			_selectedPath = temp2 + temp3 + _selectedPath;
			if (temp.Parent.GetType() == typeof(TreeView))
			{
				break;
			}
			temp = (TreeViewItem)temp.Parent;
			temp3 = "\\";
		}
		SelectedPath = _selectedPath;
	}

	private static bool ExpandAndSelectItem(ItemsControl parentContainer, string itemToSelect)
	{
		foreach (object item in (IEnumerable)parentContainer.Items)
		{
			if (parentContainer.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem currentContainer)
			{
				DriveInfo drive = currentContainer.Tag as DriveInfo;
				DirectoryInfo path = currentContainer.Tag as DirectoryInfo;
				if (drive != null && drive.RootDirectory.FullName == itemToSelect)
				{
					currentContainer.IsSelected = true;
					currentContainer.BringIntoView();
					currentContainer.Focus();
					return true;
				}
				if (path != null && path.FullName == itemToSelect)
				{
					currentContainer.IsSelected = true;
					currentContainer.BringIntoView();
					currentContainer.Focus();
					return true;
				}
			}
		}
		foreach (object item2 in (IEnumerable)parentContainer.Items)
		{
			TreeViewItem currentContainer2 = parentContainer.ItemContainerGenerator.ContainerFromItem(item2) as TreeViewItem;
			if (currentContainer2 == null)
			{
				continue;
			}
			DriveInfo drive2 = currentContainer2.Tag as DriveInfo;
			DirectoryInfo path2 = currentContainer2.Tag as DirectoryInfo;
			if ((drive2 != null && !itemToSelect.StartsWith(drive2.RootDirectory.FullName)) || (path2 != null && !itemToSelect.StartsWith(path2.FullName)) || currentContainer2 == null || currentContainer2.Items.Count <= 0)
			{
				continue;
			}
			bool wasExpanded = currentContainer2.IsExpanded;
			currentContainer2.IsExpanded = true;
			if (currentContainer2.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
			{
				EventHandler eh = null;
				eh = delegate
				{
					if (currentContainer2.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
					{
						if (!ExpandAndSelectItem(currentContainer2, itemToSelect))
						{
							currentContainer2.IsExpanded = false;
						}
						currentContainer2.ItemContainerGenerator.StatusChanged -= eh;
					}
				};
				currentContainer2.ItemContainerGenerator.StatusChanged += eh;
			}
			else
			{
				if (ExpandAndSelectItem(currentContainer2, itemToSelect))
				{
					return true;
				}
				currentContainer2.IsExpanded = wasExpanded;
			}
		}
		return false;
	}

	private void Label_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Refresh();
		ExpandAndSelectItem(trvStructure, SelectedPath);
	}
}
