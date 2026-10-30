using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CloriumAutoSizing;
public partial class Form3 : Form
{
    public class UserSetting
    {
        public bool crop;
        public bool unit_inch;
        public bool useMenuExpend;
        public bool useNumeric;
        public bool useBackup = true;
        public bool useAddScale;
        public bool useApi;
        public bool useApiShowData;
        public bool useFolder;
        public bool useBaseColor;
        public string initFolder = "D:\\";
        public Color lineColor = Color.Blue;
        public Color fontColor = Color.Black;
        public Brush baseColor = Brushes.Yellow;
        public string fontSize = "15";
        public string autoAssistantLen = "30";
        public string manualAssistantLen = "10";
        public string ApiHostName = "";
    }

    public class UserSettingManager
    {
        private static UserSettingManager instance;
        private static readonly object lockObj = new object ();
        public UserSetting Settings { get; set; }

        public static UserSettingManager Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new UserSettingManager();
                    }
                }

                return instance;
            }
        }

        private UserSettingManager()
        {
            Settings = new UserSetting();
        }
    }

    private string settingfilepath = "D:\\Clorium Data\\Clorium_Measure_Settings.txt";
    public Form3()
    {
        InitializeComponent();
        LoadSettingsFromFile(UserSettingManager.Instance.Settings, settingfilepath);
    }

    private void Form3_Load(object sender, EventArgs e)
    {
        ReadSetting(UserSettingManager.Instance.Settings);
        DisplayHint();
    }

    private void DisplayHint()
    {
        ToolTip toolTip = new ToolTip();
        toolTip.AutoPopDelay = 1500;
        toolTip.InitialDelay = 100;
        toolTip.ReshowDelay = 300;
        toolTip.ShowAlways = true;
        toolTip.IsBalloon = true;
        toolTip.SetToolTip(cB_MenuExpend, "프로그램 실행시 확장 메뉴 보이기");
        toolTip.SetToolTip(cB_useBase, "측정 치수 저장시 바탕색 보이기");
        toolTip.SetToolTip(cB_Backup, "백업 폴더에도 저장하기");
        toolTip.SetToolTip(cB_UseAddScale, "스케일 보정 버튼 보이기");
        toolTip.SetToolTip(cB_useApi, "API에 치수 정보 송신하기");
        toolTip.SetToolTip(cB_useApiShowData, "API 통신 데이터 보기");
        toolTip.SetToolTip(cB_useFolder, "열기시 지정한 폴더로 이동하기");
        toolTip.SetToolTip(btn_InitFolder, "폴더 지정 다이아로그 열기");
        toolTip.SetToolTip(cB_Crop, "프로그램 실행시 잘래내기 체크하기");
        toolTip.SetToolTip(textBox_ManualAssLen, "수동 측정시 양끝의 보조선 길이 지정");
        toolTip.SetToolTip(textBox_AutoAssLen, "자동 측정시 양끝의 보조선 길이 지정");
        toolTip.SetToolTip(richTextBox_ApiHost, "API 호스트 이름 지정");
    }

    private void setSetting(UserSetting settings)
    {
        settings.unit_inch = radioButton_inch.Checked;
        settings.useNumeric = radioButton_Numeric.Checked;
        settings.crop = cB_Crop.Checked;
        settings.useBackup = cB_Backup.Checked;
        settings.useAddScale = cB_UseAddScale.Checked;
        settings.useMenuExpend = cB_MenuExpend.Checked;
        settings.useApi = cB_useApi.Checked;
        settings.useApiShowData = cB_useApiShowData.Checked;
        settings.useFolder = cB_useFolder.Checked;
        settings.useBaseColor = cB_useBase.Checked;
        settings.initFolder = richTextBox_InitFolder.Text;
        settings.autoAssistantLen = textBox_AutoAssLen.Text;
        settings.manualAssistantLen = textBox_ManualAssLen.Text;
        settings.ApiHostName = richTextBox_ApiHost.Text;
        settings.fontSize = comboBox_FontSize.SelectedItem.ToString();
    }

    private void ReadSetting(UserSetting settings)
    {
        radioButton_inch.Checked = settings.unit_inch;
        radioButton_Numeric.Checked = settings.useNumeric;
        cB_Crop.Checked = settings.crop;
        cB_Backup.Checked = settings.useBackup;
        cB_UseAddScale.Checked = settings.useAddScale;
        cB_MenuExpend.Checked = settings.useMenuExpend;
        cB_useApi.Checked = settings.useApi;
        cB_useApiShowData.Checked = settings.useApiShowData;
        cB_useFolder.Checked = settings.useFolder;
        cB_useBase.Checked = settings.useBaseColor;
        richTextBox_InitFolder.Text = settings.initFolder;
        panel_LineColor.BackColor = settings.lineColor;
        panel_FontColor.BackColor = settings.fontColor;
        panel_BaseColor.BackColor = ((SolidBrush)settings.baseColor).Color;
        comboBox_FontSize.SelectedItem = settings.fontSize.ToString();
        textBox_AutoAssLen.Text = settings.autoAssistantLen;
        textBox_ManualAssLen.Text = settings.manualAssistantLen;
        richTextBox_ApiHost.Text = settings.ApiHostName;
    }

    // 유지보수 설명(복구 후 추가): 설정 키와 값을 원본 key=value 형식으로 저장한다. 공개 필드/키 이름과 색상 문자열 변환은 보존한다. API 호스트의 빈 기본값은 이미지 메타데이터 호스트 선택과 연결된다.
    public void SaveSettingsToFile(UserSetting settings, string filename)
    {
        using StreamWriter streamWriter = new StreamWriter(filename);
        streamWriter.WriteLine($"unit_inch={settings.unit_inch}");
        streamWriter.WriteLine($"useNumeric={settings.useNumeric}");
        streamWriter.WriteLine($"crop={settings.crop}");
        streamWriter.WriteLine($"useMenuExpend={settings.useMenuExpend}");
        streamWriter.WriteLine($"useBackup={settings.useBackup}");
        streamWriter.WriteLine($"useAddScale={settings.useAddScale}");
        streamWriter.WriteLine($"useApi={settings.useApi}");
        streamWriter.WriteLine($"useApiShowData={settings.useApiShowData}");
        streamWriter.WriteLine($"useFolder={settings.useFolder}");
        streamWriter.WriteLine($"useBaseColor={settings.useBaseColor}");
        streamWriter.WriteLine("initFolder=" + settings.initFolder);
        streamWriter.WriteLine("lineColor=" + ColorTranslator.ToHtml(settings.lineColor));
        streamWriter.WriteLine("fontColor=" + ColorTranslator.ToHtml(settings.fontColor));
        if (settings.baseColor is SolidBrush solidBrush)
        {
            streamWriter.WriteLine("baseColor=" + ColorTranslator.ToHtml(solidBrush.Color));
        }

        streamWriter.WriteLine("fontSize=" + settings.fontSize);
        streamWriter.WriteLine("autoAssistantLen=" + settings.autoAssistantLen);
        streamWriter.WriteLine("manualAssistantLen=" + settings.manualAssistantLen);
        streamWriter.WriteLine("ApiHostName=" + settings.ApiHostName);
    }

    // 유지보수 설명(복구 후 추가): 각 행을 등호로 분리하고 정확히 두 부분인 행만 기존 키별로 해석한다. 값 내부의 등호 처리/형식 오류 동작은 원본대로 두며 임의 보정하지 않는다.
    public void LoadSettingsFromFile(UserSetting settings, string filename)
    {
        if (!File.Exists(filename))
        {
            SaveSettingsToFile(settings, settingfilepath);
            ReadSetting(settings);
            MessageBox.Show("Measure 설정 파일이 존재하지 않습니다. Default 값으로 설정됩니다.");
            return;
        }

        using StreamReader streamReader = new StreamReader(filename);
        string settingsLine;
        while ((settingsLine = streamReader.ReadLine()) != null)
        {
            string[] settingParts = settingsLine.Split('=');
            if (settingParts.Length == 2)
            {
                string settingKey = settingParts[0].Trim();
                string settingValue = settingParts[1].Trim();
                switch (settingKey)
                {
                    case "unit_inch":
                        settings.unit_inch = bool.Parse(settingValue);
                        break;
                    case "useNumeric":
                        settings.useNumeric = bool.Parse(settingValue);
                        break;
                    case "crop":
                        settings.crop = bool.Parse(settingValue);
                        break;
                    case "useBackup":
                        settings.useBackup = bool.Parse(settingValue);
                        break;
                    case "useAddScale":
                        settings.useAddScale = bool.Parse(settingValue);
                        break;
                    case "useMenuExpend":
                        settings.useMenuExpend = bool.Parse(settingValue);
                        break;
                    case "useApi":
                        settings.useApi = bool.Parse(settingValue);
                        break;
                    case "useApiShowData":
                        settings.useApiShowData = bool.Parse(settingValue);
                        break;
                    case "useFolder":
                        settings.useFolder = bool.Parse(settingValue);
                        break;
                    case "useBaseColor":
                        settings.useBaseColor = bool.Parse(settingValue);
                        break;
                    case "initFolder":
                        settings.initFolder = settingValue;
                        break;
                    case "lineColor":
                        settings.lineColor = ColorTranslator.FromHtml(settingValue);
                        break;
                    case "fontColor":
                        settings.fontColor = ColorTranslator.FromHtml(settingValue);
                        break;
                    case "baseColor":
                        settings.baseColor = new SolidBrush(ColorTranslator.FromHtml(settingValue));
                        break;
                    case "fontSize":
                        settings.fontSize = settingValue;
                        break;
                    case "autoAssistantLen":
                        settings.autoAssistantLen = settingValue;
                        break;
                    case "manualAssistantLen":
                        settings.manualAssistantLen = settingValue;
                        break;
                    case "ApiHostName":
                        settings.ApiHostName = settingValue;
                        break;
                }
            }
        }
    }

    private void btn_InitFolder_Click(object sender, EventArgs e)
    {
        using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
        folderBrowserDialog.Description = "폴더를 선택하세요";
        folderBrowserDialog.ShowNewFolderButton = true;
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            richTextBox_InitFolder.Text = folderBrowserDialog.SelectedPath;
            UserSettingManager.Instance.Settings.initFolder = folderBrowserDialog.SelectedPath;
        }
    }

    private void btn_SetLineColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_LineColor.BackColor = colorDialog.Color;
            UserSettingManager.Instance.Settings.lineColor = colorDialog.Color;
        }
    }

    private void btn_SetFontColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_FontColor.BackColor = colorDialog.Color;
            UserSettingManager.Instance.Settings.fontColor = colorDialog.Color;
        }
    }

    private void btn_BaseColor_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.ShowHelp = true;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            panel_BaseColor.BackColor = colorDialog.Color;
            UserSettingManager.Instance.Settings.baseColor = new SolidBrush(colorDialog.Color);
        }
    }

    private void btn_Save_Click(object sender, EventArgs e)
    {
        setSetting(UserSettingManager.Instance.Settings);
        SaveSettingsToFile(UserSettingManager.Instance.Settings, settingfilepath);
        DialogResult = DialogResult.OK;
        Close();
    }
}
