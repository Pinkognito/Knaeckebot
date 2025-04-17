using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;
using ListView = System.Windows.Controls.ListView;
using CheckBox = System.Windows.Controls.CheckBox;
using MouseAction = Knaeckebot.Models.MouseAction;
using ComboBox = System.Windows.Controls.ComboBox;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for ActionDetailsControl.xaml
    /// </summary>
    public partial class ActionDetailsControl : UserControl
    {
        // Store currently selected branch action for details panel
        private ActionBase _selectedBranchAction;
        private string _branchContext = "";

        public ActionDetailsControl()
        {
            InitializeComponent();

            // Add event handler for the ListView
            if (FindName("LoopActionsListView") is ListView loopListView)
                loopListView.SelectionChanged += LoopActionsListView_SelectionChanged;

            // Add event handlers for If Action ListViews if they exist
            if (FindName("ThenActionsListView") is ListView thenListView)
            {
                thenListView.SelectionChanged += ThenActionsListView_SelectionChanged;
                thenListView.GotFocus += ThenActionsListView_GotFocus;
                LogManager.Log("ThenActionsListView event handlers initialized", LogLevel.Debug);
            }

            if (FindName("ElseActionsListView") is ListView elseListView)
            {
                elseListView.SelectionChanged += ElseActionsListView_SelectionChanged;
                elseListView.GotFocus += ElseActionsListView_GotFocus;
                LogManager.Log("ElseActionsListView event handlers initialized", LogLevel.Debug);
            }

            // Register for data context changed to monitor when the selected action changes
            this.DataContextChanged += ActionDetailsControl_DataContextChanged;
        }

        /// <summary>
        /// Handles data context changes to monitor selected action changes
        /// </summary>
        private void ActionDetailsControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel viewModel)
            {
                // Subscribe to selected action changes
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.SelectedAction))
                    {
                        LogManager.Log("SelectedAction changed in ActionDetailsControl", LogLevel.Debug);
                        if (viewModel.SelectedAction is IfAction ifAction)
                        {
                            LogManager.Log($"IfAction selected. UseElseBranch={ifAction.UseElseBranch}, " +
                                          $"ThenActions={ifAction.ThenActions.Count}, ElseActions={ifAction.ElseActions.Count}",
                                          LogLevel.Debug);

                            // Update UI states when an IfAction is selected
                            UpdateThenActionButtonStates();
                            UpdateElseActionButtonStates();
                        }

                        // Reset branch action details
                        HideBranchActionDetails();
                    }
                };
            }
        }

        /// <summary>
        /// Event handler for double-clicking on a branch action
        /// </summary>
        private void BranchAction_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is ActionBase action)
            {
                LogManager.Log($"Double-clicked on branch action: {action.Name}", LogLevel.Debug);

                // Determine which branch this action belongs to
                if (listView.Name == "ThenActionsListView")
                {
                    _branchContext = "THEN";
                }
                else if (listView.Name == "ElseActionsListView")
                {
                    _branchContext = "ELSE";
                }
                else if (listView.Name == "LoopActionsListView")
                {
                    _branchContext = "LOOP";
                }

                // Show the branch action details
                ShowBranchActionDetails(action);
            }
        }

        /// <summary>
        /// Shows the details for a branch action
        /// </summary>
        private void ShowBranchActionDetails(ActionBase action)
        {
            LogManager.Log($"Showing details for branch action: {action.Name} ({action.GetType().Name})", LogLevel.Debug);

            _selectedBranchAction = action;

            // Update common UI elements
            BranchActionType.Text = action.GetType().Name;
            BranchActionName.Text = action.Name;
            BranchActionDescription.Text = action.Description;
            BranchActionDelay.Text = action.DelayBefore.ToString();

            // Hide all type-specific property groups first
            MouseActionPropertiesGroup.Visibility = Visibility.Collapsed;
            KeyboardActionPropertiesGroup.Visibility = Visibility.Collapsed;
            WaitActionPropertiesGroup.Visibility = Visibility.Collapsed;
            VariableActionPropertiesGroup.Visibility = Visibility.Collapsed;
            ClipboardActionPropertiesGroup.Visibility = Visibility.Collapsed;
            JsonActionPropertiesGroup.Visibility = Visibility.Collapsed;

            // Populate type-specific properties based on action type
            if (action is MouseAction mouseAction)
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
            else if (action is KeyboardAction keyboardAction)
            {
                LogManager.Log($"Populating KeyboardAction properties", LogLevel.Debug);
                KeyboardActionPropertiesGroup.Visibility = Visibility.Visible;

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
                UpdateKeyboardCurrentKeys(keyboardAction);
            }
            else if (action is WaitAction waitAction)
            {
                LogManager.Log($"Populating WaitAction properties", LogLevel.Debug);
                WaitActionPropertiesGroup.Visibility = Visibility.Visible;

                // Set wait time
                WaitActionTime.Text = waitAction.WaitTime.ToString();
            }
            else if (action is VariableAction variableAction)
            {
                LogManager.Log($"Populating VariableAction properties", LogLevel.Debug);
                VariableActionPropertiesGroup.Visibility = Visibility.Visible;

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
            else if (action is ClipboardAction clipboardAction)
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
            else if (action is JsonAction jsonAction)
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

            // Show the branch action details group
            BranchActionDetailsGroup.Visibility = Visibility.Visible;
            BranchActionDetailsGroup.Header = $"{_branchContext} Branch Action Details";
        }

        /// <summary>
        /// Updates display of current keys for keyboard action
        /// </summary>
        private void UpdateKeyboardCurrentKeys(KeyboardAction keyboardAction)
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

        /// <summary>
        /// Hides the branch action details
        /// </summary>
        private void HideBranchActionDetails()
        {
            LogManager.Log("Hiding branch action details", LogLevel.Debug);
            _selectedBranchAction = null;
            _branchContext = "";
            BranchActionDetailsGroup.Visibility = Visibility.Collapsed;
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
        /// Updates the UI for VariableAction based on action type
        /// </summary>
        private void UpdateVariableActionUI(VariableActionType actionType)
        {
            // First hide all specific elements
            VariableValueLabel.Visibility = Visibility.Collapsed;
            VariableActionValue.Visibility = Visibility.Collapsed;
            VariableIncrementLabel.Visibility = Visibility.Collapsed;
            VariableActionIncrement.Visibility = Visibility.Collapsed;
            VariableListIndexLabel.Visibility = Visibility.Collapsed;
            VariableListIndex.Visibility = Visibility.Collapsed;
            VariableItemLabel.Visibility = Visibility.Collapsed;
            VariableItemValue.Visibility = Visibility.Collapsed;

            // Show relevant elements based on action type
            switch (actionType)
            {
                case VariableActionType.SetValue:
                    VariableValueLabel.Visibility = Visibility.Visible;
                    VariableActionValue.Visibility = Visibility.Visible;
                    break;

                case VariableActionType.AppendText:
                    VariableValueLabel.Visibility = Visibility.Visible;
                    VariableActionValue.Visibility = Visibility.Visible;
                    break;

                case VariableActionType.Increment:
                case VariableActionType.Decrement:
                    VariableIncrementLabel.Visibility = Visibility.Visible;
                    VariableActionIncrement.Visibility = Visibility.Visible;
                    break;

                case VariableActionType.RemoveListItem:
                    VariableListIndexLabel.Visibility = Visibility.Visible;
                    VariableListIndex.Visibility = Visibility.Visible;
                    break;

                case VariableActionType.AddListItem:
                case VariableActionType.AddTableRow:
                    VariableItemLabel.Visibility = Visibility.Visible;
                    VariableItemValue.Visibility = Visibility.Visible;
                    if (actionType == VariableActionType.AddTableRow)
                        VariableItemLabel.Text = "Row:";
                    else
                        VariableItemLabel.Text = "Item:";
                    break;
            }
        }

        /// <summary>
        /// Event handler for VariableAction type selection
        /// </summary>
        private void VariableActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is VariableActionType selectedType)
            {
                UpdateVariableActionUI(selectedType);
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
                UpdateKeyboardCurrentKeys(keyboardAction);
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
                switch (variableAction.ActionType)
                {
                    case VariableActionType.SetValue:
                    case VariableActionType.AppendText:
                        variableAction.Value = VariableActionValue.Text;
                        break;

                    case VariableActionType.Increment:
                    case VariableActionType.Decrement:
                        if (int.TryParse(VariableActionIncrement.Text, out int incrementValue))
                        {
                            variableAction.IncrementValue = incrementValue;
                        }
                        break;

                    case VariableActionType.AddListItem:
                    case VariableActionType.AddTableRow:
                        variableAction.Value = VariableItemValue.Text;
                        break;

                    case VariableActionType.RemoveListItem:
                        if (int.TryParse(VariableListIndex.Text, out int listIndex))
                        {
                            variableAction.ListIndex = listIndex;
                        }
                        break;
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

            // Display confirmation
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StatusMessage = $"Changes applied to {_branchContext} branch action";
            }
        }

        /// <summary>
        /// Handles focus events for the ThenActionsListView
        /// </summary>
        private void ThenActionsListView_GotFocus(object sender, RoutedEventArgs e)
        {
            LogManager.Log("ThenActionsListView got focus", LogLevel.Debug);
        }

        /// <summary>
        /// Handles focus events for the ElseActionsListView
        /// </summary>
        private void ElseActionsListView_GotFocus(object sender, RoutedEventArgs e)
        {
            LogManager.Log("ElseActionsListView got focus", LogLevel.Debug);

            // When ELSE branch gets focus, ensure buttons are properly updated
            UpdateElseActionButtonStates();
        }

        /// <summary>
        /// Handles selection changes in the LoopActions ListView
        /// </summary>
        private void LoopActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if items are selected and enable/disable buttons accordingly
            UpdateLoopActionButtonStates();
        }

        /// <summary>
        /// Handles selection changes in the ThenActions ListView
        /// </summary>
        private void ThenActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LogManager.Log("ThenActionsListView selection changed", LogLevel.Debug);
            if (sender is ListView listView)
            {
                LogManager.Log($"THEN branch selected items: {listView.SelectedItems.Count}", LogLevel.Debug);
            }
            UpdateThenActionButtonStates();
        }

        /// <summary>
        /// Handles selection changes in the ElseActions ListView
        /// </summary>
        private void ElseActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LogManager.Log("ElseActionsListView selection changed", LogLevel.Debug);
            if (sender is ListView listView)
            {
                LogManager.Log($"ELSE branch selected items: {listView.SelectedItems.Count}", LogLevel.Debug);
            }
            UpdateElseActionButtonStates();
        }

        /// <summary>
        /// Updates the activation state of the loop action buttons
        /// </summary>
        private void UpdateLoopActionButtonStates()
        {
            if (FindName("LoopActionsListView") is ListView listView)
            {
                bool hasSelection = listView.SelectedItems.Count > 0;
                LogManager.Log($"Updating Loop action buttons. HasSelection: {hasSelection}", LogLevel.Debug);

                // Find the buttons by their names and set their IsEnabled status
                if (this.FindName("MoveUpButton") is Button moveUpButton)
                    moveUpButton.IsEnabled = hasSelection;

                if (this.FindName("MoveDownButton") is Button moveDownButton)
                    moveDownButton.IsEnabled = hasSelection;

                if (this.FindName("DeleteButton") is Button deleteButton)
                    deleteButton.IsEnabled = hasSelection;

                if (this.FindName("CopyButton") is Button copyButton)
                    copyButton.IsEnabled = hasSelection;
            }
        }

        /// <summary>
        /// Updates the activation state of the then action buttons
        /// </summary>
        private void UpdateThenActionButtonStates()
        {
            if (FindName("ThenActionsListView") is ListView listView)
            {
                bool hasSelection = listView.SelectedItems.Count > 0;
                LogManager.Log($"Updating THEN action buttons. HasSelection: {hasSelection}", LogLevel.Debug);

                // Find the buttons by their names and set their IsEnabled status
                if (this.FindName("MoveUpThenButton") is Button moveUpButton)
                    moveUpButton.IsEnabled = hasSelection;

                if (this.FindName("MoveDownThenButton") is Button moveDownButton)
                    moveDownButton.IsEnabled = hasSelection;

                if (this.FindName("DeleteThenButton") is Button deleteButton)
                    deleteButton.IsEnabled = hasSelection;

                if (this.FindName("CopyThenButton") is Button copyButton)
                    copyButton.IsEnabled = hasSelection;

                // Also enable the paste button regardless of selection (as long as the THEN branch exists)
                if (this.FindName("PasteThenButton") is Button pasteButton)
                {
                    var vm = DataContext as MainViewModel;
                    pasteButton.IsEnabled = vm?.CopiedActions.Count > 0;
                    LogManager.Log($"THEN Paste button enabled: {pasteButton.IsEnabled}", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Updates the activation state of the else action buttons
        /// </summary>
        private void UpdateElseActionButtonStates()
        {
            // First check if the ELSE branch is actually enabled by checking the IfAction
            bool elseEnabled = false;
            IfAction? ifAction = null;

            if (DataContext is MainViewModel viewModel && viewModel.SelectedAction is IfAction selectedIfAction)
            {
                ifAction = selectedIfAction;
                elseEnabled = selectedIfAction.UseElseBranch;
                LogManager.Log($"ELSE branch status check: IfAction.UseElseBranch = {elseEnabled}", LogLevel.Debug);
            }

            if (FindName("ElseActionsListView") is ListView listView)
            {
                bool hasSelection = listView.SelectedItems.Count > 0;
                LogManager.Log($"Updating ELSE action buttons. ElseEnabled: {elseEnabled}, HasSelection: {hasSelection}", LogLevel.Debug);

                // Find the buttons by their names and set their IsEnabled status
                // Only enable if the ELSE branch is active AND there's a selection (for move/delete)
                if (this.FindName("MoveUpElseButton") is Button moveUpButton)
                {
                    moveUpButton.IsEnabled = elseEnabled && hasSelection;
                    LogManager.Log($"ELSE MoveUp button enabled: {moveUpButton.IsEnabled}", LogLevel.Debug);
                }

                if (this.FindName("MoveDownElseButton") is Button moveDownButton)
                {
                    moveDownButton.IsEnabled = elseEnabled && hasSelection;
                    LogManager.Log($"ELSE MoveDown button enabled: {moveDownButton.IsEnabled}", LogLevel.Debug);
                }

                if (this.FindName("DeleteElseButton") is Button deleteButton)
                {
                    deleteButton.IsEnabled = elseEnabled && hasSelection;
                    LogManager.Log($"ELSE Delete button enabled: {deleteButton.IsEnabled}", LogLevel.Debug);
                }

                if (this.FindName("CopyElseButton") is Button copyButton)
                {
                    copyButton.IsEnabled = elseEnabled && hasSelection;
                    LogManager.Log($"ELSE Copy button enabled: {copyButton.IsEnabled}", LogLevel.Debug);
                }

                // For the paste button, we only need the ELSE branch to be active, regardless of selection
                if (this.FindName("PasteElseButton") is Button pasteButton)
                {
                    var vm = DataContext as MainViewModel;
                    pasteButton.IsEnabled = elseEnabled && vm?.CopiedActions.Count > 0;
                    LogManager.Log($"ELSE Paste button enabled: {pasteButton.IsEnabled}", LogLevel.Debug);
                }
            }
            else
            {
                LogManager.Log("Could not find ElseActionsListView", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Event handler for the "Current Position" button
        /// </summary>
        private void GetCurrentMousePosition_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var mouseAction = vm?.SelectedAction as MouseAction;

            if (mouseAction != null)
            {
                // Get current mouse position
                var position = System.Windows.Forms.Cursor.Position;
                mouseAction.X = position.X;
                mouseAction.Y = position.Y;
            }
        }

        /// <summary>
        /// Event handler for copying actions from the loop
        /// </summary>
        private void CopyLoopAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("CopyLoopAction_Click called", LogLevel.Debug);

            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction &&
                FindName("LoopActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Clear the copied actions and add the selected ones
                    vm.CopiedActions.Clear();
                    foreach (var action in selectedItems)
                    {
                        vm.CopiedActions.Add(action);
                        LogManager.Log($"Loop action '{action.Name}' added to copy buffer", LogLevel.Debug);
                    }

                    vm.StatusMessage = selectedItems.Count == 1
                        ? "Action copied from loop"
                        : $"{selectedItems.Count} actions copied from loop";
                }
            }
        }

        /// <summary>
        /// Event handler for copying actions from the THEN branch
        /// </summary>
        private void CopyThenAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("CopyThenAction_Click called", LogLevel.Debug);

            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                FindName("ThenActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Clear the copied actions and add the selected ones
                    vm.CopiedActions.Clear();
                    foreach (var action in selectedItems)
                    {
                        vm.CopiedActions.Add(action);
                        LogManager.Log($"THEN action '{action.Name}' added to copy buffer", LogLevel.Debug);
                    }

                    vm.StatusMessage = selectedItems.Count == 1
                        ? "Action copied from THEN branch"
                        : $"{selectedItems.Count} actions copied from THEN branch";
                }
            }
        }

        /// <summary>
        /// Event handler for copying actions from the ELSE branch
        /// </summary>
        private void CopyElseAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("CopyElseAction_Click called", LogLevel.Debug);

            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction && ifAction.UseElseBranch &&
                FindName("ElseActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Clear the copied actions and add the selected ones
                    vm.CopiedActions.Clear();
                    foreach (var action in selectedItems)
                    {
                        vm.CopiedActions.Add(action);
                        LogManager.Log($"ELSE action '{action.Name}' added to copy buffer", LogLevel.Debug);
                    }

                    vm.StatusMessage = selectedItems.Count == 1
                        ? "Action copied from ELSE branch"
                        : $"{selectedItems.Count} actions copied from ELSE branch";
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Up" button for loop actions
        /// </summary>
        private void MoveLoopActionUp_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction &&
                FindName("LoopActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index so we move from top to bottom
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = loopAction.LoopActions.IndexOf(item) })
                        .OrderBy(x => x.Index)
                        .ToList();

                    // Check if we can move up (if first item is not at index 0)
                    if (itemsWithIndices.First().Index > 0)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item up
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = loopAction.LoopActions.IndexOf(itemWithIndex.Item);
                            if (index > 0)
                            {
                                loopAction.LoopActions.Move(index, index - 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index > 0 && index - 1 < loopAction.LoopActions.Count)
                            {
                                listView.SelectedItems.Add(loopAction.LoopActions[index - 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"Loop action '{selectedItems[0].Name}' moved up"
                            : $"{selectedItems.Count} loop actions moved up");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved up in loop"
                            : $"{selectedItems.Count} actions moved up in loop";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Down" button for loop actions
        /// </summary>
        private void MoveLoopActionDown_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction &&
                FindName("LoopActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index in descending order so we move from bottom to top
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = loopAction.LoopActions.IndexOf(item) })
                        .OrderByDescending(x => x.Index)
                        .ToList();

                    // Check if we can move down (if last item is not at the end)
                    if (itemsWithIndices.First().Index < loopAction.LoopActions.Count - 1)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item down
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = loopAction.LoopActions.IndexOf(itemWithIndex.Item);
                            if (index < loopAction.LoopActions.Count - 1)
                            {
                                loopAction.LoopActions.Move(index, index + 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index + 1 < loopAction.LoopActions.Count)
                            {
                                listView.SelectedItems.Add(loopAction.LoopActions[index + 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"Loop action '{selectedItems[0].Name}' moved down"
                            : $"{selectedItems.Count} loop actions moved down");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved down in loop"
                            : $"{selectedItems.Count} actions moved down in loop";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Delete" button for loop actions
        /// </summary>
        private void DeleteLoopAction_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction &&
                FindName("LoopActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Confirmation dialog before deleting
                    string message = selectedItems.Count == 1
                        ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the loop?"
                        : $"Are you sure you want to remove {selectedItems.Count} actions from the loop?";

                    var result = System.Windows.MessageBox.Show(
                        message,
                        "Delete Actions",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Find minimum index for selection after deletion
                        int firstIndex = selectedItems.Min(item => loopAction.LoopActions.IndexOf(item));

                        // Remove all selected actions
                        foreach (var action in selectedItems)
                        {
                            loopAction.LoopActions.Remove(action);
                            LogManager.Log($"Loop action '{action.Name}' deleted");
                        }

                        // Status message
                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action removed from loop"
                            : $"{selectedItems.Count} actions removed from loop";

                        // Select next element if available
                        if (loopAction.LoopActions.Count > 0)
                        {
                            listView.SelectedIndex = Math.Min(firstIndex, loopAction.LoopActions.Count - 1);
                            listView.Focus();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Paste" button for loop actions
        /// </summary>
        private void PasteLoopAction_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction)
            {
                if (vm.CopiedActions.Count > 0)
                {
                    // Find the selected item(s) in the loop
                    var listView = FindName("LoopActionsListView") as ListView;
                    if (listView?.SelectedItems.Count > 0)
                    {
                        // Find the index after the last selected item
                        var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();
                        var lastSelectedIndex = selectedItems
                            .Select(item => loopAction.LoopActions.IndexOf(item))
                            .Max();

                        int insertIndex = lastSelectedIndex + 1;

                        // Insert each copied action at the correct position
                        foreach (var action in vm.CopiedActions)
                        {
                            var actionCopy = action.Clone();
                            if (insertIndex <= loopAction.LoopActions.Count)
                            {
                                loopAction.LoopActions.Insert(insertIndex, actionCopy);
                                insertIndex++;
                            }
                            else
                            {
                                loopAction.LoopActions.Add(actionCopy);
                            }
                        }

                        LogManager.Log($"{vm.CopiedActions.Count} action(s) inserted after selection in loop");
                        vm.StatusMessage = vm.CopiedActions.Count == 1
                            ? "Action inserted into loop"
                            : $"{vm.CopiedActions.Count} actions inserted into loop";

                        // Select the newly inserted actions
                        listView.SelectedItems.Clear();
                        for (int i = 0; i < vm.CopiedActions.Count; i++)
                        {
                            int index = lastSelectedIndex + 1 + i;
                            if (index < loopAction.LoopActions.Count)
                            {
                                listView.SelectedItems.Add(loopAction.LoopActions[index]);
                            }
                        }
                    }
                    else
                    {
                        // No selection, add to the end as before
                        foreach (var action in vm.CopiedActions)
                        {
                            var actionCopy = action.Clone();
                            loopAction.LoopActions.Add(actionCopy);
                        }

                        LogManager.Log($"{vm.CopiedActions.Count} action(s) added to end of loop");
                        vm.StatusMessage = vm.CopiedActions.Count == 1
                            ? "Action added to loop"
                            : $"{vm.CopiedActions.Count} actions added to loop";

                        // Select the newly added actions
                        listView.SelectedItems.Clear();
                        for (int i = 0; i < vm.CopiedActions.Count; i++)
                        {
                            int index = loopAction.LoopActions.Count - vm.CopiedActions.Count + i;
                            if (index >= 0 && index < loopAction.LoopActions.Count)
                            {
                                listView.SelectedItems.Add(loopAction.LoopActions[index]);
                            }
                        }
                    }
                }
                else
                {
                    LogManager.Log("No actions to paste", LogLevel.Warning);
                    vm.StatusMessage = "No actions to paste";
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Up" button for then actions
        /// </summary>
        private void MoveThenActionUp_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                FindName("ThenActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index so we move from top to bottom
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = ifAction.ThenActions.IndexOf(item) })
                        .OrderBy(x => x.Index)
                        .ToList();

                    // Check if we can move up (if first item is not at index 0)
                    if (itemsWithIndices.First().Index > 0)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item up
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = ifAction.ThenActions.IndexOf(itemWithIndex.Item);
                            if (index > 0)
                            {
                                ifAction.ThenActions.Move(index, index - 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index > 0 && index - 1 < ifAction.ThenActions.Count)
                            {
                                listView.SelectedItems.Add(ifAction.ThenActions[index - 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"THEN action '{selectedItems[0].Name}' moved up"
                            : $"{selectedItems.Count} THEN actions moved up");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved up in THEN branch"
                            : $"{selectedItems.Count} actions moved up in THEN branch";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Down" button for then actions
        /// </summary>
        private void MoveThenActionDown_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                FindName("ThenActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index in descending order so we move from bottom to top
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = ifAction.ThenActions.IndexOf(item) })
                        .OrderByDescending(x => x.Index)
                        .ToList();

                    // Check if we can move down (if last item is not at the end)
                    if (itemsWithIndices.First().Index < ifAction.ThenActions.Count - 1)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item down
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = ifAction.ThenActions.IndexOf(itemWithIndex.Item);
                            if (index < ifAction.ThenActions.Count - 1)
                            {
                                ifAction.ThenActions.Move(index, index + 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index + 1 < ifAction.ThenActions.Count)
                            {
                                listView.SelectedItems.Add(ifAction.ThenActions[index + 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"THEN action '{selectedItems[0].Name}' moved down"
                            : $"{selectedItems.Count} THEN actions moved down");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved down in THEN branch"
                            : $"{selectedItems.Count} actions moved down in THEN branch";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Delete" button for then actions
        /// </summary>
        private void DeleteThenAction_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                FindName("ThenActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Confirmation dialog before deleting
                    string message = selectedItems.Count == 1
                        ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the THEN branch?"
                        : $"Are you sure you want to remove {selectedItems.Count} actions from the THEN branch?";

                    var result = System.Windows.MessageBox.Show(
                        message,
                        "Delete Actions",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Find minimum index for selection after deletion
                        int firstIndex = selectedItems.Min(item => ifAction.ThenActions.IndexOf(item));

                        // Remove all selected actions
                        foreach (var action in selectedItems)
                        {
                            ifAction.ThenActions.Remove(action);
                            LogManager.Log($"THEN action '{action.Name}' deleted");
                        }

                        // Status message
                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action removed from THEN branch"
                            : $"{selectedItems.Count} actions removed from THEN branch";

                        // Select next element if available
                        if (ifAction.ThenActions.Count > 0)
                        {
                            listView.SelectedIndex = Math.Min(firstIndex, ifAction.ThenActions.Count - 1);
                            listView.Focus();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Paste" button for then actions
        /// </summary>
        private void PasteThenAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("*** PasteThenAction START ***", LogLevel.Debug);
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction)
            {
                if (vm.CopiedActions.Count > 0)
                {
                    // Find the selected item(s) in the THEN branch
                    var listView = FindName("ThenActionsListView") as ListView;

                    // COMPLETELY DIFFERENT APPROACH - Direct manipulation using List

                    // Get all current actions in a List
                    List<ActionBase> allActions = new List<ActionBase>();
                    LogManager.Log($"Original ThenActions count: {ifAction.ThenActions.Count}", LogLevel.Debug);

                    // First add all existing actions to our list
                    foreach (var action in ifAction.ThenActions)
                    {
                        allActions.Add(action);
                        LogManager.Log($"Added existing action to list: {action.Name}", LogLevel.Debug);
                    }

                    int insertAt;

                    // Determine insert position
                    if (listView?.SelectedItems.Count > 0)
                    {
                        var selectedAction = listView.SelectedItems[0] as ActionBase;
                        LogManager.Log($"Selected action: {selectedAction?.Name}", LogLevel.Debug);

                        int selectedIndex = -1;

                        // Find the actual index in our List
                        for (int i = 0; i < allActions.Count; i++)
                        {
                            if (allActions[i] == selectedAction)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }

                        LogManager.Log($"Found selected action at index: {selectedIndex}", LogLevel.Debug);
                        insertAt = selectedIndex + 1; // Insert after the selected item
                    }
                    else
                    {
                        // If nothing selected, insert at the end
                        insertAt = allActions.Count;
                        LogManager.Log($"No selection, will insert at end (index {insertAt})", LogLevel.Debug);
                    }

                    // Create clones of copied actions
                    List<ActionBase> actionsToInsert = new List<ActionBase>();
                    foreach (var action in vm.CopiedActions)
                    {
                        var clone = action.Clone();
                        actionsToInsert.Add(clone);
                        LogManager.Log($"Created clone of {action.GetType().Name}: {clone.Name}", LogLevel.Debug);
                    }

                    // Insert the clones at the desired position
                    allActions.InsertRange(insertAt, actionsToInsert);
                    LogManager.Log($"Inserted {actionsToInsert.Count} actions at position {insertAt}", LogLevel.Debug);

                    // Display the final order
                    LogManager.Log("Final action list:", LogLevel.Debug);
                    for (int i = 0; i < allActions.Count; i++)
                    {
                        LogManager.Log($"  [{i}] {allActions[i].Name}", LogLevel.Debug);
                    }

                    // Now rebuild the collection from our list
                    try
                    {
                        LogManager.Log("Clearing ThenActions collection", LogLevel.Debug);
                        ifAction.ThenActions.Clear();

                        LogManager.Log("Adding actions back to ThenActions", LogLevel.Debug);
                        foreach (var action in allActions)
                        {
                            ifAction.ThenActions.Add(action);
                            LogManager.Log($"Added back to ThenActions: {action.Name}", LogLevel.Debug);
                        }

                        // Success message
                        LogManager.Log($"{actionsToInsert.Count} action(s) inserted at position {insertAt}", LogLevel.Info);
                        vm.StatusMessage = vm.CopiedActions.Count == 1
                            ? "Action inserted into THEN branch"
                            : $"{vm.CopiedActions.Count} actions inserted into THEN branch";

                        // Select the newly inserted actions
                        if (listView != null)
                        {
                            listView.SelectedItems.Clear();
                            for (int i = 0; i < actionsToInsert.Count; i++)
                            {
                                int index = insertAt + i;
                                if (index < ifAction.ThenActions.Count)
                                {
                                    listView.SelectedItems.Add(ifAction.ThenActions[index]);
                                    LogManager.Log($"Selected newly inserted action at index {index}", LogLevel.Debug);
                                }
                            }

                            // Set focus back to the list
                            listView.Focus();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log($"ERROR rebuilding ThenActions: {ex.Message}", LogLevel.Error);
                        LogManager.Log(ex.StackTrace, LogLevel.Error);
                    }
                }
                else
                {
                    LogManager.Log("No actions to paste", LogLevel.Warning);
                    vm.StatusMessage = "No actions to paste";
                }
            }
            LogManager.Log("*** PasteThenAction END ***", LogLevel.Debug);
        }

        /// <summary>
        /// Event handler for the "Move Up" button for else actions
        /// </summary>
        private void MoveElseActionUp_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("MoveElseActionUp_Click called", LogLevel.Debug);
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                ifAction.UseElseBranch &&
                FindName("ElseActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index so we move from top to bottom
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = ifAction.ElseActions.IndexOf(item) })
                        .OrderBy(x => x.Index)
                        .ToList();

                    // Check if we can move up (if first item is not at index 0)
                    if (itemsWithIndices.First().Index > 0)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item up
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = ifAction.ElseActions.IndexOf(itemWithIndex.Item);
                            if (index > 0)
                            {
                                ifAction.ElseActions.Move(index, index - 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index > 0 && index - 1 < ifAction.ElseActions.Count)
                            {
                                listView.SelectedItems.Add(ifAction.ElseActions[index - 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"ELSE action '{selectedItems[0].Name}' moved up"
                            : $"{selectedItems.Count} ELSE actions moved up");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved up in ELSE branch"
                            : $"{selectedItems.Count} actions moved up in ELSE branch";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Down" button for else actions
        /// </summary>
        private void MoveElseActionDown_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("MoveElseActionDown_Click called", LogLevel.Debug);
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                ifAction.UseElseBranch &&
                FindName("ElseActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Sort by index in descending order so we move from bottom to top
                    var itemsWithIndices = selectedItems
                        .Select(item => new { Item = item, Index = ifAction.ElseActions.IndexOf(item) })
                        .OrderByDescending(x => x.Index)
                        .ToList();

                    // Check if we can move down (if last item is not at the end)
                    if (itemsWithIndices.First().Index < ifAction.ElseActions.Count - 1)
                    {
                        // Remember selected indices
                        var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();

                        // Move each item down
                        foreach (var itemWithIndex in itemsWithIndices)
                        {
                            int index = ifAction.ElseActions.IndexOf(itemWithIndex.Item);
                            if (index < ifAction.ElseActions.Count - 1)
                            {
                                ifAction.ElseActions.Move(index, index + 1);
                            }
                        }

                        // Reselect items
                        listView.SelectedItems.Clear();
                        foreach (var index in selectedIndices)
                        {
                            if (index + 1 < ifAction.ElseActions.Count)
                            {
                                listView.SelectedItems.Add(ifAction.ElseActions[index + 1]);
                            }
                        }

                        listView.Focus();

                        LogManager.Log(selectedItems.Count == 1
                            ? $"ELSE action '{selectedItems[0].Name}' moved down"
                            : $"{selectedItems.Count} ELSE actions moved down");

                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action moved down in ELSE branch"
                            : $"{selectedItems.Count} actions moved down in ELSE branch";
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Delete" button for else actions
        /// </summary>
        private void DeleteElseAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("DeleteElseAction_Click called", LogLevel.Debug);
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction &&
                ifAction.UseElseBranch &&
                FindName("ElseActionsListView") is ListView listView)
            {
                // Get all selected items
                var selectedItems = listView.SelectedItems.Cast<ActionBase>().ToList();

                if (selectedItems.Count > 0)
                {
                    // Confirmation dialog before deleting
                    string message = selectedItems.Count == 1
                        ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the ELSE branch?"
                        : $"Are you sure you want to remove {selectedItems.Count} actions from the ELSE branch?";

                    var result = System.Windows.MessageBox.Show(
                        message,
                        "Delete Actions",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Find minimum index for selection after deletion
                        int firstIndex = selectedItems.Min(item => ifAction.ElseActions.IndexOf(item));

                        // Remove all selected actions
                        foreach (var action in selectedItems)
                        {
                            ifAction.ElseActions.Remove(action);
                            LogManager.Log($"ELSE action '{action.Name}' deleted");
                        }

                        // Status message
                        vm.StatusMessage = selectedItems.Count == 1
                            ? "Action removed from ELSE branch"
                            : $"{selectedItems.Count} actions removed from ELSE branch";

                        // Select next element if available
                        if (ifAction.ElseActions.Count > 0)
                        {
                            listView.SelectedIndex = Math.Min(firstIndex, ifAction.ElseActions.Count - 1);
                            listView.Focus();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Paste" button for else actions
        /// </summary>
        private void PasteElseAction_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Log("*** PasteElseAction START ***", LogLevel.Debug);
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is IfAction ifAction && ifAction.UseElseBranch)
            {
                LogManager.Log($"Pasting to ELSE branch. UseElseBranch={ifAction.UseElseBranch}, CopiedActions={vm.CopiedActions.Count}", LogLevel.Debug);

                if (vm.CopiedActions.Count > 0)
                {
                    // Find the selected item(s) in the ELSE branch
                    var listView = FindName("ElseActionsListView") as ListView;

                    // COMPLETELY DIFFERENT APPROACH - Direct manipulation using List

                    // Get all current actions in a List
                    List<ActionBase> allActions = new List<ActionBase>();
                    LogManager.Log($"Original ElseActions count: {ifAction.ElseActions.Count}", LogLevel.Debug);

                    // First add all existing actions to our list
                    foreach (var action in ifAction.ElseActions)
                    {
                        allActions.Add(action);
                        LogManager.Log($"Added existing action to list: {action.Name}", LogLevel.Debug);
                    }

                    int insertAt;

                    // Determine insert position
                    if (listView?.SelectedItems.Count > 0)
                    {
                        var selectedAction = listView.SelectedItems[0] as ActionBase;
                        LogManager.Log($"Selected action: {selectedAction?.Name}", LogLevel.Debug);

                        int selectedIndex = -1;

                        // Find the actual index in our List
                        for (int i = 0; i < allActions.Count; i++)
                        {
                            if (allActions[i] == selectedAction)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }

                        LogManager.Log($"Found selected action at index: {selectedIndex}", LogLevel.Debug);
                        insertAt = selectedIndex + 1; // Insert after the selected item
                    }
                    else
                    {
                        // If nothing selected, insert at the end
                        insertAt = allActions.Count;
                        LogManager.Log($"No selection, will insert at end (index {insertAt})", LogLevel.Debug);
                    }

                    // Create clones of copied actions
                    List<ActionBase> actionsToInsert = new List<ActionBase>();
                    foreach (var action in vm.CopiedActions)
                    {
                        var clone = action.Clone();
                        actionsToInsert.Add(clone);
                        LogManager.Log($"Created clone of {action.GetType().Name}: {clone.Name}", LogLevel.Debug);
                    }

                    // Insert the clones at the desired position
                    allActions.InsertRange(insertAt, actionsToInsert);
                    LogManager.Log($"Inserted {actionsToInsert.Count} actions at position {insertAt}", LogLevel.Debug);

                    // Display the final order
                    LogManager.Log("Final action list:", LogLevel.Debug);
                    for (int i = 0; i < allActions.Count; i++)
                    {
                        LogManager.Log($"  [{i}] {allActions[i].Name}", LogLevel.Debug);
                    }

                    // Now rebuild the collection from our list
                    try
                    {
                        LogManager.Log("Clearing ElseActions collection", LogLevel.Debug);
                        ifAction.ElseActions.Clear();

                        LogManager.Log("Adding actions back to ElseActions", LogLevel.Debug);
                        foreach (var action in allActions)
                        {
                            ifAction.ElseActions.Add(action);
                            LogManager.Log($"Added back to ElseActions: {action.Name}", LogLevel.Debug);
                        }

                        // Success message
                        LogManager.Log($"{actionsToInsert.Count} action(s) inserted at position {insertAt}", LogLevel.Info);
                        vm.StatusMessage = vm.CopiedActions.Count == 1
                            ? "Action inserted into ELSE branch"
                            : $"{vm.CopiedActions.Count} actions inserted into ELSE branch";

                        // Select the newly inserted actions
                        if (listView != null)
                        {
                            listView.SelectedItems.Clear();
                            for (int i = 0; i < actionsToInsert.Count; i++)
                            {
                                int index = insertAt + i;
                                if (index < ifAction.ElseActions.Count)
                                {
                                    listView.SelectedItems.Add(ifAction.ElseActions[index]);
                                    LogManager.Log($"Selected newly inserted action at index {index}", LogLevel.Debug);
                                }
                            }

                            // Set focus back to the list
                            listView.Focus();

                            // Ensure button states are updated
                            UpdateElseActionButtonStates();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log($"ERROR rebuilding ElseActions: {ex.Message}", LogLevel.Error);
                        LogManager.Log(ex.StackTrace, LogLevel.Error);
                    }
                }
                else
                {
                    LogManager.Log("No actions to paste", LogLevel.Warning);
                    vm.StatusMessage = "No actions to paste";
                }
            }
            else
            {
                LogManager.Log("Cannot paste to ELSE branch: either not an IfAction or UseElseBranch is false", LogLevel.Warning);
            }
            LogManager.Log("*** PasteElseAction END ***", LogLevel.Debug);
        }

        /// <summary>
        /// Event handler for the "Generate Sequence JSON Example" button
        /// </summary>
        private void GenerateSequenceJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use sequence name if a sequence is selected
                string sequenceName = "Example Sequence";

                if (vm?.SelectedSequence != null)
                {
                    sequenceName = vm.SelectedSequence.Name;
                }

                // Generate JSON template
                jsonAction.JsonTemplate = JsonAction.CreateSequenceJson(sequenceName);
            }
        }

        /// <summary>
        /// Event handler for the "Sequence with Vars Example" button
        /// </summary>
        private void GenerateSequenceWithVarsJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use sequence name if a sequence is selected
                string sequenceName = "Example Sequence";

                if (vm?.SelectedSequence != null)
                {
                    sequenceName = vm.SelectedSequence.Name;
                }

                // Create example variables
                var variables = new Dictionary<string, string>
                {
                    { "counter", "1" },
                    { "text", "Hello World" },
                    { "date", DateTime.Now.ToString("yyyy-MM-dd") }
                };

                // Generate JSON template
                jsonAction.JsonTemplate = JsonAction.CreateSequenceJson(sequenceName, variables);
            }
        }

        /// <summary>
        /// Event handler for the "Click JSON Example" button
        /// </summary>
        private void GenerateClickJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use current mouse position
                var position = System.Windows.Forms.Cursor.Position;

                // Generate JSON template for click action
                jsonAction.JsonTemplate = JsonAction.CreateClickJson(position.X, position.Y);
            }
        }

        /// <summary>
        /// Event handler for the "Wait JSON Example" button
        /// </summary>
        private void GenerateWaitJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Default wait time: 2 seconds
                int waitTime = 2000;

                // Generate JSON template for wait action
                jsonAction.JsonTemplate = JsonAction.CreateWaitJson(waitTime);
            }
        }

        /// <summary>
        /// Event handler for the "Direct Text" RadioButton in ClipboardAction
        /// </summary>
        private void RadioButton_DirectText_Checked(object sender, RoutedEventArgs e)
        {
            // Direct text selected
            if (DataContext is MainViewModel viewModel &&
                viewModel.SelectedAction is ClipboardAction action)
            {
                LogManager.Log($"### XAML: RadioButton DirectText set for ClipboardAction {action.Id.ToString().Substring(0, 8)}", LogLevel.Debug);

                // Explicitly set UseVariable to false
                bool oldValue = action.UseVariable;
                action.UseVariable = false;

                LogManager.Log($"### XAML: UseVariable changed from {oldValue} to {action.UseVariable} by RadioButton", LogLevel.Debug);

                // Check if the change was applied
                if (action.UseVariable != false)
                {
                    LogManager.Log($"!!! CRITICAL: UseVariable was not set to false! Still: {action.UseVariable}", LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Event handler for the "Use Variable" RadioButton in ClipboardAction
        /// </summary>
        private void RadioButton_UseVariable_Checked(object sender, RoutedEventArgs e)
        {
            // Use variable selected
            if (DataContext is MainViewModel viewModel &&
                viewModel.SelectedAction is ClipboardAction action)
            {
                LogManager.Log($"### XAML: RadioButton UseVariable set for ClipboardAction {action.Id.ToString().Substring(0, 8)}", LogLevel.Debug);

                // Explicitly set UseVariable to true
                bool oldValue = action.UseVariable;
                action.UseVariable = true;

                LogManager.Log($"### XAML: UseVariable changed from {oldValue} to {action.UseVariable} by RadioButton", LogLevel.Debug);

                // Check if the change was applied
                if (action.UseVariable != true)
                {
                    LogManager.Log($"!!! CRITICAL: UseVariable was not set to true! Still: {action.UseVariable}", LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Event handler for the UseElseBranch checkbox
        /// </summary>
        private void UseElseBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel &&
                viewModel.SelectedAction is IfAction ifAction)
            {
                bool isChecked = (sender as CheckBox)?.IsChecked ?? false;
                LogManager.Log($"UseElseBranch checkbox changed to {isChecked}", LogLevel.Debug);

                // Update the button states to match the new checkbox state
                UpdateElseActionButtonStates();
            }
        }
    }
}