using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CloriumAutoSizing;

public class UserRect
{
	public enum LineType
	{
		Virtical,
		Horizontal,
		TypeA,
		TypeB,
		Manual,
		None
	}

	private enum PosSizableRect
	{
		LeftUp,
		RightUp,
		RightBottom,
		LeftBottom,
		LeftMid,
		RightMid,
		WaterMark,
		None
	}

	public class UserBox
	{
		public int ID;

		public bool isOK;

		public bool showHelpLine;

		public AssistantLine assistantLine = new AssistantLine();

		public MeasureLine measureLine = new MeasureLine();

		public string Type = "";

		public string TypeID = "";

		public string TypeItem = "";

		public string ResultDisplay = "";
	}

	public class AssistantLine
	{
		public bool isOK;

		public Rectangle rect_L;

		public Rectangle rect_R;

		public Rectangle rect_box;

		public bool move_LB;

		public bool move_RB;

		public LineType lineType;
	}

	public class MeasureLine
	{
		public bool isOK;

		public bool isManualOK;

		public Point pt1;

		public Point pt2;

		public Rectangle rect_LP;

		public Rectangle rect_RP;

		public Rectangle rect_box;

		public bool move_box;

		public bool move_LP;

		public bool move_RP;

		public string measure_Len = "";

		public double angle;
	}

	public class WaterMark
	{
		public bool isOK;

		public bool move_water;

		public string text_water = "";

		public Rectangle rect_water;
	}

	private PictureBox mPictureBox;

	private Bitmap mBmp;

	public Color lineColor;

	public Color fontColor;

	public Brush baseColor;

	public string FontSize;

	public bool isMove_All;

	public bool isMoving;

	public bool isUnit;

	public bool isManual;

	public bool isShiftPressed;

	public bool isBaseColor;

	public int mIndex;

	public int mArrayMaxNo = 10;

	public int mSizeNodeRect = 6;

	public double mManualAssisLen;

	public double mAutoAssisLen;

	public double mZoomRatio = 1.0;

	public double mScale;

	public Rectangle rect_crop = new Rectangle(10, 10, 150, 150);

	public bool isCropped;

	public bool isCropMove;

	public bool isDrawing;

	public int oldX;

	public int oldY;

	private PosSizableRect mNodeSelected = PosSizableRect.None;

	public UserBox[] userBoxs;

	public WaterMark watermark = new WaterMark();

	private const double INCH = 25.4;

	private const double CM = 10.0;

	public UserRect(PictureBox p)
	{
		isDrawing = false;
		mIndex = 0;
		SetPictureBox(p);
		userBoxs = new UserBox[mArrayMaxNo];
		for (int i = 0; i < mArrayMaxNo; i++)
		{
			userBoxs[i] = new UserBox();
		}
		ClearAllArray();
		SetPosWatermarkRect("", new Point(20, 200));
	}

	public void SetPictureBox(PictureBox p)
	{
		mPictureBox = p;
		mPictureBox.Paint += mPictureBox_Paint;
		mPictureBox.MouseDown += mPictureBox_MouseDown;
		mPictureBox.MouseUp += mPictureBox_MouseUp;
		mPictureBox.MouseMove += mPictureBox_MouseMove;
	}

	public void ClearAllArray()
	{
		mIndex = 0;
		for (int i = 0; i < mArrayMaxNo; i++)
		{
			ClearArrayMeasure(i);
		}
	}

	public void ClearArrayMeasure(int index)
	{
		userBoxs[index].ID = index;
		userBoxs[index].isOK = false;
		userBoxs[index].showHelpLine = false;
		userBoxs[index].Type = "";
		userBoxs[index].TypeID = "";
		userBoxs[index].TypeItem = "";
		userBoxs[index].ResultDisplay = "";
		userBoxs[index].assistantLine.isOK = false;
		userBoxs[index].assistantLine.rect_L = new Rectangle(0, 0, 0, 0);
		userBoxs[index].assistantLine.rect_R = new Rectangle(0, 0, 0, 0);
		userBoxs[index].assistantLine.rect_box = new Rectangle(0, 0, 0, 0);
		userBoxs[index].assistantLine.lineType = LineType.None;
		userBoxs[index].measureLine.isOK = false;
		userBoxs[index].measureLine.isManualOK = false;
		userBoxs[index].measureLine.pt1 = new Point(0, 0);
		userBoxs[index].measureLine.pt2 = new Point(0, 0);
		userBoxs[index].measureLine.rect_box = new Rectangle(0, 0, 0, 0);
		userBoxs[index].measureLine.rect_LP = new Rectangle(0, 0, 0, 0);
		userBoxs[index].measureLine.rect_RP = new Rectangle(0, 0, 0, 0);
		userBoxs[index].measureLine.measure_Len = "";
		userBoxs[index].measureLine.angle = 0.0;
	}

	public void InitPosAssistantRect(int index, LineType type, Rectangle leftRect, Rectangle rightRect)
	{
		userBoxs[index].assistantLine.rect_L = leftRect;
		userBoxs[index].assistantLine.rect_R = rightRect;
		userBoxs[index].assistantLine.lineType = type;
		userBoxs[index].assistantLine.rect_box = GetAssitantBoxForMoving(index);
		userBoxs[index].isOK = true;
		SetShowHelpLine(index);
	}

	public void InitPosMeasureRect(int index, Point pt1, Point pt2)
	{
		userBoxs[index].measureLine.pt1 = pt1;
		userBoxs[index].measureLine.pt2 = pt2;
		userBoxs[index].measureLine.rect_LP = GetSizableRect_Measure_L(index);
		userBoxs[index].measureLine.rect_RP = GetSizableRect_Measure_R(index);
		userBoxs[index].measureLine.rect_box = GetMeasureLineRectForMoveing(index);
		userBoxs[index].isOK = true;
		SetShowHelpLine(index);
	}

	public void SetPosWatermarkRect(string text, Point pt)
	{
		watermark.rect_water.X = pt.X;
		watermark.rect_water.Y = pt.Y;
		watermark.rect_water.Width = text.Length * 13;
		watermark.rect_water.Height = 20;
	}

	public void SetShowHelpLine(int index)
	{
		for (int i = 0; i < mArrayMaxNo; i++)
		{
			userBoxs[i].showHelpLine = false;
		}
		userBoxs[index].showHelpLine = true;
	}

