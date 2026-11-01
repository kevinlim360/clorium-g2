using System;
using System.Globalization;
using System.Windows;

namespace CameraControl.Classes
{
	/// <summary>
	/// G2 UI: "큰 글자 보기". Publishes the font-size and layout-size resources the G2 XAML reads through
	/// DynamicResource (G2Fs13, G2Fs12_5, ..., G2PanelW, G2RailBtnW, ...) at application scope.
	/// Normal = the design sizes; large = text x1.35 with the rail, right panel and tool tiles widened to fit.
	/// The product-name text and barcode overlay keep fixed literal sizes and are not affected.
	/// </summary>
	public static class G2TextScale
	{
		public const double LargeFactor = 1.35;

		private static readonly double[] FontSizes = { 9, 10, 10.5, 11, 11.5, 12, 12.5, 13, 13.5, 14, 15, 16, 17, 20, 22 };

		public static bool IsLarge { get; private set; }

		public static void Apply(bool large)
		{
			Application app = Application.Current;
			if (app == null)
			{
				return;
			}
			IsLarge = large;
			double f = large ? LargeFactor : 1.0;
			ResourceDictionary r = app.Resources;
			foreach (double size in FontSizes)
			{
				r[Key(size)] = Math.Round(size * f, 1);
			}
			r["G2RailBtnW"] = large ? 80.0 : 60.0;
			r["G2RailBtnH"] = large ? 66.0 : 56.0;
			r["G2RailColW"] = new GridLength(large ? 94.0 : 74.0);
			r["G2HeaderRowH"] = new GridLength(large ? 70.0 : 62.0);
			r["G2PanelW"] = large ? 410.0 : 340.0;
			r["G2ToolTileW"] = large ? 98.0 : 76.0;
			r["G2ToolTileH"] = large ? 76.0 : 64.0;
			r["G2ToolGridW"] = large ? 408.0 : 320.0;
		}

		private static string Key(double size)
		{
			return "G2Fs" + size.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', '_');
		}
	}
}
