using System;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

/// <summary>
/// G2-styled replacement for MessageBox.Show (errors, notices, questions): same overloads and results, G2 card look in the
/// light or dark screen theme, Korean or English buttons (UiText language). Safe from any thread: off the UI thread the
/// dialog runs on its own STA thread like MessageBox, so it never waits for a busy UI thread.
/// Enter = default button, Esc = Cancel (or OK when it is the only button), Ctrl+C copies the text.
/// </summary>
public static class G2Dialog
{
	/// <summary>Screen theme (G2Palette.Dark keeps it in step).</summary>
	public static bool Dark { get; set; }

	/// <summary>G2 font (set by G2 at start-up; FontFamily can be used from any thread).</summary>
	public static FontFamily Font { get; set; } = new FontFamily("Malgun Gothic");

	public static MessageBoxResult Show(string text) => Show(null, text, "", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(string text, string caption) => Show(null, text, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(string text, string caption, MessageBoxButton button) => Show(null, text, caption, button, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon) => Show(null, text, caption, button, icon, MessageBoxResult.None);
	public static MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult) => Show(null, text, caption, button, icon, defaultResult);
	public static MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options) => Show(null, text, caption, button, icon, defaultResult);
	public static MessageBoxResult Show(Window owner, string text) => Show(owner, text, "", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(Window owner, string text, string caption) => Show(owner, text, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button) => Show(owner, text, caption, button, MessageBoxImage.None, MessageBoxResult.None);
	public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon) => Show(owner, text, caption, button, icon, MessageBoxResult.None);

	public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
	{
		try
		{
			Dispatcher ui = Application.Current?.Dispatcher;
			if (ui != null && ui.CheckAccess())
			{
				return ShowHere(OwnerOf(owner), text, caption, button, icon, defaultResult);
			}
			if (owner != null && owner.Dispatcher.CheckAccess())
			{
				return ShowHere(owner, text, caption, button, icon, defaultResult);
			}
			// Another thread (camera events, downloads...): an STA thread of its own, as MessageBox does.
			MessageBoxResult result = MessageBoxResult.None;
			Exception failure = null;
			var thread = new Thread(() =>
			{
				try { result = ShowHere(null, text, caption, button, icon, defaultResult); }
				catch (Exception ex) { failure = ex; }
				finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
			}) { IsBackground = true, Name = "G2 dialog" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
			if (failure != null) throw failure;
			return result;
		}
		catch (Exception ex)
		{
			// The message must still reach the user.
			Log.Error("G2Dialog : falling back to MessageBox", ex);
			return System.Windows.MessageBox.Show(text ?? "", caption ?? "", button, icon, defaultResult);
		}
	}

	private static Window OwnerOf(Window owner)
	{
		if (owner != null) return owner.IsLoaded && owner.IsVisible ? owner : null;
		Application app = Application.Current;
		if (app == null) return null;
		Window active = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible && w.IsLoaded);
		if (active != null) return active;
		Window main = app.MainWindow;
		return main != null && main.IsVisible && main.IsLoaded && main.WindowState != WindowState.Minimized ? main : null;
	}

	// ---- look ----

	private static readonly string[] Keys = { "Panel", "Line", "Text", "Sub", "Accent", "AccentSoft", "OnAccent", "Pr", "PrBg", "Err", "ErrBg", "Hover", "Shadow" };
	private static readonly uint[] LightValues = { 0xFFFFFFFF, 0xFFE1E4E9, 0xFF16181C, 0xFF56606C, 0xFF1F5FD6, 0xFFE3ECFB, 0xFFFFFFFF, 0xFFB35400, 0xFFFFF1DF, 0xFFC62828, 0xFFFDE8E8, 0xFFEAECEF, 0x40000000 };
	private static readonly uint[] DarkValues = { 0xFF1B1D21, 0xFF363B44, 0xFFF1F3F5, 0xFFA8AFBB, 0xFF3D7EF0, 0xFF213858, 0xFFFFFFFF, 0xFFFFB65E, 0xFF49321C, 0xFFFF8A8A, 0xFF4A2326, 0xFF2A2E35, 0x80000000 };