	public void SetBitmapFile(string filename)
	{
		mBmp = new Bitmap(filename);
	}

	public void SetBitmap(Bitmap bmp)
	{
		mBmp = bmp;
	}

	public void DrawAll(Graphics g)
	{
		if (isCropped)
		{
			DrawCropBox(g);
			return;
		}
		for (int i = 0; i < mArrayMaxNo; i++)
		{
			if (userBoxs[i].isOK)
			{
				DrawBoxAssistant(g, i);
				DrawLineMeasure(g, i);
				DrawBoxMove(g, mIndex);
			}
			if (userBoxs[i].measureLine.isManualOK)
			{
				DrawManualLineMeasure(g, i);
			}
		}
		if (watermark.text_water != "")
		{
			DrawWaterMark(g);
		}
	}

	public void DrawBoxAssistant(Graphics g, int index)
	{
		if (userBoxs[index].showHelpLine)
		{
			g.DrawRectangle(new Pen(Color.Gray), userBoxs[index].assistantLine.rect_L);
			g.DrawRectangle(new Pen(Color.Gray), userBoxs[index].assistantLine.rect_R);
		}
		switch (userBoxs[index].assistantLine.lineType)
		{
		case LineType.Virtical:
			DrawLineVirtical(g, index);
			break;
		case LineType.Horizontal:
			DrawLineHorizontal(g, index);
			break;
		case LineType.TypeA:
			if (userBoxs[index].measureLine.angle < 0.0)
			{
				DrawDiagonalTypeA(g, index);
			}
			else
			{
				DrawDiagonalTypeB(g, index);
			}
			break;
		case LineType.TypeB:
			if (userBoxs[index].measureLine.angle > 0.0)
			{
				DrawDiagonalTypeB(g, index);
			}
			else
			{
				DrawDiagonalTypeA(g, index);
			}
			break;
		}
		foreach (PosSizableRect value in Enum.GetValues(typeof(PosSizableRect)))
		{
			if ((value == PosSizableRect.LeftUp || value == PosSizableRect.RightBottom) && userBoxs[index].showHelpLine)
			{
				g.FillRectangle(new SolidBrush(Color.Red), GetSizableRect_L(index, value));
				g.FillRectangle(new SolidBrush(Color.Red), GetSizableRect_R(index, value));
				if (value == PosSizableRect.LeftUp)
				{
					g.DrawString("1", new Font("Arial", 10f), new SolidBrush(fontColor), GetSizableRect_L(index, value).Left - 2, GetSizableRect_L(index, value).Top - 15);
					g.DrawString("2", new Font("Arial", 10f), new SolidBrush(fontColor), GetSizableRect_R(index, value).Left - 2, GetSizableRect_R(index, value).Top - 15);
				}
			}
			if (userBoxs[index].showHelpLine)
			{
				g.DrawRectangle(new Pen(Color.Gray), GetSizableRect_L(index, value));
				g.DrawRectangle(new Pen(Color.Gray), GetSizableRect_R(index, value));
			}
		}
	}

	public void DrawBoxMove(Graphics g, int index)
	{
		if (isMove_All)
		{
			g.DrawRectangle(new Pen(Color.Lime), userBoxs[index].assistantLine.rect_box);
		}
	}

	public void DrawManualLineMeasure(Graphics g, int index)
	{
		if (userBoxs[index].measureLine.pt2.X != 0 || userBoxs[index].measureLine.pt2.Y != 0)
		{
			g.DrawLine(new Pen(lineColor, 2f), userBoxs[index].measureLine.pt1, userBoxs[index].measureLine.pt2);
			DrawManualEndPoint(g, index, userBoxs[index].measureLine.pt1.X, userBoxs[index].measureLine.pt1.Y, 0);
			DrawManualEndPoint(g, index, userBoxs[index].measureLine.pt2.X, userBoxs[index].measureLine.pt2.Y, 0);
			DrawText(g, index);
		}
	}

	public void DrawLineMeasure(Graphics g, int index)
	{
		g.DrawLine(new Pen(lineColor, 2f), userBoxs[index].measureLine.pt1, userBoxs[index].measureLine.pt2);
		if (userBoxs[index].showHelpLine)
		{
			g.FillRectangle(new SolidBrush(Color.Red), userBoxs[index].measureLine.rect_LP);
			g.FillRectangle(new SolidBrush(Color.Red), userBoxs[index].measureLine.rect_RP);
			g.DrawRectangle(new Pen(Color.Gray), userBoxs[index].measureLine.rect_box);
		}
		DrawText(g, index);
	}

	public void DrawText(Graphics g, int index)
	{
		SizeF sizeF = g.MeasureString(userBoxs[index].ResultDisplay, new Font("Arial", int.Parse(FontSize), FontStyle.Bold));
		int labelLeftPx = (int)((float)(userBoxs[index].measureLine.pt1.X + (userBoxs[index].measureLine.pt2.X - userBoxs[index].measureLine.pt1.X) / 2) - sizeF.Width / 2f);
		int labelTopPx = userBoxs[index].measureLine.pt1.Y + (userBoxs[index].measureLine.pt2.Y - userBoxs[index].measureLine.pt1.Y) / 2 + 3;
		if (isBaseColor)
		{
			g.FillRectangle(baseColor, new Rectangle(labelLeftPx, labelTopPx, (int)sizeF.Width, (int)sizeF.Height));
		}
		g.DrawString(userBoxs[index].ResultDisplay, new Font("Arial", int.Parse(FontSize), FontStyle.Bold), new SolidBrush(fontColor), labelLeftPx, labelTopPx);
	}

