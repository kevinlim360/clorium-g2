using System;using System.IO;using System.Text;using System.Security.Cryptography;
enum MessageBoxButton{OK} enum MessageBoxImage{Asterisk,Hand}
static class Messages{public static void Show(params object[] ignored){}}
class Legacy {
 public string Root,Encryption_key,Bios="TEST-BIOS",Mac="TEST-MAC";
 string GetBiosId(){return Bios;} string GetMacAddress(){return Mac;} string GetMainBoardId(){return "TEST-BOARD";} string GetDiskDriveId(){return "TEST-DISK";}
 void AttributesHide(string path){}public bool CheckLicense()
	{
		try
		{
			string unique_code = "";
			string whoesID = "";
			string information = "";
			switch (unique_code)
			{
			default:
				if (!(unique_code == "N/A"))
				{
					break;
				}
				goto case "";
			case "":
			case "Default string":
			case "To be filled by OEM":
				unique_code = GetBiosId();
				whoesID = "Bios ID";
				break;
			}
			switch (unique_code)
			{
			default:
				if (!(unique_code == "N/A"))
				{
					break;
				}
				goto case "";
			case "":
			case "Default string":
			case "To be filled by OEM":
				unique_code = GetMacAddress();
				whoesID = "Mac Address ID";
				break;
			}
			switch (unique_code)
			{
			default:
				if (!(unique_code == "N/A"))
				{
					break;
				}
				goto case "";
			case "":
			case "Default string":
			case "To be filled by OEM":
				unique_code = GetMainBoardId();
				whoesID = "Main Board ID";
				break;
			}
			switch (unique_code)
			{
			default:
				if (!(unique_code == "N/A"))
				{
					break;
				}
				goto case "";
			case "":
			case "Default string":
			case "To be filled by OEM":
				unique_code = GetDiskDriveId();
				whoesID = "Disk Drive ID";
				break;
			}
			switch (unique_code)
			{
			default:
				if (!(unique_code == "N/A"))
				{
					information = "[" + whoesID + "] " + unique_code;
					string sEncrypt = Encrypt(unique_code, Encryption_key);
					sEncrypt = RemoveSpecialCharacters(sEncrypt);
					string sPath = Root+"\\" + sEncrypt + ".lock";
					if (!File.Exists(sPath))
					{
						Messages.Show("\ud83d\udea8 라이센스키가 필요합니다.\n\n\ud83d\udea8 클로리움(주)에 ID 코드를 알려주고 키파일을 받으세요...\ud83d\udea8\n\n" + information, "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
						return false;
					}
					AttributesHide(sPath);
					return true;
				}
				goto case "";
			case "":
			case "Default string":
			case "To be filled by OEM":
				Messages.Show("\ud83d\udea8 본 PC는 라이센스를 발급할 수 없는 PC입니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
				return false;
			}
		}
		catch
		{
			Messages.Show("\ud83d\udea8 라이센스키 확인 중 에러가 발생했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
			return false;
		}
	}public string Encrypt(string textToEncrypt, string key)
	{
		RijndaelManaged rijndaelCipher = new RijndaelManaged();
		rijndaelCipher.Mode = CipherMode.CBC;
		rijndaelCipher.Padding = PaddingMode.PKCS7;
		rijndaelCipher.KeySize = 128;
		rijndaelCipher.BlockSize = 128;
		byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
		byte[] keyBytes = new byte[16];
		int len = pwdBytes.Length;
		if (len > keyBytes.Length)
		{
			len = keyBytes.Length;
		}
		Array.Copy(pwdBytes, keyBytes, len);
		rijndaelCipher.Key = keyBytes;
		rijndaelCipher.IV = keyBytes;
		ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
		byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);
		return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));
	}public string RemoveSpecialCharacters(string str)
	{
		char[] buffer = new char[str.Length];
		int idx = 0;
		foreach (char c in str)
		{
			if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
			{
				buffer[idx] = c;
				idx++;
			}
		}
		return new string(buffer, 0, idx);
	} static int Main(){
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