using System;
using System.Windows;

namespace CameraControl.Devices
{
	/// <summary>
	/// Message boxes raised by the device layer. G2 sets <see cref="Handler"/> to its styled dialog (Core G2Dialog, which this
	/// assembly cannot reference); without it the plain MessageBox is used.
	/// </summary>
	public static class UiMessage
	{
		public static Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> Handler { get; set; }

		public static MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
		{
			Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> handler = Handler;
			return handler != null ? handler(text, caption, button, icon) : MessageBox.Show(text, caption, button, icon);
		}
	}
}
