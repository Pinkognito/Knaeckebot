using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using Knaeckebot.Controls.Actions;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
using MouseAction = Knaeckebot.Models.MouseAction;
using ComboBox = System.Windows.Controls.ComboBox;
namespace Knaeckebot.Controls
{
/// <summary>
/// Interaction logic for ActionDetailsControl.xaml
/// This is a refactored version that acts as a container for specialized action controls
/// </summary>
public partial class ActionDetailsControl : UserControl
{
// Reference to the current specialized action control
private IActionControl _currentActionControl;
    // Reference to keyboard-specific view model (needed for keyboard actions)
    private KeyboardActionViewModel _keyboardViewModel;
    // Reference to the currently selected branch action for details panel
    private ActionBase _selectedBranchAction;
    // Reference to the branch context (THEN, ELSE, LOOP, etc.)
    private string _branchContext = "";
    public ActionDetailsControl()
    {
        InitializeComponent();
        LogManager.Log("ActionDetailsControl initialized", LogLevel.Debug);
        // Register for data context changed to monitor when the selected action changes
        DataContextChanged += ActionDetailsControl_DataContextChanged;
    }
    /// <summary>
    /// Handles data context changes to monitor selected action changes
    /// </summary>
    private void ActionDetailsControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel viewModel)
        {
            // Create the keyboard action view model
            _keyboardViewModel = new KeyboardActionViewModel();
            // Subscribe to selected action changes
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedAction))
                {
                    LogManager.Log("SelectedAction changed in ActionDetailsControl", LogLevel.Debug);
                    // Update the control for the new action type
                    UpdateActionTypeControl(viewModel.SelectedAction);
                    // Reset branch action details
                    HideBranchActionDetails();
                }
            };
        }
    }
    /// <summary>
    /// Creates and initializes the appropriate control for the selected action type
    /// </summary>
    private void UpdateActionTypeControl(ActionBase action)
    {
        // Clear current content
        ActionTypeContent.Content = null;
        _currentActionControl = null;
        if (action == null)
        {
            LogManager.Log("No action selected, cleared action type control", LogLevel.Debug);
            return;
        }
        LogManager.Log($"Creating control for action type: {action.GetType().Name}", LogLevel.Debug);
        // Create the appropriate control based on action type
        if (action is MouseAction)
        {
            _currentActionControl = new MouseActionControl();
        }
        else if (action is KeyboardAction)
        {
            var keyboardControl = new KeyboardActionControl();
            _currentActionControl = keyboardControl;
            // Special initialization for keyboard actions
            keyboardControl.Initialize(action, _keyboardViewModel);
            // Skip the normal initialize call since we did custom initialization
            ActionTypeContent.Content = _currentActionControl;
            // Subscribe to branch action selection events
            _currentActionControl.BranchActionSelected += OnBranchActionSelected;
            return;
        }
        else if (action is WaitAction)
        {
            _currentActionControl = new WaitActionControl();
        }
        else if (action is VariableAction)
        {
            _currentActionControl = new VariableActionControl();
        }
        else if (action is ClipboardAction)
        {
            _currentActionControl = new ClipboardActionControl();
        }
        else if (action is JsonAction)
        {
            _currentActionControl = new JsonActionControl();
        }
        else if (action is BrowserAction)
        {
            _currentActionControl = new BrowserActionControl();
        }
        else if (action is LoopAction)
        {
            _currentActionControl = new LoopActionControl();
        }
        else if (action is IfAction)
        {
            _currentActionControl = new IfActionControl();
        }
        else
        {
            LogManager.Log($"No specialized control available for action type: {action.GetType().Name}", LogLevel.Warning);
            return;
        }
        // Initialize the control with the action and display it
        ((UserControl)_currentActionControl).DataContext = DataContext;
        _currentActionControl.Initialize(action);
        ActionTypeContent.Content = _currentActionControl;
        // Subscribe to branch action selection events
        _currentActionControl.BranchActionSelected += OnBranchActionSelected;
    }
    /// <summary>
    /// Apply changes from the current action control back to the action
    /// This method should be called when saving or switching actions
    /// </summary>
    public void ApplyChanges()
    {
        if (_currentActionControl != null)
        {
            LogManager.Log("Applying changes from specialized action control", LogLevel.Debug);
            _currentActionControl.UpdateActionFromControl();
        }
    }
    /// <summary>
    /// Event handler for branch action selection events from specialized controls
    /// </summary>
    private void OnBranchActionSelected(object sender, BranchActionEventArgs e)
    {
        if (e.SelectedAction != null)
        {
            LogManager.Log($"Branch action selected for editing: {e.SelectedAction.Name} in {e.BranchContext} branch", LogLevel.Debug);
            _selectedBranchAction = e.SelectedAction;
            _branchContext = e.BranchContext;
            // Show branch action details panel
            ShowBranchActionDetails();
        }
    }
    /// <summary>
    /// Shows the branch action details panel for the selected branch action
    /// </summary>
    private void ShowBranchActionDetails()
    {
        if (_selectedBranchAction == null)
            return;
        LogManager.Log($"Showing details for branch action: {_selectedBranchAction.Name} ({_selectedBranchAction.GetType().Name})", LogLevel.Debug);
        // Update common UI elements
        BranchActionType.Text = _selectedBranchAction.GetType().Name;
        BranchActionName.Text = _selectedBranchAction.Name;
        BranchActionDescription.Text = _selectedBranchAction.Description;
        BranchActionDelay.Text = _selectedBranchAction.DelayBefore.ToString();
        // Hide all type-specific property groups first
        MouseActionPropertiesGroup.Visibility = Visibility.Collapsed;
        KeyboardActionPropertiesGroup.Visibility = Visibility.Collapsed;
        WaitActionPropertiesGroup.Visibility = Visibility.Collapsed;
        VariableActionPropertiesGroup.Visibility = Visibility.Collapsed;
        ClipboardActionPropertiesGroup.Visibility = Visibility.Collapsed;
        JsonActionPropertiesGroup.Visibility = Visibility.Collapsed;
        BrowserActionPropertiesGroup.Visibility = Visibility.Collapsed;
        // Populate type-specific properties based on action type
        if (_selectedBranchAction is MouseAction mouseAction)
        {
            LogManager.Log($"Populating MouseAction properties", LogLevel.Debug);
            MouseActionPropertiesGroup.Visibility = Visibility.Visible;
            // Set X, Y positions
            MouseActionXPosition.Text = mouseAction.X.ToString();
            MouseActionYPosition.Text = mouseAction.Y.ToString();
            MouseActionWheelDelta.Text = mouseAction.WheelDelta.ToString();
            // Set action type radio button
            switch (mouseAction.ActionType)
            {
                case MouseActionType.LeftClick:
                    rbLeftClick.IsChecked = true;
                    break;
                case MouseActionType.RightClick:
                    rbRightClick.IsChecked = true;
                    break;
                case MouseActionType.MiddleClick:
                    rbMiddleClick.IsChecked = true;
                    break;
                case MouseActionType.DoubleClick:
                    rbDoubleClick.IsChecked = true;
                    break;
                case MouseActionType.MouseWheel:
                    rbMouseWheel.IsChecked = true;
                    break;
            }
        }
        else if (_selectedBranchAction is KeyboardAction keyboardAction)
        {
            LogManager.Log($"Populating KeyboardAction properties", LogLevel.Debug);
            KeyboardActionPropertiesGroup.Visibility = Visibility.Visible;
            // Initialize key combo box if it wasn't done yet
            if (KeyboardActionKey.ItemsSource == null)
            {
                KeyboardActionKey.ItemsSource = KeyHelper.GetAllKeys();
            }
            // Set keyboard action type radio button
            switch (keyboardAction.ActionType)
            {
                case KeyboardActionType.TypeText:
                    rbTypeText.IsChecked = true;
                    // Show text input fields
                    KeyboardTextLabel.Visibility = Visibility.Visible;
                    KeyboardActionText.Visibility = Visibility.Visible;
                    KeyboardDelayLabel.Visibility = Visibility.Visible;
                    KeyboardActionDelay.Visibility = Visibility.Visible;
                    // Set values
                    KeyboardActionText.Text = keyboardAction.Text ?? string.Empty;
                    KeyboardActionDelay.Text = keyboardAction.DelayBetweenChars.ToString();
                    break;
                case KeyboardActionType.KeyPress:
                    rbKeyPress.IsChecked = true;
                    // Show key input field
                    KeyboardKeyLabel.Visibility = Visibility.Visible;
                    KeyboardActionKey.Visibility = Visibility.Visible;
                    // Set the key if available
                    if (keyboardAction.Keys != null && keyboardAction.Keys.Length > 0)
                    {
                        var key = keyboardAction.Keys[keyboardAction.Keys.Length - 1];
                        KeyboardActionKey.SelectedItem = new KeyItem(key);
                    }
                    break;
                case KeyboardActionType.KeyCombination:
                case KeyboardActionType.Hotkey:
                    if (keyboardAction.ActionType == KeyboardActionType.KeyCombination)
                        rbKeyCombination.IsChecked = true;
                    else
                        rbHotkey.IsChecked = true;
                    // Show key and modifier input fields
                    KeyboardKeyLabel.Visibility = Visibility.Visible;
                    KeyboardActionKey.Visibility = Visibility.Visible;
                    KeyboardModifiersLabel.Visibility = Visibility.Visible;
                    KeyboardModifiersPanel.Visibility = Visibility.Visible;
                    // Set the key if available
                    if (keyboardAction.Keys != null && keyboardAction.Keys.Length > 0)
                    {
                        // Find modifiers in the keys
                        bool hasCtrl = keyboardAction.Keys.Any(k => k == Key.LeftCtrl || k == Key.RightCtrl);
                        bool hasAlt = keyboardAction.Keys.Any(k => k == Key.LeftAlt || k == Key.RightAlt);
                        bool hasShift = keyboardAction.Keys.Any(k => k == Key.LeftShift || k == Key.RightShift);
                        // Set modifier checkboxes
                        KeyboardCtrlCheckbox.IsChecked = hasCtrl;
                        KeyboardAltCheckbox.IsChecked = hasAlt;
                        KeyboardShiftCheckbox.IsChecked = hasShift;
                        // Find the main key (the non-modifier key)
                        var mainKey = keyboardAction.Keys.LastOrDefault(k =>
                            k != Key.LeftCtrl && k != Key.RightCtrl &&
                            k != Key.LeftAlt && k != Key.RightAlt &&
                            k != Key.LeftShift && k != Key.RightShift);
                        if (mainKey != Key.None)
                        {
                            KeyboardActionKey.SelectedItem = new KeyItem(mainKey);
                        }
                    }
                    break;
            }
            // Show current keys
            UpdateKeyboardCurrentKeys();
        }
        else if (_selectedBranchAction is WaitAction waitAction)
        {
            LogManager.Log($"Populating WaitAction properties", LogLevel.Debug);
            WaitActionPropertiesGroup.Visibility = Visibility.Visible;
            // Set wait time
            WaitActionTime.Text = waitAction.WaitTime.ToString();
        }
        else if (_selectedBranchAction is VariableAction variableAction)
        {
            LogManager.Log($"Populating VariableAction properties", LogLevel.Debug);
            VariableActionPropertiesGroup.Visibility = Visibility.Visible;
            // Initialize comboboxes if not done yet
            if (VariableActionTypeCombo.ItemsSource == null)
            {
                VariableActionTypeCombo.ItemsSource = Enum.GetValues(typeof(VariableActionType));
            }
            if (VariableActionNameCombo.ItemsSource == null && DataContext is MainViewModel viewModel)
            {
                VariableActionNameCombo.ItemsSource = viewModel.VariableNames;
            }
            // Set variable action type
            VariableActionTypeCombo.SelectedItem = variableAction.ActionType;
            // Set variable name
            VariableActionNameCombo.Text = variableAction.VariableName;
            // Update UI based on action type
            UpdateVariableActionUI(variableAction.ActionType);
            // Set values based on action type
            if (variableAction.ActionType == VariableActionType.SetValue ||
                variableAction.ActionType == VariableActionType.AppendText)
            {
                VariableActionValue.Text = variableAction.Value;
            }
            else if (variableAction.ActionType == VariableActionType.Increment ||
                    variableAction.ActionType == VariableActionType.Decrement)
            {
                VariableActionIncrement.Text = variableAction.IncrementValue.ToString();
            }
        }
        else if (_selectedBranchAction is ClipboardAction clipboardAction)
        {
            LogManager.Log($"Populating ClipboardAction properties", LogLevel.Debug);
            ClipboardActionPropertiesGroup.Visibility = Visibility.Visible;
            // Set the source radio button
            if (clipboardAction.UseVariable)
            {
                ClipboardVarRadioButton.IsChecked = true;
                ClipboardVariableLabel.Visibility = Visibility.Visible;
                ClipboardVariableName.Visibility = Visibility.Visible;
                ClipboardVariableName.Text = clipboardAction.VariableName;
            }
            else
            {
                ClipboardTextRadioButton.IsChecked = true;
                ClipboardTextLabel.Visibility = Visibility.Visible;
                ClipboardText.Visibility = Visibility.Visible;
                ClipboardText.Text = clipboardAction.Text;
            }
            // Set additional properties
            ClipboardAppendCheck.IsChecked = clipboardAction.AppendToClipboard;
            ClipboardRetryCount.Text = clipboardAction.RetryCount.ToString();
            ClipboardRetryWaitTime.Text = clipboardAction.RetryWaitTime.ToString();
        }
        else if (_selectedBranchAction is JsonAction jsonAction)
        {
            LogManager.Log($"Populating JsonAction properties", LogLevel.Debug);
            JsonActionPropertiesGroup.Visibility = Visibility.Visible;
            // Set properties
            JsonCheckClipboard.IsChecked = jsonAction.CheckClipboard;
            JsonTemplate.Text = jsonAction.JsonTemplate;
            JsonOffsetX.Text = jsonAction.OffsetX.ToString();
            JsonOffsetY.Text = jsonAction.OffsetY.ToString();
            JsonRetryCount.Text = jsonAction.RetryCount.ToString();
            JsonRetryWaitTime.Text = jsonAction.RetryWaitTime.ToString();
            JsonContinueOnError.IsChecked = jsonAction.ContinueOnError;
        }
        else if (_selectedBranchAction is BrowserAction browserAction)
        {
            LogManager.Log($"Populating BrowserAction properties", LogLevel.Debug);
            BrowserActionPropertiesGroup.Visibility = Visibility.Visible;
            // Initialize comboboxes if not done yet
            if (BrowserActionTypeCombo.ItemsSource == null)
            {
                BrowserActionTypeCombo.ItemsSource = Enum.GetValues(typeof(BrowserActionType));
            }
            // Set action type
            BrowserActionTypeCombo.SelectedItem = browserAction.ActionType;
            // Update UI based on action type
            UpdateBrowserActionUI(browserAction.ActionType);
            // Set type-specific properties
            switch (browserAction.ActionType)
            {
                case BrowserActionType.FindElementAndClick:
                    BrowserSelector.Text = browserAction.Selector;
                    break;
                case BrowserActionType.ExecuteJavaScript:
                    BrowserJavaScript.Text = browserAction.JavaScript;
                    break;
                case BrowserActionType.GetCoordinates:
                    BrowserXResult.Text = browserAction.XResult.ToString();
                    BrowserYResult.Text = browserAction.YResult.ToString();
                    BrowserUseLastResults.IsChecked = browserAction.UseLastResults;
                    break;
            }
        }
        // Show the branch action details group
        BranchActionDetailsGroup.Visibility = Visibility.Visible;
        BranchActionDetailsGroup.Header = $"{_branchContext} Branch Action Details";
    }
    /// <summary>
    /// Updates the UI for variable action based on action type
    /// </summary>
    private void UpdateVariableActionUI(VariableActionType actionType)
    {
        // First hide all specific elements
        VariableValueLabel.Visibility = Visibility.Collapsed;
        VariableActionValue.Visibility = Visibility.Collapsed;
        VariableIncrementLabel.Visibility = Visibility.Collapsed;
        VariableActionIncrement.Visibility = Visibility.Collapsed;
        // Show relevant elements based on action type
        switch (actionType)
        {
            case VariableActionType.SetValue:
            case VariableActionType.AppendText:
                VariableValueLabel.Visibility = Visibility.Visible;
                VariableActionValue.Visibility = Visibility.Visible;
                break;
            case VariableActionType.Increment:
            case VariableActionType.Decrement:
                VariableIncrementLabel.Visibility = Visibility.Visible;
                VariableActionIncrement.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Updates the UI for browser action based on action type
    /// </summary>
    private void UpdateBrowserActionUI(BrowserActionType actionType)
    {
        // First hide all specific elements
        BrowserSelectorLabel.Visibility = Visibility.Collapsed;
        BrowserSelector.Visibility = Visibility.Collapsed;
        BrowserJavaScriptLabel.Visibility = Visibility.Collapsed;
        BrowserJavaScript.Visibility = Visibility.Collapsed;
        BrowserCoordinatesLabel.Visibility = Visibility.Collapsed;
        BrowserCoordinatesPanel.Visibility = Visibility.Collapsed;
        BrowserUseLastResultsLabel.Visibility = Visibility.Collapsed;
        BrowserUseLastResults.Visibility = Visibility.Collapsed;
        // Show relevant elements based on action type
        switch (actionType)
        {
            case BrowserActionType.FindElementAndClick:
                BrowserSelectorLabel.Visibility = Visibility.Visible;
                BrowserSelector.Visibility = Visibility.Visible;
                break;
            case BrowserActionType.ExecuteJavaScript:
                BrowserJavaScriptLabel.Visibility = Visibility.Visible;
                BrowserJavaScript.Visibility = Visibility.Visible;
                break;
            case BrowserActionType.GetCoordinates:
                BrowserCoordinatesLabel.Visibility = Visibility.Visible;
                BrowserCoordinatesPanel.Visibility = Visibility.Visible;
                BrowserUseLastResultsLabel.Visibility = Visibility.Visible;
                BrowserUseLastResults.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Hides the branch action details panel
    /// </summary>
    private void HideBranchActionDetails()
    {
        LogManager.Log("Hiding branch action details", LogLevel.Debug);
        _selectedBranchAction = null;
        _branchContext = "";
        BranchActionDetailsGroup.Visibility = Visibility.Collapsed;
    }
    /// <summary>
    /// Event handler for browser action type selection changes
    /// </summary>
    private void BrowserActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is BrowserActionType selectedType)
        {
            UpdateBrowserActionUI(selectedType);
        }
    }
    /// <summary>
    /// Event handler for variable action type selection changes
    /// </summary>
    private void VariableActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is VariableActionType selectedType)
        {
            UpdateVariableActionUI(selectedType);
        }
    }
    /// <summary>
    /// Event handler for clipboard source radio buttons
    /// </summary>
    private void ClipboardSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Hide all source-specific items first
        ClipboardTextLabel.Visibility = Visibility.Collapsed;
        ClipboardText.Visibility = Visibility.Collapsed;
        ClipboardVariableLabel.Visibility = Visibility.Collapsed;
        ClipboardVariableName.Visibility = Visibility.Collapsed;
        // Show the relevant items based on selection
        if (sender == ClipboardTextRadioButton)
        {
            ClipboardTextLabel.Visibility = Visibility.Visible;
            ClipboardText.Visibility = Visibility.Visible;
        }
        else if (sender == ClipboardVarRadioButton)
        {
            ClipboardVariableLabel.Visibility = Visibility.Visible;
            ClipboardVariableName.Visibility = Visibility.Visible;
        }
    }
    /// <summary>
    /// Event handler for KeyboardAction type radio buttons
    /// </summary>
    private void KeyboardActionTypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        // First hide all elements
        KeyboardTextLabel.Visibility = Visibility.Collapsed;
        KeyboardActionText.Visibility = Visibility.Collapsed;
        KeyboardKeyLabel.Visibility = Visibility.Collapsed;
        KeyboardActionKey.Visibility = Visibility.Collapsed;
        KeyboardModifiersLabel.Visibility = Visibility.Collapsed;
        KeyboardModifiersPanel.Visibility = Visibility.Collapsed;
        KeyboardDelayLabel.Visibility = Visibility.Collapsed;
        KeyboardActionDelay.Visibility = Visibility.Collapsed;
        // Show relevant elements based on the selected radio button
        if (sender == rbTypeText)
        {
            KeyboardTextLabel.Visibility = Visibility.Visible;
            KeyboardActionText.Visibility = Visibility.Visible;
            KeyboardDelayLabel.Visibility = Visibility.Visible;
            KeyboardActionDelay.Visibility = Visibility.Visible;
        }
        else if (sender == rbKeyPress)
        {
            KeyboardKeyLabel.Visibility = Visibility.Visible;
            KeyboardActionKey.Visibility = Visibility.Visible;
        }
        else if (sender == rbKeyCombination || sender == rbHotkey)
        {
            KeyboardKeyLabel.Visibility = Visibility.Visible;
            KeyboardActionKey.Visibility = Visibility.Visible;
            KeyboardModifiersLabel.Visibility = Visibility.Visible;
            KeyboardModifiersPanel.Visibility = Visibility.Visible;
        }
    }
    /// <summary>
    /// Updates display of current keys for keyboard action
    /// </summary>
    private void UpdateKeyboardCurrentKeys()
    {
        if (_selectedBranchAction is KeyboardAction keyboardAction)
        {
            // Show current keys panel
            KeyboardCurrentKeysLabel.Visibility = Visibility.Visible;
            KeyboardCurrentKeysPanel.Visibility = Visibility.Visible;
            // Build string representation of keys
            string keyString = "";
            if (keyboardAction.Keys != null && keyboardAction.Keys.Length > 0)
            {
                keyString = string.Join(" + ", keyboardAction.Keys.Select(k => new KeyItem(k).DisplayValue));
            }
            else
            {
                keyString = "(No keys assigned)";
            }
            KeyboardCurrentKeys.Text = keyString;
        }
    }
    /// <summary>
    /// Gets the current mouse position for a branch action
    /// </summary>
    private void GetCurrentBranchMousePosition_Click(object sender, RoutedEventArgs e)
    {
        // Get current mouse position
        var position = System.Windows.Forms.Cursor.Position;
        MouseActionXPosition.Text = position.X.ToString();
        MouseActionYPosition.Text = position.Y.ToString();
    }
    /// <summary>
    /// Event handler for the Apply Changes button in branch action details
    /// </summary>
    private void ApplyBranchActionChanges_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBranchAction == null)
            return;
        LogManager.Log($"Applying changes to branch action: {_selectedBranchAction.Name}", LogLevel.Debug);
        // Update common action properties
        _selectedBranchAction.Name = BranchActionName.Text;
        _selectedBranchAction.Description = BranchActionDescription.Text;
        // Parse delay with error handling
        if (int.TryParse(BranchActionDelay.Text, out int delay))
        {
            _selectedBranchAction.DelayBefore = delay;
        }
        // Update type-specific properties
        if (_selectedBranchAction is MouseAction mouseAction)
        {
            LogManager.Log("Updating MouseAction properties", LogLevel.Debug);
            // Parse X, Y positions
            if (int.TryParse(MouseActionXPosition.Text, out int x))
            {
                mouseAction.X = x;
            }
            if (int.TryParse(MouseActionYPosition.Text, out int y))
            {
                mouseAction.Y = y;
            }
            // Parse wheel delta
            if (int.TryParse(MouseActionWheelDelta.Text, out int wheelDelta))
            {
                mouseAction.WheelDelta = wheelDelta;
            }
            // Set action type based on selected radio button
            if (rbLeftClick.IsChecked == true)
                mouseAction.ActionType = MouseActionType.LeftClick;
            else if (rbRightClick.IsChecked == true)
                mouseAction.ActionType = MouseActionType.RightClick;
            else if (rbMiddleClick.IsChecked == true)
                mouseAction.ActionType = MouseActionType.MiddleClick;
            else if (rbDoubleClick.IsChecked == true)
                mouseAction.ActionType = MouseActionType.DoubleClick;
            else if (rbMouseWheel.IsChecked == true)
                mouseAction.ActionType = MouseActionType.MouseWheel;
        }
        else if (_selectedBranchAction is KeyboardAction keyboardAction)
        {
            LogManager.Log("Updating KeyboardAction properties", LogLevel.Debug);
            // Set action type based on selected radio button
            if (rbTypeText.IsChecked == true)
            {
                keyboardAction.ActionType = KeyboardActionType.TypeText;
                keyboardAction.Text = KeyboardActionText.Text;
                // Parse delay between chars
                if (int.TryParse(KeyboardActionDelay.Text, out int charDelay))
                {
                    keyboardAction.DelayBetweenChars = charDelay;
                }
            }
            else if (rbKeyPress.IsChecked == true)
            {
                keyboardAction.ActionType = KeyboardActionType.KeyPress;
                // Set key if one is selected
                if (KeyboardActionKey.SelectedItem is KeyItem keyItem)
                {
                    keyboardAction.Keys = new Key[] { keyItem.KeyValue };
                }
            }
            else if (rbKeyCombination.IsChecked == true || rbHotkey.IsChecked == true)
            {
                keyboardAction.ActionType = rbKeyCombination.IsChecked == true
                    ? KeyboardActionType.KeyCombination
                    : KeyboardActionType.Hotkey;
                // Build key combination with modifiers and main key
                List<Key> keys = new List<Key>();
                // Add modifiers in a specific order: Ctrl, Alt, Shift
                if (KeyboardCtrlCheckbox.IsChecked == true)
                    keys.Add(Key.LeftCtrl);
                if (KeyboardAltCheckbox.IsChecked == true)
                    keys.Add(Key.LeftAlt);
                if (KeyboardShiftCheckbox.IsChecked == true)
                    keys.Add(Key.LeftShift);
                // Add main key if one is selected
                if (KeyboardActionKey.SelectedItem is KeyItem keyItem && keyItem.KeyValue != Key.None)
                {
                    keys.Add(keyItem.KeyValue);
                }
                // Set keys if any are defined
                if (keys.Count > 0)
                {
                    keyboardAction.Keys = keys.ToArray();
                }
            }
            // After updating, refresh the current keys display
            UpdateKeyboardCurrentKeys();
        }
        else if (_selectedBranchAction is WaitAction waitAction)
        {
            LogManager.Log("Updating WaitAction properties", LogLevel.Debug);
            // Parse wait time
            if (int.TryParse(WaitActionTime.Text, out int waitTime))
            {
                waitAction.WaitTime = waitTime;
            }
        }
        else if (_selectedBranchAction is VariableAction variableAction)
        {
            LogManager.Log("Updating VariableAction properties", LogLevel.Debug);
            // Set variable name
            variableAction.VariableName = VariableActionNameCombo.Text;
            // Set action type if one is selected
            if (VariableActionTypeCombo.SelectedItem is VariableActionType selectedType)
            {
                variableAction.ActionType = selectedType;
            }
            // Set properties based on action type
            if (variableAction.ActionType == VariableActionType.SetValue ||
                variableAction.ActionType == VariableActionType.AppendText)
            {
                variableAction.Value = VariableActionValue.Text;
            }
            else if (variableAction.ActionType == VariableActionType.Increment ||
                    variableAction.ActionType == VariableActionType.Decrement)
            {
                if (int.TryParse(VariableActionIncrement.Text, out int incrementValue))
                {
                    variableAction.IncrementValue = incrementValue;
                }
            }
        }
        else if (_selectedBranchAction is ClipboardAction clipboardAction)
        {
            LogManager.Log("Updating ClipboardAction properties", LogLevel.Debug);
            // Set source based on radio button
            clipboardAction.UseVariable = ClipboardVarRadioButton.IsChecked == true;
            // Set text or variable name based on source
            if (clipboardAction.UseVariable)
            {
                clipboardAction.VariableName = ClipboardVariableName.Text;
            }
            else
            {
                clipboardAction.Text = ClipboardText.Text;
            }
            // Set other properties
            clipboardAction.AppendToClipboard = ClipboardAppendCheck.IsChecked == true;
            // Parse retry settings
            if (int.TryParse(ClipboardRetryCount.Text, out int retryCount))
            {
                clipboardAction.RetryCount = retryCount;
            }
            if (int.TryParse(ClipboardRetryWaitTime.Text, out int retryWait))
            {
                clipboardAction.RetryWaitTime = retryWait;
            }
        }
        else if (_selectedBranchAction is JsonAction jsonAction)
        {
            LogManager.Log("Updating JsonAction properties", LogLevel.Debug);
            // Set properties
            jsonAction.CheckClipboard = JsonCheckClipboard.IsChecked == true;
            jsonAction.JsonTemplate = JsonTemplate.Text;
            // Parse numeric values
            if (int.TryParse(JsonOffsetX.Text, out int offsetX))
            {
                jsonAction.OffsetX = offsetX;
            }
            if (int.TryParse(JsonOffsetY.Text, out int offsetY))
            {
                jsonAction.OffsetY = offsetY;
            }
            if (int.TryParse(JsonRetryCount.Text, out int retryCount))
            {
                jsonAction.RetryCount = retryCount;
            }
            if (int.TryParse(JsonRetryWaitTime.Text, out int retryWait))
            {
                jsonAction.RetryWaitTime = retryWait;
            }
            jsonAction.ContinueOnError = JsonContinueOnError.IsChecked == true;
        }
        else if (_selectedBranchAction is BrowserAction browserAction)
        {
            LogManager.Log("Updating BrowserAction properties", LogLevel.Debug);
            // Set action type if one is selected
            if (BrowserActionTypeCombo.SelectedItem is BrowserActionType selectedType)
            {
                browserAction.ActionType = selectedType;
            }
            // Set type-specific properties
            switch (browserAction.ActionType)
            {
                case BrowserActionType.FindElementAndClick:
                    browserAction.Selector = BrowserSelector.Text;
                    break;
                case BrowserActionType.ExecuteJavaScript:
                    browserAction.JavaScript = BrowserJavaScript.Text;
                    break;
                case BrowserActionType.GetCoordinates:
                    if (int.TryParse(BrowserXResult.Text, out int x))
                    {
                        browserAction.XResult = x;
                    }
                    if (int.TryParse(BrowserYResult.Text, out int y))
                    {
                        browserAction.YResult = y;
                    }
                    browserAction.UseLastResults = BrowserUseLastResults.IsChecked == true;
                    break;
            }
        }
        // Display confirmation
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.StatusMessage = $"Changes applied to {_branchContext} branch action";
        }
    }
}
}
