using System;
using System.Diagnostics;
using System.Windows.Input;
using Knaeckebot.Services;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Possible keyboard action types
    /// </summary>
    public enum KeyboardActionType
    {
        TypeText,
        KeyPress,
        KeyCombination,
        Hotkey
    }

    /// <summary>
    /// Represents a keyboard input
    /// </summary>
    public class KeyboardAction : ActionBase
    {
        private string? _text = string.Empty;
        private Key[]? _keys = Array.Empty<Key>();
        private int _delayBetweenChars = 10;
        private KeyboardActionType _actionType = KeyboardActionType.TypeText;
        private bool _useClipboard = false;
        private bool _autoUpdateName = true; // Controls whether the name should be automatically updated

        /// <summary>
        /// Text to be entered (with TypeText)
        /// </summary>
        public string? Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();

                    // Automatically update name when ActionType is TypeText
                    if (_autoUpdateName && ActionType == KeyboardActionType.TypeText)
                    {
                        UpdateNameBasedOnContent();
                    }
                }
            }
        }

        /// <summary>
        /// Keys to be pressed (with KeyPress, KeyCombination)
        /// </summary>
        public Key[]? Keys
        {
            get => _keys;
            set
            {
                if (_keys != value)
                {
                    _keys = value;
                    OnPropertyChanged();

                    // Automatically update name when ActionType is not TypeText
                    if (_autoUpdateName && ActionType != KeyboardActionType.TypeText)
                    {
                        UpdateNameBasedOnContent();
                    }
                }
            }
        }

        /// <summary>
        /// Delay between characters when typing (in milliseconds)
        /// </summary>
        public int DelayBetweenChars
        {
            get => _delayBetweenChars;
            set
            {
                if (_delayBetweenChars != value)
                {
                    _delayBetweenChars = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of keyboard input
        /// </summary>
        public KeyboardActionType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType != value)
                {
                    _actionType = value;
                    OnPropertyChanged();

                    // Update name when action type changes
                    if (_autoUpdateName)
                    {
                        UpdateNameBasedOnContent();
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the text should be pasted from the clipboard
        /// </summary>
        public bool UseClipboard
        {
            get => _useClipboard;
            set
            {
                if (_useClipboard != value)
                {
                    _useClipboard = value;
                    OnPropertyChanged();

                    // Update name when clipboard option changes and ActionType is TypeText
                    if (_autoUpdateName && ActionType == KeyboardActionType.TypeText)
                    {
                        UpdateNameBasedOnContent();
                    }
                }
            }
        }

        /// <summary>
        /// Updates the name of the action based on its content
        /// </summary>
        private void UpdateNameBasedOnContent()
        {
            try
            {
                // Temporarily disable automatic updating to avoid recursion
                _autoUpdateName = false;

                switch (ActionType)
                {
                    case KeyboardActionType.TypeText:
                        if (UseClipboard)
                        {
                            Name = "Text from Clipboard";
                        }
                        else if (!string.IsNullOrEmpty(Text))
                        {
                            // Shorten text for the name if it's too long
                            string displayText = Text?.Length > 20 ? Text.Substring(0, 17) + "..." : Text;
                            Name = $"Text: \"{displayText}\"";
                        }
                        else
                        {
                            Name = "Text Input";
                        }
                        break;

                    case KeyboardActionType.KeyPress:
                        if (Keys != null && Keys.Length > 0)
                        {
                            Name = $"Key: {FormatKeysForDisplay(Keys)}";
                        }
                        else
                        {
                            Name = "Single Key";
                        }
                        break;

                    case KeyboardActionType.KeyCombination:
                        if (Keys != null && Keys.Length > 0)
                        {
                            Name = $"Combination: {FormatKeysForDisplay(Keys)}";
                        }
                        else
                        {
                            Name = "Key Combination";
                        }
                        break;

                    case KeyboardActionType.Hotkey:
                        if (Keys != null && Keys.Length > 0)
                        {
                            Name = $"Hotkey: {FormatKeysForDisplay(Keys)}";
                        }
                        else
                        {
                            Name = "Hotkey";
                        }
                        break;
                }

                // Also update description
                Description = $"Keyboard input: {ToString()}";
            }
            finally
            {
                // Re-enable automatic updating
                _autoUpdateName = true;
            }
        }

        /// <summary>
        /// Formats a Key array for display in the UI
        /// </summary>
        private string FormatKeysForDisplay(Key[] keys)
        {
            if (keys == null || keys.Length == 0)
                return "(none)";

            return string.Join(" + ", Array.ConvertAll(keys, k => new KeyItem(k).DisplayValue));
        }

        /// <summary>
        /// Executes the keyboard input
        /// </summary>
        public override void Execute()
        {
            try
            {
                LogManager.Log($"Executing keyboard input: {this}");

                // Use KeyboardService for the actual execution
                switch (ActionType)
                {
                    case KeyboardActionType.TypeText:
                        if (UseClipboard)
                        {
                            // Directly paste the current content of the clipboard
                            LogManager.Log("Pasting content from clipboard");
                            KeyboardService.Instance.PasteFromClipboard();
                        }
                        else
                        {
                            // Directly enter text
                            LogManager.Log($"Typing text: {Text}");
                            KeyboardService.Instance.TypeText(Text, DelayBetweenChars);
                        }
                        break;

                    case KeyboardActionType.KeyPress:
                        if (Keys != null && Keys.Length > 0)
                        {
                            // Debug output of keys to be pressed
                            LogManager.Log($"Pressing keys: {string.Join(", ", Keys)}");

                            foreach (var key in Keys)
                            {
                                LogManager.Log($"Pressing key: {key}");
                                KeyboardService.Instance.PressKey(key);
                                System.Threading.Thread.Sleep(30); // Short delay between individual keys
                            }
                        }
                        else
                        {
                            LogManager.Log("No keys defined to press", LogLevel.Warning);
                        }
                        break;

                    case KeyboardActionType.KeyCombination:
                    case KeyboardActionType.Hotkey:
                        if (Keys != null && Keys.Length > 0)
                        {
                            // Debug output of key combination
                            LogManager.Log($"Pressing key combination: {string.Join(" + ", Keys)}");

                            // Execute key combination
                            KeyboardService.Instance.PressKeyCombination(Keys);
                        }
                        else
                        {
                            LogManager.Log("No keys defined for combination", LogLevel.Warning);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error during keyboard input: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Creates a copy of this keyboard input
        /// </summary>
        public override ActionBase Clone()
        {
            return new KeyboardAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                Text = this.Text,
                Keys = (Key[]?)this.Keys?.Clone(),
                DelayBetweenChars = this.DelayBetweenChars,
                ActionType = this.ActionType,
                UseClipboard = this.UseClipboard
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return ActionType switch
            {
                KeyboardActionType.TypeText => $"Text: \"{(Text?.Length > 20 ? Text?.Substring(0, 17) + "..." : Text)}\"" + (UseClipboard ? " (from clipboard)" : ""),
                KeyboardActionType.KeyPress => $"Key: {FormatKeysForDisplay(Keys ?? Array.Empty<Key>())}",
                KeyboardActionType.KeyCombination => $"Combination: {FormatKeysForDisplay(Keys ?? Array.Empty<Key>())}",
                KeyboardActionType.Hotkey => $"Hotkey: {FormatKeysForDisplay(Keys ?? Array.Empty<Key>())}",
                _ => "Keyboard Input"
            };
        }
    }
}