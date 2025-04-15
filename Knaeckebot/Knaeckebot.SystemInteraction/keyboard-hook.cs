using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Global keyboard hook for intercepting keyboard inputs
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<KeyboardHookEventArgs>? KeyDown;
        public event EventHandler<KeyboardHookEventArgs>? KeyUp;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    KeyDown?.Invoke(this, new KeyboardHookEventArgs
                    {
                        Key = key,
                        IsSystemKey = wParam == (IntPtr)WM_SYSKEYDOWN
                    });
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    KeyUp?.Invoke(this, new KeyboardHookEventArgs
                    {
                        Key = key,
                        IsSystemKey = wParam == (IntPtr)WM_SYSKEYUP
                    });
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }

    /// <summary>
    /// Event arguments for keyboard hook events
    /// </summary>
    public class KeyboardHookEventArgs : EventArgs
    {
        public Key Key { get; set; }
        public bool IsSystemKey { get; set; }
    }
}