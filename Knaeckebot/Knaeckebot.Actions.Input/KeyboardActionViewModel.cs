using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Knaeckebot.Models;
using Knaeckebot.Services;

namespace Knaeckebot.ViewModels
{
    /// <summary>
    /// ViewModel for keyboard inputs with support for key combinations
    /// </summary>
    public class KeyboardActionViewModel : INotifyPropertyChanged
    {
        private KeyboardAction _keyboardAction;
        private bool _isCtrlPressed;
        private bool _isAltPressed;
        private bool _isShiftPressed;
        private Key _selectedKey = Key.None;
        private string _actionId = ""; // Unique ID for debugging purposes

        /// <summary>
        /// Constructor
        /// </summary>
        public KeyboardActionViewModel()
        {
            // Initialize with an empty KeyboardAction to avoid null values
            _keyboardAction = new KeyboardAction();
            _actionId = "none";
        }

        /// <summary>
        /// The underlying KeyboardAction
        /// </summary>
        public KeyboardAction KeyboardAction
        {
            get => _keyboardAction;
            set
            {
                if (_keyboardAction != value)
                {
                    _keyboardAction = value;
                    _actionId = value?.Id.ToString().Substring(0, 8) ?? "none"; // For debugging purposes
                    UpdateModifierFlags();
                    UpdateSelectedKey();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActionId)); // New property
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// Unique ID of the action for debugging purposes
        /// </summary>
        public string ActionId => _actionId;

        /// <summary>
        /// Indicates whether the Ctrl key is pressed
        /// </summary>
        public bool IsCtrlPressed
        {
            get => _isCtrlPressed;
            set
            {
                if (_isCtrlPressed != value)
                {
                    _isCtrlPressed = value;
                    UpdateKeyCombination();
                    OnPropertyChanged();
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// Indicates whether the Alt key is pressed
        /// </summary>
        public bool IsAltPressed
        {
            get => _isAltPressed;
            set
            {
                if (_isAltPressed != value)
                {
                    _isAltPressed = value;
                    UpdateKeyCombination();
                    OnPropertyChanged();
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// Indicates whether the Shift key is pressed
        /// </summary>
        public bool IsShiftPressed
        {
            get => _isShiftPressed;
            set
            {
                if (_isShiftPressed != value)
                {
                    _isShiftPressed = value;
                    UpdateKeyCombination();
                    OnPropertyChanged();
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// The selected main key
        /// </summary>
        public Key SelectedKey
        {
            get => _selectedKey;
            set
            {
                if (_selectedKey != value)
                {
                    _selectedKey = value;
                    UpdateKeyCombination();
                    OnPropertyChanged();
                    // Debug output
                    LogManager.Log($"SelectedKey for {_actionId} changed to: {value}", LogLevel.KeyDebug);
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// The main key as KeyItem for binding to ComboBox
        /// </summary>
        public KeyItem SelectedKeyItem
        {
            get => new KeyItem(_selectedKey);
            set
            {
                if (value != null && _selectedKey != value.KeyValue)
                {
                    _selectedKey = value.KeyValue;
                    UpdateKeyCombination();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedKey));
                    // Debug output
                    LogManager.Log($"SelectedKeyItem for {_actionId} changed to: {value.KeyValue}", LogLevel.KeyDebug);
                    // Also update the display of saved keys
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// Returns the currently stored keys as a formatted string
        /// </summary>
        public string CurrentKeys
        {
            get
            {
                if (_keyboardAction?.Keys == null || _keyboardAction.Keys.Length == 0)
                {
                    return "(No keys assigned)";
                }

                return string.Join(" + ", _keyboardAction.Keys.Select(k => new KeyItem(k).DisplayValue));
            }
        }

        /// <summary>
        /// Updates the modifier flag properties based on the Keys array
        /// </summary>
        private void UpdateModifierFlags()
        {
            if (_keyboardAction?.Keys == null) return;

            _isCtrlPressed = KeyHelper.HasModifierKey(_keyboardAction.Keys, Key.LeftCtrl);
            _isAltPressed = KeyHelper.HasModifierKey(_keyboardAction.Keys, Key.LeftAlt);
            _isShiftPressed = KeyHelper.HasModifierKey(_keyboardAction.Keys, Key.LeftShift);

            OnPropertyChanged(nameof(IsCtrlPressed));
            OnPropertyChanged(nameof(IsAltPressed));
            OnPropertyChanged(nameof(IsShiftPressed));
            OnPropertyChanged(nameof(CurrentKeys));

            // Debug output
            LogManager.Log($"UpdateModifierFlags for {_actionId}: Ctrl={_isCtrlPressed}, Alt={_isAltPressed}, Shift={_isShiftPressed}",
                LogLevel.KeyDebug);
        }

        /// <summary>
        /// Updates the selected main key based on the Keys array
        /// </summary>
        private void UpdateSelectedKey()
        {
            if (_keyboardAction?.Keys == null || _keyboardAction.Keys.Length == 0)
            {
                _selectedKey = Key.None;
                OnPropertyChanged(nameof(SelectedKey));
                OnPropertyChanged(nameof(SelectedKeyItem));
                OnPropertyChanged(nameof(CurrentKeys));
                LogManager.Log($"UpdateSelectedKey for {_actionId}: None (empty array)", LogLevel.KeyDebug);
                return;
            }

            // The last key in the array should be the main key
            // (after the modifier keys)
            var lastKey = _keyboardAction.Keys[_keyboardAction.Keys.Length - 1];

            // If the last key is a modifier key, set SelectedKey to None
            if (lastKey == Key.LeftCtrl || lastKey == Key.RightCtrl ||
                lastKey == Key.LeftAlt || lastKey == Key.RightAlt ||
                lastKey == Key.LeftShift || lastKey == Key.RightShift)
            {
                _selectedKey = Key.None;
                LogManager.Log($"UpdateSelectedKey for {_actionId}: None (last key is modifier)", LogLevel.KeyDebug);
            }
            else
            {
                _selectedKey = lastKey;
                LogManager.Log($"UpdateSelectedKey for {_actionId}: {lastKey}", LogLevel.KeyDebug);
            }

            OnPropertyChanged(nameof(SelectedKey));
            OnPropertyChanged(nameof(SelectedKeyItem));
            OnPropertyChanged(nameof(CurrentKeys));
        }

        /// <summary>
        /// Updates the Keys array based on the modifier flags and SelectedKey
        /// </summary>
        private void UpdateKeyCombination()
        {
            if (_keyboardAction == null) return;

            // Save old keys for debug output
            var oldKeys = _keyboardAction.Keys?.ToArray();
            LogManager.LogKeyArray(oldKeys, $"UpdateKeyCombination for {_actionId} BEFORE change");

            _keyboardAction.Keys = KeyHelper.CreateKeyCombination(
                _isCtrlPressed,
                _isAltPressed,
                _isShiftPressed,
                _selectedKey);

            LogManager.LogKeyArray(_keyboardAction.Keys, $"UpdateKeyCombination for {_actionId} AFTER change");

            // Notify that the displayed keys have changed
            OnPropertyChanged(nameof(CurrentKeys));
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}