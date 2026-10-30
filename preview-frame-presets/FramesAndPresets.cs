using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CloriumG2 {
 // This decorator fits the image frame itself, not just the image inside a wide panel.
 public sealed class AspectFrame:Decorator {
  public static readonly DependencyProperty RatioProperty=DependencyProperty.Register("Ratio",typeof(double),typeof(AspectFrame),new FrameworkPropertyMetadata(2.0/3,FrameworkPropertyMetadataOptions.AffectsMeasure|FrameworkPropertyMetadataOptions.AffectsArrange),v=> (double)v>0 && !double.IsInfinity((double)v) && !double.IsNaN((double)v));
  public double Ratio{get{return (double)GetValue(RatioProperty);}set{SetValue(RatioProperty,value);}}
  public AspectFrame(){UseLayoutRounding=false;}
  Size Fit(Size s){double w=double.IsInfinity(s.Width)?300:s.Width,h=double.IsInfinity(s.Height)?450:s.Height;return w/h>Ratio?new Size(h*Ratio,h):new Size(w,w/Ratio);}
  protected override Size MeasureOverride(Size s){if(Child==null)return new Size();var f=Fit(s);Child.Measure(f);return new Size(double.IsInfinity(s.Width)?f.Width:s.Width,double.IsInfinity(s.Height)?f.Height:s.Height);}
  protected override Size ArrangeOverride(Size s){var f=Fit(s);if(Child!=null)Child.Arrange(new Rect((s.Width-f.Width)/2,(s.Height-f.Height)/2,f.Width,f.Height));return s;}
 }
}
public static partial class Preview {
 sealed class Conditions {
  public string Aperture,Shutter,ISO,WB;
  public Conditions(string a,string s,string i,string w){Aperture=a;Shutter=s;ISO=i;WB=w;}
  public Conditions Copy(){return new Conditions(Aperture,Shutter,ISO,WB);}
 }
 static Dictionary<string,Conditions> presets=new Dictionary<string,Conditions>{{"기본 의류",new Conditions("f/8","1/125","100","5600 K")},{"어두운 의류",new Conditions("f/5.6","1/125","200","5600 K")},{"디테일 촬영",new Conditions("f/11","1/160","100","5200 K")}};
 static Conditions current=new Conditions("f/8","1/125","100","5600 K");static string applied="기본 의류";static int example=0;static bool syncing=false;
 static string[] frameNames={"FrameMain","FramePreview","FrameThumbF","FrameThumbB","FrameThumbD"};
 static double SourceRatio(int width,int height,int orientation){if(width<=0||height<=0)throw new ArgumentOutOfRangeException();return orientation>=5&&orientation<=8?(double)height/width:(double)width/height;}
 static double[] ratios={SourceRatio(4000,6000,1),SourceRatio(6000,4000,1),SourceRatio(4000,3000,1),SourceRatio(4000,4000,1),SourceRatio(6000,4000,6)};
 static void ApplyRatio(){foreach(string n in frameNames)C<CloriumG2.AspectFrame>(n).Ratio=ratios[example];}
 static void Fill(string name,string[] values,string selected){var c=C<ComboBox>(name);foreach(string v in values)c.Items.Add(v);c.SelectedItem=selected;}
 static void ReadConditions(){if(syncing||busy)return;current=new Conditions((string)C<ComboBox>("Aperture").SelectedItem,(string)C<ComboBox>("Shutter").SelectedItem,(string)C<ComboBox>("ISO").SelectedItem,(string)C<ComboBox>("WhiteBalance").SelectedItem);Label("PresetApplied","적용: "+applied+" · 조건 변경됨");}
 static void ApplyConditions(){syncing=true;C<ComboBox>("Aperture").SelectedItem=current.Aperture;C<ComboBox>("Shutter").SelectedItem=current.Shutter;C<ComboBox>("ISO").SelectedItem=current.ISO;C<ComboBox>("WhiteBalance").SelectedItem=current.WB;syncing=false;Label("PresetApplied","적용: "+applied);}
 static void LoadSelected(){if(busy)return;string selected=C<ComboBox>("PresetList").SelectedItem as string;if(selected==null)return;applied=selected;current=presets[selected].Copy();ApplyConditions();Label("Status","프리셋 ‘"+applied+"’ 불러오기 예시 · 실제 카메라에는 적용하지 않습니다.");}
 static bool SaveCurrent(string name){if(busy)return false;name=name.Trim();if(name.Length==0){Label("Status","저장할 프리셋 이름을 입력해 주세요.");return false;}if(presets.ContainsKey(name)){Label("Status","같은 이름의 프리셋이 있습니다. 새 이름으로 저장해 주세요.");return false;}presets[name]=current.Copy();applied=name;C<ComboBox>("PresetList").Items.Add(name);C<ComboBox>("PresetList").SelectedItem=name;Label("PresetApplied","적용: "+name);C<StackPanel>("PresetSavePane").Visibility=Visibility.Collapsed;Label("Status","프리셋 저장 예시 · 시안 실행 중에만 유지됩니다. 제품 설정 파일은 변경하지 않습니다.");return true;}
 static void WireExtras(){
  syncing=true;
  Fill("Aperture",new[]{"f/4","f/5.6","f/8","f/11","f/16"},current.Aperture);Fill("Shutter",new[]{"1/60","1/125","1/160","1/200"},current.Shutter);Fill("ISO",new[]{"100","200","400","800"},current.ISO);Fill("WhiteBalance",new[]{"자동","5200 K","5600 K","6000 K"},current.WB);
  foreach(string s in presets.Keys)C<ComboBox>("PresetList").Items.Add(s);C<ComboBox>("PresetList").SelectedItem=applied;Label("PresetApplied","적용: "+applied);
  Fill("FrameExample",new[]{"촬영 프레임 예시 · 세로 2:3","촬영 프레임 예시 · 가로 3:2","촬영 프레임 예시 · 가로 4:3","촬영 프레임 예시 · 정방형 1:1","방향 보정 예시 · 회전 2:3"},null);C<ComboBox>("FrameExample").SelectedIndex=example;ApplyRatio();
  syncing=false;
  foreach(string n in new[]{"Aperture","Shutter","ISO","WhiteBalance"})C<ComboBox>(n).SelectionChanged+=(s,e)=>ReadConditions();
  C<ComboBox>("FrameExample").SelectionChanged+=(s,e)=>{if(syncing)return;example=C<ComboBox>("FrameExample").SelectedIndex;if(example>=0)ApplyRatio();};
  C<Button>("LoadPreset").Click+=(s,e)=>LoadSelected();C<Button>("SavePreset").Click+=(s,e)=>{if(busy)return;C<StackPanel>("PresetSavePane").Visibility=C<StackPanel>("PresetSavePane").Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;};C<Button>("ConfirmPresetSave").Click+=(s,e)=>SaveCurrent(C<TextBox>("PresetName").Text);
 }
 static void CheckExtras(StringBuilder log){
  for(int d=1;d<=3;d++)foreach(bool black in new[]{false,true}){
   Select(d,black);
   foreach(Size size in new[]{new Size(1440,920),new Size(1200,760)})for(int a=0;a<ratios.Length;a++){
    example=a;ApplyRatio();C<CloriumG2.AspectFrame>("FramePreview").Visibility=Visibility.Visible;C<Border>("Overlay").Visibility=Visibility.Visible;C<Grid>("SettingsPane").Visibility=Visibility.Collapsed;
    shell.Width=size.Width;shell.Height=size.Height;shell.Measure(size);shell.Arrange(new Rect(size));shell.UpdateLayout();
    foreach(string n in frameNames){var f=C<CloriumG2.AspectFrame>(n);var bounds=f.Child.RenderSize;Check(bounds.Width>0&&bounds.Height>0&&Math.Abs(bounds.Width/bounds.Height-ratios[a])<0.003&&bounds.Width<=f.ActualWidth+1&&bounds.Height<=f.ActualHeight+1,"ratio "+d+"/"+black+"/"+size.Width+"/"+a+"/"+n,log);}
   }
  }
  example=0;product="32987";sequence=4;root=null;Select(2,false);C<ComboBox>("PresetList").SelectedItem="어두운 의류";LoadSelected();Check(current.ISO=="200"&&current.Aperture=="f/5.6","preset load conditions",log);
  C<ComboBox>("ISO").SelectedItem="400";Check(SaveCurrent("테스트 의류"),"preset save",log);C<ComboBox>("ISO").SelectedItem="100";LoadSelected();Check(current.ISO=="400","saved preset snapshot",log);Select(3,true);Check(applied=="테스트 의류"&&current.ISO=="400","theme/layout retains preset",log);
  Check(!SaveCurrent("")&&!SaveCurrent("기본 의류"),"empty/duplicate preset guarded",log);busy=true;Check(!SaveCurrent("촬영중 저장"),"busy preset guarded",log);busy=false;
  applied="기본 의류";current=presets[applied].Copy();presets.Remove("테스트 의류");
  for(int d=1;d<=3;d++)foreach(bool black in new[]{false,true}){root=null;Select(d,black);Render("design-"+d+(black?"-black":"-gray"),1440,920,2);}
  Select(2,true);example=1;ApplyRatio();Render("landscape-3-2",1440,920,2);example=0;ApplyRatio();Dialog(true);Render("portrait-preview",1440,920,2);C<Border>("Overlay").Visibility=Visibility.Collapsed;
 }
}
