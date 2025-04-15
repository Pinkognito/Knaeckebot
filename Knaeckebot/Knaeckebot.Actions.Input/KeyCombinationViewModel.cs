using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Knaeckebot.Models;
using Knaeckebot.Services;

namespace Knaeckebot.ViewModels
{
    /// <summary>
    /// ViewModel for a single key in a key combination
    /// </summary>
    public class KeyEntryViewModel : INotifyPropertyChanged
    {
        private Key _selectedKey = Key.None;
        private KeyItem _selectedKeyItem;

        /// <summary>
        /// Constructor with default values
        /// </summary>
        public KeyEntryViewModel()
        {
            _selectedKey = Key.None;
            _selectedKeyItem = new KeyItem(Key.None);
        }

        /// <summary>
        /// The selected key
        /// </summary>
        public Key SelectedKey
        {
            get => _selectedKey;
            set
            {
                if (_selectedKey != value)
                {
                    _selectedKey = value;
                    _selectedKeyItem = new KeyItem(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedKeyItem));
                }
            }
        }

        /// <summary>
        /// The selected key as KeyItem for binding to ComboBox
        /// </summary>
        public KeyItem SelectedKeyItem
        {
            get => _selectedKeyItem;
            set
            {
                if (value != null && (_selectedKeyItem == null || _selectedKeyItem.KeyValue != value.KeyValue))
                {
                    _selectedKeyItem = value;
                    _selectedKey = value.KeyValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedKey));
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for key combinations with variable number of keys
    /// </summary>
    public class KeyCombinationViewModel : INotifyPropertyChanged
    {
        private KeyboardAction? _keyboardAction;
        private ObservableCollection<KeyEntryViewModel> _keyEntries = new ObservableCollection<KeyEntryViewModel>();
        private bool _isUpdatingFromAction = false;
        private bool _isUpdatingToAction = false;
        private string _actionId = "not set"; // For debugging purposes

        /// <summary>
        /// The underlying KeyboardAction
        /// </summary>
        public KeyboardAction? KeyboardAction
        {
            get => _keyboardAction;
            set
            {
                if (_keyboardAction != value)
                {
                    _keyboardAction = value;

                    // Save action ID for debugging
                    _actionId = value?.Id.ToString().Substring(0, 8) ?? "none";
                    LogManager.Log($"KeyCombinationViewModel: KeyboardAction set to ID {_actionId}", LogLevel.KeyDebug);

                    InitializeFromAction();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentKeys));
                }
            }
        }

        /// <summary>
        /// List of keys in the combination
        /// </summary>
        public ObservableCollection<KeyEntryViewModel> KeyEntries
        {
            get => _keyEntries;
            set
            {
                if (_keyEntries != value)
                {
                    // Remove old event handlers
                    foreach (var entry in _keyEntries)
                    {
                        entry.PropertyChanged -= KeyEntry_PropertyChanged;
                    }

                    _keyEntries = value;

                    // Register new event handlers
                    foreach (var entry in _keyEntries)
                    {
                        entry.PropertyChanged += KeyEntry_PropertyChanged;
                    }

                    OnPropertyChanged();
                    UpdateKeyboardAction();
                }
            }
        }

        /// <summary>
        /// Returns the currently stored keys as formatted string
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
        /// Command to add a new key
        /// </summary>
        public ICommand AddKeyCommand { get; }

