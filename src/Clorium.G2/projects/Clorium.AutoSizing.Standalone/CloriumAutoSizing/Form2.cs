using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CloriumAutoSizing;
public partial class Form2 : Form
{
    private const string itemsfilename = "D:\\Clorium Data\\Clorium_MeasureItems.txt";
    public Form2()
    {
        InitializeComponent();
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
    }

    private void Form2_Load(object sender, EventArgs e)
    {
        LoadItems("D:\\Clorium Data\\Clorium_MeasureItems.txt");
        textBox_InputType.Focus();
    }

    private void Form2_FormClosed(object sender, FormClosedEventArgs e)
    {
        DialogResult = DialogResult.OK;
    }

    private void comboBox_DisplayType_SelectedIndexChanged(object sender, EventArgs e)
    {
        using StreamReader streamReader = new StreamReader("D:\\Clorium Data\\Clorium_MeasureItems.txt");
        string definitionLine;
        while ((definitionLine = streamReader.ReadLine()) != null)
        {
            if (!(definitionLine == "[타입]") || !(streamReader.ReadLine() == comboBox_DisplayType.SelectedItem.ToString()))
            {
                continue;
            }

            if (streamReader.ReadLine() == "[ID]")
            {
                string selectedTypeIdText = streamReader.ReadLine();
                textBox_DisplayID.Text = selectedTypeIdText;
            }

            if (!(streamReader.ReadLine() == "[항목]"))
            {
                continue;
            }

            listBox_DisplayItems.Items.Clear();
            string measurementItemLine = streamReader.ReadLine();
            while (measurementItemLine != null && !measurementItemLine.Contains("-"))
            {
                if (!listBox_DisplayItems.Items.Contains(measurementItemLine))
                {
                    listBox_DisplayItems.Items.Add(measurementItemLine.Trim());
                }

                measurementItemLine = streamReader.ReadLine();
            }
        }
    }

    private void button_SaveAppend_Click(object sender, EventArgs e)
    {
        if (Add("D:\\Clorium Data\\Clorium_MeasureItems.txt"))
        {
            if (!string.IsNullOrWhiteSpace(textBox_InputType.Text))
            {
                comboBox_DisplayType.Items.Add(textBox_InputType.Text);
                comboBox_DisplayType.SelectedItem = textBox_InputType.Text;
            }

            if (!string.IsNullOrWhiteSpace(textBox_InputID.Text))
            {
                textBox_DisplayID.Text = textBox_InputID.Text;
            }

            MessageBox.Show("항목이 저장되었습니다!");
            textBox_InputType.TabStop = false;
            textBox_InputType.Text = "";
            textBox_InputID.Text = "";
            richTextBox_InputItems.Clear();
        }
    }

    private void button_DeleteType_Click(object sender, EventArgs e)
    {
        if (comboBox_DisplayType.SelectedItem == null)
        {
            MessageBox.Show("삭제할 타입을 선택하세요.");
        }
        else
        {
            DeleteType("D:\\Clorium Data\\Clorium_MeasureItems.txt");
        }
    }

    private void button_DeleteItem_Click(object sender, EventArgs e)
    {
        string deleteItem = listBox_DisplayItems.SelectedItem?.ToString();
        DeleteItem("D:\\Clorium Data\\Clorium_MeasureItems.txt", comboBox_DisplayType.SelectedItem.ToString(), deleteItem);
        listBox_DisplayItems.Items.Remove(listBox_DisplayItems.SelectedItem);
    }

    private void button_InsertItem_Click(object sender, EventArgs e)
    {
        listBox_DisplayItems.Items.Add(textBox_InputItem.Text.Trim());
        InsertItem("D:\\Clorium Data\\Clorium_MeasureItems.txt", comboBox_DisplayType.SelectedItem.ToString(), textBox_InputItem.Text);
        textBox_InputItem.Clear();
        textBox_InputItem_Leave(sender, e);
    }