	public void DrawLineVirtical(Graphics g, int index)
	{
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_L.Left, userBoxs[index].assistantLine.rect_L.Top), pt2: new Point(userBoxs[index].assistantLine.rect_L.Left, userBoxs[index].assistantLine.rect_L.Top + userBoxs[index].assistantLine.rect_L.Height), pen: new Pen(lineColor, 2f));
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_R.Left, userBoxs[index].assistantLine.rect_R.Top), pt2: new Point(userBoxs[index].assistantLine.rect_R.Left, userBoxs[index].assistantLine.rect_R.Top + userBoxs[index].assistantLine.rect_R.Height), pen: new Pen(lineColor, 2f));
	}

	public void DrawLineHorizontal(Graphics g, int index)
	{
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_L.Left, userBoxs[index].assistantLine.rect_L.Top), pt2: new Point(userBoxs[index].assistantLine.rect_L.Left + userBoxs[index].assistantLine.rect_L.Width, userBoxs[index].assistantLine.rect_L.Top), pen: new Pen(lineColor, 2f));
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_R.Left, userBoxs[index].assistantLine.rect_R.Top), pt2: new Point(userBoxs[index].assistantLine.rect_R.Left + userBoxs[index].assistantLine.rect_R.Width, userBoxs[index].assistantLine.rect_R.Top), pen: new Pen(lineColor, 2f));
	}

	public void DrawDiagonalTypeA(Graphics g, int index)
	{
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_L.Left, userBoxs[index].assistantLine.rect_L.Top), pt2: new Point(userBoxs[index].assistantLine.rect_L.Left + userBoxs[index].assistantLine.rect_L.Width, userBoxs[index].assistantLine.rect_L.Top + userBoxs[index].assistantLine.rect_L.Height), pen: new Pen(lineColor, 2f));
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_R.Left, userBoxs[index].assistantLine.rect_R.Top), pt2: new Point(userBoxs[index].assistantLine.rect_R.Left + userBoxs[index].assistantLine.rect_R.Width, userBoxs[index].assistantLine.rect_R.Top + userBoxs[index].assistantLine.rect_R.Height), pen: new Pen(lineColor, 2f));
	}

	public void DrawDiagonalTypeB(Graphics g, int index)
	{
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_L.Left + userBoxs[index].assistantLine.rect_L.Width, userBoxs[index].assistantLine.rect_L.Top), pt2: new Point(userBoxs[index].assistantLine.rect_L.Left, userBoxs[index].assistantLine.rect_L.Top + userBoxs[index].assistantLine.rect_L.Height), pen: new Pen(lineColor, 2f));
		g.DrawLine(pt1: new Point(userBoxs[index].assistantLine.rect_R.Left + userBoxs[index].assistantLine.rect_R.Width, userBoxs[index].assistantLine.rect_R.Top), pt2: new Point(userBoxs[index].assistantLine.rect_R.Left, userBoxs[index].assistantLine.rect_R.Top + userBoxs[index].assistantLine.rect_R.Height), pen: new Pen(lineColor, 2f));
	}

	public void DrawWaterMark(Graphics g)
	{
		if (isBaseColor)
		{
			g.FillRectangle(baseColor, watermark.rect_water);
			g.FillRectangle(new SolidBrush(Color.Red), GetSizableRect_Water());
		}
		Point watermarkOrigin = new Point
		{
			X = watermark.rect_water.X,
			Y = watermark.rect_water.Y
		};
		g.DrawString(watermark.text_water, new Font("Arial", 15f), new SolidBrush(Color.Black), watermarkOrigin.X, watermarkOrigin.Y);
	}

	public void DrawCropBox(Graphics g)
	{
		g.DrawRectangle(new Pen(Color.LightGreen, 2f), rect_crop);
		for (int i = 0; i < 4; i++)
		{
			g.DrawRectangle(new Pen(Color.Red, 2f), GetSizableRect_Crop((PosSizableRect)i));
		}
	}

	// 유지보수 설명(복구 후 추가): 화면 끝점을 중심으로 측정 각도에 수직인 보조선 또는 십자를 그린다. 각도는 라디안이고 좌표/길이는 표시 픽셀 기준이다.
	private void DrawManualEndPoint(Graphics g, int index, int cx, int cy, int type)
	{
		switch (type)
		{
		case 0:
		{
			double angle = userBoxs[index].measureLine.angle;
			double assistantHalfLengthPx = mManualAssisLen;
			double positiveNormalX = (double)cx + assistantHalfLengthPx * Math.Cos(angle + Math.PI / 2.0);
			double positiveNormalY = (double)cy + assistantHalfLengthPx * Math.Sin(angle + Math.PI / 2.0);
			double negativeNormalX = (double)cx + assistantHalfLengthPx * Math.Cos(angle - Math.PI / 2.0);
			double negativeNormalY = (double)cy + assistantHalfLengthPx * Math.Sin(angle - Math.PI / 2.0);
			g.DrawLine(new Pen(lineColor, 2f), (float)positiveNormalX, (float)positiveNormalY, (float)negativeNormalX, (float)negativeNormalY);
			break;
		}
		case 1:
		{
			int crossHalfSizePx = 10;
			g.DrawLine(new Pen(Color.Red, 2f), cx - crossHalfSizePx, cy, cx + crossHalfSizePx, cy);
			g.DrawLine(new Pen(Color.Red, 2f), cx, cy - crossHalfSizePx, cx, cy + crossHalfSizePx);
			break;
		}
		}
	}

	private void mPictureBox_MouseDown(object sender, MouseEventArgs e)
	{
		isDrawing = true;
		mNodeSelected = GetNodeSelectable(mIndex, e.Location);
		if (!isManual)
		{
			checkMovingMeasureLine(mIndex, e);
			if (userBoxs[mIndex].assistantLine.rect_L.Width > 0 && userBoxs[mIndex].assistantLine.rect_L.Height > 0)
			{
				checkMovingInPositiveRect(mIndex, e);
			}
			else
			{
				checkMovingInNegativeRect(mIndex, e);
			}
		}
		if (isMove_All && (userBoxs[mIndex].assistantLine.rect_box.Contains(new Point(e.X, e.Y)) || userBoxs[mIndex].measureLine.rect_box.Contains(new Point(e.X, e.Y))))
		{
			isMoving = true;
		}
		if (watermark.isOK)
		{
			Rectangle watermarkMoveHitRect = default;
			watermarkMoveHitRect = watermark.rect_water;
			watermarkMoveHitRect.Width = watermark.rect_water.Width - mSizeNodeRect / 2;
			if (watermarkMoveHitRect.Contains(e.Location))
			{
				watermark.move_water = true;
			}
		}
		if (isCropped && rect_crop.Contains(e.Location))
		{
			isCropMove = true;
		}
		mPictureBox.Cursor = GetCursor(mIndex, e.Location);
		if (isManual)
		{
			userBoxs[mIndex].measureLine.pt1 = new Point(e.X, e.Y);
		}
		oldX = e.X;
		oldY = e.Y;
	}

	private void mPictureBox_MouseMove(object sender, MouseEventArgs e)
	{
		if (!isDrawing)
		{
			mPictureBox.Cursor = GetCursor(mIndex, e.Location);
			oldX = e.X;
			oldY = e.Y;
		}
		else
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}
			if (isCropMove)
			{
				rect_crop.X = rect_crop.X + e.X - oldX;
				rect_crop.Y = rect_crop.Y + e.Y - oldY;
				oldX = e.X;
				oldY = e.Y;
				mPictureBox.Invalidate();
			}
			else if (watermark.move_water)
			{
				watermark.rect_water.X = watermark.rect_water.X + e.X - oldX;
				watermark.rect_water.Y = watermark.rect_water.Y + e.Y - oldY;
				oldX = e.X;
				oldY = e.Y;
				mPictureBox.Invalidate();
			}
			else if (isMove_All)
			{
				MoveBox(mIndex, e);
				oldX = e.X;
				oldY = e.Y;
				mPictureBox.Invalidate();
			}
			else if (!isManual && (userBoxs[mIndex].assistantLine.lineType == LineType.Virtical || userBoxs[mIndex].assistantLine.lineType == LineType.Horizontal))
			{
				if (userBoxs[mIndex].assistantLine.move_LB)
				{
					MoveBox(mIndex, e);
				}
				else if (userBoxs[mIndex].assistantLine.move_RB)
				{
					MeasureRightMoveBox(mIndex, e);
					LineMeasure(mIndex);
				}
				oldX = e.X;
				oldY = e.Y;
				mPictureBox.Invalidate();
			}
			else
			{
				if (e.X < 10 || e.Y < 10 || e.X > mPictureBox.Width - 10 || e.Y > mPictureBox.Height - 10)
				{
					return;
				}
				if (isCropped)
				{
					AdjustRect_Crop(e);
				}
				else
				{
					AdjustMeasureLine(mIndex, e);
					AdjustRect(mIndex, e);
				}
				if (isManual)
				{
					userBoxs[mIndex].measureLine.pt2 = new Point(e.X, e.Y);
					if (isShiftPressed)
					{
						AdjustShiftMeasureLine(mIndex, e);
					}
				}
				oldX = e.X;
				oldY = e.Y;
				LineMeasure(mIndex);
				mPictureBox.Invalidate();
			}
		}
	}

	private void mPictureBox_MouseUp(object sender, MouseEventArgs e)
	{
		isMoving = false;
		isDrawing = false;
		watermark.move_water = false;
		isCropMove = false;
		userBoxs[mIndex].assistantLine.move_LB = false;
		userBoxs[mIndex].assistantLine.move_RB = false;
		userBoxs[mIndex].measureLine.move_LP = false;
		userBoxs[mIndex].measureLine.move_RP = false;
		userBoxs[mIndex].measureLine.move_box = false;
	}

	private void mPictureBox_Paint(object sender, PaintEventArgs e)
	{
		try
		{
			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			DrawAll(e.Graphics);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void MoveBox(int index, MouseEventArgs e)
	{
		userBoxs[index].assistantLine.rect_L.X = userBoxs[index].assistantLine.rect_L.X + e.X - oldX;
		userBoxs[index].assistantLine.rect_L.Y = userBoxs[index].assistantLine.rect_L.Y + e.Y - oldY;
		userBoxs[index].assistantLine.rect_R.X = userBoxs[index].assistantLine.rect_R.X + e.X - oldX;
		userBoxs[index].assistantLine.rect_R.Y = userBoxs[index].assistantLine.rect_R.Y + e.Y - oldY;
		userBoxs[index].measureLine.pt1.X = userBoxs[index].measureLine.pt1.X + e.X - oldX;
		userBoxs[index].measureLine.pt1.Y = userBoxs[index].measureLine.pt1.Y + e.Y - oldY;
		userBoxs[index].measureLine.pt2.X = userBoxs[index].measureLine.pt2.X + e.X - oldX;
		userBoxs[index].measureLine.pt2.Y = userBoxs[index].measureLine.pt2.Y + e.Y - oldY;
		userBoxs[index].assistantLine.rect_box = GetAssitantBoxForMoving(mIndex);
		userBoxs[index].measureLine.rect_box = GetMeasureLineRectForMoveing(mIndex);
		userBoxs[index].measureLine.rect_LP = GetSizableRect_Measure_L(index);
		userBoxs[index].measureLine.rect_RP = GetSizableRect_Measure_R(index);
	}

	private void MeasureRightMoveBox(int index, MouseEventArgs e)
	{
		if (userBoxs[index].assistantLine.lineType == LineType.Virtical)
		{
			userBoxs[index].measureLine.pt2.X = userBoxs[index].measureLine.pt2.X + e.X - oldX;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].measureLine.pt1.Y;
			userBoxs[index].assistantLine.rect_R.X = userBoxs[index].assistantLine.rect_R.X + e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Y = userBoxs[index].assistantLine.rect_L.Y;
		}
		else if (userBoxs[index].assistantLine.lineType == LineType.Horizontal)
		{
			userBoxs[index].measureLine.pt2.X = userBoxs[index].measureLine.pt1.X;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].measureLine.pt2.Y + e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.X = userBoxs[index].assistantLine.rect_L.X;
			userBoxs[index].assistantLine.rect_R.Y = userBoxs[index].assistantLine.rect_R.Y + e.Y - oldY;
		}
		userBoxs[index].assistantLine.rect_box = GetAssitantBoxForMoving(index);
		userBoxs[index].measureLine.rect_LP = GetSizableRect_Measure_L(index);
		userBoxs[index].measureLine.rect_RP = GetSizableRect_Measure_R(index);
	}

	private void AdjustMeasureLine(int index, MouseEventArgs e)
	{
		if (userBoxs[index].measureLine.move_LP || userBoxs[index].measureLine.move_box)
		{
			userBoxs[index].measureLine.pt1.X = userBoxs[index].measureLine.pt1.X + e.X - oldX;
			userBoxs[index].measureLine.pt1.Y = userBoxs[index].measureLine.pt1.Y + e.Y - oldY;
			userBoxs[index].measureLine.rect_LP = GetSizableRect_Measure_L(index);
		}
		if (userBoxs[index].measureLine.move_RP || userBoxs[index].measureLine.move_box)
		{
			userBoxs[index].measureLine.pt2.X = userBoxs[index].measureLine.pt2.X + e.X - oldX;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].measureLine.pt2.Y + e.Y - oldY;
			userBoxs[index].measureLine.rect_RP = GetSizableRect_Measure_R(index);
		}
		userBoxs[index].measureLine.rect_box = GetMeasureLineRectForMoveing(index);
	}

	private void AdjustRect(int index, MouseEventArgs e)
	{
		switch (mNodeSelected)
		{
		case PosSizableRect.LeftUp:
			userBoxs[index].assistantLine.rect_L.X += e.X - oldX;
			userBoxs[index].assistantLine.rect_L.Width -= e.X - oldX;
			userBoxs[index].assistantLine.rect_L.Y += e.Y - oldY;
			userBoxs[index].assistantLine.rect_L.Height -= e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.X += e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Width -= e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Y += e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.Height -= e.Y - oldY;
			break;
		case PosSizableRect.RightUp:
			userBoxs[index].assistantLine.rect_L.Width += e.X - oldX;
			userBoxs[index].assistantLine.rect_L.Y += e.Y - oldY;
			userBoxs[index].assistantLine.rect_L.Height -= e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.Width += e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Y += e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.Height -= e.Y - oldY;
			break;
		case PosSizableRect.RightBottom:
			userBoxs[index].assistantLine.rect_L.Width += e.X - oldX;
			userBoxs[index].assistantLine.rect_L.Height += e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.Width += e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Height += e.Y - oldY;
			break;
		case PosSizableRect.LeftBottom:
			userBoxs[index].assistantLine.rect_L.Width -= e.X - oldX;
			userBoxs[index].assistantLine.rect_L.X += e.X - oldX;
			userBoxs[index].assistantLine.rect_L.Height += e.Y - oldY;
			userBoxs[index].assistantLine.rect_R.Width -= e.X - oldX;
			userBoxs[index].assistantLine.rect_R.X += e.X - oldX;
			userBoxs[index].assistantLine.rect_R.Height += e.Y - oldY;
			break;
		case PosSizableRect.WaterMark:
			watermark.rect_water.Width += e.X - oldX;
			break;
		case PosSizableRect.None:
			if (userBoxs[index].assistantLine.move_LB)
			{
				userBoxs[index].assistantLine.rect_L.X = userBoxs[index].assistantLine.rect_L.X + e.X - oldX;
				userBoxs[index].assistantLine.rect_L.Y = userBoxs[index].assistantLine.rect_L.Y + e.Y - oldY;
			}
			else if (userBoxs[index].assistantLine.move_RB)
			{
				userBoxs[index].assistantLine.rect_R.X = userBoxs[index].assistantLine.rect_R.X + e.X - oldX;
				userBoxs[index].assistantLine.rect_R.Y = userBoxs[index].assistantLine.rect_R.Y + e.Y - oldY;
			}
			break;
		}
		userBoxs[index].assistantLine.rect_box = GetAssitantBoxForMoving(index);
	}

	private void AdjustRect_Crop(MouseEventArgs e)
	{
		switch (mNodeSelected)
		{
		case PosSizableRect.LeftUp:
			rect_crop.X += e.X - oldX;
			rect_crop.Width -= e.X - oldX;
			rect_crop.Y += e.Y - oldY;
			rect_crop.Height -= e.Y - oldY;
			break;
		case PosSizableRect.RightUp:
			rect_crop.Width += e.X - oldX;
			rect_crop.Y += e.Y - oldY;
			rect_crop.Height -= e.Y - oldY;
			break;
		case PosSizableRect.RightBottom:
			rect_crop.Width += e.X - oldX;
			rect_crop.Height += e.Y - oldY;
			break;
		case PosSizableRect.LeftBottom:
			rect_crop.Width -= e.X - oldX;
			rect_crop.X += e.X - oldX;
			rect_crop.Height += e.Y - oldY;
			break;
		}
	}

	private void AdjustShiftMeasureLine(int index, MouseEventArgs e)
	{
		double angleDegree = GetAngleDegree(userBoxs[index].measureLine.pt1, userBoxs[index].measureLine.pt2);
		if ((angleDegree > 0.0 && angleDegree <= 45.0) || (angleDegree > 135.0 && angleDegree <= 180.0) || (angleDegree > 180.0 && angleDegree <= 225.0) || (angleDegree > 315.0 && angleDegree <= 360.0))
		{
			userBoxs[index].measureLine.pt2 = new Point(e.X, userBoxs[index].measureLine.pt1.Y);
		}
		else if ((angleDegree > 45.0 && angleDegree <= 90.0) || (angleDegree > 90.0 && angleDegree <= 135.0) || (angleDegree > 225.0 && angleDegree <= 270.0) || (angleDegree > 270.0 && angleDegree <= 315.0))
		{
			userBoxs[index].measureLine.pt2 = new Point(userBoxs[index].measureLine.pt1.X, e.Y);
		}
	}

	private Cursor GetCursor(int index, Point p)
	{
		Cursor result = null;
		if (isMove_All)
		{
			if (userBoxs[index].assistantLine.rect_box.Contains(p) || userBoxs[index].measureLine.rect_box.Contains(p))
			{
				return Cursors.Hand;
			}
			return Cursors.Default;
		}
		PosSizableRect posSizableRect = PosSizableRect.LeftUp;
		foreach (PosSizableRect value in Enum.GetValues(typeof(PosSizableRect)))
		{
			if (GetSizableRect_L(index, value).Contains(p))
			{
				posSizableRect = value;
				break;
			}
			if (GetSizableRect_R(index, value).Contains(p))
			{
				posSizableRect = value;
				break;
			}
			if (GetSizableRect_Crop(value).Contains(p))
			{
				posSizableRect = value;
				break;
			}
			posSizableRect = PosSizableRect.None;
		}
		if (GetSizableRect_Measure_L(index).Contains(p))
		{
			posSizableRect = PosSizableRect.LeftMid;
		}
		else if (GetSizableRect_Measure_R(index).Contains(p))
		{
			posSizableRect = PosSizableRect.RightMid;
		}
		else if (GetSizableRect_Water().Contains(p))
		{
			posSizableRect = PosSizableRect.WaterMark;
		}
		switch (posSizableRect)
		{
		case PosSizableRect.LeftUp:
			result = Cursors.SizeNWSE;
			break;
		case PosSizableRect.RightUp:
			result = Cursors.SizeNESW;
			break;
		case PosSizableRect.RightBottom:
			result = Cursors.SizeNWSE;
			break;
		case PosSizableRect.LeftBottom:
			result = Cursors.SizeNESW;
			break;
		case PosSizableRect.LeftMid:
		case PosSizableRect.RightMid:
			result = Cursors.SizeWE;
			break;
		case PosSizableRect.WaterMark:
			result = Cursors.SizeNWSE;
			break;
		case PosSizableRect.None:
			result = ((!userBoxs[index].assistantLine.rect_L.Contains(p) && !userBoxs[index].assistantLine.rect_R.Contains(p) && !userBoxs[index].measureLine.rect_box.Contains(p) && !watermark.rect_water.Contains(p)) ? Cursors.Default : Cursors.Hand);
			break;
		}
		if (isCropped && rect_crop.Contains(p))
		{
			result = Cursors.Hand;
		}
		return result;
	}

	private PosSizableRect GetNodeSelectable(int index, Point p)
	{
		foreach (PosSizableRect value in Enum.GetValues(typeof(PosSizableRect)))
		{
			if (GetSizableRect_L(index, value).Contains(p))
			{
				return value;
			}
			if (GetSizableRect_R(index, value).Contains(p))
			{
				return value;
			}
			if (GetSizableRect_Crop(value).Contains(p))
			{
				return value;
			}
		}
		if (GetSizableRect_Water().Contains(p))
		{
			return PosSizableRect.WaterMark;
		}
		return PosSizableRect.None;
	}

	private Rectangle GetSizableRect_Crop(PosSizableRect p)
	{
		return p switch
		{
			PosSizableRect.LeftUp => CreateRectSizableNode(rect_crop.X, rect_crop.Y), 
			PosSizableRect.RightUp => CreateRectSizableNode(rect_crop.X + rect_crop.Width, rect_crop.Y), 
			PosSizableRect.RightBottom => CreateRectSizableNode(rect_crop.X + rect_crop.Width, rect_crop.Y + rect_crop.Height), 
			PosSizableRect.LeftBottom => CreateRectSizableNode(rect_crop.X, rect_crop.Y + rect_crop.Height), 
			_ => default, 
		};
	}

	private Rectangle GetSizableRect_Water()
	{
		return CreateRectSizableNode(watermark.rect_water.X + watermark.rect_water.Width, watermark.rect_water.Y + watermark.rect_water.Height);
	}

	private Rectangle GetSizableRect_Measure_L(int index)
	{
		return CreateRectSizableNode(userBoxs[index].measureLine.pt1.X, userBoxs[index].measureLine.pt1.Y);
	}

	private Rectangle GetSizableRect_Measure_R(int index)
	{
		return CreateRectSizableNode(userBoxs[index].measureLine.pt2.X, userBoxs[index].measureLine.pt2.Y);
	}

	private Rectangle GetSizableRect_L(int index, PosSizableRect p)
	{
		return p switch
		{
			PosSizableRect.LeftUp => CreateRectSizableNode(userBoxs[index].assistantLine.rect_L.X, userBoxs[index].assistantLine.rect_L.Y), 
			PosSizableRect.RightUp => CreateRectSizableNode(userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width, userBoxs[index].assistantLine.rect_L.Y), 
			PosSizableRect.RightBottom => CreateRectSizableNode(userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width, userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height), 
			PosSizableRect.LeftBottom => CreateRectSizableNode(userBoxs[index].assistantLine.rect_L.X, userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height), 
			_ => default, 
		};
	}

	private Rectangle GetSizableRect_R(int index, PosSizableRect p)
	{
		return p switch
		{
			PosSizableRect.LeftUp => CreateRectSizableNode(userBoxs[index].assistantLine.rect_R.X, userBoxs[index].assistantLine.rect_R.Y), 
			PosSizableRect.RightUp => CreateRectSizableNode(userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width, userBoxs[index].assistantLine.rect_R.Y), 
			PosSizableRect.RightBottom => CreateRectSizableNode(userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width, userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height), 
			PosSizableRect.LeftBottom => CreateRectSizableNode(userBoxs[index].assistantLine.rect_R.X, userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height), 
			_ => default, 
		};
	}

	private Rectangle CreateRectSizableNode(int x, int y)
	{
		if (x == 0 || y == 0)
		{
			return new Rectangle(0, 0, 0, 0);
		}
		return new Rectangle(x - mSizeNodeRect / 2, y - mSizeNodeRect / 2, mSizeNodeRect, mSizeNodeRect);
	}

	private void checkMovingMeasureLine(int index, MouseEventArgs e)
	{
		if (userBoxs[index].measureLine.rect_LP.Contains(e.Location))
		{
			userBoxs[index].measureLine.move_LP = true;
		}
		else if (userBoxs[index].measureLine.rect_RP.Contains(e.Location))
		{
			userBoxs[index].measureLine.move_RP = true;
		}
		else if (userBoxs[index].measureLine.rect_box.Contains(e.Location))
		{
			userBoxs[index].measureLine.move_box = true;
		}
	}

	private void checkMovingInPositiveRect(int index, MouseEventArgs e)
	{
		if (userBoxs[index].assistantLine.rect_L.Contains(new Point(e.X, e.Y)))
		{
			userBoxs[index].assistantLine.move_LB = true;
		}
		else if (userBoxs[index].assistantLine.rect_R.Contains(new Point(e.X, e.Y)))
		{
			userBoxs[index].assistantLine.move_RB = true;
		}
	}

	private void checkMovingInNegativeRect(int index, MouseEventArgs e)
	{
		Rectangle rectangle = default;
		if (userBoxs[index].assistantLine.rect_L.Width < 0 && userBoxs[index].assistantLine.rect_L.Height > 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width;
			rectangle.Y = userBoxs[index].assistantLine.rect_L.Y;
		}
		else if (userBoxs[index].assistantLine.rect_L.Width > 0 && userBoxs[index].assistantLine.rect_L.Height < 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_L.X;
			rectangle.Y = userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height;
		}
		else if (userBoxs[index].assistantLine.rect_L.Width < 0 && userBoxs[index].assistantLine.rect_L.Height < 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width;
			rectangle.Y = userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height;
		}
		rectangle.Width = Math.Abs(userBoxs[index].assistantLine.rect_L.Width);
		rectangle.Height = Math.Abs(userBoxs[index].assistantLine.rect_L.Height);
		if (rectangle.Contains(new Point(e.X, e.Y)))
		{
			userBoxs[index].assistantLine.move_LB = true;
		}
		if (userBoxs[index].assistantLine.rect_R.Width < 0 && userBoxs[index].assistantLine.rect_R.Height > 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width;
			rectangle.Y = userBoxs[index].assistantLine.rect_R.Y;
		}
		else if (userBoxs[index].assistantLine.rect_R.Width > 0 && userBoxs[index].assistantLine.rect_R.Height < 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_R.X;
			rectangle.Y = userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height;
		}
		else if (userBoxs[index].assistantLine.rect_R.Width < 0 && userBoxs[index].assistantLine.rect_R.Height < 0)
		{
			rectangle.X = userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width;
			rectangle.Y = userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height;
		}
		rectangle.Width = Math.Abs(userBoxs[index].assistantLine.rect_R.Width);
		rectangle.Height = Math.Abs(userBoxs[index].assistantLine.rect_R.Height);
		if (rectangle.Contains(new Point(e.X, e.Y)))
		{
			userBoxs[index].assistantLine.move_RB = true;
		}
	}

	// 유지보수 설명(복구 후 추가): 두 보조 사각형의 상대 X/Y 부호에 따라 함께 이동할 바깥 사각형을 구성한다. 좌표가 같은 축인 경우의 기존 else 분기도 그대로 유지한다.
	public Rectangle GetAssitantBoxForMoving(int index)
	{
		Rectangle result = default;
		int deltaX = userBoxs[index].assistantLine.rect_R.X - userBoxs[index].assistantLine.rect_L.X;
		int deltaY = userBoxs[index].assistantLine.rect_R.Y - userBoxs[index].assistantLine.rect_L.Y;
		if (deltaX > 0 && deltaY < 0)
		{
			result.X = userBoxs[index].assistantLine.rect_L.X;
			result.Y = userBoxs[index].assistantLine.rect_R.Y;
			result.Width = userBoxs[index].assistantLine.rect_R.X - userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_R.Width;
			result.Height = userBoxs[index].assistantLine.rect_L.Y - userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_L.Height;
		}
		else if (deltaX < 0 && deltaY < 0)
		{
			result.X = userBoxs[index].assistantLine.rect_R.X;
			result.Y = userBoxs[index].assistantLine.rect_R.Y;
			result.Width = userBoxs[index].assistantLine.rect_L.X - userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_L.Width;
			result.Height = userBoxs[index].assistantLine.rect_L.Y - userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_L.Height;
		}
		else if (deltaX < 0 && deltaY > 0)
		{
			result.X = userBoxs[index].assistantLine.rect_R.X;
			result.Y = userBoxs[index].assistantLine.rect_L.Y;
			result.Width = userBoxs[index].assistantLine.rect_L.X - userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_L.Width;
			result.Height = userBoxs[index].assistantLine.rect_R.Y - userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_R.Height;
		}
		else
		{
			result.X = userBoxs[index].assistantLine.rect_L.X;
			result.Y = userBoxs[index].assistantLine.rect_L.Y;
			result.Width = userBoxs[index].assistantLine.rect_R.X - userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_R.Width;
			result.Height = userBoxs[index].assistantLine.rect_R.Y - userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_R.Height;
		}
		return result;
	}

	// 유지보수 설명(복구 후 추가): 대각선 측정선의 클릭/이동 사각형을 만들고 좁은 축에는 20픽셀 여유를 둔다. 원본 메서드명 오탈자도 외부 계약으로 보존한다.
	public Rectangle GetMeasureLineRectForMoveing(int index)
	{
		Rectangle result = new Rectangle(0, 0, 0, 0);
		int deltaX = userBoxs[index].measureLine.pt2.X - userBoxs[index].measureLine.pt1.X;
		int deltaY = userBoxs[index].measureLine.pt2.Y - userBoxs[index].measureLine.pt1.Y;
		if (userBoxs[index].assistantLine.lineType == LineType.Virtical || userBoxs[index].assistantLine.lineType == LineType.Horizontal)
		{
			return result;
		}
		if (deltaX > 0 && deltaY < 0)
		{
			result.X = userBoxs[index].measureLine.pt1.X;
			result.Y = userBoxs[index].measureLine.pt2.Y;
		}
		else if (deltaX < 0 && deltaY < 0)
		{
			result.X = userBoxs[index].measureLine.pt2.X;
			result.Y = userBoxs[index].measureLine.pt2.Y;
		}
		else if (deltaX < 0 && deltaY > 0)
		{
			result.X = userBoxs[index].measureLine.pt2.X;
			result.Y = userBoxs[index].measureLine.pt1.Y;
		}
		else
		{
			result.X = userBoxs[index].measureLine.pt1.X;
			result.Y = userBoxs[index].measureLine.pt1.Y;
		}
		if (Math.Abs(userBoxs[index].measureLine.pt1.X - userBoxs[index].measureLine.pt2.X) < 20)
		{
			result.X = userBoxs[index].measureLine.pt1.X - 20;
			result.Width = Math.Abs(deltaX) + 40;
			result.Height = Math.Abs(deltaY);
		}
		else if (Math.Abs(userBoxs[index].measureLine.pt1.Y - userBoxs[index].measureLine.pt2.Y) < 20)
		{
			result.Y = userBoxs[index].measureLine.pt1.Y - 20;
			result.Width = Math.Abs(deltaX);
			result.Height = Math.Abs(deltaY) + 40;
		}
		else
		{
			result.Width = Math.Abs(deltaX);
			result.Height = Math.Abs(deltaY);
		}
		return result;
	}

	// 유지보수 설명(복구 후 추가): 선 종류별 보조 사각형의 가장자리/중심에서 측정 끝점을 선택한다. 화면 좌표에서 계산한 뒤 끝점 조작 사각형을 갱신한다.
	public void calcPosAutoMeasureLine(int index, LineType type)
	{
		switch (type)
		{
		case LineType.Virtical:
			userBoxs[index].measureLine.pt1.X = userBoxs[index].assistantLine.rect_L.X;
			userBoxs[index].measureLine.pt1.Y = userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height / 2;
			userBoxs[index].measureLine.pt2.X = userBoxs[index].assistantLine.rect_R.X;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height / 2;
			break;
		case LineType.Horizontal:
			userBoxs[index].measureLine.pt1.X = userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width / 2;
			userBoxs[index].measureLine.pt1.Y = userBoxs[index].assistantLine.rect_L.Y;
			userBoxs[index].measureLine.pt2.X = userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width / 2;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].assistantLine.rect_R.Y;
			break;
		case LineType.TypeA:
		case LineType.TypeB:
			userBoxs[index].measureLine.pt1.X = userBoxs[index].assistantLine.rect_L.X + userBoxs[index].assistantLine.rect_L.Width / 2;
			userBoxs[index].measureLine.pt1.Y = userBoxs[index].assistantLine.rect_L.Y + userBoxs[index].assistantLine.rect_L.Height / 2;
			userBoxs[index].measureLine.pt2.X = userBoxs[index].assistantLine.rect_R.X + userBoxs[index].assistantLine.rect_R.Width / 2;
			userBoxs[index].measureLine.pt2.Y = userBoxs[index].assistantLine.rect_R.Y + userBoxs[index].assistantLine.rect_R.Height / 2;
			break;
		}
		if (type != LineType.None)
		{
			userBoxs[index].measureLine.rect_LP = GetSizableRect_Measure_L(index);
			userBoxs[index].measureLine.rect_RP = GetSizableRect_Measure_R(index);
		}
	}

	// 유지보수 설명(복구 후 추가): 표시 픽셀의 두 끝점 거리를 zoomRatio로 나누고 mScale을 곱한 뒤 cm 선택은 /10, inch 선택은 /25.4로 환산한다. 원본의 0 또는 0.5 초과 조건과 소수 1자리 반올림을 유지한다. 작은 양수에서는 이전 문자열이 남을 수 있으며 실제 교정 정확도는 미검증이다.
	public void LineMeasure(int index)
	{
		if (userBoxs[index].measureLine.pt2.X != 0 || userBoxs[index].measureLine.pt2.Y != 0)
		{
			double displayDistancePx = Math.Sqrt(Math.Pow(userBoxs[index].measureLine.pt2.X - userBoxs[index].measureLine.pt1.X, 2.0) + Math.Pow(userBoxs[index].measureLine.pt2.Y - userBoxs[index].measureLine.pt1.Y, 2.0));
			double unitConvertedLength = 0.0;
			unitConvertedLength = ((!isUnit) ? (displayDistancePx * (1.0 / mZoomRatio) * mScale / 10.0) : (displayDistancePx * (1.0 / mZoomRatio) * mScale / 25.4));
			if (unitConvertedLength == 0.0 || unitConvertedLength > 0.5)
			{
				userBoxs[index].measureLine.measure_Len = Math.Round(unitConvertedLength, 1).ToString();
			}
			userBoxs[index].ResultDisplay = "[" + userBoxs[index].TypeItem + "] " + userBoxs[index].measureLine.measure_Len;
			userBoxs[index].measureLine.angle = GetAngleRadian(userBoxs[index].measureLine.pt1, userBoxs[index].measureLine.pt2);
		}
	}

	// 유지보수 설명(복구 후 추가): 화면 좌표 두 점의 방향을 Atan2로 구하며 출력 단위는 라디안이다. 화면 Y축 방향을 수학 좌표로 뒤집지 않는 원본 계약을 유지한다.
	private double GetAngleRadian(Point pt1, Point pt2)
	{
		double x = pt2.X - pt1.X;
		return Math.Atan2(pt2.Y - pt1.Y, x);
	}

	// 유지보수 설명(복구 후 추가): 같은 화면 좌표 방향을 도로 환산해 0~360 구간으로 정리한다. 라디안 사용 메서드와 단위를 혼동하지 않는다.
	private double GetAngleDegree(Point pt1, Point pt2)
	{
		double x = pt2.X - pt1.X;
		return (Math.Atan2(pt2.Y - pt1.Y, x) * (180.0 / Math.PI) + 360.0) % 360.0;
	}

	// 유지보수 설명(복구 후 추가): 각 측정 끝점에서 측정 각도±PI/2 방향의 보조선 끝점을 구하고 최소/최대 좌표로 사각형을 만든다. int 절삭과 기존 폭/높이 계산을 보존한다.
	public void calcAssistantBoxByRotate(int index)
	{
		double angle = userBoxs[index].measureLine.angle;
		double assistantHalfLengthPx = mAutoAssisLen;
		double firstPositiveNormalX = (double)userBoxs[index].measureLine.pt1.X + assistantHalfLengthPx * Math.Cos(angle + Math.PI / 2.0);
		double positiveNormalY = (double)userBoxs[index].measureLine.pt1.Y + assistantHalfLengthPx * Math.Sin(angle + Math.PI / 2.0);
		double negativeNormalX = (double)userBoxs[index].measureLine.pt1.X + assistantHalfLengthPx * Math.Cos(angle - Math.PI / 2.0);
		double negativeNormalY = (double)userBoxs[index].measureLine.pt1.Y + assistantHalfLengthPx * Math.Sin(angle - Math.PI / 2.0);
		int minX = Math.Min((int)firstPositiveNormalX, (int)negativeNormalX);
		int minY = Math.Min((int)positiveNormalY, (int)negativeNormalY);
		int maxX = Math.Max((int)firstPositiveNormalX, (int)negativeNormalX);
		int firstMaxY = Math.Max((int)positiveNormalY, (int)negativeNormalY);
		int width = maxX - minX;
		int height = firstMaxY - minY;
		userBoxs[index].assistantLine.rect_L = new Rectangle(minX, minY, width, height);
		double secondPositiveNormalX = (double)userBoxs[index].measureLine.pt2.X + assistantHalfLengthPx * Math.Cos(angle + Math.PI / 2.0);
		positiveNormalY = (double)userBoxs[index].measureLine.pt2.Y + assistantHalfLengthPx * Math.Sin(angle + Math.PI / 2.0);
		negativeNormalX = (double)userBoxs[index].measureLine.pt2.X + assistantHalfLengthPx * Math.Cos(angle - Math.PI / 2.0);
		negativeNormalY = (double)userBoxs[index].measureLine.pt2.Y + assistantHalfLengthPx * Math.Sin(angle - Math.PI / 2.0);
		minX = Math.Min((int)secondPositiveNormalX, (int)negativeNormalX);
		minY = Math.Min((int)positiveNormalY, (int)negativeNormalY);
		maxX = Math.Max((int)secondPositiveNormalX, (int)negativeNormalX);
		int secondMaxY = Math.Max((int)positiveNormalY, (int)negativeNormalY);
		width = maxX - minX;
		height = secondMaxY - minY;
		userBoxs[index].assistantLine.rect_R = new Rectangle(minX, minY, width, height);
	}
}
