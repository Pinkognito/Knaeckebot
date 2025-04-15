using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Knaeckebot.Models;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Service for mouse operations
    /// </summary>
    public class MouseService
    {
        #region Singleton

        private static readonly Lazy<MouseService> _instance = new Lazy<MouseService>(() => new MouseService());

        /// <summary>
        /// Singleton instance of MouseService
        /// </summary>
        public static MouseService Instance => _instance.Value;

        #endregion

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;  // For middle click (mouse wheel click)
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;    // For middle click (mouse wheel click)
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        #endregion

        /// <summary>
        /// Event triggered when a mouse click is recorded
        /// </summary>
        public event EventHandler<MouseAction>? MouseActionRecorded;

        /// <summary>
        /// Event triggered when a mouse wheel event is recorded
        /// </summary>
        public event EventHandler<MouseAction>? MouseWheelRecorded;

        private bool _isRecording = false;
        private DateTime _lastActionTime;
        private GlobalMouseHook? _mouseHook;

        /// <summary>
        /// Indicates whether recording is currently active
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Constructor
        /// </summary>
        private MouseService()
        {
        }

        /// <summary>
        /// Starts recording mouse clicks
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording) return;

            _isRecording = true;
            _lastActionTime = DateTime.Now;

            // Start global mouse hook
            _mouseHook = new GlobalMouseHook();
            _mouseHook.MouseDown += MouseHook_MouseDown;
            _mouseHook.MouseWheel += MouseHook_MouseWheel;

            LogManager.Log("Mouse recording started");
        }

        /// <summary>
        /// Stops recording mouse clicks
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording) return;

            _isRecording = false;

            if (_mouseHook != null)
            {
                _mouseHook.MouseDown -= MouseHook_MouseDown;
                _mouseHook.MouseWheel -= MouseHook_MouseWheel;
                _mouseHook.Dispose();
                _mouseHook = null;
            }

            LogManager.Log("Mouse recording stopped");
        }

        /// <summary>
        /// Handles mouse click events during recording
        /// </summary>
        private void MouseHook_MouseDown(object? sender, MouseHookEventArgs e)
        {
            if (!_isRecording) return;

            var now = DateTime.Now;
            int delayMs = (int)(now - _lastActionTime).TotalMilliseconds;
            _lastActionTime = now;

            // Determine the type of mouse click based on the message
            MouseActionType actionType;

            if (e.Message == 0x0201) // WM_LBUTTONDOWN
            {
                actionType = MouseActionType.LeftClick;
            }
            else if (e.Message == 0x0204) // WM_RBUTTONDOWN
            {
                actionType = MouseActionType.RightClick;
            }
            else if (e.Message == 0x0207) // WM_MBUTTONDOWN
            {
                actionType = MouseActionType.MiddleClick;
            }
            else
            {
                // Default: Left click if the message is not recognized
                actionType = MouseActionType.LeftClick;
            }

            // Create new mouse action
            var action = new MouseAction
            {
                X = e.X,
                Y = e.Y,
                DelayBefore = delayMs,
                ActionType = actionType,
                Name = $"Click at ({e.X}, {e.Y})",
                Description = $"{actionType} at position ({e.X}, {e.Y})"
            };

            LogManager.Log($"Mouse click recorded: {action.ActionType} at position ({e.X}, {e.Y}) with delay {delayMs} ms");

            // Trigger event
            MouseActionRecorded?.Invoke(this, action);
        }

        /// <summary>
        /// Handles mouse wheel events during recording
        /// </summary>
        private void MouseHook_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (!_isRecording) return;

            var now = DateTime.Now;
            int delayMs = (int)(now - _lastActionTime).TotalMilliseconds;
            _lastActionTime = now;

            // Create new mouse wheel action
            var action = new MouseAction
            {
                X = e.X,
                Y = e.Y,
                DelayBefore = delayMs,
                ActionType = MouseActionType.MouseWheel,
                WheelDelta = e.Delta,
                Name = $"Mouse wheel at ({e.X}, {e.Y})",
                Description = $"Mouse wheel {(e.Delta > 0 ? "up" : "down")} at position ({e.X}, {e.Y}), Delta: {e.Delta}"
            };

            LogManager.Log($"Mouse wheel recorded: Delta {e.Delta} at position ({e.X}, {e.Y}) with delay {delayMs} ms");

            // Trigger event
            MouseWheelRecorded?.Invoke(this, action);
        }

        /// <summary>
        /// Executes a mouse action
        /// </summary>
        public void ExecuteMouseAction(MouseAction action)
        {
            if (action == null) return;

            try
            {
                // Move cursor to the specified position
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(action.X, action.Y);

                // Delay for mouse movement
                Task.Delay(50).Wait();

                // Execute the appropriate mouse action based on the action type
                switch (action.ActionType)
                {
                    case MouseActionType.LeftClick:
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.RightClick:
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, IntPtr.Zero);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.MiddleClick:
                        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, IntPtr.Zero);
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.DoubleClick:
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                        Task.Delay(50).Wait();
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.MouseDown:
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.MouseUp:
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                        break;

                    case MouseActionType.MouseMove:
                        // Just mouse movement, no click needed
                        break;

                    case MouseActionType.MouseWheel:
                        ScrollMouseWheel(action.X, action.Y, action.WheelDelta);
                        break;
                }

                LogManager.Log($"Mouse action executed: {action.ActionType} at position ({action.X}, {action.Y})");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing mouse action: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Scrolls the mouse wheel at a specific position
        /// </summary>
        public void ScrollMouseWheel(int x, int y, int delta)
        {
            try
            {
                // Move cursor to the specified position
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);

                // Small delay for mouse movement
                Task.Delay(50).Wait();

                // Trigger mouse wheel event
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, IntPtr.Zero);

                LogManager.Log($"Mouse wheel action executed: Delta {delta} at position ({x}, {y})");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in mouse wheel action: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Clicks on a screen point
        /// </summary>
        public void ClickOnPosition(int x, int y, MouseActionType clickType = MouseActionType.LeftClick)
        {
            var action = new MouseAction
            {
                X = x,
                Y = y,
                ActionType = clickType
            };

            ExecuteMouseAction(action);
        }

        /// <summary>
        /// Clicks on a position based on relative browser coordinates
        /// </summary>
        public void ClickOnRelativeCoordinates(int relX, int relY)
        {
            try
            {
                // Find browser window (supports different browsers)
                var browserProcess = Process.GetProcessesByName("chrome").FirstOrDefault() ??
                                    Process.GetProcessesByName("msedge").FirstOrDefault() ??
                                    Process.GetProcessesByName("firefox").FirstOrDefault();

                if (browserProcess == null)
                {
                    LogManager.Log("No supported browser found", LogLevel.Warning);
                    return;
                }

                // Get browser window position
                IntPtr browserHandle = browserProcess.MainWindowHandle;
                RECT rect = new RECT();
                GetWindowRect(browserHandle, ref rect);

                // Browser header height (adjustable)
                int browserHeaderHeight = 72;

                // Convert relative coordinates to absolute screen coordinates
                int screenX = rect.Left + relX;
                int screenY = rect.Top + browserHeaderHeight + relY;

                // Click on the calculated position
                ClickOnPosition(screenX, screenY);

                LogManager.Log($"Click on relative browser position: ({relX}, {relY}) -> absolute: ({screenX}, {screenY})");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error clicking on relative coordinates: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #region Win32 Helper Functions

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }

    /// <summary>
    /// Global mouse hook to capture mouse actions
    /// </summary>
    public class GlobalMouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<MouseHookEventArgs>? MouseDown;
        public event EventHandler<MouseWheelEventArgs>? MouseWheel;

        public GlobalMouseHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;  // For middle click (mouse wheel click)
        private const int WM_MOUSEWHEEL = 0x020A;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN ||
                    wParam == (IntPtr)WM_RBUTTONDOWN ||
                    wParam == (IntPtr)WM_MBUTTONDOWN)  // Detect middle click
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    MouseDown?.Invoke(this, new MouseHookEventArgs
                    {
                        Message = wParam.ToInt32(),
                        X = hookStruct.pt.x,
                        Y = hookStruct.pt.y
                    });
                }
                else if (wParam == (IntPtr)WM_MOUSEWHEEL)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    // The mouseData field contains the scroll value in the high word
                    int wheelDelta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                    MouseWheel?.Invoke(this, new MouseWheelEventArgs
                    {
                        X = hookStruct.pt.x,
                        Y = hookStruct.pt.y,
                        Delta = wheelDelta
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
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }

    /// <summary>
    /// Event arguments for mouse hook events
    /// </summary>
    public class MouseHookEventArgs : EventArgs
    {
        public int Message { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// Event arguments for mouse wheel events
    /// </summary>
    public class MouseWheelEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Delta { get; set; }
    }
}