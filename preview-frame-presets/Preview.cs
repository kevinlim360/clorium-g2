using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Standalone, offline design preview. Never load product assemblies or connect to devices.
public static partial class Preview {
 static Grid root; static Window window; static int sequence=4; static string product="32987"; static bool busy=false;
 static T C<T>(string n) where T:FrameworkElement { return (T)root.FindName(n); }
 static void Label(string n,string s){ C<TextBlock>(n).Text=s; }
 static bool Active { get { return C<CheckBox>("EnableAesthetic").IsChecked==true && C<CheckBox>("Aesthetic").IsChecked==true; } }
 static bool Defect { get {return C<CheckBox>("Defect").IsChecked==true;} }
 static Brush Color(string s){return (Brush)new BrushConverter().ConvertFromString(Palette(s));}
 static string Name(string face){return "C05_"+product+"_"+sequence.ToString("000")+(face=="D"?"_D":(Active?"_PR_":"_")+face)+".jpg";}
 static void Refresh(){
  C<CheckBox>("Aesthetic").Visibility=C<CheckBox>("EnableAesthetic").IsChecked==true?Visibility.Visible:Visibility.Collapsed;
  Label("FrontTitle",Defect?"하자 촬영":"앞면 촬영"); Label("FrontFlag",Defect?"_D":Active?"_PR_F":"_F"); Label("BackFlag",Active?"_PR_B":"_B");
  C<Button>("Back").Visibility=Defect?Visibility.Hidden:Visibility.Visible;
  Label("ModeBadge",Active?"●  미감개선 ON":"○  미감개선 OFF");
  Label("ModeHint",Defect?"하자 사진은 항상 _D로 저장됩니다.":Active?"미감개선이 켜져 있습니다.":"기존 방식으로 저장합니다.");
  C<TextBlock>("ModeHint").Foreground=Color(Active?"#A95100":"#68717D");
  C<TextBlock>("ModeBadge").Foreground=Color(Active?"#A95100":"#68717D");
  C<Border>("ImageFrame").BorderBrush=Color(Active?"#ED9227":"#E1E4E9"); C<Border>("PreviewPane").BorderBrush=Color(Active?"#ED9227":"#E1E4E9");
  Label("NextName",Name(Defect?"D":"F"));
 }
 static void Capture(string face){
  if(busy)return; busy=true; foreach(string n in new[]{"Front","Back","NextProduct","Aesthetic","Defect","EnableAesthetic","Barcode","PresetList","LoadPreset","SavePreset","ConfirmPresetSave","Aperture","Shutter","ISO","WhiteBalance","FrameExample"})C<Control>(n).IsEnabled=false;
  string pending=Name(Defect?"D":face); Label("Status","촬영·저장 중 상태의 시안입니다. 실제 카메라 및 파일 저장은 실행하지 않습니다.");
  var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(1000)};t.Tick+=(s,e)=>{t.Stop();Label("ImageName",pending);sequence++;Label("CountLabel","샘플 "+(sequence-1)+"장");busy=false;foreach(string n in new[]{"Front","Back","NextProduct","Aesthetic","Defect","EnableAesthetic","Barcode","PresetList","LoadPreset","SavePreset","ConfirmPresetSave","Aperture","Shutter","ISO","WhiteBalance","FrameExample"})C<Control>(n).IsEnabled=true;Refresh();Label("Status","촬영 완료 표시 예시 · 실제 파일은 저장하지 않았습니다.");};t.Start();
 }
 static void Next(){if(busy)return;string p=C<TextBox>("Barcode").Text.Trim();if(p.Length==0){Label("Status","상품번호를 입력해 주세요. 현재 상품은 유지됩니다.");return;}product=p;sequence=1;C<CheckBox>("Defect").IsChecked=false;Label("ProductLabel",p);Label("FolderLabel","D:\\Clorium Images\\2026년10월\\27일\\C05_"+p+"\\");Label("ImageName","새 상품 · 이미지 대기");Label("CountLabel","샘플 0장");Refresh();Label("Status","새 상품 표시 예시 · 미감개선 ON/OFF 선택은 유지됩니다. 실제 폴더는 만들지 않습니다.");}
 static void Dialog(bool preview){C<Border>("Overlay").Visibility=Visibility.Visible;C<Grid>("SettingsPane").Visibility=preview?Visibility.Collapsed:Visibility.Visible;C<CloriumG2.AspectFrame>("FramePreview").Visibility=preview?Visibility.Visible:Visibility.Collapsed;Label("DialogTitle",preview?"이미지 미리보기":"설정");Label("DialogSub",preview?C<TextBlock>("ImageName").Text:"작업 방식에 맞게 클로리움을 설정하세요.");Label("OverlayNote",preview?"시안용 이미지 · 테두리 색은 화면에만 표시됩니다.":"설정 화면 시안 · 실제 프로그램 설정은 변경되지 않습니다.");}
 static void Error(){Label("Status","포커스를 맞추지 못했습니다. 상품 위치와 초점을 확인하고 다시 촬영해 주세요. · 오류 표시 예시");C<TextBlock>("Status").Foreground=Color("#A32424");}
 static void Wire(){
  string[] navNames={"NavCapture","NavPhotos","NavMeasure","NavLive","NavLight","NavColor","NavMore"};
  string[] navTexts={"촬영 스튜디오","촬영 이미지","치수 측정","미리보기","조명 설정","색상 보정","전체 메뉴"};
  string[] navPaths={"M 2,5 L 6,5 8,2 14,2 16,5 20,5 20,18 2,18 Z M 15,11 A 4,4 0 1 1 7,11 A 4,4 0 1 1 15,11","M 2,2 L 20,2 20,19 2,19 Z M 3,16 L 8,10 12,14 16,9 20,14 M 8,6 L 8,7","M 2,6 L 20,6 20,16 2,16 Z M 6,6 L 6,11 M 10,6 L 10,13 M 14,6 L 14,11 M 18,6 L 18,13","M 1,11 Q 11,-2 21,11 Q 11,24 1,11 M 15,11 A 4,4 0 1 1 7,11 A 4,4 0 1 1 15,11","M 15,11 A 4,4 0 1 1 7,11 A 4,4 0 1 1 15,11 M 11,0 L 11,3 M 11,19 L 11,22 M 0,11 L 3,11 M 19,11 L 22,11 M 3,3 L 5,5 M 17,17 L 19,19 M 3,19 L 5,17 M 17,5 L 19,3","M 11,2 C 8,6 4,10 4,14 A 7,7 0 0 0 18,14 C 18,10 14,6 11,2 Z M 8,14 Q 8,18 12,18","M 3,11 L 5,11 M 10,11 L 12,11 M 17,11 L 19,11"};
  for(int i=0;i<navNames.Length;i++){
   var icon=new System.Windows.Shapes.Path{Data=Geometry.Parse(navPaths[i]),Stroke=Color(i==0?"#075CB8":"#586577"),StrokeThickness=1.5,Width=18,Height=18,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Center};
   var text=new TextBlock{Text=navTexts[i],FontSize=design==2?10:13,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Foreground=Color(i==0?"#075CB8":"#333B46")};
   var row=new StackPanel{Orientation=design==2?Orientation.Vertical:Orientation.Horizontal};
   text.Margin=design==2?new Thickness(0,5,0,0):new Thickness(9,0,0,0);row.Children.Add(icon);row.Children.Add(text);C<Button>(navNames[i]).Content=row;C<Button>(navNames[i]).ToolTip=navTexts[i];
   System.Windows.Automation.AutomationProperties.SetName(C<Button>(navNames[i]),navTexts[i]);
  }
  C<CheckBox>("Aesthetic").Checked+=(s,e)=>Refresh();C<CheckBox>("Aesthetic").Unchecked+=(s,e)=>Refresh();C<CheckBox>("Defect").Checked+=(s,e)=>Refresh();C<CheckBox>("Defect").Unchecked+=(s,e)=>Refresh();C<CheckBox>("EnableAesthetic").Checked+=(s,e)=>Refresh();C<CheckBox>("EnableAesthetic").Unchecked+=(s,e)=>Refresh();
  C<Button>("Front").Click+=(s,e)=>Capture("F");C<Button>("Back").Click+=(s,e)=>Capture("B");C<Button>("NextProduct").Click+=(s,e)=>Next();C<Button>("OpenSettings").Click+=(s,e)=>Dialog(false);C<Button>("ExpandPreview").Click+=(s,e)=>Dialog(true);C<Button>("NavLive").Click+=(s,e)=>Dialog(true);C<Button>("CloseOverlay").Click+=(s,e)=>C<Border>("Overlay").Visibility=Visibility.Collapsed;C<Button>("ErrorDemo").Click+=(s,e)=>Error();
  foreach(string f in new[]{"F","B","D"}){string flag=f;C<Button>("Thumb"+f).Click+=(s,e)=>{Label("ImageName","C05_"+product+(flag=="F"?"_001_PR_F.jpg":flag=="B"?"_002_PR_B.jpg":"_003_D.jpg"));Label("ViewTitle",flag=="F"?"앞면 이미지":flag=="B"?"뒷면 이미지":"하자 이미지");};}
  foreach(string n in new[]{"NavCapture","NavPhotos","NavMeasure","NavLight","NavColor"}){string name=n;C<Button>(n).Click+=(s,e)=>Label("Status",name=="NavMeasure"?"치수 측정 메뉴 시안 · 기존 AutoSizing 연결을 유지할 예정입니다. 치수사진에는 _PR을 붙이지 않습니다.":"메뉴 위치 시안 · 실제 장비와 프로그램 기능은 연결하지 않았습니다.");}
  foreach(string n in new[]{"SetGeneral","SetSave","SetCamera","SetCustom"}){string name=n;C<Button>(n).Click+=(s,e)=>{bool custom=name=="SetCustom";Label("SettingsHeading",custom?"Custom":name=="SetSave"?"파일 저장":name=="SetCamera"?"카메라":"일반");C<CheckBox>("EnableAesthetic").Visibility=custom?Visibility.Visible:Visibility.Collapsed;Label("SettingsDescription",custom?"촬영 화면에 미감개선 선택 항목을 표시합니다.\n프로그램을 시작하면 미감개선 ON으로 시작합니다.\n상품이 바뀌어도 촬영 중 선택한 상태를 유지합니다.":name=="SetSave"?"기존 저장 경로 및 상품별 폴더 규칙을 유지합니다.\n미감개선은 원본 파일명에만 반영합니다.\n이 화면에서는 설정 파일을 변경하지 않습니다.":name=="SetCamera"?"카메라 및 촬영 프리셋 설정 영역입니다.\n기존 카메라 명령과 연결 방식은 유지합니다.\n실제 장비는 연결하지 않았습니다.":"화면 글자와 메뉴를 일관되게 정돈했습니다.\n키보드 포커스는 파란 테두리로 표시합니다.\n시안에서 바꾼 값은 종료하면 초기화됩니다.");};}
  var menu=new ContextMenu{FontFamily=root.GetValue(TextElement.FontFamilyProperty) as FontFamily,FontSize=14,Padding=new Thickness(8),Background=Color("White"),Foreground=Color("#20242A")};
  foreach(string group in new[]{"파일","보기","상품 작업","카메라","도구"}){var m=new MenuItem{Header=group,Padding=new Thickness(12,8,30,8)};string[] items=group=="파일"?new[]{"설정","종료"}:group=="보기"?new[]{"미리보기","썸네일 크기","레이아웃"}:group=="상품 작업"?new[]{"새 상품 시작","작업 편집"}:group=="카메라"?new[]{"카메라 속성","카메라 목록 새로고침"}:new[]{"치수 측정","조명","색상 보정","기타 도구"};foreach(string text in items){string action=text;var child=new MenuItem{Header=text,Padding=new Thickness(12,8,30,8)};child.Click+=(s,e)=>{if(action=="설정")Dialog(false);else if(action=="미리보기")Dialog(true);else if(action=="새 상품 시작")Next();else if(action=="종료")window.Close();else Label("Status",action+" 메뉴 시안 · 실제 기능은 연결하지 않았습니다.");};m.Items.Add(child);}menu.Items.Add(m);}C<Button>("NavMore").ContextMenu=menu;C<Button>("NavMore").Click+=(s,e)=>{menu.PlacementTarget=C<Button>("NavMore");menu.IsOpen=true;};
  Refresh();
 }
 static void Render(string name,int width,int height,double scale){
  shell.Width=width;shell.Height=height;shell.Measure(new Size(width,height));shell.Arrange(new Rect(0,0,width,height));shell.UpdateLayout();
  var bitmap=new RenderTargetBitmap((int)(width*scale),(int)(height*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(shell);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine("..","design-frame-presets",name+".png")))encoder.Save(f);
 }
 static void Check(bool ok,string name,StringBuilder log){if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}

}
