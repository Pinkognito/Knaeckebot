using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using Knaeckebot.Models;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Service for keyboard inputs - Highly optimized version
    /// </summary>
    public partial class KeyboardService
    {
        #region Singleton

        private static readonly Lazy<KeyboardService> _instance = new Lazy<KeyboardService>(() => new KeyboardService());

        /// <summary>
        /// Singleton instance of KeyboardService
        /// </summary>
        public static KeyboardService Instance => _instance.Value;

        #endregion

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern int SendInput(int nInputs, ref INPUT pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        // Constant definitions
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint INPUT_KEYBOARD = 1;

        #endregion

        #region Events and Properties

        /// <summary>
        /// Event triggered when a keyboard input is recorded
        /// </summary>
        public event EventHandler<KeyboardAction>? KeyActionRecorded;

        /// <summary>
        /// Event triggered when a key combination is recorded
        /// </summary>
        public event EventHandler<KeyboardAction>? KeyCombinationRecorded;

        private bool _isRecording = false;
        private DateTime _lastActionTime;
        private GlobalKeyboardHook? _keyboardHook;
        private bool _isTextBuffer = false;
        private string _currentTextBuffer = "";
        private System.Timers.Timer _textBufferTimer;
        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;
        private bool _isShiftPressed = false;

        /// <summary>
        /// Indicates if recording is currently active
        /// </summary>
        public bool IsRecording => _isRecording;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        private KeyboardService()
        {
            // Initialize timer for text buffering
            _textBufferTimer = new System.Timers.Timer(750); // 750ms buffer for text inputs
            _textBufferTimer.Elapsed += (s, e) => FlushTextBuffer();
            _textBufferTimer.AutoReset = false;
        }

        /// <summary>
        /// Executes a keyboard action
        /// </summary>
        public void ExecuteKeyboardAction(KeyboardAction action)
        {
            if (action == null) return;

            try
            {
                switch (action.ActionType)
                {
                    case KeyboardActionType.TypeText:
                        if (action.UseClipboard)
                        {
                            // Directly paste from clipboard without detour
                            PasteFromClipboard();
                        }
                        else
                        {
                            // Type text with the specified speed
                            TypeText(action.Text, action.DelayBetweenChars);
                        }
                        break;

                    case KeyboardActionType.KeyPress:
                        if (action.Keys != null)
                        {
                            foreach (var key in action.Keys)
                            {
                                PressKey(key);
                            }
                        }
                        break;

                    case KeyboardActionType.KeyCombination:
                    case KeyboardActionType.Hotkey:
                        if (action.Keys != null)
                        {
                            PressKeyCombination(action.Keys);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing keyboard action: {ex.Message}", LogLevel.Error);
                throw;
            }
        }
    }
}