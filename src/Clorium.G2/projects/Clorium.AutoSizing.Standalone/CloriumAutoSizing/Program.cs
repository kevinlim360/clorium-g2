using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CloriumAutoSizing;

internal static class Program
{
	private static Mutex mutex = new Mutex(initiallyOwned: true, "MyUniqueAppMutex");

	private static readonly string Encryption_key1 = Environment.GetEnvironmentVariable("CLORIUM_AUTOSIZING_STANDALONE_LICENSE_KEY1");

	private static readonly string Encryption_key2 = Environment.GetEnvironmentVariable("CLORIUM_AUTOSIZING_STANDALONE_LICENSE_KEY2");

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetProcessDpiAwarenessContext(int dpiContext);

	[STAThread]
	private static void Main()
	{
		SetProcessDpiAwarenessContext(-1);
		if (!mutex.WaitOne(TimeSpan.Zero, exitContext: true))
		{
			MessageBox.Show("이미 프로그램이 실행 중입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
			return;
		}
		if (!CheckLicense())
		{
			Environment.Exit(0);
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new F_Measure());
		mutex.ReleaseMutex();
	}

	// 유지보수 설명(복구 후 추가): 원본 하드웨어 식별자 선택과 암호화된 라이선스 파일명 확인을 유지한다. 두 판본의 키/허용 파일 조건을 혼합하지 않는다. 비밀값 외부분리는 별도 단계이며 새 키 생성이나 라이선스 우회는 하지 않는다.
	private static bool CheckLicense()
	{
		// 유지보수 설정 변경: 두 기존 키 중 사용할 라이선스에 맞는 키만 공급해도 원래 OR 조건을 유지합니다.
		if (string.IsNullOrEmpty(Encryption_key1) && string.IsNullOrEmpty(Encryption_key2))
		{
			MessageBox.Show("기존 라이선스 키 설정이 필요합니다: CLORIUM_AUTOSIZING_STANDALONE_LICENSE_KEY1 또는 CLORIUM_AUTOSIZING_STANDALONE_LICENSE_KEY2");
			return false;
		}
		try
		{
			string hardwareIdentifier = "";
			string hardwareSourceLabel = "";
			string hardwareDiagnostic = "";
			if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
			{
				switch (hardwareIdentifier)
				{
				case "Default string":
				case "To be filled by OEM":
				case "N/A":
					break;
				default:
					goto IL_004d;
				}
			}
			hardwareIdentifier = GetMainBoardId();
			hardwareSourceLabel = "Main Board ID";
			goto IL_004d;
			IL_0088:
			if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
			{
				switch (hardwareIdentifier)
				{
				case "Default string":
				case "To be filled by OEM":
				case "N/A":
					break;
				default:
					goto IL_00c3;
				}
			}
			hardwareIdentifier = GetDiskDriveId();
			hardwareSourceLabel = "Disk Drive ID";
			goto IL_00c3;
			IL_00fe:
			if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
			{
				switch (hardwareIdentifier)
				{
				case "Default string":
				case "To be filled by OEM":
				case "N/A":
					break;
				default:
				{
					hardwareDiagnostic = "[" + hardwareSourceLabel + "] " + hardwareIdentifier;
					string firstLicenseNameToken = string.IsNullOrEmpty(Encryption_key1) ? null : Encrypt(hardwareIdentifier, Encryption_key1);
					string secondLicenseNameToken = string.IsNullOrEmpty(Encryption_key2) ? null : Encrypt(hardwareIdentifier, Encryption_key2);
					firstLicenseNameToken = (firstLicenseNameToken == null) ? null : RemoveSpecialCharacters(firstLicenseNameToken);
					secondLicenseNameToken = (secondLicenseNameToken == null) ? null : RemoveSpecialCharacters(secondLicenseNameToken);
					string firstLicenseFilePath = (firstLicenseNameToken == null) ? null : ("D:\\" + firstLicenseNameToken + ".lock");
					string secondLicenseFilePath = (secondLicenseNameToken == null) ? null : ("D:\\" + secondLicenseNameToken + ".lock");
					if (!File.Exists(firstLicenseFilePath) && !File.Exists(secondLicenseFilePath))
					{
						MessageBox.Show("\ud83d\udea8 버전 3.0.0 라이센스키가 필요합니다.\n\n\ud83d\udea8 (주)포비스에 ID 코드를 알려주고 키파일을 받으세요...\ud83d\udea8\n\n" + hardwareDiagnostic);
						return false;
					}
					if (File.Exists(firstLicenseFilePath))
					{
						AttributesHide(firstLicenseFilePath);
					}
					if (File.Exists(secondLicenseFilePath))
					{
						AttributesHide(secondLicenseFilePath);
					}
					return true;
				}
				}
			}
			MessageBox.Show("\ud83d\udea8 본 PC는 라이센스를 발급할 수 없는 PC입니다.");
			return false;
			IL_004d:
			if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
			{
				switch (hardwareIdentifier)
				{
				case "Default string":
				case "To be filled by OEM":
				case "N/A":
					break;
				default:
					goto IL_0088;
				}
			}
			hardwareIdentifier = GetBiosId();
			hardwareSourceLabel = "Bios ID";
			goto IL_0088;
			IL_00c3:
			if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
			{
				switch (hardwareIdentifier)
				{
				case "Default string":
				case "To be filled by OEM":
				case "N/A":
					break;
				default:
					goto IL_00fe;
				}
			}
			hardwareIdentifier = GetMacAddress();
			hardwareSourceLabel = "Mac Address ID";
			goto IL_00fe;
		}
		catch
		{
			MessageBox.Show("\ud83d\udea8 라이센스키 확인 중 에러가 발생했습니다.");
			return false;
		}
	}

	private static void AttributesHide(string filePath)
	{
		FileAttributes fileAttributes = File.GetAttributes(filePath) | FileAttributes.Hidden;
		File.SetAttributes(filePath, fileAttributes);
	}

	// 유지보수 설명(복구 후 추가): UTF8 키를 최대 16바이트 복사한 배열을 원본과 동일하게 CBC Key와 IV 모두에 사용한다. 라이선스 파일명 호환을 위한 기존 알고리즘이며 보안 개선으로 해석하지 않는다. 공개 매개변수 이름을 보존한다.
	public static string Encrypt(string textToEncrypt, string key)
	{
		RijndaelManaged rijndaelManaged = new RijndaelManaged
		{
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7,
			KeySize = 128,
			BlockSize = 128
		};
		byte[] utf8KeyBytes = Encoding.UTF8.GetBytes(key);
		byte[] keyAndIvBytes = new byte[16];
		int keyByteCopyCount = utf8KeyBytes.Length;
		if (keyByteCopyCount > keyAndIvBytes.Length)
		{
			keyByteCopyCount = keyAndIvBytes.Length;
		}
		Array.Copy(utf8KeyBytes, keyAndIvBytes, keyByteCopyCount);
		rijndaelManaged.Key = keyAndIvBytes;
		rijndaelManaged.IV = keyAndIvBytes;
		ICryptoTransform cryptoTransform = rijndaelManaged.CreateEncryptor();
		byte[] plaintextBytes = Encoding.UTF8.GetBytes(textToEncrypt);
		return Convert.ToBase64String(cryptoTransform.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length));
	}

