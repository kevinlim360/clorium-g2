$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$text=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'App.before.cs.txt'))
function Method([string]$signature){
 $start=$text.IndexOf($signature);if($start -lt 0){throw 'Method missing'}
 $brace=$text.IndexOf('{',$start);$level=1;$i=$brace+1
 while($level -gt 0){if($text[$i] -eq '{'){$level++};if($text[$i] -eq '}'){$level--};$i++}
 return $text.Substring($start,$i-$start)
}
$check=(Method 'private bool CheckLicense()').Replace('private bool','public bool').Replace('"D:\\"','Root+"\\"').Replace('System.Windows.MessageBox.Show','Messages.Show')
$body=@'
using System;using System.IO;using System.Text;using System.Security.Cryptography;
enum MessageBoxButton{OK} enum MessageBoxImage{Asterisk,Hand}
static class Messages{public static void Show(params object[] ignored){}}
class Legacy {
 public string Root,Encryption_key,Bios="TEST-BIOS",Mac="TEST-MAC";
 string GetBiosId(){return Bios;} string GetMacAddress(){return Mac;} string GetMainBoardId(){return "TEST-BOARD";} string GetDiskDriveId(){return "TEST-DISK";}
 void AttributesHide(string path){}
'@
$body+=$check+(Method 'public string Encrypt(')+(Method 'public string RemoveSpecialCharacters(')+@'
 static int Main(){
  var temp=Path.Combine(Path.GetTempPath(),"CloriumLicenseBaseline-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   var app=new Legacy{Root=temp,Encryption_key="TEST-KEY"};
   string mac=Path.Combine(temp,app.RemoveSpecialCharacters(app.Encrypt("TEST-MAC","TEST-KEY"))+".lock");File.WriteAllText(mac,"");
   bool rejected=!app.CheckLicense();Console.WriteLine("REPRODUCED: matching MAC license ignored when BIOS valid: "+rejected);if(!rejected)return 1;
   File.Delete(mac);string bios=Path.Combine(temp,app.RemoveSpecialCharacters(app.Encrypt("TEST-BIOS","TEST-KEY"))+".lock");File.WriteAllText(bios,"");
   if(!app.CheckLicense())return 2;Console.WriteLine("CONTROL: matching BIOS and configured key accepted");
   app.Encryption_key="";rejected=!app.CheckLicense();Console.WriteLine("REPRODUCED: missing launch environment rejects valid license: "+rejected);return rejected?0:3;
  }finally{Directory.Delete(temp,true);}
 }
}
'@
$file=Join-Path $PSScriptRoot 'LegacyReproduction.cs';[IO.File]::WriteAllText($file,$body)
$out=Join-Path $PSScriptRoot 'LegacyReproduction.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe "/out:$out" $file
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
& $out | Tee-Object -FilePath (Join-Path $PSScriptRoot 'baseline.log')
if($LASTEXITCODE -ne 0){throw 'Reproduction failed'}