    // 유지보수 설명(복구 후 추가): 원본 표식 기반 유형 정의 파일에서 표시 유형 목록을 만든다. 표식 문자열/파일명은 저장 자료와 연결되므로 이름 정리의 대상이 아니다.
    private void LoadItems(string filepath)
    {
        if (File.Exists(filepath))
        {
            comboBox_DisplayType.Items.Clear();
            textBox_DisplayID.Text = "";
            listBox_DisplayItems.Items.Clear();
            using StreamReader streamReader = new StreamReader(filepath);
            string definitionLine;
            while ((definitionLine = streamReader.ReadLine()) != null)
            {
                if (definitionLine == "[타입]")
                {
                    string typeName = streamReader.ReadLine();
                    comboBox_DisplayType.Items.Add(typeName.Trim());
                }
            }

            comboBox_DisplayType.SelectedIndex = 0;
            return;
        }

        MessageBox.Show("목록이 저장된 파일이 없습니다.");
    }

    // 유지보수 설명(복구 후 추가): 입력 유형 이름과 ID가 기존 정의에 중복되는지 각각 검사한 뒤 항목을 추가한다. 기존 표식 순서와 중복 판단, 파일 추가 모드를 그대로 유지한다.
    private bool Add(string filepath)
    {
        string newTypeName = textBox_InputType.Text.Trim();
        string newTypeIdText = textBox_InputID.Text.Trim();
        bool duplicateTypeFound = false;
        bool duplicateIdFound = false;
        if (File.Exists(filepath))
        {
            using StreamReader streamReader = new StreamReader(filepath);
            string definitionLine;
            while ((definitionLine = streamReader.ReadLine()) != null)
            {
                if (definitionLine == "[타입]")
                {
                    string existingTypeName = streamReader.ReadLine();
                    if (existingTypeName != null && existingTypeName.Trim() == newTypeName)
                    {
                        duplicateTypeFound = true;
                    }
                }

                if (definitionLine == "[ID]")
                {
                    string existingTypeIdText = streamReader.ReadLine();
                    if (existingTypeIdText != null && existingTypeIdText.Trim() == newTypeIdText)
                    {
                        duplicateIdFound = true;
                    }
                }

                if (duplicateTypeFound | duplicateIdFound)
                {
                    break;
                }
            }
        }

        if (duplicateTypeFound)
        {
            MessageBox.Show("이미 동일한 [타입]이 존재합니다.");
            return false;
        }

        if (duplicateIdFound)
        {
            MessageBox.Show("이미 동일한 [ID]가 존재합니다.");
            return false;
        }

        using (StreamWriter streamWriter = new StreamWriter(filepath, append: true))
        {
            streamWriter.WriteLine("-----------------------");
            streamWriter.WriteLine("[타입]");
            streamWriter.WriteLine(textBox_InputType.Text.Trim());
            streamWriter.WriteLine("[ID]");
            streamWriter.WriteLine(textBox_InputID.Text.Trim());
            streamWriter.WriteLine("[항목]");
            string[] inputItemLines = richTextBox_InputItems.Text.Trim().Split('\n');
            listBox_DisplayItems.Items.Clear();
            string[] itemLinesToAppend = inputItemLines;
            foreach (string text6 in itemLinesToAppend)
            {
                listBox_DisplayItems.Items.Add(text6.Trim());
                streamWriter.WriteLine(text6.Trim().ToString());
            }
        }

        return true;
    }

    // 유지보수 설명(복구 후 추가): 선택 유형의 표식 블록을 건너뛰어 나머지 행을 다시 기록한다. 다음 표식과 파일 끝 처리의 원본 동작은 이 유지보수 단계에서 수정하지 않았다.
    private void DeleteType(string filepath)
    {
        List<string> list = new List<string>();
        using (StreamReader streamReader = new StreamReader(filepath))
        {
            string definitionLine = "";
            while ((definitionLine = streamReader.ReadLine()) != null)
            {
                if (definitionLine == "[타입]")
                {
                    string candidateTypeName = streamReader.ReadLine();
                    if (candidateTypeName == comboBox_DisplayType.SelectedItem.ToString())
                    {
                        list.RemoveAt(list.Count - 1);
                        string skippedSectionLine;
                        do
                        {
                            skippedSectionLine = streamReader.ReadLine();
                            if (skippedSectionLine.Contains("-"))
                            {
                                list.Add(skippedSectionLine);
                                break;
                            }
                        }
                        while (skippedSectionLine != null);
                    }
                    else
                    {
                        list.Add(definitionLine);
                        list.Add(candidateTypeName);
                    }
                }
                else
                {
                    list.Add(definitionLine);
                }
            }
        }

        using (StreamWriter streamWriter = new StreamWriter("D:\\Clorium Data\\Clorium_MeasureItems.txt", append: false))
        {
            foreach (string item in list)
            {
                streamWriter.WriteLine(item);
            }
        }

        string value = comboBox_DisplayType.SelectedItem.ToString();
        comboBox_DisplayType.Items.Remove(value);
        listBox_DisplayItems.Items.Clear();
        textBox_DisplayID.Text = "";
        LoadItems("D:\\Clorium Data\\Clorium_MeasureItems.txt");
    }

