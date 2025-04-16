using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for KeyboardActionControl.xaml
/// </summary>
public partial class KeyboardActionControl : UserControl, IActionControl
{
private ActionBase _action;
private KeyboardAction _keyboardAction => _action as KeyboardAction;
private KeyboardActionViewModel _keyboardViewModel;
private ObservableCollection<KeyCombinationEntryViewModel> _keyEntries;
private MainViewModel ViewModel => DataContext as MainViewModel;
private ActionBase _selectedBranchAction;
private string _branchContext = "";
    /// <summary>
    /// Event that is raised when a branch action is selected for editing
    /// </summary>
    public event EventHandler<BranchActionEventArgs> BranchActionSelected;
    /// <summary>
    /// Gets the currently selected branch action (if any)
    /// </summary>
    public ActionBase SelectedBranchAction => _selectedBranchAction;
    /// <summary>
    /// Gets the context of the branch (THEN, ELSE, LOOP, etc.)
    /// </summary>
    public string BranchContext => _branchContext;
    public KeyboardActionControl()
    {
        InitializeComponent();
        _keyEntries = new ObservableCollection<KeyCombinationEntryViewModel>();
        icKeyEntries.ItemsSource = _keyEntries;
        // Initialize the key combo box
        cmbKey.ItemsSource = KeyHelper.GetAllKeys();
        cmbCombinationKey.ItemsSource = KeyHelper.GetAllKeys();
        // Add event handlers for add/remove key buttons
        btnAddKey.Click += BtnAddKey_Click;
        btnRemoveKey.Click += BtnRemoveKey_Click;
    }
    /// <summary>
    /// Initialize the control with an action
    /// </summary>
    public void Initialize(ActionBase action)
    {
        _action = action;
        UpdateControlFromAction();
    }
    /// <summary>
    /// Initialize with both the action and view model references
    /// </summary>
    public void Initialize(ActionBase action, KeyboardActionViewModel viewModel)
    {
        _action = action;
        _keyboardViewModel = viewModel;
        // Set event handlers for properties in KeyboardActionViewModel
        if (_keyboardViewModel != null)
        {
            _keyboardViewModel.PropertyChanged += (s, e) =>
            {
                // When the view model's properties change, update the keyboard action
                if (_keyboardAction != null)
                {
                    // Build key combination based on ViewModel state
                    if (_keyboardAction.ActionType == KeyboardActionType.KeyPress)
                    {
                        // For KeyPress, just use the selected key
                        if (_keyboardViewModel.SelectedKey != Key.None)
                        {
                            _keyboardAction.Keys = new Key[] { _keyboardViewModel.SelectedKey };
                        }
                    }
                    else if (_keyboardAction.ActionType == KeyboardActionType.KeyCombination ||
                             _keyboardAction.ActionType == KeyboardActionType.Hotkey)
                    {
                        // For combinations, include modifiers and main key
                        List<Key> keys = new List<Key>();
                        // Add modifiers in standard order
                        if (_keyboardViewModel.IsCtrlPressed)
                            keys.Add(Key.LeftCtrl);
                        if (_keyboardViewModel.IsAltPressed)
                            keys.Add(Key.LeftAlt);
                        if (_keyboardViewModel.IsShiftPressed)
                            keys.Add(Key.LeftShift);
                        // Add the main key
                        if (_keyboardViewModel.SelectedKey != Key.None)
                            keys.Add(_keyboardViewModel.SelectedKey);
                        // Set the keys
                        if (keys.Count > 0)
                            _keyboardAction.Keys = keys.ToArray();
                    }
                    // Update the text display
                    UpdateStoredKeysDisplay();
                }
            };
        }
        UpdateControlFromAction();
    }
    /// <summary>
    /// Update the control's UI elements from the action's properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_keyboardAction == null) return;
        // Set action type radio button
        switch (_keyboardAction.ActionType)
        {
            case KeyboardActionType.TypeText:
                rbTypeText.IsChecked = true;
                break;
            case KeyboardActionType.KeyPress:
                rbKeyPress.IsChecked = true;
                break;
            case KeyboardActionType.KeyCombination:
                rbKeyCombination.IsChecked = true;
                break;
            case KeyboardActionType.Hotkey:
                rbHotkey.IsChecked = true;
                break;
        }
        // Set text and clipboard flag
        txtText.Text = _keyboardAction.Text ?? string.Empty;
        chkUseClipboard.IsChecked = _keyboardAction.UseClipboard;
        txtCharDelay.Text = _keyboardAction.DelayBetweenChars.ToString();
        // Update visible fields based on the action type
        UpdateVisibleFieldsForActionType(_keyboardAction.ActionType);
        // Update key selection and ViewModel
        if (_keyboardAction.Keys != null && _keyboardAction.Keys.Length > 0)
        {
            if (_keyboardAction.ActionType == KeyboardActionType.KeyPress)
            {
                // For key press, use the first key
                var key = _keyboardAction.Keys[0];
                cmbKey.SelectedItem = new KeyItem(key);
                // Update the ViewModel
                if (_keyboardViewModel != null)
                {
                    _keyboardViewModel.SelectedKey = key;
                }
            }
            else if (_keyboardAction.ActionType == KeyboardActionType.KeyCombination ||
                     _keyboardAction.ActionType == KeyboardActionType.Hotkey)
            {
                // Find modifiers in the keys
                bool hasCtrl = _keyboardAction.Keys.Any(k => k == Key.LeftCtrl || k == Key.RightCtrl);
                bool hasAlt = _keyboardAction.Keys.Any(k => k == Key.LeftAlt || k == Key.RightAlt);
                bool hasShift = _keyboardAction.Keys.Any(k => k == Key.LeftShift || k == Key.RightShift);
                // Set modifier checkboxes
                chkCtrl.IsChecked = hasCtrl;
                chkAlt.IsChecked = hasAlt;
                chkShift.IsChecked = hasShift;
                // Update the ViewModel
                if (_keyboardViewModel != null)
                {
                    _keyboardViewModel.IsCtrlPressed = hasCtrl;
                    _keyboardViewModel.IsAltPressed = hasAlt;
                    _keyboardViewModel.IsShiftPressed = hasShift;
                }
                // Find the main key (the non-modifier key)
                var mainKey = _keyboardAction.Keys.LastOrDefault(k =>
                    k != Key.LeftCtrl && k != Key.RightCtrl &&
                    k != Key.LeftAlt && k != Key.RightAlt &&
                    k != Key.LeftShift && k != Key.RightShift);
                if (mainKey != Key.None)
                {
                    cmbCombinationKey.SelectedItem = new KeyItem(mainKey);
                    // Update the ViewModel
                    if (_keyboardViewModel != null)
                    {
                        _keyboardViewModel.SelectedKey = mainKey;
                    }
                }
                // Initialize key entries for flexible combination
                InitializeKeyEntries();
            }
        }
        // Update the stored keys display
        UpdateStoredKeysDisplay();
    }
    /// <summary>
    /// Update the keyboard action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_keyboardAction == null) return;
        // Determine action type
        KeyboardActionType actionType = KeyboardActionType.TypeText;
        if (rbTypeText.IsChecked == true)
            actionType = KeyboardActionType.TypeText;
        else if (rbKeyPress.IsChecked == true)
            actionType = KeyboardActionType.KeyPress;
        else if (rbKeyCombination.IsChecked == true)
            actionType = KeyboardActionType.KeyCombination;
        else if (rbHotkey.IsChecked == true)
            actionType = KeyboardActionType.Hotkey;
        _keyboardAction.ActionType = actionType;
        // Update text-specific properties
        if (actionType == KeyboardActionType.TypeText)
        {
            _keyboardAction.Text = txtText.Text;
            _keyboardAction.UseClipboard = chkUseClipboard.IsChecked == true;
            if (ActionControlHelper.TryParseTextBoxToInt(txtCharDelay, out int charDelay))
            {
                _keyboardAction.DelayBetweenChars = charDelay;
            }
        }
        // Update key-specific properties
        if (actionType == KeyboardActionType.KeyPress)
        {
            // Set key if one is selected
            if (cmbKey.SelectedItem is KeyItem keyItem)
            {
                _keyboardAction.Keys = new Key[] { keyItem.KeyValue };
                // Update ViewModel if available
                if (_keyboardViewModel != null)
                {
                    _keyboardViewModel.SelectedKey = keyItem.KeyValue;
                }
            }
        }
        else if (actionType == KeyboardActionType.KeyCombination || actionType == KeyboardActionType.Hotkey)
        {
            // Build key combination with modifiers and main key
            List<Key> keys = new List<Key>();
            // Add modifiers in a specific order: Ctrl, Alt, Shift
            if (chkCtrl.IsChecked == true)
                keys.Add(Key.LeftCtrl);
            if (chkAlt.IsChecked == true)
                keys.Add(Key.LeftAlt);
            if (chkShift.IsChecked == true)
                keys.Add(Key.LeftShift);
            // Update ViewModel if available
            if (_keyboardViewModel != null)
            {
                _keyboardViewModel.IsCtrlPressed = chkCtrl.IsChecked == true;
                _keyboardViewModel.IsAltPressed = chkAlt.IsChecked == true;
                _keyboardViewModel.IsShiftPressed = chkShift.IsChecked == true;
            }
            // Add main key if one is selected
            if (cmbCombinationKey.SelectedItem is KeyItem keyItem && keyItem.KeyValue != Key.None)
            {
                keys.Add(keyItem.KeyValue);
                // Update ViewModel if available
                if (_keyboardViewModel != null)
                {
                    _keyboardViewModel.SelectedKey = keyItem.KeyValue;
                }
            }
            // Set keys if any are defined
            if (keys.Count > 0)
            {
                _keyboardAction.Keys = keys.ToArray();
            }
        }
        // Update the stored keys display
        UpdateStoredKeysDisplay();
    }
    /// <summary>
    /// Event handler for keyboard action type radio buttons
    /// </summary>
    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        // Determine which action type is selected
        KeyboardActionType actionType = KeyboardActionType.TypeText;
        if (sender == rbTypeText)
            actionType = KeyboardActionType.TypeText;
        else if (sender == rbKeyPress)
            actionType = KeyboardActionType.KeyPress;
        else if (sender == rbKeyCombination)
            actionType = KeyboardActionType.KeyCombination;
        else if (sender == rbHotkey)
            actionType = KeyboardActionType.Hotkey;
        // Update the visible fields for the selected action type
        UpdateVisibleFieldsForActionType(actionType);
        // Update the action type in the keyboard action as well
        if (_keyboardAction != null)
        {
            _keyboardAction.ActionType = actionType;
            // Also update the keys array to match the new action type
            switch (actionType)
            {
                case KeyboardActionType.KeyPress:
                    if (cmbKey.SelectedItem is KeyItem keyItem)
                    {
                        _keyboardAction.Keys = new Key[] { keyItem.KeyValue };
                    }
                    break;
                case KeyboardActionType.KeyCombination:
                case KeyboardActionType.Hotkey:
                    UpdateActionFromControl(); // This will rebuild the proper key array
                    break;
            }
            // Update the display
            UpdateStoredKeysDisplay();
        }
    }
    /// <summary>
    /// Update which fields are visible based on the action type
    /// </summary>
    private void UpdateVisibleFieldsForActionType(KeyboardActionType actionType)
    {
        // First hide all elements
        lblText.Visibility = Visibility.Collapsed;
        txtText.Visibility = Visibility.Collapsed;
        lblClipboard.Visibility = Visibility.Collapsed;
        chkUseClipboard.Visibility = Visibility.Collapsed;
        lblKey.Visibility = Visibility.Collapsed;
        cmbKey.Visibility = Visibility.Collapsed;
        lblCombination.Visibility = Visibility.Collapsed;
        gridCombination.Visibility = Visibility.Collapsed;
        lblFlexibleCombination.Visibility = Visibility.Collapsed;
        panelFlexibleCombination.Visibility = Visibility.Collapsed;
        lblCharDelay.Visibility = Visibility.Collapsed;
        txtCharDelay.Visibility = Visibility.Collapsed;
        // Show relevant elements based on the action type
        switch (actionType)
        {
            case KeyboardActionType.TypeText:
                lblText.Visibility = Visibility.Visible;
                txtText.Visibility = Visibility.Visible;
                lblClipboard.Visibility = Visibility.Visible;
                chkUseClipboard.Visibility = Visibility.Visible;
                lblCharDelay.Visibility = Visibility.Visible;
                txtCharDelay.Visibility = Visibility.Visible;
                break;
            case KeyboardActionType.KeyPress:
                lblKey.Visibility = Visibility.Visible;
                cmbKey.Visibility = Visibility.Visible;
                break;
            case KeyboardActionType.KeyCombination:
            case KeyboardActionType.Hotkey:
                lblCombination.Visibility = Visibility.Visible;
                gridCombination.Visibility = Visibility.Visible;
                lblFlexibleCombination.Visibility = Visibility.Visible;
                panelFlexibleCombination.Visibility = Visibility.Visible;
                break;
        }
        // Always show the stored keys for key-related actions
        if (actionType != KeyboardActionType.TypeText)
        {
            lblStoredKeys.Visibility = Visibility.Visible;
            borderStoredKeys.Visibility = Visibility.Visible;
        }
        else
        {
            lblStoredKeys.Visibility = Visibility.Collapsed;
            borderStoredKeys.Visibility = Visibility.Collapsed;
        }
    }
    /// <summary>
    /// Update the display of stored keys
    /// </summary>
    private void UpdateStoredKeysDisplay()
    {
        if (_keyboardAction?.Keys == null || _keyboardAction.Keys.Length == 0)
        {
            txtStoredKeys.Text = "(No keys assigned)";
            return;
        }
        // Build string representation of keys
        string keyString = string.Join(" + ", _keyboardAction.Keys.Select(k => new KeyItem(k).DisplayValue));
        txtStoredKeys.Text = keyString;
        // Log for debugging
        LogManager.Log($"Keyboard keys updated: {keyString}", LogLevel.Debug);
    }
    /// <summary>
    /// Initialize key entries for flexible combination
    /// </summary>
    private void InitializeKeyEntries()
    {
        _keyEntries.Clear();
        if (_keyboardAction?.Keys != null && _keyboardAction.Keys.Length > 0)
        {
            foreach (var key in _keyboardAction.Keys)
            {
                var entry = new KeyCombinationEntryViewModel
                {
                    SelectedKey = key
                };
                _keyEntries.Add(entry);
            }
        }
    }
    /// <summary>
    /// Event handler for the Add Key button
    /// </summary>
    private void BtnAddKey_Click(object sender, RoutedEventArgs e)
    {
        var entry = new KeyCombinationEntryViewModel
        {
            SelectedKey = Key.A // Default key
        };
        _keyEntries.Add(entry);
        // Update the action to include the new key if necessary
        if (_keyboardAction != null && (_keyboardAction.ActionType == KeyboardActionType.KeyCombination || 
                                        _keyboardAction.ActionType == KeyboardActionType.Hotkey))
        {
            var existingKeys = _keyboardAction.Keys?.ToList() ?? new List<Key>();
            existingKeys.Add(Key.A); // Add default key
            _keyboardAction.Keys = existingKeys.ToArray();
            UpdateStoredKeysDisplay();
        }
    }
    /// <summary>
    /// Event handler for the Remove Key button
    /// </summary>
    private void BtnRemoveKey_Click(object sender, RoutedEventArgs e)
    {
        if (_keyEntries.Count > 0)
        {
            // Remove the last key entry
            _keyEntries.RemoveAt(_keyEntries.Count - 1);
            // Update the action to remove the key if necessary
            if (_keyboardAction != null && _keyboardAction.Keys != null && _keyboardAction.Keys.Length > 0)
            {
                var existingKeys = _keyboardAction.Keys.ToList();
                if (existingKeys.Count > 0)
                {
                    existingKeys.RemoveAt(existingKeys.Count - 1);
                    _keyboardAction.Keys = existingKeys.ToArray();
                    UpdateStoredKeysDisplay();
                }
            }
        }
    }
}
/// <summary>
/// Represents a single key entry in the key combination list
/// </summary>
public class KeyCombinationEntryViewModel : ViewModelBase
{
    private Key _selectedKey;
    public Key SelectedKey
    {
        get { return _selectedKey; }
        set
        {
            if (_selectedKey != value)
            {
                _selectedKey = value;
                OnPropertyChanged(nameof(SelectedKey));
            }
        }
    }
}
}
