using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CameraControl.Core.Classes;

public class KeyboardHookEventArgs
{
	private enum VirtualKeyStates
	{
		VK_LWIN = 91,
		VK_RWIN = 92,
		VK_LSHIFT = 160,
		VK_RSHIFT = 161,
		VK_LCONTROL = 162,
		VK_RCONTROL = 163,
		VK_LALT = 164,
		VK_RALT = 165
	}

	private const int KEY_PRESSED = 32768;

	public Keys Key { get; private set; }

	public bool isAltPressed => isLAltPressed || isRAltPressed;

	public bool isLAltPressed { get; private set; }

	public bool isRAltPressed { get; private set; }

	public bool isCtrlPressed => isLCtrlPressed || isRCtrlPressed;

	public bool isLCtrlPressed { get; private set; }

	public bool isRCtrlPressed { get; private set; }

	public bool isShiftPressed => isLShiftPressed || isRShiftPressed;

	public bool isLShiftPressed { get; private set; }

	public bool isRShiftPressed { get; private set; }

	public bool isWinPressed => isLWinPressed || isRWinPressed;

	public bool isLWinPressed { get; private set; }

	public bool isRWinPressed { get; private set; }

	[DllImport("user32.dll")]
	private static extern short GetKeyState(VirtualKeyStates nVirtKey);

	internal KeyboardHookEventArgs(KeyboardHook.KBDLLHOOKSTRUCT lParam)
	{
		Key = (Keys)lParam.vkCode;
		isLAltPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_LALT) & 0x8000) || Key == Keys.LMenu;
		isRAltPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_RALT) & 0x8000) || Key == Keys.RMenu;
		isLCtrlPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_LCONTROL) & 0x8000) || Key == Keys.LControlKey;
		isRCtrlPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_RCONTROL) & 0x8000) || Key == Keys.RControlKey;
		isLShiftPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_LSHIFT) & 0x8000) || Key == Keys.LShiftKey;
		isRShiftPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_RSHIFT) & 0x8000) || Key == Keys.RShiftKey;
		isLWinPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_LWIN) & 0x8000) || Key == Keys.LWin;
		isRWinPressed = Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_RWIN) & 0x8000) || Key == Keys.RWin;
		if (new Keys[8]
		{
			Keys.LMenu,
			Keys.RMenu,
			Keys.LControlKey,
			Keys.RControlKey,
			Keys.LShiftKey,
			Keys.RShiftKey,
			Keys.LWin,
			Keys.RWin
		}.Contains(Key))
		{
			Key = Keys.None;
		}
	}

	public override string ToString()
	{
		return $"Key={Key}; Win={isWinPressed}; Alt={isAltPressed}; Ctrl={isCtrlPressed}; Shift={isShiftPressed}";
	}
}
