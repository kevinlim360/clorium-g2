using System.Windows;
using System.Windows.Media;

namespace CameraControl.Classes;

// G2 colour tokens for 밝은 화면 / 어두운 화면 (클로리움 설정 > 화면과 언어 > 테마). Merged first by styles/g2theme.xaml, so
// every StaticResource G2* colour in the G2 windows resolves here. Dark is read once at start-up (App), so switching the
// theme takes effect after a restart. Dark values follow the Codex dark design (preview-comparison/palette.txt).
public sealed class G2Palette : ResourceDictionary
{
	public static bool Dark { get; set; }

	// key, light, dark (ARGB)
	private static readonly (string Key, uint Light, uint Dark)[] Tokens =
	{
		("G2Page", 0xFFF3F4F6, 0xFF101113),
		("G2Panel", 0xFFFFFFFF, 0xFF1B1D21),
		("G2Panel2", 0xFFF3F4F6, 0xFF15171A),
		("G2Hover", 0xFFEAECEF, 0xFF2A2E35),
		("G2Line", 0xFFE1E4E9, 0xFF363B44),
		("G2Line2", 0xFFA9B0BA, 0xFF5A616D),
		("G2Stage", 0xFFE4E6EA, 0xFF0B0C0E),
		("G2Overlay", 0xF0FFFFFF, 0xF01B1D21),
		("G2Text", 0xFF16181C, 0xFFF1F3F5),
		("G2Sub", 0xFF56606C, 0xFFA8AFBB),
		("G2Accent", 0xFF1F5FD6, 0xFF3D7EF0),
		("G2AccentHover", 0xFF1A52BA, 0xFF2F6BD8),
		("G2AccentSoft", 0xFFE3ECFB, 0xFF213858),
		("G2Pr", 0xFFB35400, 0xFFFFB65E),
		("G2PrBg", 0xFFFFF1DF, 0xFF49321C),
		("G2PrText", 0xFF8A5300, 0xFFFFB65E),
		("G2Ok", 0xFF1A7F37, 0xFF4CC27A),
		("G2OkBg", 0xFFE8F5EC, 0xFF1D3A27),
		("G2Err", 0xFFC62828, 0xFFFF8A8A),
		("G2SwitchOff", 0xFFC3C8CF, 0xFF4A505A),
		("G2Neutral", 0xFFEEF0F3, 0xFF2A2E35),
		("G2NeutralText", 0xFF3A4654, 0xFFD2D8E2),
		("G2Thumb", 0xFFC9CED5, 0xFF4A505A),
		// text/icons drawn on G2Accent backgrounds
		("G2OnAccent", 0xFFFFFFFF, 0xFFFFFFFF),
	};

	public G2Palette()
	{
		foreach (var t in Tokens)
		{
			var brush = new SolidColorBrush(ToColor(Dark ? t.Dark : t.Light));
			brush.Freeze();
			Add(t.Key, brush);
		}
		Add("G2AccentColor", ToColor(Dark ? 0xFF3D7EF0 : 0xFF1F5FD6));
	}

	public static Color ToColor(uint argb) => Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

	internal static uint Value(string key)
	{
		foreach (var t in Tokens)
		{
			if (t.Key == key)
			{
				return Dark ? t.Dark : t.Light;
			}
		}
		throw new System.ArgumentException("unknown G2 colour " + key);
	}
}

// MainWindow: MaterialDesign/MahApps brushes used by embedded legacy panels follow the G2 palette (they were pinned light).
public sealed class G2MaterialOverrides : ResourceDictionary
{
	private static readonly (string Key, string Token)[] Map =
	{
		("MaterialDesignBackground", "G2Panel"), ("MaterialDesignPaper", "G2Page"), ("MaterialDesignCardBackground", "G2Panel"),
		("MaterialDesignBody", "G2Text"), ("MaterialDesignBodyLight", "G2Sub"), ("MaterialDesignDivider", "G2Line"),
		("MaterialDesignSelection", "G2AccentSoft"), ("MaterialDesignPaperLight", "G2Panel"), ("MaterialDesignTextBoxBorder", "G2Line2"),
		("MaterialDesignCheckBoxOff", "G2Line2"), ("MaterialDesignToolBackground", "G2Line"), ("MaterialDesignToolForeground", "G2Text"),
		("MaterialDesignFlatButtonClick", "G2AccentSoft"), ("MahApps.Brushes.ThemeBackground", "G2Panel"), ("MahApps.Brushes.ThemeForeground", "G2Text"),
		("MahApps.Brushes.Text", "G2Text"), ("MahApps.Brushes.Label.Text", "G2Text"), ("MahApps.Brushes.Control.Background", "G2Panel"),
		("MahApps.Brushes.Window.Background", "G2Page"), ("WhiteBrush", "G2Panel"), ("BlackBrush", "G2Text"), ("TextBrush", "G2Text"),
		("LabelTextBrush", "G2Text"), ("ControlBackgroundBrush", "G2Panel"),
	};

	public G2MaterialOverrides()
	{
		foreach (var m in Map)
		{
			var brush = new SolidColorBrush(G2Palette.ToColor(G2Palette.Value(m.Token)));
			brush.Freeze();
			Add(m.Key, brush);
		}
	}
}