	private static Brush B(string key)
	{
		uint[] values = Dark ? DarkValues : LightValues;
		uint v = values[Array.IndexOf(Keys, key)];
		var brush = new SolidColorBrush(Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v));
		brush.Freeze();
		return brush;
	}

	private static bool Korean => UiText.IsKorean;

	private static string Label(MessageBoxResult r)
	{
		switch (r)
		{
			case MessageBoxResult.Yes: return Korean ? "예" : "Yes";
			case MessageBoxResult.No: return Korean ? "아니요" : "No";
			case MessageBoxResult.Cancel: return Korean ? "취소" : "Cancel";
			default: return Korean ? "확인" : "OK";
		}
	}

	private static string DefaultCaption(MessageBoxImage icon)
	{
		switch (icon)
		{
			case MessageBoxImage.Error: return Korean ? "오류" : "Error";
			case MessageBoxImage.Warning: return Korean ? "주의" : "Warning";
			case MessageBoxImage.Question: return Korean ? "확인" : "Confirm";
			default: return Korean ? "알림" : "Notice";
		}
	}

	private static MessageBoxResult[] ButtonsOf(MessageBoxButton button)
	{
		switch (button)
		{
			case MessageBoxButton.OKCancel: return new[] { MessageBoxResult.Cancel, MessageBoxResult.OK };
			case MessageBoxButton.YesNo: return new[] { MessageBoxResult.No, MessageBoxResult.Yes };
			case MessageBoxButton.YesNoCancel: return new[] { MessageBoxResult.Cancel, MessageBoxResult.No, MessageBoxResult.Yes };
			default: return new[] { MessageBoxResult.OK };
		}
	}

	private static Geometry IconGeometry(MessageBoxImage icon)
	{
		switch (icon)
		{
			case MessageBoxImage.Error: return Geometry.Parse("M12,3 A9,9 0 1 0 12.001,3 Z M9,9 L15,15 M15,9 L9,15");
			case MessageBoxImage.Warning: return Geometry.Parse("M12,3 L22,20 H2 Z M12,10 V14 M12,17 L12.01,17");
			case MessageBoxImage.Question: return Geometry.Parse("M12,3 A9,9 0 1 0 12.001,3 Z M9.5,9.5 A2.5,2.5 0 1 1 13,11.8 C12.3,12.2 12,12.7 12,13.5 M12,17 L12.01,17");
			default: return Geometry.Parse("M12,3 A9,9 0 1 0 12.001,3 Z M12,11 V16 M12,8 L12.01,8");
		}
	}

	private static Button MakeButton(string text, bool primary)
	{
		var face = new Border
		{
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(18, 0, 18, 0),
			MinWidth = 84,
			Height = 36,
			Background = primary ? B("Accent") : B("Panel"),
			BorderBrush = primary ? B("Accent") : B("Line"),
			BorderThickness = new Thickness(1),
			Child = new TextBlock { Text = text, FontSize = 13, FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal, Foreground = primary ? B("OnAccent") : B("Text"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		var template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
		var b = new Button { Content = face, Template = template, Margin = new Thickness(8, 0, 0, 0), Cursor = Cursors.Hand, Focusable = true };
		Brush normal = face.Background, hover = primary ? B("Accent") : B("Hover");
		b.MouseEnter += (s, e) => { face.Background = hover; face.Opacity = primary ? 0.9 : 1; };
		b.MouseLeave += (s, e) => { face.Background = normal; face.Opacity = 1; };
		b.GotKeyboardFocus += (s, e) => face.BorderBrush = B("Accent");
		b.LostKeyboardFocus += (s, e) => face.BorderBrush = primary ? B("Accent") : B("Line");
		return b;
	}

	/// <summary>Builds the dialog window (also used by tests to render it).</summary>
	public static Window Create(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, Action<MessageBoxResult> done)
	{
		text = text ?? "";
		var buttons = ButtonsOf(button);
		MessageBoxResult primary = buttons.Contains(defaultResult) ? defaultResult : buttons[buttons.Length - 1];
		MessageBoxResult escape = buttons.Contains(MessageBoxResult.Cancel) ? MessageBoxResult.Cancel : button == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.None;
		MessageBoxResult chosen = escape != MessageBoxResult.None ? escape : buttons[0];

		var window = new Window
		{
			WindowStyle = WindowStyle.None,
			AllowsTransparency = true,
			Background = Brushes.Transparent,
			ResizeMode = ResizeMode.NoResize,
			SizeToContent = SizeToContent.WidthAndHeight,
			ShowInTaskbar = false,
			FontFamily = Font,
			Title = string.IsNullOrEmpty(caption) ? DefaultCaption(icon) : caption,
			UseLayoutRounding = true
		};
		TextOptions.SetTextFormattingMode(window, TextFormattingMode.Ideal);

		var titleRow = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
		if (icon != MessageBoxImage.None)
		{
			string tone = icon == MessageBoxImage.Error ? "Err" : icon == MessageBoxImage.Warning ? "Pr" : "Accent";
			string toneBg = icon == MessageBoxImage.Error ? "ErrBg" : icon == MessageBoxImage.Warning ? "PrBg" : "AccentSoft";
			var badge = new Border
			{
				Width = 36, Height = 36, CornerRadius = new CornerRadius(18), Background = B(toneBg), Margin = new Thickness(0, 0, 12, 0),
				Child = new Viewbox { Width = 20, Height = 20, Child = new Path { Data = IconGeometry(icon), Stroke = B(tone), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Width = 24, Height = 24, Stretch = Stretch.None } }
			};
			DockPanel.SetDock(badge, Dock.Left);
			titleRow.Children.Add(badge);
		}
		titleRow.Children.Add(new TextBlock { Text = window.Title, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = B("Text"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 400 });

		var message = new TextBlock
		{
			Text = text.TrimEnd(),
			FontSize = 13.5,
			LineHeight = 21,
			TextWrapping = TextWrapping.Wrap,
			Foreground = B("Text"),
			MaxWidth = 440,
			MinWidth = 260,
			Margin = new Thickness(icon != MessageBoxImage.None ? 48 : 0, 0, 0, 0)
		};
		var scroll = new ScrollViewer { Content = message, MaxHeight = Math.Max(200, SystemParameters.WorkArea.Height * 0.6), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false };

		var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
		Button primaryButton = null;
		foreach (MessageBoxResult r in buttons)
		{
			MessageBoxResult value = r;
			Button b = MakeButton(Label(r), r == primary);
			b.Click += (s, e) => { chosen = value; window.Close(); };
			if (r == primary) primaryButton = b;
			row.Children.Add(b);
		}

		var content = new StackPanel { Margin = new Thickness(22, 20, 22, 18) };
		content.Children.Add(titleRow);
		content.Children.Add(scroll);
		content.Children.Add(row);
		var card = new Border
		{
			CornerRadius = new CornerRadius(12),
			Background = B("Panel"),
			BorderBrush = B("Line"),
			BorderThickness = new Thickness(1),
			Margin = new Thickness(18),
			Child = content,
			Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 4, Direction = 270, Opacity = Dark ? 0.6 : 0.22, Color = Colors.Black }
		};
		card.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) try { window.DragMove(); } catch (InvalidOperationException) { } };
		window.Content = card;

		window.PreviewKeyDown += (s, e) =>
		{
			if (e.Key == Key.Escape && escape != MessageBoxResult.None) { chosen = escape; window.Close(); e.Handled = true; }
			else if (e.Key == Key.Enter && !(Keyboard.FocusedElement is Button)) { chosen = primary; window.Close(); e.Handled = true; }
			else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
			{
				try { Clipboard.SetText(window.Title + Environment.NewLine + Environment.NewLine + text); } catch (Exception) { }
				e.Handled = true;
			}
		};
		window.Loaded += (s, e) => primaryButton?.Focus();
		window.Closed += (s, e) => done?.Invoke(chosen);
		return window;
	}

	private static MessageBoxResult ShowHere(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
	{
		MessageBoxResult result = MessageBoxResult.None;
		Window window = Create(text, caption, button, icon, defaultResult, r => result = r);
		if (owner != null)
		{
			window.Owner = owner;
			window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}
		else
		{
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			window.Topmost = true;
			window.ShowInTaskbar = true;
		}
		switch (icon)
		{
			case MessageBoxImage.Error: SystemSounds.Hand.Play(); break;
			case MessageBoxImage.Warning: SystemSounds.Exclamation.Play(); break;
			case MessageBoxImage.Question: SystemSounds.Question.Play(); break;
			case MessageBoxImage.Information: SystemSounds.Asterisk.Play(); break;
		}
		window.ShowDialog();
		return result;
	}
}
