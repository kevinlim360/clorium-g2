using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static partial class Preview {
 static int design=1;static bool dark=false;static Grid shell;static ContentControl host;static Border selector;
 static Dictionary<string,string> palette=new Dictionary<string,string>();
 static string Palette(string s){return dark&&palette.ContainsKey(s)?palette[s]:s;}
 static void Select(int number,bool black){
  if(busy){Label("Status","촬영 표시가 끝난 뒤 시안과 배경을 변경해 주세요.");return;}
  bool mode=true,enabled=true,defect=false;string barcode=product,image="C05_32987_001_PR_F.jpg",count="샘플 3장";
  if(root!=null){mode=C<CheckBox>("Aesthetic").IsChecked==true;enabled=C<CheckBox>("EnableAesthetic").IsChecked==true;defect=Defect;barcode=C<TextBox>("Barcode").Text;image=C<TextBlock>("ImageName").Text;count=C<TextBlock>("CountLabel").Text;}
  design=number;dark=black;string file="Main"+design+(dark?"Dark":"Light")+".xaml";
  var pc=new ParserContext{BaseUri=new Uri(Path.Combine(Environment.CurrentDirectory,file))};using(var f=File.OpenRead(file))root=(Grid)XamlReader.Load(f,pc);
  C<CheckBox>("Aesthetic").IsChecked=mode;C<CheckBox>("EnableAesthetic").IsChecked=enabled;C<CheckBox>("Defect").IsChecked=defect;C<TextBox>("Barcode").Text=barcode;
  Label("ProductLabel",product);Label("ImageName",image);Label("CountLabel",count);Label("FolderLabel","D:\\Clorium Images\\2026년10월\\27일\\C05_"+product+"\\");
  C<TextBlock>("ProductLabel").SetBinding(FrameworkElement.ToolTipProperty,new System.Windows.Data.Binding("Text"){Source=C<TextBlock>("ProductLabel")});
  Wire();WireExtras();host.Content=root;shell.Resources=root.Resources;shell.Background=Color("#F7F8FA");Header();
 }
 static void Header(){
  var bar=new Grid{Margin=new Thickness(20,0,20,0)};selector.Child=bar;selector.Background=Color("#EFF1F4");selector.BorderBrush=Color("#DEE2E8");
  var choices=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};bar.Children.Add(choices);
  choices.Children.Add(new TextBlock{Text="디자인 비교",Foreground=Color("#20242A"),FontSize=13,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,18,0),VerticalAlignment=VerticalAlignment.Center});
  string[] titles={"시안 1 · 기본형","시안 2 · 컴팩트형","시안 3 · 포커스형"};
  for(int i=1;i<=3;i++){int n=i;var b=new Button{Content=titles[i-1],Margin=new Thickness(0,0,6,0),MinHeight=30,Padding=new Thickness(12,5,12,5)};if(i==design)b.Style=(Style)root.FindResource("Primary");b.Click+=(s,e)=>Select(n,dark);choices.Children.Add(b);}
  var themes=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};bar.Children.Add(themes);
  themes.Children.Add(new TextBlock{Text="배경",Foreground=Color("#68717D"),FontSize=12,Margin=new Thickness(0,0,10,0),VerticalAlignment=VerticalAlignment.Center});
  foreach(bool black in new[]{false,true}){bool d=black;var b=new Button{Content=black?"블랙":"연한 그레이",MinHeight=30,Padding=new Thickness(12,5,12,5),Margin=new Thickness(0,0,6,0)};if(dark==black){b.Background=Color("#DCE9FB");b.Foreground=Color("#075CB8");b.BorderBrush=Color("#075CB8");}b.Click+=(s,e)=>Select(design,d);themes.Children.Add(b);}
 }
 static void Pump(){shell.UpdateLayout();var f=new System.Windows.Threading.DispatcherFrame();System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(()=>f.Continue=false));System.Windows.Threading.Dispatcher.PushFrame(f);}
 static void Overview(){
  var canvas=new DrawingVisual();using(var dc=canvas.RenderOpen()){
   dc.DrawRectangle(ColorForOverview("#E9ECF1"),null,new Rect(0,0,1480,1580));
   string[] labels={"01  기본형 · 기존 배치 유지","02  컴팩트형 · 좌측 76 / 우측 252","03  포커스형 · 상단 메뉴 / 하단 촬영"};
   for(int i=1;i<=3;i++){
    dc.DrawText(new FormattedText(labels[i-1],System.Globalization.CultureInfo.GetCultureInfo("ko-KR"),FlowDirection.LeftToRight,new Typeface("Segoe UI, Malgun Gothic"),20,Brushes.Black),new Point(20,18+(i-1)*520));
    for(int t=0;t<2;t++){string file=Path.GetFullPath(Path.Combine("..","design-frame-presets","design-"+i+(t==0?"-gray.png":"-black.png")));var img=new BitmapImage(new Uri(file));dc.DrawImage(img,new Rect(20+t*730,56+(i-1)*520,710,454));}
   }
  }var bmp=new RenderTargetBitmap(1480,1580,96,96,PixelFormats.Pbgra32);bmp.Render(canvas);var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine("..","design-frame-presets","00-overview.png")))e.Save(f);
 }
 static Brush ColorForOverview(string s){return (Brush)new BrushConverter().ConvertFromString(s);}
 [STAThread] public static int Main(string[] args){try{
  Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);Directory.CreateDirectory(Path.Combine("..","design-frame-presets"));
  foreach(string line in File.ReadAllLines("palette.txt")){var p=line.Split('=');if(p.Length==2)palette[p[0]]=p[1];}
  shell=new Grid();shell.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});shell.RowDefinitions.Add(new RowDefinition());
  selector=new Border{BorderThickness=new Thickness(0,0,0,1)};shell.Children.Add(selector);host=new ContentControl();Grid.SetRow(host,1);shell.Children.Add(host);
  window=new Window{Title="클로리움 G2 · 시안 수정 · 이미지 비율 / 촬영 프리셋",Width=1440,Height=960,MinWidth=1200,MinHeight=760,Content=shell,WindowStartupLocation=WindowStartupLocation.CenterScreen};
  window.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)C<Border>("Overlay").Visibility=Visibility.Collapsed;};Select(1,false);
  if(args.Length>0&&args[0]=="--render"){
   var log=new StringBuilder();
   for(int n=1;n<=3;n++)foreach(bool black in new[]{false,true}){
    product="32987";sequence=4;root=null;Select(n,black);C<CheckBox>("EnableAesthetic").IsChecked=true;C<CheckBox>("Aesthetic").IsChecked=true;C<CheckBox>("Defect").IsChecked=false;Refresh();
    string id="design-"+n+(black?"-black":"-gray");Render(id,1440,920,2);
    if(black)Check(((SolidColorBrush)C<TextBlock>("SettingsHeading").Foreground).Color.R>200,id+" dark heading contrast",log);
    Check(Name("F")=="C05_32987_004_PR_F.jpg",id+" ON front",log);Check(Name("B").EndsWith("_PR_B.jpg"),id+" ON back",log);
    C<CheckBox>("Defect").IsChecked=true;Check(C<TextBlock>("FrontFlag").Text=="_D"&&C<Button>("Back").Visibility==Visibility.Hidden,id+" defect",log);
    C<CheckBox>("Defect").IsChecked=false;C<CheckBox>("Aesthetic").IsChecked=false;Check(Name("B").EndsWith("_B.jpg")&&!Name("B").Contains("_PR"),id+" OFF",log);
    Select(n,!black);Check(!Active,id+" theme preserves OFF",log);Select(n,black);
    C<TextBox>("Barcode").Text="32988";Next();Check(!Active&&product=="32988",id+" product retains OFF",log);
    C<CheckBox>("EnableAesthetic").IsChecked=false;Select(n,!black);Check(!Active&&C<CheckBox>("Aesthetic").Visibility==Visibility.Collapsed,id+" Custom persists",log);Select(n,black);C<CheckBox>("EnableAesthetic").IsChecked=true;C<CheckBox>("Aesthetic").IsChecked=true;
    Dialog(false);Render(id+"-settings",1440,920,1.5);Dialog(true);Render(id+"-preview",1440,920,1.5);C<Border>("Overlay").Visibility=Visibility.Collapsed;
    C<TextBox>("Barcode").Text="LONG_PRODUCT_32987_ABCDEFG";Next();Render(id+"-compact",1200,760,1.5);
   }
   product="32987";sequence=4;Select(2,true);C<CheckBox>("Defect").IsChecked=true;Select(3,false);Check(Defect&&C<TextBlock>("FrontFlag").Text=="_D","layout change retains defect",log);
   busy=true;Select(1,true);Check(design==3&&!dark,"busy prevents layout/theme change",log);busy=false;
   CheckExtras(log);
   Overview();log.AppendLine("Six main renders + twelve overlay renders + six compact renders. UI prototype only; production files untouched.");File.WriteAllText(Path.Combine("..","design-frame-presets","checks.txt"),log.ToString());return 0;
  }
  new Application().Run(window);return 0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"comparison-error.txt"),ex.ToString());return 1;}}
}