	// 유지보수 설명(복구 후 추가): 원본 Base64/CBC/PKCS7 복호화 계약을 유지한다. UTF8 키의 16바이트 절삭/0 채움과 Key/IV 동일 사용을 Encrypt와 맞춘다.
	public static string Decrypt(string textToDecrypt, string key)
	{
		RijndaelManaged rijndaelManaged = new RijndaelManaged
		{
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7,
			KeySize = 128,
			BlockSize = 128
		};
		byte[] ciphertextBytes = Convert.FromBase64String(textToDecrypt);
		byte[] utf8KeyBytes = Encoding.UTF8.GetBytes(key);
		byte[] keyAndIvBytes = new byte[16];
		int keyByteCopyCount = utf8KeyBytes.Length;
		if (keyByteCopyCount > keyAndIvBytes.Length)
		{
			keyByteCopyCount = keyAndIvBytes.Length;
		}
		Array.Copy(utf8KeyBytes, keyAndIvBytes, keyByteCopyCount);
		rijndaelManaged.Key = keyAndIvBytes;
		rijndaelManaged.IV = keyAndIvBytes;
		byte[] plaintextBytes = rijndaelManaged.CreateDecryptor().TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
		return Encoding.UTF8.GetString(plaintextBytes);
	}

	// 유지보수 설명(복구 후 추가): 라이선스 파일명에 허용된 문자만 원본 순서대로 남긴다. 허용 문자 집합은 기존 파일명과 연결되므로 넓히거나 바꾸지 않는다.
	public static string RemoveSpecialCharacters(string str)
	{
		char[] allowedCharacters = new char[str.Length];
		int writtenCharacterCount = 0;
		foreach (char c in str)
		{
			if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
			{
				allowedCharacters[writtenCharacterCount] = c;
				writtenCharacterCount++;
			}
		}
		return new string(allowedCharacters, 0, writtenCharacterCount);
	}

	public static string GetMainBoardId()
	{
		try
		{
			string result = string.Empty;
			using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard").Get().GetEnumerator())
			{
				if (managementObjectEnumerator.MoveNext())
				{
					result = ((ManagementObject)managementObjectEnumerator.Current)["SerialNumber"].ToString().Trim();
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
			return "";
		}
	}

	public static string GetBiosId()
	{
		try
		{
			string result = string.Empty;
			using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS").Get().GetEnumerator())
			{
				if (managementObjectEnumerator.MoveNext())
				{
					result = ((ManagementObject)managementObjectEnumerator.Current)["SerialNumber"].ToString().Trim();
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
			return "";
		}
	}

	public static string GetMacAddress()
	{
		try
		{
			string result = string.Empty;
			foreach (ManagementObject item in new ManagementObjectSearcher("SELECT MACAddress, IPEnabled FROM Win32_NetworkAdapterConfiguration").Get())
			{
				if (item["IPEnabled"] != null && (bool)item["IPEnabled"])
				{
					result = item["MACAddress"].ToString().Trim();
					break;
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
			return "";
		}
	}

	public static string GetDiskDriveId()
	{
		try
		{
			string result = string.Empty;
			foreach (ManagementObject item in new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive").Get())
			{
				if (item["SerialNumber"] != null)
				{
					result = item["SerialNumber"].ToString().Trim();
					break;
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
			return "";
		}
	}
}
