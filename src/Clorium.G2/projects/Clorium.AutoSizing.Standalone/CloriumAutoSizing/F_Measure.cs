using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace CloriumAutoSizing;
public partial class F_Measure : Form
{
    private class ImageInfo
    {
        public Bitmap srcImage;
        public double zoomRatio = 1.0;
        public double scale;
        public string cameraID;
        public string host;
    }

    public class SendDataInfo
    {
        public string Vendor_aip_key = Environment.GetEnvironmentVariable("CLORIUM_AUTOSIZING_STANDALONE_VENDOR_API_KEY") ?? string.Empty;
        public string VendorID = System.Environment.GetEnvironmentVariable("CLORIUM_AUTOSIZING_STANDALONE_VENDOR_ID") ?? string.Empty;
        public string URL = "";
        public string JsonData = "";
    }

    private enum ColorAdjustmentType
    {
        Brightness,
        Contrast,
        Illumination
    }

    private enum CommandType
    {
        CMD_POST_Folder,
        CMD_POST_Measure,
        CMD_GET_SizeTypeID,
        RES_NoData
    }

    public class JsonData_MeasureInform
    {
        public string barcodeId { get; set; }
        public string cameraUnit { get; set; }
        public string folder { get; set; }
        public string imageFolder { get; set; }
        public string[] sizeFiles { get; set; }
        public string[] images { get; set; }
        public Measureitem measureItemGroup { get; set; }
    }

    public class Measureitem
    {
        public int typeId { get; set; }
        public Item[] items { get; set; }
    }

    public class Item
    {
        public string name { get; set; }
        public float value { get; set; }
    }

