using System;
using System.Threading;
using System.Windows.Input;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Part of the KeyboardService that provides keyboard input execution
    /// </summary>
    public partial class KeyboardService
    {
        #region Execution Methods

        /// <summary>
        /// Types text via keyboard with correct speed
        /// </summary>
        /// <param name="text">Text to type</param>
        /// <param name="delayBetweenChars">Delay between characters (ms)</param>
        public void TypeText(string? text, int delayBetweenChars = 10)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                // Text input directly without using clipboard
                foreach (char c in text)
                {
                    // For special characters we sometimes need to use special SendKeys sequences
                    string sendKeysChar = c switch
                    {
                        '+' => "{+}",  // + is a special character in SendKeys
                        '^' => "{^}",  // ^ is a special character in SendKeys
                        '%' => "{%}",  // % is a special character in SendKeys
                        '~' => "{~}",  // ~ is a special character in SendKeys
                        '{' => "{{}",  // { is a special character in SendKeys
                        '}' => "{}}",  // } is a special character in SendKeys
                        '[' => "{[}",  // [ could be problematic in some cases
                        ']' => "{]}",  // ] could be problematic in some cases
                        '(' => "{(}",  // ( could be problematic in some cases
                        ')' => "{)}",  // ) could be problematic in some cases
                        _ => c.ToString()  // Use all other characters directly
                    };

                    // Send text character by character
                    System.Windows.Forms.SendKeys.SendWait(sendKeysChar);

                    // Delay between characters with the specified value
                    if (delayBetweenChars > 0)
                    {
                        Thread.Sleep(delayBetweenChars);
                    }
                }

                LogManager.Log($"Text typed: {text}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error typing text: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Presses a single key
        /// </summary>
        /// <param name="key">Key to press</param>
        public void PressKey(Key key)
        {
            try
            {
                // Press and release key
                byte vkCode = (byte)KeyInterop.VirtualKeyFromKey(key);
                keybd_event(vkCode, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
                Thread.Sleep(30);
                keybd_event(vkCode, 0, KEYEVENTF_KEYUP, IntPtr.Zero);

                LogManager.Log($"Key pressed: {key}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error pressing key: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Presses a key combination
        /// </summary>
        /// <param name="keys">Keys to press</param>
        public void PressKeyCombination(Key[] keys)
        {
            if (keys == null || keys.Length == 0) return;

            try
            {
                byte[] vkCodes = new byte[keys.Length];

                // Convert all keys to virtual key codes
                for (int i = 0; i < keys.Length; i++)
                {
                    vkCodes[i] = (byte)KeyInterop.VirtualKeyFromKey(keys[i]);
                }

                // Press all keys in sequence
                for (int i = 0; i < vkCodes.Length; i++)
                {
                    keybd_event(vkCodes[i], 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
                    Thread.Sleep(30);  // Short delay between key presses
                }

                // Longer delay while all keys are pressed to ensure the combination is registered
                Thread.Sleep(100);

                // Release all keys in reverse order
                for (int i = vkCodes.Length - 1; i >= 0; i--)
                {
                    keybd_event(vkCodes[i], 0, KEYEVENTF_KEYUP, IntPtr.Zero);
                    Thread.Sleep(30);  // Short delay between key releases
                }

                LogManager.Log($"Key combination pressed: {string.Join("+", keys)}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in key combination: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Pastes text from clipboard (CTRL+V)
        /// </summary>
        public void PasteFromClipboard()
        {
            try
            {
                // Execute in UI thread if not already in STA thread
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PressKeyCombination(new Key[] { Key.LeftCtrl, Key.V });
                    });
                }
                else
                {
                    PressKeyCombination(new Key[] { Key.LeftCtrl, Key.V });
                }

                // Small delay after pasting
                Thread.Sleep(100);

                LogManager.Log("Text pasted from clipboard");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error pasting from clipboard: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Copies selected text to clipboard (CTRL+C)
        /// </summary>
        public void CopyToClipboard()
        {
            try
            {
                PressKeyCombination(new Key[] { Key.LeftCtrl, Key.C });
                // Small delay to allow clipboard to update
                Thread.Sleep(100);
                LogManager.Log("Text copied to clipboard");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error copying to clipboard: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Sets text to clipboard and then pastes it
        /// </summary>
        /// <param name="text">Text to paste</param>
        public void SetClipboardTextAndPaste(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                // Clipboard operations must be done in an STA thread
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    // Move clipboard operations to the UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PerformClipboardOperations(text);
                    });
                }
                else
                {
                    // Execute directly as we're already in an STA thread
                    PerformClipboardOperations(text);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error pasting from clipboard: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Performs the actual clipboard operations
        /// </summary>
        private void PerformClipboardOperations(string text)
        {
            string? originalClipboardText = null;
            bool restoreClipboard = false;

            // Save current clipboard content
            try
            {
                if (System.Windows.Forms.Clipboard.ContainsText())
                {
                    originalClipboardText = System.Windows.Forms.Clipboard.GetText();
                    restoreClipboard = true;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Could not read clipboard: {ex.Message}", LogLevel.Warning);
                restoreClipboard = false;
            }

            // Set text to clipboard
            System.Windows.Forms.Clipboard.SetText(text);
            Thread.Sleep(50);

            // Paste with CTRL+V
            PressKeyCombination(new Key[] { Key.LeftCtrl, Key.V });
            Thread.Sleep(100);

            LogManager.Log($"Text set to clipboard and pasted: {(text.Length > 30 ? text.Substring(0, 27) + "..." : text)}");

            // Restore original clipboard content
            if (restoreClipboard && originalClipboardText != null)
            {
                try
                {
                    Thread.Sleep(50);
                    System.Windows.Forms.Clipboard.SetText(originalClipboardText);
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Could not restore clipboard: {ex.Message}", LogLevel.Warning);
                }
            }
        }

        #endregion
    }
}