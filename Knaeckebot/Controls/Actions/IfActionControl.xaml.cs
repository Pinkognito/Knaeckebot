using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using ListView = System.Windows.Controls.ListView;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for IfActionControl.xaml
/// </summary>
public partial class IfActionControl : UserControl, IActionControl
{
private ActionBase _action;
private IfAction _ifAction => _action as IfAction;
private ActionBase _selectedBranchAction;
private string _branchContext = "";
private MainViewModel ViewModel => DataContext as MainViewModel;
    public IfActionControl()
    {
        InitializeComponent();
        // Initialize combo boxes
        cmbLeftSourceType.ItemsSource = System.Enum.GetValues(typeof(ConditionSourceType));
        cmbRightSourceType.ItemsSource = System.Enum.GetValues(typeof(ConditionSourceType));
        cmbOperator.ItemsSource = System.Enum.GetValues(typeof(ComparisonOperator));
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
    /// Update the UI controls based on the if action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_ifAction == null) return;
        // Set condition properties
        cmbLeftSourceType.SelectedItem = _ifAction.LeftSourceType;
        cmbLeftVariableName.Text = _ifAction.LeftVariableName;
        txtLeftCustomText.Text = _ifAction.LeftCustomText;
        cmbOperator.SelectedItem = _ifAction.Operator;
        cmbRightSourceType.SelectedItem = _ifAction.RightSourceType;
        cmbRightVariableName.Text = _ifAction.RightVariableName;
        txtRightCustomText.Text = _ifAction.RightCustomText;
        // Update variable dropdowns
        if (ViewModel != null)
        {
            cmbLeftVariableName.ItemsSource = ViewModel.VariableNames;
            cmbRightVariableName.ItemsSource = ViewModel.VariableNames;
        }
        // Set else branch flag
        chkUseElseBranch.IsChecked = _ifAction.UseElseBranch;
        // Bind actions to ListViews
        lvThenActions.ItemsSource = _ifAction.ThenActions;
        lvElseActions.ItemsSource = _ifAction.ElseActions;
        // Update action counts
        UpdateThenActionCount();
        UpdateElseActionCount();
        // Update UI based on condition sources
        UpdateUIForLeftSourceType(_ifAction.LeftSourceType);
        UpdateUIForRightSourceType(_ifAction.RightSourceType);
        // Update button states
        UpdateThenButtonStates();
        UpdateElseButtonStates();
    }
    /// <summary>
    /// Update the if action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_ifAction == null) return;
        // Set condition properties
        if (cmbLeftSourceType.SelectedItem is ConditionSourceType leftSourceType)
        {
            _ifAction.LeftSourceType = leftSourceType;
        }
        _ifAction.LeftVariableName = cmbLeftVariableName.Text;
        _ifAction.LeftCustomText = txtLeftCustomText.Text;
        if (cmbOperator.SelectedItem is ComparisonOperator op)
        {
            _ifAction.Operator = op;
        }
        if (cmbRightSourceType.SelectedItem is ConditionSourceType rightSourceType)
        {
            _ifAction.RightSourceType = rightSourceType;
        }
        _ifAction.RightVariableName = cmbRightVariableName.Text;
        _ifAction.RightCustomText = txtRightCustomText.Text;
        // Set else branch flag
        _ifAction.UseElseBranch = chkUseElseBranch.IsChecked == true;
    }
    /// <summary>
    /// Event handler for condition source type selection changes
    /// </summary>
    private void CmbSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender == cmbLeftSourceType && cmbLeftSourceType.SelectedItem is ConditionSourceType leftType)
        {
            UpdateUIForLeftSourceType(leftType);
        }
        else if (sender == cmbRightSourceType && cmbRightSourceType.SelectedItem is ConditionSourceType rightType)
        {
            UpdateUIForRightSourceType(rightType);
        }
    }
    /// <summary>
    /// Update which left-side fields are visible based on the source type
    /// </summary>
    private void UpdateUIForLeftSourceType(ConditionSourceType sourceType)
    {
        // Hide all input fields first
        cmbLeftVariableName.Visibility = Visibility.Collapsed;
        txtLeftCustomText.Visibility = Visibility.Collapsed;
        lblLeftClipboard.Visibility = Visibility.Collapsed;
        // Show the appropriate input field
        switch (sourceType)
        {
            case ConditionSourceType.Variable:
                cmbLeftVariableName.Visibility = Visibility.Visible;
                break;
            case ConditionSourceType.Text:
                txtLeftCustomText.Visibility = Visibility.Visible;
                break;
            case ConditionSourceType.Clipboard:
                lblLeftClipboard.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Update which right-side fields are visible based on the source type
    /// </summary>
    private void UpdateUIForRightSourceType(ConditionSourceType sourceType)
    {
        // Hide all input fields first
        cmbRightVariableName.Visibility = Visibility.Collapsed;
        txtRightCustomText.Visibility = Visibility.Collapsed;
        lblRightClipboard.Visibility = Visibility.Collapsed;
        // Show the appropriate input field
        switch (sourceType)
        {
            case ConditionSourceType.Variable:
                cmbRightVariableName.Visibility = Visibility.Visible;
                break;
            case ConditionSourceType.Text:
                txtRightCustomText.Visibility = Visibility.Visible;
                break;
            case ConditionSourceType.Clipboard:
                lblRightClipboard.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Event handler for the UseElseBranch checkbox
    /// </summary>
    private void ChkUseElseBranch_CheckedChanged(object sender, RoutedEventArgs e)
    {
        LogManager.Log($"If UseElseBranch changed to: {chkUseElseBranch.IsChecked}", LogLevel.Debug);
        UpdateElseButtonStates();
    }
    /// <summary>
    /// Update the THEN action count display
    /// </summary>
    private void UpdateThenActionCount()
    {
        if (_ifAction != null)
        {
            txtThenActionCount.Text = $"Number of actions: {_ifAction.ThenActions.Count}";
        }
        else
        {
            txtThenActionCount.Text = "Number of actions: 0";
        }
    }
    /// <summary>
    /// Update the ELSE action count display
    /// </summary>
    private void UpdateElseActionCount()
    {
        if (_ifAction != null)
        {
            txtElseActionCount.Text = $"Number of actions: {_ifAction.ElseActions.Count}";
        }
        else
        {
            txtElseActionCount.Text = "Number of actions: 0";
        }
    }
    /// <summary>
    /// Event handler for selection changes in the THEN actions ListView
    /// </summary>
    private void LvThenActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateThenButtonStates();
    }
    /// <summary>
    /// Event handler for selection changes in the ELSE actions ListView
    /// </summary>
    private void LvElseActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateElseButtonStates();
    }
    /// <summary>
    /// Update the enabled state of THEN buttons based on selection state
    /// </summary>
    private void UpdateThenButtonStates()
    {
        bool hasSelection = lvThenActions.SelectedItems.Count > 0;
        btnMoveThenUp.IsEnabled = hasSelection;
        btnMoveThenDown.IsEnabled = hasSelection;
        btnDeleteThen.IsEnabled = hasSelection;
        btnCopyThen.IsEnabled = hasSelection;
        // Paste is enabled if there are actions to paste
        btnPasteThen.IsEnabled = ViewModel?.CopiedActions.Count > 0;
    }
    /// <summary>
    /// Update the enabled state of ELSE buttons based on selection state and ELSE branch enabled
    /// </summary>
    private void UpdateElseButtonStates()
    {
        bool elseEnabled = chkUseElseBranch.IsChecked == true;
        bool hasSelection = lvElseActions.SelectedItems.Count > 0;
        btnMoveElseUp.IsEnabled = elseEnabled && hasSelection;
        btnMoveElseDown.IsEnabled = elseEnabled && hasSelection;
        btnDeleteElse.IsEnabled = elseEnabled && hasSelection;
        btnCopyElse.IsEnabled = elseEnabled && hasSelection;
        // Paste is enabled if ELSE branch is enabled and there are actions to paste
        btnPasteElse.IsEnabled = elseEnabled && (ViewModel?.CopiedActions.Count > 0);
    }
    /// <summary>
    /// Event handler for the Move Up button for THEN actions
    /// </summary>
    private void BtnMoveThenUp_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null) return;
        // Get all selected items
        var selectedItems = lvThenActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index so we move from top to bottom
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = _ifAction.ThenActions.IndexOf(item) })
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
                    int index = _ifAction.ThenActions.IndexOf(itemWithIndex.Item);
                    if (index > 0)
                    {
                        _ifAction.ThenActions.Move(index, index - 1);
                    }
                }
                // Reselect items
                lvThenActions.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index > 0 && index - 1 < _ifAction.ThenActions.Count)
                    {
                        lvThenActions.SelectedItems.Add(_ifAction.ThenActions[index - 1]);
                    }
                }
                lvThenActions.Focus();
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action moved up in THEN branch"
                    : $"{selectedItems.Count} actions moved up in THEN branch");
            }
        }
    }
    /// <summary>
    /// Event handler for the Move Down button for THEN actions
    /// </summary>
    private void BtnMoveThenDown_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null) return;
        // Get all selected items
        var selectedItems = lvThenActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index in descending order so we move from bottom to top
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = _ifAction.ThenActions.IndexOf(item) })
                .OrderByDescending(x => x.Index)
                .ToList();
            // Check if we can move down (if last item is not at the end)
            if (itemsWithIndices.First().Index < _ifAction.ThenActions.Count - 1)
            {
                // Remember selected indices
                var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();
                // Move each item down
                foreach (var itemWithIndex in itemsWithIndices)
                {
                    int index = _ifAction.ThenActions.IndexOf(itemWithIndex.Item);
                    if (index < _ifAction.ThenActions.Count - 1)
                    {
                        _ifAction.ThenActions.Move(index, index + 1);
                    }
                }
                // Reselect items
                lvThenActions.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index + 1 < _ifAction.ThenActions.Count)
                    {
                        lvThenActions.SelectedItems.Add(_ifAction.ThenActions[index + 1]);
                    }
                }
                lvThenActions.Focus();
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action moved down in THEN branch"
                    : $"{selectedItems.Count} actions moved down in THEN branch");
            }
        }
    }
    /// <summary>
    /// Event handler for the Delete button for THEN actions
    /// </summary>
    private void BtnDeleteThen_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null) return;
        // Get all selected items
        var selectedItems = lvThenActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Confirmation dialog before deleting
            string message = selectedItems.Count == 1
                ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the THEN branch?"
                : $"Are you sure you want to remove {selectedItems.Count} actions from the THEN branch?";
            var result = ActionControlHelper.ShowConfirmationDialog(message, "Delete Actions");
            if (result == MessageBoxResult.Yes)
            {
                // Find minimum index for selection after deletion
                int firstIndex = selectedItems.Min(item => _ifAction.ThenActions.IndexOf(item));
                // Remove all selected actions
                foreach (var action in selectedItems)
                {
                    _ifAction.ThenActions.Remove(action);
                    LogManager.Log($"THEN action '{action.Name}' deleted", LogLevel.Debug);
                }
                // Update action count
                UpdateThenActionCount();
                // Status message
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action removed from THEN branch"
                    : $"{selectedItems.Count} actions removed from THEN branch");
                // Select next element if available
                if (_ifAction.ThenActions.Count > 0)
                {
                    lvThenActions.SelectedIndex = System.Math.Min(firstIndex, _ifAction.ThenActions.Count - 1);
                    lvThenActions.Focus();
                }
            }
        }
    }
    /// <summary>
    /// Event handler for the Copy button for THEN actions
    /// </summary>
    private void BtnCopyThen_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || ViewModel == null) return;
        // Get all selected items
        var selectedItems = lvThenActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Clear the copied actions and add the selected ones
            ViewModel.CopiedActions.Clear();
            foreach (var action in selectedItems)
            {
                ViewModel.CopiedActions.Add(action);
                LogManager.Log($"THEN action '{action.Name}' added to copy buffer", LogLevel.Debug);
            }
            ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                ? "Action copied from THEN branch"
                : $"{selectedItems.Count} actions copied from THEN branch");
        }
    }
    /// <summary>
    /// Event handler for the Paste button for THEN actions
    /// </summary>
    private void BtnPasteThen_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || ViewModel == null) return;
        if (ViewModel.CopiedActions.Count > 0)
        {
            LogManager.Log("*** PasteThen START ***", LogLevel.Debug);
            // Get all current actions in a List
            System.Collections.Generic.List<ActionBase> allActions = new System.Collections.Generic.List<ActionBase>();
            LogManager.Log($"Original ThenActions count: {_ifAction.ThenActions.Count}", LogLevel.Debug);
            // First add all existing actions to our list
            foreach (var action in _ifAction.ThenActions)
            {
                allActions.Add(action);
                LogManager.Log($"Added existing action to list: {action.Name}", LogLevel.Debug);
            }
            int insertAt;
            // Determine insert position
            if (lvThenActions.SelectedItems.Count > 0)
            {
                var selectedAction = lvThenActions.SelectedItems[0] as ActionBase;
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
            System.Collections.Generic.List<ActionBase> actionsToInsert = new System.Collections.Generic.List<ActionBase>();
            foreach (var action in ViewModel.CopiedActions)
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
                _ifAction.ThenActions.Clear();
                LogManager.Log("Adding actions back to ThenActions", LogLevel.Debug);
                foreach (var action in allActions)
                {
                    _ifAction.ThenActions.Add(action);
                    LogManager.Log($"Added back to ThenActions: {action.Name}", LogLevel.Debug);
                }
                // Update action count
                UpdateThenActionCount();
                // Success message
                LogManager.Log($"{actionsToInsert.Count} action(s) inserted at position {insertAt}", LogLevel.Info);
                ActionControlHelper.UpdateStatusMessage(ViewModel, ViewModel.CopiedActions.Count == 1
                    ? "Action inserted into THEN branch"
                    : $"{ViewModel.CopiedActions.Count} actions inserted into THEN branch");
                // Select the newly inserted actions
                lvThenActions.SelectedItems.Clear();
                for (int i = 0; i < actionsToInsert.Count; i++)
                {
                    int index = insertAt + i;
                    if (index < _ifAction.ThenActions.Count)
                    {
                        lvThenActions.SelectedItems.Add(_ifAction.ThenActions[index]);
                        LogManager.Log($"Selected newly inserted action at index {index}", LogLevel.Debug);
                    }
                }
                // Set focus back to the list
                lvThenActions.Focus();
            }
            catch (System.Exception ex)
            {
                LogManager.Log($"ERROR rebuilding ThenActions: {ex.Message}", LogLevel.Error);
                LogManager.Log(ex.StackTrace, LogLevel.Error);
            }
            LogManager.Log("*** PasteThen END ***", LogLevel.Debug);
        }
        else
        {
            LogManager.Log("No actions to paste", LogLevel.Warning);
            ActionControlHelper.UpdateStatusMessage(ViewModel, "No actions to paste");
        }
    }
    /// <summary>
    /// Event handler for the Move Up button for ELSE actions
    /// </summary>
    private void BtnMoveElseUp_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || !_ifAction.UseElseBranch) return;
        // Get all selected items
        var selectedItems = lvElseActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index so we move from top to bottom
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = _ifAction.ElseActions.IndexOf(item) })
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
                    int index = _ifAction.ElseActions.IndexOf(itemWithIndex.Item);
                    if (index > 0)
                    {
                        _ifAction.ElseActions.Move(index, index - 1);
                    }
                }
                // Reselect items
                lvElseActions.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index > 0 && index - 1 < _ifAction.ElseActions.Count)
                    {
                        lvElseActions.SelectedItems.Add(_ifAction.ElseActions[index - 1]);
                    }
                }
                lvElseActions.Focus();
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action moved up in ELSE branch"
                    : $"{selectedItems.Count} actions moved up in ELSE branch");
            }
        }
    }
    /// <summary>
    /// Event handler for the Move Down button for ELSE actions
    /// </summary>
    private void BtnMoveElseDown_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || !_ifAction.UseElseBranch) return;
        // Get all selected items
        var selectedItems = lvElseActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index in descending order so we move from bottom to top
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = _ifAction.ElseActions.IndexOf(item) })
                .OrderByDescending(x => x.Index)
                .ToList();
            // Check if we can move down (if last item is not at the end)
            if (itemsWithIndices.First().Index < _ifAction.ElseActions.Count - 1)
            {
                // Remember selected indices
                var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();
                // Move each item down
                foreach (var itemWithIndex in itemsWithIndices)
                {
                    int index = _ifAction.ElseActions.IndexOf(itemWithIndex.Item);
                    if (index < _ifAction.ElseActions.Count - 1)
                    {
                        _ifAction.ElseActions.Move(index, index + 1);
                    }
                }
                // Reselect items
                lvElseActions.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index + 1 < _ifAction.ElseActions.Count)
                    {
                        lvElseActions.SelectedItems.Add(_ifAction.ElseActions[index + 1]);
                    }
                }
                lvElseActions.Focus();
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action moved down in ELSE branch"
                    : $"{selectedItems.Count} actions moved down in ELSE branch");
            }
        }
    }
    /// <summary>
    /// Event handler for the Delete button for ELSE actions
    /// </summary>
    private void BtnDeleteElse_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || !_ifAction.UseElseBranch) return;
        // Get all selected items
        var selectedItems = lvElseActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Confirmation dialog before deleting
            string message = selectedItems.Count == 1
                ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the ELSE branch?"
                : $"Are you sure you want to remove {selectedItems.Count} actions from the ELSE branch?";
            var result = ActionControlHelper.ShowConfirmationDialog(message, "Delete Actions");
            if (result == MessageBoxResult.Yes)
            {
                // Find minimum index for selection after deletion
                int firstIndex = selectedItems.Min(item => _ifAction.ElseActions.IndexOf(item));
                // Remove all selected actions
                foreach (var action in selectedItems)
                {
                    _ifAction.ElseActions.Remove(action);
                    LogManager.Log($"ELSE action '{action.Name}' deleted", LogLevel.Debug);
                }
                // Update action count
                UpdateElseActionCount();
                // Status message
                ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                    ? "Action removed from ELSE branch"
                    : $"{selectedItems.Count} actions removed from ELSE branch");
                // Select next element if available
                if (_ifAction.ElseActions.Count > 0)
                {
                    lvElseActions.SelectedIndex = System.Math.Min(firstIndex, _ifAction.ElseActions.Count - 1);
                    lvElseActions.Focus();
                }
            }
        }
    }
    /// <summary>
    /// Event handler for the Copy button for ELSE actions
    /// </summary>
    private void BtnCopyElse_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || !_ifAction.UseElseBranch || ViewModel == null) return;
        // Get all selected items
        var selectedItems = lvElseActions.SelectedItems.Cast<ActionBase>().ToList();
        if (selectedItems.Count > 0)
        {
            // Clear the copied actions and add the selected ones
            ViewModel.CopiedActions.Clear();
            foreach (var action in selectedItems)
            {
                ViewModel.CopiedActions.Add(action);
                LogManager.Log($"ELSE action '{action.Name}' added to copy buffer", LogLevel.Debug);
            }
            ActionControlHelper.UpdateStatusMessage(ViewModel, selectedItems.Count == 1
                ? "Action copied from ELSE branch"
                : $"{selectedItems.Count} actions copied from ELSE branch");
        }
    }
    /// <summary>
    /// Event handler for the Paste button for ELSE actions
    /// </summary>
    private void BtnPasteElse_Click(object sender, RoutedEventArgs e)
    {
        if (_ifAction == null || !_ifAction.UseElseBranch || ViewModel == null) return;
        if (ViewModel.CopiedActions.Count > 0)
        {
            LogManager.Log("*** PasteElse START ***", LogLevel.Debug);
            // Get all current actions in a List
            System.Collections.Generic.List<ActionBase> allActions = new System.Collections.Generic.List<ActionBase>();
            LogManager.Log($"Original ElseActions count: {_ifAction.ElseActions.Count}", LogLevel.Debug);
            // First add all existing actions to our list
            foreach (var action in _ifAction.ElseActions)
            {
                allActions.Add(action);
                LogManager.Log($"Added existing action to list: {action.Name}", LogLevel.Debug);
            }
            int insertAt;
            // Determine insert position
            if (lvElseActions.SelectedItems.Count > 0)
            {
                var selectedAction = lvElseActions.SelectedItems[0] as ActionBase;
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
            System.Collections.Generic.List<ActionBase> actionsToInsert = new System.Collections.Generic.List<ActionBase>();
            foreach (var action in ViewModel.CopiedActions)
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
                _ifAction.ElseActions.Clear();
                LogManager.Log("Adding actions back to ElseActions", LogLevel.Debug);
                foreach (var action in allActions)
                {
                    _ifAction.ElseActions.Add(action);
                    LogManager.Log($"Added back to ElseActions: {action.Name}", LogLevel.Debug);
                }
                // Update action count
                UpdateElseActionCount();
                // Success message
                LogManager.Log($"{actionsToInsert.Count} action(s) inserted at position {insertAt}", LogLevel.Info);
                ActionControlHelper.UpdateStatusMessage(ViewModel, ViewModel.CopiedActions.Count == 1
                    ? "Action inserted into ELSE branch"
                    : $"{ViewModel.CopiedActions.Count} actions inserted into ELSE branch");
                // Select the newly inserted actions
                lvElseActions.SelectedItems.Clear();
                for (int i = 0; i < actionsToInsert.Count; i++)
                {
                    int index = insertAt + i;
                    if (index < _ifAction.ElseActions.Count)
                    {
                        lvElseActions.SelectedItems.Add(_ifAction.ElseActions[index]);
                        LogManager.Log($"Selected newly inserted action at index {index}", LogLevel.Debug);
                    }
                }
                // Set focus back to the list
                lvElseActions.Focus();
                // Ensure button states are updated
                UpdateElseButtonStates();
            }
            catch (System.Exception ex)
            {
                LogManager.Log($"ERROR rebuilding ElseActions: {ex.Message}", LogLevel.Error);
                LogManager.Log(ex.StackTrace, LogLevel.Error);
            }
            LogManager.Log("*** PasteElse END ***", LogLevel.Debug);
        }
        else
        {
            LogManager.Log("No actions to paste", LogLevel.Warning);
            ActionControlHelper.UpdateStatusMessage(ViewModel, "No actions to paste");
        }
    }
    /// <summary>
    /// Event handler for double-clicking on an action
    /// </summary>
    private void LvActions_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is ActionBase action)
        {
            LogManager.Log($"Double-clicked on branch action: {action.Name}", LogLevel.Debug);
            // Determine which branch this action belongs to
            if (listView.Name == "lvThenActions")
            {
                _branchContext = "THEN";
            }
            else if (listView.Name == "lvElseActions")
            {
                _branchContext = "ELSE";
            }
            // Notify the parent control about the double-clicked action
            _selectedBranchAction = action;
            // This will be handled by the main ActionDetailsControl through an event
            ActionControlHelper.UpdateStatusMessage(ViewModel, $"Editing {_branchContext} branch action: {action.Name}");
        }
    }
}
}