    public class JsonData_SizeType
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
        public int code { get; set; }
    }

    public class Data
    {
        public int sizeTypeId { get; set; }
        public string sizeTypeName { get; set; }
    }

    public class Meta
    {
        public string result { get; set; }
    }

    // 유지보수 설명(복구 후 추가): 전송 JSON의 float 값을 문자열로 기록하는 기존 계약이다. 속성명과 숫자 문자열 형식은 서버와 연결되므로 임의 변경하지 않는다.
    public class FloatToStringConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return float.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("F2"));
        }
    }

    private const string itemsfilename = "D:\\Clorium Data\\Clorium_MeasureItems.txt";
    public Bitmap ExposureImage;
    private string pathfilename;
    private string filename;
    private string CSVfilename;
    private string Measurefilename;
    private UserRect userBox;
    private ImageInfo imageInfo = new ImageInfo();
    public SendDataInfo APISendData = new SendDataInfo();
    private Form3 Config = new Form3();
    private int originalButtonPanelX;
    private int originalButtonPanelY;
    private int originalRichTextBoxX;
    private int originalRichTextBoxY;
    private int originalRichTextBoxWidth;
    private int originalRichTextBoxHeight;
    private JsonData_MeasureInform Data_MeasureInform;
    private JsonData_SizeType Data_SizeType;
    private const int MAX_MEASUREITEM = 10;
    private const int MAX_ITEM = 20;
    public F_Measure()
    {
        InitializeComponent();
        string windowTitle = "Clorium AutoSizing 배포판 ver-3.0.0";
        Text = windowTitle;
        KeyPreview = true;
        userBox = new UserRect(pictureBox1);
        InitArray();
    }

    private void F_Measure_Load(object sender, EventArgs e)
    {
        FormInit();
        DisplayHint();
        LoadItems();
    }

    private void FormInit()
    {
        FormPosition();
        Image image = new Bitmap(Image.FromFile("D:\\Clorium Data\\Clorium_Measure_Logo.jpg"), panel2.Size);
        pictureBox1.Image = image;
        pictureBox1.Location = new Point(0, 0);
        pictureBox1.Size = panel2.Size;
        pictureBox1.AllowDrop = true;
        SaveOriginalPanelPositions();
        ReadSettings();
        CheckMenuExpend();
        Location = new Point(50, 0);
    }

    private void FormPosition()
    {
        Screen[] allScreens = Screen.AllScreens;
        if (allScreens.Length > 1)
        {
            Rectangle bounds = allScreens[1].Bounds;
            Left = bounds.X;
            Top = bounds.Y;
            WindowState = FormWindowState.Maximized;
        }
    }

    private void ReadSettings()
    {
        cB_Crop.Checked = Form3.UserSettingManager.Instance.Settings.crop;
        cB_Unit.Checked = Form3.UserSettingManager.Instance.Settings.unit_inch;
        userBox.lineColor = Form3.UserSettingManager.Instance.Settings.lineColor;
        userBox.fontColor = Form3.UserSettingManager.Instance.Settings.fontColor;
        userBox.baseColor = Form3.UserSettingManager.Instance.Settings.baseColor;
        userBox.FontSize = Form3.UserSettingManager.Instance.Settings.fontSize;
        userBox.mManualAssisLen = double.Parse(Form3.UserSettingManager.Instance.Settings.manualAssistantLen);
        userBox.mAutoAssisLen = double.Parse(Form3.UserSettingManager.Instance.Settings.autoAssistantLen);
        panel_Color.BackColor = userBox.lineColor;
        panel_FontColor.BackColor = userBox.fontColor;
        panel_BaseColor.BackColor = ((SolidBrush)userBox.baseColor).Color;
        cB_Unit_CheckedChanged(this, null);
        if (Form3.UserSettingManager.Instance.Settings.useApi)
        {
            btn_SendAPI.ForeColor = SystemColors.ControlText;
        }
        else
        {
            btn_SendAPI.ForeColor = SystemColors.ScrollBar;
        }

        if (Form3.UserSettingManager.Instance.Settings.useAddScale)
        {
            textBox_AddScale.Visible = true;
            btn_SetAddScale.Visible = true;
        }
        else
        {
            textBox_AddScale.Visible = false;
            btn_SetAddScale.Visible = false;
        }
    }

    private void DisplayHint()
    {
        ToolTip toolTip = new ToolTip();
        toolTip.AutoPopDelay = 1500;
        toolTip.InitialDelay = 100;
        toolTip.ReshowDelay = 300;
        toolTip.ShowAlways = true;
        toolTip.IsBalloon = true;
        toolTip.SetToolTip(btn_ZoomIn, "확대");
        toolTip.SetToolTip(btn_ZoomOut, "축소");
        toolTip.SetToolTip(btn_Original, "원본");
        toolTip.SetToolTip(btn_LineMeasure, "자동 측정");
        toolTip.SetToolTip(panel_Color, "보조선 색상");
        toolTip.SetToolTip(panel_FontColor, "숫자 색상");
        toolTip.SetToolTip(panel_BaseColor, "바탕 표시 색상");
        toolTip.SetToolTip(label_Type, "아래 보조선 유형 선택");
        toolTip.SetToolTip(cB_Crop, "잘라내기 박스 표시");
        toolTip.SetToolTip(cB_Unit, "측정 단위 환산");
        toolTip.SetToolTip(cB_Manual, "수동 측정");
        toolTip.SetToolTip(btn_EditItem, "측정 항목 편집기");
        toolTip.SetToolTip(cB_Move, "치수선 그룹 이동 선택 : CTRL+D");
        toolTip.SetToolTip(btn_Delete, "치수선 번호 그룹 삭제 : ESC");
        toolTip.SetToolTip(btn_ImageRotate, "이미지 회전");
        toolTip.SetToolTip(btn_ColorFilter, "색상 보정");
        toolTip.SetToolTip(btn_SetAddScale, "스케일 보정값");
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        lb_pixel.Text = "픽셀 : (" + userBox.oldX + "," + userBox.oldY + ")";
    }

    private void F_Measure_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            btn_Delete_Click(sender, e);
        }
        else if (e.Control && e.KeyCode == Keys.D)
        {
            if (cB_Move.Checked)
            {
                cB_Move.Checked = false;
            }
            else
            {
                cB_Move.Checked = true;
            }

            cB_Move_Click(sender, e);
        }
        else if (e.KeyCode == Keys.ShiftKey && !userBox.isShiftPressed)
        {
            userBox.isShiftPressed = true;
        }
    }

    private void F_Measure_KeyUp(object sender, KeyEventArgs e)
    {
        userBox.isShiftPressed = false;
    }

    private void comboBox_DisplayType_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (comboBox_DisplayType.SelectedItem == null)
        {
            return;
        }

        try
        {
            using (StreamReader streamReader = new StreamReader("D:\\Clorium Data\\Clorium_MeasureItems.txt"))
            {
                string text;
                while ((text = streamReader.ReadLine()) != null)
                {
                    if (!(text == "[타입]") || !(streamReader.ReadLine() == comboBox_DisplayType.SelectedItem.ToString()))
                    {
                        continue;
                    }

                    if (streamReader.ReadLine() == "[ID]")
                    {
                        string text2 = streamReader.ReadLine();
                        textBox_TypeID.Text = text2;
                    }

                    if (!(streamReader.ReadLine() == "[항목]"))
                    {
                        continue;
                    }

                    listBox_DisplayItems.Items.Clear();
                    string text3 = streamReader.ReadLine();
                    while (text3 != null && !text3.Contains("-"))
                    {
                        if (!listBox_DisplayItems.Items.Contains(text3))
                        {
                            listBox_DisplayItems.Items.Add(text3.Trim());
                        }

                        text3 = streamReader.ReadLine();
                    }
                }
            }

            textBox_Type.Text = comboBox_DisplayType.SelectedItem.ToString();
            listBox_DisplayItems.SelectedIndex = 0;
            textBox_TypeItem.Text = listBox_DisplayItems.SelectedItem.ToString();
            textBox_Length.Text = "";
            textBox_Item_Leave(this, null);
            textBox_Length_Leave(this, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show("정보를 읽어오는 중 에러가 발생했습니다.\r\n" + ex.ToString());
        }
    }

    private void listBox_DisplayItems_SelectedIndexChanged(object sender, EventArgs e)
    {
        userBox.mIndex = listBox_DisplayItems.SelectedIndex;
        textBox_Item_Enter(this, null);
        textBox_TypeItem.Text = listBox_DisplayItems.SelectedItem.ToString();
        textBox_Length.Text = "";
        textBox_Length_Leave(this, null);
        userBox.userBoxs[userBox.mIndex].TypeItem = listBox_DisplayItems.SelectedItem.ToString();
        userBox.SetShowHelpLine(userBox.mIndex);
        CheckMenuExpend();
        pictureBox1.Invalidate();
    }

    private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
        if (!CheckCondition())
        {
            userBox.userBoxs[userBox.mIndex].measureLine.pt1 = new Point(0, 0);
            userBox.userBoxs[userBox.mIndex].measureLine.pt2 = new Point(0, 0);
            pictureBox1.Invalidate();
        }
        else
        {
            textBox_Length_Enter(this, null);
            textBox_Length.Text = userBox.userBoxs[userBox.mIndex].measureLine.measure_Len.ToString();
            userBox.userBoxs[userBox.mIndex].Type = textBox_Type.Text;
            userBox.userBoxs[userBox.mIndex].TypeID = textBox_TypeID.Text;
            userBox.userBoxs[userBox.mIndex].TypeItem = textBox_TypeItem.Text;
        }
    }

    private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
        if (userBox.isDrawing && !(userBox.userBoxs[userBox.mIndex].measureLine.measure_Len.ToString() == "0"))
        {
            textBox_Length_Enter(this, null);
            textBox_Length.Text = userBox.userBoxs[userBox.mIndex].measureLine.measure_Len.ToString();
        }
    }

    private void btn_Setting_Click(object sender, EventArgs e)
    {
        if (Application.OpenForms.OfType<Form3>().FirstOrDefault() == null)
        {
            Form3 form = new Form3
            {
                StartPosition = FormStartPosition.Manual
            };
            int x = Location.X;
            int y = Location.Y;
            form.Location = new Point(x + 245, y + 30);
            if (form.ShowDialog() == DialogResult.OK)
            {
                ReadSettings();
                CheckMenuExpend();
                DisplayMeasureResult();
            }
        }
    }

    private void btn_SetAddScale_Click(object sender, EventArgs e)
    {
        if (double.TryParse(textBox_AddScale.Text, out var result))
        {
            if (result >= 1.0)
            {
                MessageBox.Show("소숫점 자리 숫자만 입력 가능합니다.");
                return;
            }

            imageInfo.scale += result;
            userBox.mScale = imageInfo.scale;
            richTextBox1.AppendText("New Scale : " + imageInfo.scale + "\r\n");
            richTextBox1.ScrollToCaret();
        }
        else
        {
            MessageBox.Show("소수점 숫자를 입력하세요.");
        }
    }

    private void btn_ImageRotate_Click(object sender, EventArgs e)
    {
        if (pictureBox1.Image != null)
        {
            Image image = pictureBox1.Image;
            Image image2 = (Image)image.Clone();
            image2.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox1.Image = image2;
            image.Dispose();
        }
    }

    private void btn_ColorFilter_Click(object sender, EventArgs e)
    {
        if (imageInfo.srcImage != null && Application.OpenForms.OfType<Form4>().FirstOrDefault() == null)
        {
            Form4 form = new Form4(this);
            form.StartPosition = FormStartPosition.Manual;
            Point location = panel2.PointToScreen(Point.Empty);
            location.X += pictureBox1.Width;
            form.Location = location;
            if (form.Width > panel2.Width)
            {
                form.Width = panel2.Width;
            }

            if (form.Height > panel2.Height)
            {
                form.Height = panel2.Height;
            }

            form.ShowDialog();
        }
    }

    private void SaveOriginalPanelPositions()
    {
        originalButtonPanelX = pan_Button.Location.X;
        originalButtonPanelY = pan_Button.Location.Y;
        originalRichTextBoxX = richTextBox1.Location.X;
        originalRichTextBoxY = richTextBox1.Location.Y;
        originalRichTextBoxWidth = richTextBox1.Width;
        originalRichTextBoxHeight = richTextBox1.Height;
    }

    private void ShowPanel()
    {
        pan_Function.Visible = true;
        label11.Visible = true;
        pan_Button.Location = new Point(originalButtonPanelX, originalButtonPanelY);
        richTextBox1.Location = new Point(originalRichTextBoxX, originalRichTextBoxY);
        richTextBox1.Height = originalRichTextBoxHeight;
    }

    private void HidePanel()
    {
        pan_Function.Visible = false;
        label11.Visible = false;
        pan_Button.Location = new Point(pan_Function.Location.X + 1, pan_Function.Location.Y);
        richTextBox1.Location = new Point(pan_Function.Location.X + 3, pan_Function.Location.Y + pan_Button.Height);
        richTextBox1.Height = originalRichTextBoxHeight + pan_Function.Height + 7;
    }

    private void btn_FileOpen_Click(object sender, EventArgs e)
    {
        string text4;
        if (!Form3.UserSettingManager.Instance.Settings.useFolder)
        {
            string text = DateTime.Now.ToString("yyyy");
            string text2 = DateTime.Now.ToString("MM");
            string text3 = DateTime.Now.ToString("dd");
            text4 = Path.Combine("D:\\Clorium Images\\", text.ToString() + "년" + text2.ToString() + "월\\", text3.ToString() + "일\\");
        }
        else
        {
            text4 = Form3.UserSettingManager.Instance.Settings.initFolder;
        }

        if (!Directory.Exists(text4))
        {
            Directory.CreateDirectory(text4);
        }

        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = text4;
        openFileDialog.Filter = "Image files (*.jpeg)|*.jpeg;|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            InitialImage(openFileDialog.FileName);
        }
    }

    private void InitialImage(string imagename)
    {
        richTextBox1.Clear();
        InitImageInform();
        pathfilename = imagename;
        filename = Path.GetFileNameWithoutExtension(imagename);
        imageInfo.srcImage = LoadImageWithOrientation(imagename);
        ReadMetadata(pathfilename, out imageInfo.cameraID, out imageInfo.scale, out imageInfo.host);
        userBox.mScale = imageInfo.scale;
        lb_filename.Text = "파일 : " + filename + Path.GetExtension(imagename);
        lb_imgsize.Text = "크기 : (" + imageInfo.srcImage.Width + "," + imageInfo.srcImage.Height + ")";
        lb_Scale.Text = "단위 : " + imageInfo.scale.ToString("F4") + " mm/pixel";
        richTextBox1.AppendText("Host : " + imageInfo.host + "\r\n");
        richTextBox1.AppendText("Camera ID : " + imageInfo.cameraID + "\r\n");
        richTextBox1.ScrollToCaret();
        Text = Text + " : " + Path.GetDirectoryName(pathfilename) + "\\" + lb_filename.Text;
        btn_ZoomOut_Click(this, null);
        btn_ZoomOut_Click(this, null);
        userBox.mZoomRatio = imageInfo.zoomRatio;
        ExposureImage = new Bitmap(pictureBox1.Image);
        CSVfilename = Path.GetDirectoryName(pathfilename) + "\\" + Path.GetFileNameWithoutExtension(pathfilename) + ".csv";
        Measurefilename = Path.GetDirectoryName(pathfilename) + "\\" + Path.GetFileNameWithoutExtension(pathfilename) + "Size.jpg";
        if (File.Exists(CSVfilename) && MessageBox.Show("이미 csv 파일이 존재합니다.\n\r기존 파일을 지우고 새로 작성하시겠습니까?", "확인", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            File.Delete(CSVfilename);
        }

        listBox_DisplayItems.SelectedIndex = 0;
        userBox.mIndex = listBox_DisplayItems.SelectedIndex;
        cB_Crop.Checked = false;
        textBox_Type.Text = comboBox_DisplayType.SelectedItem.ToString();
        textBox_TypeItem.Text = listBox_DisplayItems.SelectedItem.ToString();
        textBox_Length.Text = "";
        userBox.userBoxs[userBox.mIndex].TypeItem = textBox_TypeItem.Text;
        textBox_Item_Leave(this, null);
        textBox_Length_Leave(this, null);
        CheckMenuExpend();
        if (Form3.UserSettingManager.Instance.Settings.useApi)
        {
            if (Form3.UserSettingManager.Instance.Settings.ApiHostName != "")
            {
                imageInfo.host = Form3.UserSettingManager.Instance.Settings.ApiHostName;
            }

            API_GetSizeTypeID();
            SetDisplayTypeIndex(Data_SizeType.data.sizeTypeName);
        }
    }

    private void SetDisplayTypeIndex(string type)
    {
        for (int i = 0; i < comboBox_DisplayType.Items.Count; i++)
        {
            if (type == comboBox_DisplayType.Items[i].ToString())
            {
                comboBox_DisplayType.SelectedIndex = i;
                break;
            }
        }
    }

    private void InitImageInform()
    {
        imageInfo.zoomRatio = 1.0;
        userBox.mZoomRatio = imageInfo.zoomRatio;
        userBox.ClearAllArray();
        userBox.SetShowHelpLine(userBox.mIndex);
        userBox.isBaseColor = false;
        richTextBox1.AppendText("Ratio : " + imageInfo.zoomRatio + "\r\n");
        richTextBox1.ScrollToCaret();
    }

    // 유지보수 설명(복구 후 추가): 이미지 Title/Subject/Comment를 카메라 ID/측정 스케일/API 호스트로 읽는다. Subject는 double로 변환하며 없는 경우와 읽기 실패 시 원본 기본값을 사용한다. 실제 카메라 스케일의 교정 정확도는 이 복구 작업에서 검증하지 않았다.
    private void ReadMetadata(string fullpathfilename, out string cameraID, out double scale, out string host)
    {
        try
        {
            using FileStream bitmapStream = new FileStream(new FileInfo(fullpathfilename).FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            BitmapMetadata bitmapMetadata = (BitmapMetadata)BitmapFrame.Create(bitmapStream).Metadata;
            if (bitmapMetadata.Title == null)
            {
                cameraID = "None";
            }
            else
            {
                cameraID = bitmapMetadata.Title;
            }

            if (bitmapMetadata.Subject == null)
            {
                scale = 1.0;
            }
            else
            {
                scale = Convert.ToDouble(bitmapMetadata.Subject);
            }

            if (bitmapMetadata.Comment == null)
            {
                host = "None";
            }
            else
            {
                host = bitmapMetadata.Comment;
            }
        }
        catch (Exception ex)
        {
            cameraID = "None";
            scale = 1.0;
            host = "None";
            MessageBox.Show("메타데이터를 읽는 중 오류가 발생했습니다.\r\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
    }

    private void pictureBox1_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect |= DragDropEffects.Copy;
        }
    }

    private void pictureBox1_DragDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        string[] droppedFilePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (string text in droppedFilePaths)
        {
            if (text.EndsWith("jpeg") || text.EndsWith("JPEG"))
            {
                InitialImage(text);
            }
        }
    }

    private void SetExifOrientation(string jpgPath, ushort orientationValue)
    {
        using Image image = Image.FromFile(jpgPath);
        PropertyItem propertyItem = image.PropertyItems.FirstOrDefault((PropertyItem p) => p.Id == 274);
        if (propertyItem == null)
        {
            propertyItem = image.PropertyItems[0];
            propertyItem.Id = 274;
            propertyItem.Type = 3;
            propertyItem.Len = 2;
            propertyItem.Value = new byte[2];
        }

        propertyItem.Value[0] = (byte)(orientationValue & 0xFF);
        propertyItem.Value[1] = (byte)((orientationValue >> 8) & 0xFF);
        image.SetPropertyItem(propertyItem);
        string sourceFileName = jpgPath + ".tmp";
        image.Save(sourceFileName, ImageFormat.Jpeg);
        image.Dispose();
        File.Delete(jpgPath);
        File.Move(sourceFileName, jpgPath);
    }

    private ushort GetExifOrientation(string jpgPath)
    {
        using (Image image = Image.FromFile(jpgPath))
        {
            PropertyItem propertyItem = image.PropertyItems.FirstOrDefault((PropertyItem p) => p.Id == 274);
            if (propertyItem != null && propertyItem.Value.Length >= 2)
            {
                return BitConverter.ToUInt16(propertyItem.Value, 0);
            }
        }

        return 1;
    }

    // 유지보수 설명(복구 후 추가): EXIF 방향 274의 값 3/6/8에 대해 회전한 새 Bitmap을 반환한다. 그 외 방향 값의 기존 처리와 입력 파일 스트림 해제 흐름을 보존한다.
    private Bitmap LoadImageWithOrientation(string jpgPath)
    {
        using FileStream fileStream = new FileStream(jpgPath, FileMode.Open, FileAccess.Read);
        using MemoryStream memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        using Image image = Image.FromStream(memoryStream);
        PropertyItem propertyItem = image.PropertyItems.FirstOrDefault((PropertyItem p) => p.Id == 274);
        if (propertyItem != null && propertyItem.Value.Length >= 2)
        {
            ushort exifOrientation = BitConverter.ToUInt16(propertyItem.Value, 0);
            RotateFlipType rotateFlipType = RotateFlipType.RotateNoneFlipNone;
            switch (exifOrientation)
            {
                case 3:
                    rotateFlipType = RotateFlipType.Rotate180FlipNone;
                    break;
                case 6:
                    rotateFlipType = RotateFlipType.Rotate90FlipNone;
                    break;
                case 8:
                    rotateFlipType = RotateFlipType.Rotate270FlipNone;
                    break;
            }

            image.RotateFlip(rotateFlipType);
        }

        return new Bitmap(image);
    }

    // 유지보수 설명(복구 후 추가): 표시 크기는 원본 픽셀 수에 zoomRatio를 곱해 반올림한다. 폭/높이가 10~10000 픽셀 밖이면 이번 확대율 변경을 되돌리며 원본 이미지 자체는 유지한다.
    private void zoom(bool mode)
    {
        if (mode)
        {
            imageInfo.zoomRatio *= 1.5;
            if (imageInfo.zoomRatio > 100.0)
            {
                imageInfo.zoomRatio = 100.0;
            }
        }
        else
        {
            imageInfo.zoomRatio *= 0.5;
        }

        int displayWidthPx = (int)Math.Round((double)imageInfo.srcImage.Width * imageInfo.zoomRatio);
        int displayHeightPx = (int)Math.Round((double)imageInfo.srcImage.Height * imageInfo.zoomRatio);
        if (displayWidthPx > 10000 || displayWidthPx < 10 || displayHeightPx > 10000 || displayHeightPx < 10)
        {
            if (mode)
            {
                imageInfo.zoomRatio /= 1.5;
            }
            else
            {
                imageInfo.zoomRatio /= 0.5;
            }
        }
        else
        {
            Bitmap image = new Bitmap(newSize: new Size(displayWidthPx, displayHeightPx), original: imageInfo.srcImage);
            pictureBox1.Image = image;
        }
    }

    private void btn_ZoomIn_Click(object sender, EventArgs e)
    {
        if (pathfilename != null)
        {
            zoom(mode: true);
            userBox.mZoomRatio = imageInfo.zoomRatio;
            richTextBox1.AppendText("Ratio : " + imageInfo.zoomRatio + "\r\n");
            richTextBox1.ScrollToCaret();
        }
    }

    private void btn_ZoomOut_Click(object sender, EventArgs e)
    {
        if (pathfilename != null)
        {
            zoom(mode: false);
            userBox.mZoomRatio = imageInfo.zoomRatio;
            richTextBox1.AppendText("Ratio : " + imageInfo.zoomRatio + "\r\n");
            richTextBox1.ScrollToCaret();
        }
    }

    private void btn_Original_Click(object sender, EventArgs e)
    {
        if (pathfilename != null)
        {
            pictureBox1.Image = imageInfo.srcImage;
            imageInfo.zoomRatio = 1.0;
            userBox.mZoomRatio = imageInfo.zoomRatio;
            richTextBox1.AppendText("Ratio : " + imageInfo.zoomRatio + "\r\n");
            richTextBox1.ScrollToCaret();
        }
    }

    private void cB_Crop_CheckedChanged(object sender, EventArgs e)
    {
        userBox.isCropped = cB_Crop.Checked;
        int cropWidthPx = pictureBox1.Width - 600;
        int cropHeightPx = pictureBox1.Height - 600;
        int x = pictureBox1.Width / 2 - cropWidthPx / 2;
        int y = pictureBox1.Height / 2 - cropHeightPx / 2;
        userBox.rect_crop = new Rectangle(x, y, cropWidthPx, cropHeightPx);
        pictureBox1.Invalidate();
    }

    // 유지보수 설명(복구 후 추가): 화면의 자르기 사각형을 확대율로 나누어 원본 픽셀 좌표로 옮긴다. int 변환의 절삭과 범위 오류 처리 순서는 원본대로 유지한다.
    private void btn_Crop_Click(object sender, EventArgs e)
    {
        if (!cB_Crop.Checked)
        {
            return;
        }

        try
        {
            Rectangle rect = new Rectangle
            {
                X = (int)((double)userBox.rect_crop.X / imageInfo.zoomRatio),
                Y = (int)((double)userBox.rect_crop.Y / imageInfo.zoomRatio),
                Width = (int)((double)userBox.rect_crop.Width / imageInfo.zoomRatio),
                Height = (int)((double)userBox.rect_crop.Height / imageInfo.zoomRatio)
            };
            imageInfo.srcImage = imageInfo.srcImage.Clone(rect, imageInfo.srcImage.PixelFormat);
            pictureBox1.Image = imageInfo.srcImage;
            cB_Crop.Checked = false;
            lb_imgsize.Text = "크기 : (" + imageInfo.srcImage.Width + "," + imageInfo.srcImage.Height + ")";
            InitImageInform();
        }
        catch
        {
            MessageBox.Show("잘라내기를 할 수 없습니다.");
            pictureBox1.Invalidate();
        }
    }

    private void cB_Move_Click(object sender, EventArgs e)
    {
        if (cB_Move.Checked)
        {
            cB_Move.Text = "\u3000[ON] 이동";
            cB_Move.BackColor = Color.Green;
            cB_Move.ForeColor = Color.White;
        }
        else
        {
            cB_Move.Text = "   그룹 이동";
            cB_Move.BackColor = Color.DarkGray;
            cB_Move.ForeColor = Color.Black;
        }

        userBox.isMove_All = cB_Move.Checked;
        userBox.SetShowHelpLine(userBox.mIndex);
        pictureBox1.Invalidate();
    }

    private void btn_SetColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_Color.BackColor = colorDialog.Color;
            userBox.lineColor = colorDialog.Color;
            pictureBox1.Invalidate();
        }
    }

    private void btn_SetFontColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_FontColor.BackColor = colorDialog.Color;
            userBox.fontColor = colorDialog.Color;
            pictureBox1.Invalidate();
        }
    }

    private void btn_BaseColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_BaseColor.BackColor = colorDialog.Color;
            userBox.baseColor = new SolidBrush(colorDialog.Color);
            userBox.isBaseColor = true;
            pictureBox1.Invalidate();
        }
    }

    private void btn_Delete_Click(object sender, EventArgs e)
    {
        userBox.ClearArrayMeasure(userBox.mIndex);
        userBox.SetShowHelpLine(userBox.mIndex);
        userBox.userBoxs[userBox.mIndex].TypeItem = textBox_TypeItem.Text;
        CheckMenuExpend();
        pictureBox1.Invalidate();
    }

    // 유지보수 설명(복구 후 추가): 선 종류별 초기 보조 사각형과 측정 끝점을 화면 픽셀 좌표로 구성한다. 외부에 쓰이는 LineType 값과 기존 종류 이름은 보존한다.
    private void Select_Type(UserRect.LineType type)
    {
        int initialLeftPx = 25;
        int initialTopPx = 20;
        int assistantSeparationPx = 200;
        int diagonalEndpointInsetPx = 30;
        int firstAssistantLeftPx = 0;
        int firstAssistantTopPx = 0;
        int secondAssistantLeftPx = 0;
        int secondAssistantTopPx = 0;
        int assistantWidthPx = 0;
        int assistantHeightPx = 0;
        Point pt = default;
        Point pt2 = default;
        switch (type)
        {
            case UserRect.LineType.Virtical:
                firstAssistantLeftPx = initialLeftPx;
                firstAssistantTopPx = initialTopPx;
                secondAssistantLeftPx = firstAssistantLeftPx + assistantSeparationPx;
                secondAssistantTopPx = initialTopPx;
                assistantWidthPx = 10;
                assistantHeightPx = 50;
                pt.X = firstAssistantLeftPx;
                pt.Y = firstAssistantTopPx + assistantHeightPx / 2;
                pt2.X = secondAssistantLeftPx;
                pt2.Y = secondAssistantTopPx + assistantHeightPx / 2;
                break;
            case UserRect.LineType.Horizontal:
                firstAssistantLeftPx = initialLeftPx;
                firstAssistantTopPx = initialTopPx;
                secondAssistantLeftPx = initialLeftPx;
                secondAssistantTopPx = firstAssistantTopPx + assistantSeparationPx;
                assistantWidthPx = 50;
                assistantHeightPx = 10;
                pt.X = firstAssistantLeftPx + assistantWidthPx / 2;
                pt.Y = firstAssistantTopPx;
                pt2.X = secondAssistantLeftPx + assistantWidthPx / 2;
                pt2.Y = secondAssistantTopPx;
                break;
            case UserRect.LineType.TypeA:
            case UserRect.LineType.TypeB:
                firstAssistantLeftPx = initialLeftPx;
                firstAssistantTopPx = initialTopPx;
                secondAssistantLeftPx = firstAssistantLeftPx + assistantSeparationPx;
                secondAssistantTopPx = initialTopPx;
                assistantWidthPx = 20;
                assistantHeightPx = 50;
                pt.X = firstAssistantLeftPx + diagonalEndpointInsetPx;
                pt.Y = firstAssistantTopPx + assistantHeightPx / 2;
                pt2.X = secondAssistantLeftPx - diagonalEndpointInsetPx;
                pt2.Y = secondAssistantTopPx + assistantHeightPx / 2;
                break;
        }

        userBox.mZoomRatio = imageInfo.zoomRatio;
        userBox.isBaseColor = false;
        cB_Move.Checked = false;
        cB_Move_Click(this, null);
        userBox.InitPosAssistantRect(userBox.mIndex, type, new Rectangle(firstAssistantLeftPx, firstAssistantTopPx, assistantWidthPx, assistantHeightPx), new Rectangle(secondAssistantLeftPx, secondAssistantTopPx, assistantWidthPx, assistantHeightPx));
        userBox.InitPosMeasureRect(userBox.mIndex, pt, pt2);
        userBox.LineMeasure(userBox.mIndex);
        pictureBox1.Invalidate();
    }

    private bool CheckCondition()
    {
        if (textBox_TypeItem.Text == "항목 입력")
        {
            MessageBox.Show("항목을 선택하세요.");
            return false;
        }

        userBox.userBoxs[userBox.mIndex].Type = textBox_Type.Text;
        userBox.userBoxs[userBox.mIndex].TypeID = textBox_TypeID.Text;
        userBox.userBoxs[userBox.mIndex].TypeItem = textBox_TypeItem.Text;
        return true;
    }

    private void btn_Virtical_Click(object sender, EventArgs e)
    {
        if (CheckCondition())
        {
            Select_Type(UserRect.LineType.Virtical);
        }
    }

    private void btn_Horizontal_Click(object sender, EventArgs e)
    {
        if (CheckCondition())
        {
            Select_Type(UserRect.LineType.Horizontal);
        }
    }

    private void btn_TypeB_Click(object sender, EventArgs e)
    {
        if (CheckCondition())
        {
            Select_Type(UserRect.LineType.TypeB);
        }
    }

    private void btn_LineMeasure_Click(object sender, EventArgs e)
    {
        if (textBox_TypeItem.Text == "항목 입력" || textBox_TypeItem.Text == "")
        {
            MessageBox.Show("먼저 측정 항목을 선택하세요.");
            return;
        }

        userBox.mZoomRatio = imageInfo.zoomRatio;
        userBox.calcPosAutoMeasureLine(userBox.mIndex, userBox.userBoxs[userBox.mIndex].assistantLine.lineType);
        userBox.userBoxs[userBox.mIndex].measureLine.rect_box = userBox.GetMeasureLineRectForMoveing(userBox.mIndex);
        userBox.LineMeasure(userBox.mIndex);
        userBox.calcAssistantBoxByRotate(userBox.mIndex);
        textBox_Length_Enter(this, null);
        textBox_Length.Text = userBox.userBoxs[userBox.mIndex].measureLine.measure_Len.ToString();
        pictureBox1.Invalidate();
    }

    private void textBox_Length_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            userBox.userBoxs[userBox.mIndex].measureLine.measure_Len = textBox_Length.Text;
            pictureBox1.Invalidate();
        }
    }

    private void CheckMenuExpend()
    {
        if (Form3.UserSettingManager.Instance.Settings.useMenuExpend)
        {
            ShowPanel();
            cB_Manual.Checked = false;
        }
        else
        {
            HidePanel();
            cB_Manual.Checked = true;
        }

        checkBox_Manual_CheckedChanged(this, null);
    }

    private void checkBox_Manual_CheckedChanged(object sender, EventArgs e)
    {
        if (cB_Manual.Checked)
        {
            cB_Manual.Text = "    [ON]";
            cB_Manual.BackColor = Color.Green;
            cB_Manual.ForeColor = Color.White;
            userBox.userBoxs[userBox.mIndex].measureLine.isManualOK = true;
            userBox.userBoxs[userBox.mIndex].assistantLine.lineType = UserRect.LineType.Manual;
        }
        else
        {
            cB_Manual.Text = "    수동";
            cB_Manual.BackColor = Color.White;
            cB_Manual.ForeColor = Color.Black;
            textBox_Length.Text = "";
            textBox_Length_Leave(this, null);
            userBox.ClearArrayMeasure(userBox.mIndex);
            userBox.userBoxs[userBox.mIndex].measureLine.isManualOK = false;
            userBox.userBoxs[userBox.mIndex].assistantLine.lineType = UserRect.LineType.None;
        }

        userBox.isBaseColor = false;
        userBox.isManual = cB_Manual.Checked;
        userBox.mZoomRatio = imageInfo.zoomRatio;
        pictureBox1.Invalidate();
    }

    private void textBox_Item_Enter(object sender, EventArgs e)
    {
        if (textBox_TypeItem.ForeColor == Color.LightGray)
        {
            textBox_TypeItem.Text = "";
            textBox_TypeItem.ForeColor = Color.Black;
        }
    }

    private void textBox_Item_Leave(object sender, EventArgs e)
    {
        if (textBox_TypeItem.Text == "")
        {
            textBox_TypeItem.Text = "항목 입력";
            textBox_TypeItem.ForeColor = Color.LightGray;
        }
    }

    private void textBox_Length_Enter(object sender, EventArgs e)
    {
        if (textBox_Length.ForeColor == Color.LightGray)
        {
            textBox_Length.Text = "";
            textBox_Length.ForeColor = Color.Black;
        }
    }

    private void textBox_Length_Leave(object sender, EventArgs e)
    {
        if (textBox_Length.Text == "")
        {
            textBox_Length.Text = "치수 입력";
            textBox_Length.ForeColor = Color.LightGray;
        }
    }

    private void textBox_AddScale_Enter(object sender, EventArgs e)
    {
        if (textBox_AddScale.ForeColor == Color.LightGray)
        {
            textBox_AddScale.Text = "";
            textBox_AddScale.ForeColor = Color.Black;
        }
    }

    private void textBox_AddScale_Leave(object sender, EventArgs e)
    {
        if (textBox_AddScale.Text == "")
        {
            textBox_AddScale.Text = "보정값";
            textBox_AddScale.ForeColor = Color.LightGray;
        }
    }

    private void cB_Unit_CheckedChanged(object sender, EventArgs e)
    {
        if (!cB_Unit.Checked)
        {
            cB_Unit.Text = "cm";
            userBox.isUnit = false;
        }
        else
        {
            cB_Unit.Text = "inch";
            userBox.isUnit = true;
        }

        userBox.LineMeasure(userBox.mIndex);
        pictureBox1.Invalidate();
    }

    private void DisplayMeasureResult()
    {
        if (!Form3.UserSettingManager.Instance.Settings.useNumeric)
        {
            for (int i = 0; i < userBox.mArrayMaxNo; i++)
            {
                if (!(userBox.userBoxs[i].measureLine.measure_Len == ""))
                {
                    userBox.userBoxs[i].ResultDisplay = userBox.userBoxs[i].TypeItem;
                }
            }
        }

        if (Form3.UserSettingManager.Instance.Settings.useBaseColor)
        {
            userBox.isBaseColor = true;
        }
        else
        {
            userBox.isBaseColor = false;
        }
    }

    private void btn_Save_Click(object sender, EventArgs e)
    {
        if (filename == null)
        {
            MessageBox.Show("파일을 먼저 열어 주세요.");
            return;
        }

        WriteCSV(CSVfilename);
        DisplayMeasureResult();
        userBox.userBoxs[userBox.mIndex].showHelpLine = false;
        pictureBox1.Invalidate();
        if (File.Exists(Measurefilename))
        {
            if (MessageBox.Show("이미 같은 이름의 파일이 존재합니다. 저장할까요?", "확인", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                userBox.userBoxs[userBox.mIndex].showHelpLine = true;
                pictureBox1.Invalidate();
                return;
            }

            File.Delete(Measurefilename);
        }

        using (Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height))
        {
            pictureBox1.DrawToBitmap(bitmap, pictureBox1.ClientRectangle);
            _ = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
            ImageCodecInfo encoder = GetEncoder(ImageFormat.Jpeg);
            EncoderParameters encoderParameters = new EncoderParameters(1);
            EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
            encoderParameters.Param[0] = encoderParameter;
            bitmap.SetResolution(300f, 300f);
            bitmap.Save(Measurefilename, encoder, encoderParameters);
        }

        if (Form3.UserSettingManager.Instance.Settings.useBackup)
        {
            Backup(CSVfilename);
            Backup(Measurefilename);
        }

        cB_Move.Checked = false;
        if (MessageBox.Show("호스트에 정보를 보냅니다.", "확인", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            btn_SendAPI_Click(sender, e);
        }
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo imageCodecInfo in imageEncoders)
        {
            if (imageCodecInfo.FormatID == format.Guid)
            {
                return imageCodecInfo;
            }
        }

        return null;
    }

    private void btn_SendAPI_Click(object sender, EventArgs e)
    {
        if (!Form3.UserSettingManager.Instance.Settings.useApi)
        {
            MessageBox.Show("API 통신 설정이 꺼져 있습니다.");
        }
        else if (!File.Exists(Measurefilename) || !File.Exists(CSVfilename))
        {
            MessageBox.Show("측정 이미지 파일 또는 CSV 파일이 없습니다.");
        }
        else
        {
            API_PostMeasureInform();
        }
    }

    private void Backup(string filename)
    {
        try
        {
            string directoryName = Path.GetDirectoryName(filename);
            string backupFilePath = Path.Combine(path2: Path.GetFileName(filename), path1: directoryName.Replace("Clorium Images", "Clorium Images Backup"));
            backupFilePath = "D:" + backupFilePath.Substring(2);
            if (!Directory.Exists(Path.GetDirectoryName(backupFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath));
            }

            if (File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
            }

            File.Copy(filename, backupFilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
    }

    private void btn_listBoxItemEdit_Click(object sender, EventArgs e)
    {
        if (Application.OpenForms.OfType<Form2>().FirstOrDefault() == null)
        {
            Form2 form = new Form2
            {
                StartPosition = FormStartPosition.Manual
            };
            int x = Location.X;
            int y = Location.Y;
            form.Location = new Point(x + 245, y + 175);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadItems();
            }
        }
    }

    private void LoadItems()
    {
        if (File.Exists("D:\\Clorium Data\\Clorium_MeasureItems.txt"))
        {
            comboBox_DisplayType.Items.Clear();
            listBox_DisplayItems.Items.Clear();
            textBox_TypeID.Text = "";
            using StreamReader streamReader = new StreamReader("D:\\Clorium Data\\Clorium_MeasureItems.txt");
            string itemDefinitionLine;
            while ((itemDefinitionLine = streamReader.ReadLine()) != null)
            {
                if (itemDefinitionLine == "[타입]")
                {
                    string measurementTypeName = streamReader.ReadLine();
                    comboBox_DisplayType.Items.Add(measurementTypeName.Trim());
                }
            }

            comboBox_DisplayType.SelectedIndex = 0;
            return;
        }

        MessageBox.Show("목록이 저장된 파일이 없습니다.");
    }

    // 유지보수 설명(복구 후 추가): 측정 유형/ID/항목/결과 네 열을 UTF8 CSV로 쓴다. 원본은 파일을 덮어쓰면서도 기존 파일이면 헤더를 생략하므로 반복 저장 뒤 읽기 계약 확인이 필요하다. 이번 주석 보강에서 그 동작은 수정하지 않았다.
    private void WriteCSV(string CSVfileName)
    {
        bool csvFileAlreadyExists = false;
        if (File.Exists(CSVfileName))
        {
            csvFileAlreadyExists = true;
        }

        using StreamWriter streamWriter = new StreamWriter(CSVfileName, append: false, Encoding.UTF8);
        if (!csvFileAlreadyExists)
        {
            string[] value = new string[4]
            {
                "타입",
                "ID",
                "항목",
                "치수"
            };
            streamWriter.WriteLine(string.Join(",", value));
        }

        for (int i = 0; i < userBox.mArrayMaxNo; i++)
        {
            if (!(userBox.userBoxs[i].measureLine.measure_Len == ""))
            {
                string[] value2 = new string[4]
                {
                    userBox.userBoxs[i].Type,
                    userBox.userBoxs[i].TypeID,
                    userBox.userBoxs[i].TypeItem,
                    userBox.userBoxs[i].measureLine.measure_Len.ToString()
                };
                streamWriter.WriteLine(string.Join(",", value2));
            }
        }
    }

    // 유지보수 설명(복구 후 추가): 헤더 1행을 건너뛰고 쉼표로 나눈 네 열을 측정 items에 반영한다. 인용/이스케이프 CSV 파서가 아니며 float.Parse는 현재 문화권을 따른다. 반환값은 물리 행 수-1로 빈 행을 포함하고, 첫 유형 ID의 0 sentinel 처리도 원본대로 유지한다.
    private int ReadCSV(string CSVfileName)
    {
        int lineCountExcludingHeader = 0;
        if (!File.Exists(CSVfileName))
        {
            MessageBox.Show("CSV 파일이 존재하지 않습니다.");
            return 0;
        }

        int nonEmptyDataLineCount = File.ReadLines(CSVfileName).Skip(1).Count((string line) => !string.IsNullOrWhiteSpace(line));
        Data_MeasureInform.measureItemGroup.items = new Item[nonEmptyDataLineCount];
        for (int itemInitializationIndex = 0; itemInitializationIndex < nonEmptyDataLineCount; itemInitializationIndex++)
        {
            Data_MeasureInform.measureItemGroup.items[itemInitializationIndex] = new Item();
        }

        using StreamReader streamReader = new StreamReader(CSVfileName);
        string csvLine = "";
        int parsedRowPosition = 0;
        int firstTypeId = 0;
        string firstTypeName = "";
        lineCountExcludingHeader = File.ReadLines(CSVfileName).Count() - 1;
        while ((csvLine = streamReader.ReadLine()) != null)
        {
            if (parsedRowPosition == 0)
            {
                parsedRowPosition++;
            }
            else if (!string.IsNullOrWhiteSpace(csvLine))
            {
                string[] csvFields = csvLine.Split(',');
                if (csvFields.Length != 4)
                {
                    MessageBox.Show("CSV 파일의 형식이 잘못되었습니다.");
                    return 0;
                }

                string currentTypeName = csvFields[0];
                Data_MeasureInform.measureItemGroup.typeId = int.Parse(csvFields[1]);
                Data_MeasureInform.measureItemGroup.items[parsedRowPosition - 1].name = csvFields[2];
                Data_MeasureInform.measureItemGroup.items[parsedRowPosition - 1].value = float.Parse(csvFields[3]);
                parsedRowPosition++;
                if (firstTypeId == 0)
                {
                    firstTypeName = csvFields[0];
                    firstTypeId = int.Parse(csvFields[1]);
                }
                else if (firstTypeId != int.Parse(csvFields[1]))
                {
                    MessageBox.Show("측정 항목이 다릅니다.\r\n측정 항목 : " + firstTypeName + " / " + currentTypeName + "\r\n측정 항목을 확인하세요.");
                    return 0;
                }
            }
        }

        return lineCountExcludingHeader;
    }

    private void API_GetSizeTypeID()
    {
        // 유지보수 설정 변경: API 사용 경로에서만 누락을 안내하며 기존 헤더 이름은 유지합니다.
        if (string.IsNullOrEmpty(APISendData.Vendor_aip_key))
        {
            MessageBox.Show("API 키 설정이 필요합니다: CLORIUM_AUTOSIZING_STANDALONE_VENDOR_API_KEY");
            return;
        }
        Data_MeasureInform.barcodeId = GetBarcodeID(filename);
        APISendData.URL = imageInfo.host + "/api2/recommerce/external/" + APISendData.VendorID + "/barcodes/" + Data_MeasureInform.barcodeId + "/size-type";
        if (Form3.UserSettingManager.Instance.Settings.useApiShowData)
        {
            string decodedRequestUrl = Regex.Unescape(APISendData.URL);
            richTextBox1.AppendText("GET Msg\r\n" + decodedRequestUrl + "\r\n\r\n");
        }

        webAPI_Get(APISendData.Vendor_aip_key, APISendData.URL, CommandType.CMD_GET_SizeTypeID);
        if (Form3.UserSettingManager.Instance.Settings.useApiShowData)
        {
            richTextBox1.AppendText("수신 [Type] : " + Data_SizeType.data.sizeTypeName.ToString() + "\r\n");
            richTextBox1.AppendText("수신 [ID]   : " + Data_SizeType.data.sizeTypeId + "\r\n\r\n");
        }
    }

    private void API_PostMeasureInform()
    {
        // 유지보수 설정 변경: API 사용 경로에서만 누락을 안내하며 기존 헤더 이름은 유지합니다.
        if (string.IsNullOrEmpty(APISendData.Vendor_aip_key))
        {
            MessageBox.Show("API 키 설정이 필요합니다: CLORIUM_AUTOSIZING_STANDALONE_VENDOR_API_KEY");
            return;
        }
        makeMeasureInformData();
        APISendData.JsonData = MakeJsonData_Measure(Data_MeasureInform.cameraUnit, Data_MeasureInform.folder, Data_MeasureInform.imageFolder, Data_MeasureInform.barcodeId, Data_MeasureInform.sizeFiles, Data_MeasureInform.measureItemGroup, Data_MeasureInform.images);
        APISendData.URL = imageInfo.host + "/api2/recommerce/external/" + APISendData.VendorID + "/barcodes/" + Data_MeasureInform.barcodeId + "/measurements";
        if (Form3.UserSettingManager.Instance.Settings.useApiShowData)
        {
            string decodedRequestJson = Regex.Unescape(APISendData.JsonData);
            richTextBox1.AppendText("POST Msg\r\n" + decodedRequestJson + "\r\n\r\n");
        }

        webAPI_Post(APISendData.Vendor_aip_key, APISendData.URL, APISendData.JsonData);
    }

    private void makeMeasureInformData()
    {
        Data_MeasureInform.cameraUnit = imageInfo.cameraID;
        Data_MeasureInform.folder = GetMeasureFolder(pathfilename);
        Data_MeasureInform.imageFolder = GetImageFolder(pathfilename);
        Data_MeasureInform.barcodeId = GetBarcodeID(filename);
        Data_MeasureInform.sizeFiles[0] = Path.GetFileName(pathfilename);
        Data_MeasureInform.sizeFiles[1] = Path.GetFileName(Measurefilename);
        Data_MeasureInform.sizeFiles[2] = Path.GetFileName(CSVfilename);
        GetCapturedFilename(pathfilename, Data_MeasureInform.images);
        int csvDataLineCount = ReadCSV(CSVfilename);
        richTextBox1.AppendText("측정항목 : " + csvDataLineCount + "개\r\n");
    }

    private string GetBarcodeID(string filename)
    {
        string barcodeStem = Regex.Replace(filename.Replace(imageInfo.cameraID, "").Replace("_M", "").Replace("_F", ""), "_\\d+$", "");
        int lastSeparatorIndex = barcodeStem.LastIndexOf("___");
        if (lastSeparatorIndex != -1)
        {
            barcodeStem = barcodeStem.Substring(0, lastSeparatorIndex);
        }

        return barcodeStem;
    }

    private string GetMeasureFolder(string pathfilename)
    {
        return Path.GetDirectoryName(pathfilename);
    }

    private string GetImageFolder(string pathfilename)
    {
        return Directory.GetParent(Path.GetDirectoryName(pathfilename))?.FullName;
    }

    // 유지보수 설명(복구 후 추가): 부모 디렉터리 파일을 정렬한 뒤 이미지 파일명을 전달받은 배열에 순서대로 채운다. 배열 크기보다 많은 일치 파일에 대한 별도 상한 검사는 원본에 없으며 이번 복구에서 추가하지 않았다.
    private void GetCapturedFilename(string pathfilename, string[] imagenames)
    {
        string directoryName = Path.GetDirectoryName(pathfilename);
        directoryName = Directory.GetParent(directoryName)?.FullName;
        for (int i = 0; i < imagenames.Length; i++)
        {
            imagenames[i] = null;
        }

        if (Directory.Exists(directoryName))
        {
            string[] files = Directory.GetFiles(directoryName);
            Array.Sort(files);
            int imageNameIndex = 0;
            string[] captureFilePaths = files;
            foreach (string path in captureFilePaths)
            {
                if (Path.GetExtension(path).ToLower() == ".jpg")
                {
                    imagenames[imageNameIndex++] = Path.GetFileName(path);
                }
            }
        }
        else
        {
            MessageBox.Show("이미지 폴더가 존재하지 않습니다.");
        }
    }

    private void InitArray()
    {
        Data_MeasureInform = new JsonData_MeasureInform
        {
            barcodeId = "",
            cameraUnit = "",
            folder = "",
            imageFolder = "",
            sizeFiles = new string[20],
            images = new string[20],
            measureItemGroup = new Measureitem()
        };
        for (int i = 0; i < 10; i++)
        {
            Data_MeasureInform.measureItemGroup = new Measureitem
            {
                typeId = 0,
                items = new Item[20]
            };
            for (int j = 0; j < 20; j++)
            {
                Data_MeasureInform.measureItemGroup.items[j] = new Item
                {
                    name = "",
                    value = 0f
                };
            }
        }

        Data_SizeType = new JsonData_SizeType
        {
            data = new Data
            {
                sizeTypeId = 0,
                sizeTypeName = ""
            },
            meta = new Meta
            {
                result = ""
            },
            code = 0
        };
    }

    // 유지보수 설명(복구 후 추가): 빈 파일명과 값 0/이름 없는 측정 항목을 걸러 전송 JSON을 만든다. 전달받은 measureItemGroup.items 자체를 교체하는 기존 부수효과와 공개 매개변수 이름/JSON 속성 계약을 보존한다.
    public static string MakeJsonData_Measure(string cameraUnit, string folder, string imagefolder, string barcodeid, string[] sizefiles, Measureitem measureItemGroup, string[] images)
    {
        JsonData_MeasureInform jsonData_MeasureInform = new JsonData_MeasureInform
        {
            barcodeId = barcodeid,
            cameraUnit = cameraUnit,
            folder = folder,
            imageFolder = imagefolder,
            sizeFiles = sizefiles,
            images = images,
            measureItemGroup = measureItemGroup
        };
        jsonData_MeasureInform.sizeFiles = jsonData_MeasureInform.sizeFiles.Where((string image) => !string.IsNullOrEmpty(image)).ToArray();
        jsonData_MeasureInform.images = jsonData_MeasureInform.images.Where((string image) => !string.IsNullOrEmpty(image)).ToArray();
        measureItemGroup.items = measureItemGroup.items.Where((Item item) => !string.IsNullOrEmpty(item.name) && item.value != 0f).ToArray();
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                (JsonConverter)new FloatToStringConverter()
            }
        };
        return JsonSerializer.Serialize(jsonData_MeasureInform, options);
    }

    // 유지보수 설명(복구 후 추가): 기존 GET 메서드/콘텐츠 유형/헤더 이름과 30초 제한으로 요청을 구성한다. 실제 네트워크 연결과 서버 응답은 실행 검증하지 않았다.
    private void webAPI_Get(string venderapikey, string url, CommandType command = CommandType.RES_NoData)
    {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.Method = "GET";
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Timeout = 30000;
        httpWebRequest.Headers.Add("x-api-key", venderapikey);
        API_Respose(httpWebRequest, command);
    }

    // 유지보수 설명(복구 후 추가): 본문을 원본과 동일하게 ASCII 바이트로 만들어 POST한다. 헤더 이름/JSON 속성과 응답 처리 연결은 보존하며 외부 설정 분리는 별도 변경 단계로 기록한다.
    private void webAPI_Post(string venderapikey, string url, string data, CommandType command = CommandType.RES_NoData)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.Method = "POST";
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Timeout = 30000;
        httpWebRequest.ContentLength = bytes.Length;
        httpWebRequest.Headers.Add("x-api-key", venderapikey);
        using (Stream stream = httpWebRequest.GetRequestStream())
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
        }

        API_Respose(httpWebRequest, command);
    }

    private void API_Respose(HttpWebRequest request, CommandType command)
    {
        try
        {
            using HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
            if (command == CommandType.RES_NoData)
            {
                return;
            }

            using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
            string json = streamReader.ReadToEnd();
            if (command == CommandType.CMD_GET_SizeTypeID)
            {
                Data_SizeType = JsonSerializer.Deserialize<JsonData_SizeType>(json);
            }
        }
        catch (WebException ex)
        {
            string errorMessage = "네트워크 오류: " + ex.Message;
            if (ex.Response != null && ex.Response is HttpWebResponse httpWebResponse2)
            {
                using Stream stream = httpWebResponse2.GetResponseStream();
                if (stream != null)
                {
                    using StreamReader streamReader2 = new StreamReader(stream);
                    string errorResponseBody = streamReader2.ReadToEnd();
                    errorMessage = errorMessage + $"\nHTTP {(int)httpWebResponse2.StatusCode} - {httpWebResponse2.StatusCode}" + "\n응답 메시지:\n" + errorResponseBody;
                }
            }

            MessageBox.Show(errorMessage);
        }
    }
}
