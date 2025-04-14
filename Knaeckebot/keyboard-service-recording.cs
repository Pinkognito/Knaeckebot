using System;
using System.Collections.Generic;
using System.Windows.Input;
using Knaeckebot.Models;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Part of the KeyboardService that provides recording functionality
    /// </summary>
    public partial class KeyboardService
    {
        #region Recording Methods

        /// <summary>
        /// Starts recording keyboard inputs
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording) return;

            _isRecording = true;
            _lastActionTime = DateTime.Now;
            _currentTextBuffer = "";
            _isTextBuffer = false;

            // Start global keyboard hook
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.KeyUp += KeyboardHook_KeyUp;

            LogManager.Log("Keyboard recording started");
        }

        /// <summary>
        /// Stops recording keyboard inputs
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording) return;

            _isRecording = false;

            // Process any remaining text in the buffer
            FlushTextBuffer();

            if (_keyboardHook != null)
            {
                _keyboardHook.KeyDown -= KeyboardHook_KeyDown;
                _keyboardHook.KeyUp -= KeyboardHook_KeyUp;
                _keyboardHook.Dispose();
                _keyboardHook = null;
            }

            LogManager.Log("Keyboard recording stopped");
        }

        /// <summary>
        /// Handles key presses during recording
        /// </summary>
        private void KeyboardHook_KeyDown(object? sender, KeyboardHookEventArgs e)
        {
            if (!_isRecording) return;

            // Update modifier keys state
            UpdateModifierKeyState(e.Key, true);

            // Check if a key combination like Ctrl+C is being used
            if (IsModifierPressed())
            {
                // Clear text buffer as we're switching from text input to key combination
                FlushTextBuffer();

                // We collect keys and wait for KeyUp events
                if (!IsStandardModifierKey(e.Key))
                {
                    // Process key combination
                    RecordKeyCombination(e.Key);
                }
            }
            else if (IsPrintableKey(e.Key))
            {
                // If it's a printable character, add to text buffer
                HandleTextInput(e.Key);
            }
            else
            {
                // Clear text buffer as we're switching from text input to single key
                FlushTextBuffer();

                // Record single non-printable key (Enter, Tab, etc.)
                RecordSingleKey(e.Key);
            }
        }

        /// <summary>
        /// Handles key releases during recording
        /// </summary>
        private void KeyboardHook_KeyUp(object? sender, KeyboardHookEventArgs e)
        {
            if (!_isRecording) return;

            // Update modifier keys state
            UpdateModifierKeyState(e.Key, false);
        }

        /// <summary>
        /// Updates the state of modifier keys (Ctrl, Alt, Shift)
        /// </summary>
        private void UpdateModifierKeyState(Key key, bool isPressed)
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _isCtrlPressed = isPressed;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    _isAltPressed = isPressed;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    _isShiftPressed = isPressed;
                    break;
            }
        }

        /// <summary>
        /// Checks if a modifier key is pressed
        /// </summary>
        private bool IsModifierPressed()
        {
            return _isCtrlPressed || _isAltPressed;
        }

        /// <summary>
        /// Checks if the key is a modifier key
        /// </summary>
        private bool IsStandardModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift;
        }

        /// <summary>
        /// Checks if the key produces a printable character
        /// </summary>
        private bool IsPrintableKey(Key key)
        {
            // Letters, numbers, punctuation, etc.
            return (key >= Key.A && key <= Key.Z) ||
                   (key >= Key.D0 && key <= Key.D9) ||
                   (key >= Key.NumPad0 && key <= Key.NumPad9) ||
                   key == Key.Space || key == Key.OemPeriod ||
                   key == Key.OemComma || key == Key.OemMinus ||
                   key == Key.OemPlus || key == Key.OemQuestion ||
                   key == Key.Oem1 || key == Key.Oem2 ||
                   key == Key.Oem3 || key == Key.Oem4 ||
                   key == Key.Oem5 || key == Key.Oem6 ||
                   key == Key.Oem7 || key == Key.Oem8;
        }

        /// <summary>
        /// Processes text inputs and adds them to the buffer
        /// </summary>
        private void HandleTextInput(Key key)
        {
            // Reset timer
            _textBufferTimer.Stop();

            // If we're not in text buffer mode yet, activate it now
            if (!_isTextBuffer)
            {
                _isTextBuffer = true;
                _currentTextBuffer = "";
            }

            // Convert key to character and add to buffer
            char? character = KeyToChar(key, _isShiftPressed);
            if (character.HasValue)
            {
                _currentTextBuffer += character.Value;
            }

            // Start timer to flush buffer after inactivity
            _textBufferTimer.Start();
        }

        /// <summary>
        /// Converts a Key enumeration to a character
        /// </summary>
        private char? KeyToChar(Key key, bool shift)
        {
            // Simple conversion for common keys
            if (key >= Key.A && key <= Key.Z)
            {
                return (char)(shift ? 'A' + (key - Key.A) : 'a' + (key - Key.A));
            }
            else if (key >= Key.D0 && key <= Key.D9 && !shift)
            {
                return (char)('0' + (key - Key.D0));
            }
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return (char)('0' + (key - Key.NumPad0));
            }
            else if (key == Key.Space)
            {
                return ' ';
            }
            // German keyboard specifics
            else if (key == Key.OemPeriod)
            {
                return shift ? ':' : '.';
            }
            else if (key == Key.OemComma)
            {
                return shift ? ';' : ',';
            }
            else if (key == Key.OemMinus)
            {
                return shift ? '_' : '-';
            }
            // Additional special cases for German keyboard could be added here

            // For keys not covered, return a question mark
            return '?';
        }

        /// <summary>
        /// Flushes the text buffer and creates a text input action
        /// </summary>
        private void FlushTextBuffer()
        {
            if (_isTextBuffer && !string.IsNullOrEmpty(_currentTextBuffer))
            {
                var now = DateTime.Now;
                int delayMs = (int)(now - _lastActionTime).TotalMilliseconds;
                _lastActionTime = now;

                // Create new keyboard input
                var action = new KeyboardAction
                {
                    Text = _currentTextBuffer,
                    DelayBefore = delayMs,
                    ActionType = KeyboardActionType.TypeText,
                    Name = $"Text: {(_currentTextBuffer.Length > 20 ? _currentTextBuffer.Substring(0, 17) + "..." : _currentTextBuffer)}",
                    Description = $"Type text: \"{_currentTextBuffer}\"",
                    // Set standard delay to 10ms for appropriate playback
                    DelayBetweenChars = 10
                };

                LogManager.Log($"Text input recorded: \"{_currentTextBuffer}\" with delay {delayMs} ms");

                // Trigger event
                KeyActionRecorded?.Invoke(this, action);

                // Reset buffer
                _currentTextBuffer = "";
                _isTextBuffer = false;
            }

            _textBufferTimer.Stop();
        }

        /// <summary>
        /// Records a single key
        /// </summary>
        private void RecordSingleKey(Key key)
        {
            var now = DateTime.Now;
            int delayMs = (int)(now - _lastActionTime).TotalMilliseconds;
            _lastActionTime = now;

            // Create new keyboard input
            var action = new KeyboardAction
            {
                Keys = new[] { key },
                DelayBefore = delayMs,
                ActionType = KeyboardActionType.KeyPress,
                Name = $"Key: {key}",
                Description = $"Press key: {key}"
            };

            LogManager.Log($"Key press recorded: {key} with delay {delayMs} ms");

            // Trigger event
            KeyActionRecorded?.Invoke(this, action);
        }

        /// <summary>
        /// Records a key combination
        /// </summary>
        private void RecordKeyCombination(Key key)
        {
            var now = DateTime.Now;
            int delayMs = (int)(now - _lastActionTime).TotalMilliseconds;
            _lastActionTime = now;

            // Create list of currently pressed modifier keys
            var keys = new List<Key>();
            if (_isCtrlPressed) keys.Add(Key.LeftCtrl);
            if (_isAltPressed) keys.Add(Key.LeftAlt);
            if (_isShiftPressed) keys.Add(Key.LeftShift);
            keys.Add(key);

            // Create new key combination
            var action = new KeyboardAction
            {
                Keys = keys.ToArray(),
                DelayBefore = delayMs,
                ActionType = KeyboardActionType.KeyCombination,
                Name = $"Combination: {string.Join("+", keys)}",
                Description = $"Key combination: {string.Join("+", keys)}"
            };

            LogManager.Log($"Key combination recorded: {string.Join("+", keys)} with delay {delayMs} ms");

            // Trigger event
            KeyCombinationRecorded?.Invoke(this, action);
        }

        #endregion
    }
}