    private void InsertItem(string filePath, string targetType, string newItem)
    {
        List<string> list = new List<string>(File.ReadAllLines(filePath, Encoding.UTF8));
        bool targetTypeSectionSeen = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].StartsWith(targetType) && i + 1 < list.Count)
            {
                targetTypeSectionSeen = true;
            }
            else if (targetTypeSectionSeen && list[i] == "[항목]")
            {
                list.Insert(i + 1, newItem);
                break;
            }
        }

        if (targetTypeSectionSeen)
        {
            File.WriteAllLines(filePath, list, Encoding.UTF8);
        }
        else
        {
            MessageBox.Show("지정된 타입을 찾을 수 없습니다.");
        }
    }

    private void DeleteItem(string filePath, string targetType, string deleteItem)
    {
        List<string> list = new List<string>(File.ReadAllLines(filePath, Encoding.UTF8));
        bool targetTypeSectionSeen = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].StartsWith(targetType) && i + 1 < list.Count)
            {
                targetTypeSectionSeen = true;
            }
            else if (targetTypeSectionSeen && list[i] == deleteItem)
            {
                list.RemoveAt(i);
                break;
            }
        }

        if (targetTypeSectionSeen)
        {
            File.WriteAllLines(filePath, list, Encoding.UTF8);
        }
        else
        {
            MessageBox.Show("지정된 타입을 찾을 수 없습니다.");
        }
    }

    private void textBox_InputType_Enter(object sender, EventArgs e)
    {
        if (textBox_InputType.ForeColor == Color.LightGray)
        {
            textBox_InputType.Text = "";
            textBox_InputType.ForeColor = Color.Black;
        }
    }

    private void textBox_InputType_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            textBox_InputID.Focus();
        }
    }

    private void textBox_InputType_Leave(object sender, EventArgs e)
    {
        if (textBox_InputType.Text == "")
        {
            textBox_InputType.Text = "타입 입력창";
            textBox_InputType.ForeColor = Color.LightGray;
        }
    }

    private void textBox_InputID_Enter(object sender, EventArgs e)
    {
        if (textBox_InputID.ForeColor == Color.LightGray)
        {
            textBox_InputID.Text = "";
            textBox_InputID.ForeColor = Color.Black;
        }
    }

    private void textBox_InputID_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            richTextBox_InputItems.Focus();
        }
    }

    private void textBox_InputID_Leave(object sender, EventArgs e)
    {
        if (textBox_InputID.Text == "")
        {
            textBox_InputID.Text = "ID 입력창";
            textBox_InputID.ForeColor = Color.LightGray;
        }
    }

    private void richTextBox_InputItems_Enter(object sender, EventArgs e)
    {
        if (richTextBox_InputItems.ForeColor == Color.LightGray)
        {
            richTextBox_InputItems.Text = "";
            richTextBox_InputItems.ForeColor = Color.Black;
        }
    }

    private void richTextBox_InputItems_Leave(object sender, EventArgs e)
    {
        if (richTextBox_InputItems.Text == "")
        {
            richTextBox_InputItems.Text = "항목 입력창";
            richTextBox_InputItems.ForeColor = Color.LightGray;
        }
    }

    private void textBox_InputItem_Enter(object sender, EventArgs e)
    {
        if (textBox_InputItem.ForeColor == Color.LightGray)
        {
            textBox_InputItem.Text = "";
            textBox_InputItem.ForeColor = Color.Black;
        }
    }

    private void textBox_InputItem_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return && !string.IsNullOrWhiteSpace(textBox_InputItem.Text))
        {
            textBox_InputItem.Focus();
        }
    }

    private void textBox_InputItem_Leave(object sender, EventArgs e)
    {
        if (textBox_InputItem.Text == "")
        {
            textBox_InputItem.Text = "항목 추가창";
            textBox_InputItem.ForeColor = Color.LightGray;
        }
    }
}