        /// <summary>
        /// Command to remove a key
        /// </summary>
        public ICommand RemoveKeyCommand { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public KeyCombinationViewModel()
        {
            // Initialize commands
            AddKeyCommand = new RelayCommand(AddKey);
            RemoveKeyCommand = new RelayCommand(RemoveKey, CanRemoveKey);

            LogManager.Log("KeyCombinationViewModel created", LogLevel.Debug);
        }

        /// <summary>
        /// Initializes the ViewModel from an existing KeyboardAction
        /// </summary>
        public void InitializeFromAction()
        {
            if (_isUpdatingToAction) return; // Prevent recursion
            _isUpdatingFromAction = true;

            try
            {
                LogManager.Log($"InitializeFromAction for action {_actionId}", LogLevel.KeyDebug);
                LogManager.LogKeyArray(_keyboardAction?.Keys, $"Keys in action {_actionId}");

                // Clear KeyEntries and remove event handlers
                foreach (var entry in KeyEntries)
                {
                    entry.PropertyChanged -= KeyEntry_PropertyChanged;
                }
                KeyEntries.Clear();

                // If keys exist, create a ViewModel for each key
                if (_keyboardAction?.Keys != null && _keyboardAction.Keys.Length > 0)
                {
                    foreach (var key in _keyboardAction.Keys)
                    {
                        var keyEntry = new KeyEntryViewModel { SelectedKey = key };
                        keyEntry.PropertyChanged += KeyEntry_PropertyChanged;
                        KeyEntries.Add(keyEntry);

                        LogManager.Log($"Key {key} added to KeyEntries", LogLevel.KeyDebug);
                    }
                }
                else
                {
                    // Add at least one empty key
                    AddKey();
                    LogManager.Log("Empty key added since no keys were present", LogLevel.KeyDebug);
                }

                // Update display
                OnPropertyChanged(nameof(CurrentKeys));
            }
            finally
            {
                _isUpdatingFromAction = false;
            }
        }

        /// <summary>
        /// Adds a new key to the combination
        /// </summary>
        private void AddKey()
        {
            var keyEntry = new KeyEntryViewModel();
            keyEntry.PropertyChanged += KeyEntry_PropertyChanged;
            KeyEntries.Add(keyEntry);

            LogManager.Log($"New key added (Index: {KeyEntries.Count - 1})", LogLevel.KeyDebug);

            UpdateKeyboardAction();
        }

        /// <summary>
        /// Checks if a key can be removed
        /// </summary>
        private bool CanRemoveKey()
        {
            return KeyEntries.Count > 1;
        }

        /// <summary>
        /// Removes the last key from the combination
        /// </summary>
        private void RemoveKey()
        {
            if (KeyEntries.Count > 1)
            {
                var lastEntry = KeyEntries.Last();
                lastEntry.PropertyChanged -= KeyEntry_PropertyChanged;
                KeyEntries.Remove(lastEntry);

                LogManager.Log($"Last key removed (Remaining: {KeyEntries.Count})", LogLevel.KeyDebug);

                UpdateKeyboardAction();
            }
        }

        /// <summary>
        /// Handles changes to key entries
        /// </summary>
        private void KeyEntry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingFromAction) return; // No update during initialization

            if (e.PropertyName == nameof(KeyEntryViewModel.SelectedKey) ||
                e.PropertyName == nameof(KeyEntryViewModel.SelectedKeyItem))
            {
                UpdateKeyboardAction();

                if (sender is KeyEntryViewModel keyEntry)
                {
                    LogManager.Log($"Key changed to {keyEntry.SelectedKey}", LogLevel.KeyDebug);
                }
            }
        }

        /// <summary>
        /// Updates the KeyboardAction with the current keys
        /// </summary>
        private void UpdateKeyboardAction()
        {
            if (_keyboardAction == null || _isUpdatingFromAction) return;

            try
            {
                _isUpdatingToAction = true;

                // Create list of selected keys (only non-empty keys)
                var keys = KeyEntries
                    .Select(entry => entry.SelectedKey)
                    .Where(k => k != Key.None)
                    .ToArray();

                // Debug output before change
                LogManager.LogKeyArray(_keyboardAction.Keys, $"Keys BEFORE update in action {_actionId}");

                // Assign new array
                _keyboardAction.Keys = keys;

                // Debug output after change
                LogManager.LogKeyArray(_keyboardAction.Keys, $"Keys AFTER update in action {_actionId}");

                // Trigger PropertyChanged for KeyboardAction
                OnPropertyChanged(nameof(KeyboardAction));
                OnPropertyChanged(nameof(KeyboardAction.Keys));
                OnPropertyChanged(nameof(CurrentKeys));
            }
            finally
            {
                _isUpdatingToAction = false;
            }
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