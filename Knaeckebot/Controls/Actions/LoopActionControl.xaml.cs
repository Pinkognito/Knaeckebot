using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for LoopActionControl.xaml
/// </summary>
public partial class LoopActionControl : UserControl, IActionControl
{
private ActionBase _action;
private LoopAction _loopAction => _action as LoopAction;
private ActionBase _selectedBranchAction;
private string _branchContext = "LOOP";
private MainViewModel ViewModel => DataContext as MainViewModel;
/// <summary>
/// Event that is raised when a branch action is selected for editing
/// </summary>
public event System.EventHandler<BranchActionEventArgs> BranchActionSelected;
public LoopActionControl()
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
/// Update the UI controls based on the loop action properties
/// </summary>
public void UpdateControlFromAction()
{
    if (_loopAction == null) return;
    // Set basic properties
    txtMaxIterations.Text = _loopAction.MaxIterations.ToString();
    chkUseCondition.IsChecked = _loopAction.UseCondition;
    // Set condition properties
    cmbLeftSourceType.SelectedItem = _loopAction.LeftSourceType;
    cmbLeftVariableName.Text = _loopAction.LeftVariableName;
    txtLeftCustomText.Text = _loopAction.LeftCustomText;
    cmbOperator.SelectedItem = _loopAction.Operator;
    cmbRightSourceType.SelectedItem = _loopAction.RightSourceType;
    cmbRightVariableName.Text = _loopAction.RightVariableName;
    txtRightCustomText.Text = _loopAction.RightCustomText;
    // Update variable dropdowns
    if (ViewModel != null)
    {
        cmbLeftVariableName.ItemsSource = ViewModel.VariableNames;
        cmbRightVariableName.ItemsSource = ViewModel.VariableNames;
    }
    // Bind loop actions to ListView
    lvLoopActions.ItemsSource = _loopAction.LoopActions;
    UpdateActionCount();
    // Update UI based on condition sources
    UpdateUIForLeftSourceType(_loopAction.LeftSourceType);
    UpdateUIForRightSourceType(_loopAction.RightSourceType);
    // Update button states
    UpdateButtonStates();
}
/// <summary>
/// Update the loop action with values from the UI controls
/// </summary>
public void UpdateActionFromControl()
{
    if (_loopAction == null) return;
    // Set basic properties
    if (int.TryParse(txtMaxIterations.Text, out int maxIterations))
    {
        _loopAction.MaxIterations = maxIterations;
    }
    _loopAction.UseCondition = chkUseCondition.IsChecked == true;
    // Set condition properties
    if (cmbLeftSourceType.SelectedItem is ConditionSourceType leftSourceType)
    {
        _loopAction.LeftSourceType = leftSourceType;
    }
    _loopAction.LeftVariableName = cmbLeftVariableName.Text;
    _loopAction.LeftCustomText = txtLeftCustomText.Text;
    if (cmbOperator.SelectedItem is ComparisonOperator op)
    {
        _loopAction.Operator = op;
    }
    if (cmbRightSourceType.SelectedItem is ConditionSourceType rightSourceType)
    {
        _loopAction.RightSourceType = rightSourceType;
    }
    _loopAction.RightVariableName = cmbRightVariableName.Text;
    _loopAction.RightCustomText = txtRightCustomText.Text;
}
/// <summary>
/// Event handler for the UseCondition checkbox
/// </summary>
private void ChkUseCondition_CheckedChanged(object sender, RoutedEventArgs e)
{
    LogManager.Log($"Loop UseCondition changed to: {chkUseCondition.IsChecked}", LogLevel.Debug);
    // Visibility is bound to the checkbox directly in XAML
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
/// Update the action count display
/// </summary>
private void UpdateActionCount()
{
    if (_loopAction != null)
    {
        txtActionCount.Text = $"Number of actions: {_loopAction.LoopActions.Count}";
    }
    else
    {
        txtActionCount.Text = "Number of actions: 0";
    }
}
/// <summary>
/// Event handler for selection changes in the loop actions ListView
/// </summary>
private void LvLoopActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    UpdateButtonStates();
}
/// <summary>
/// Update the enabled state of buttons based on selection state
/// </summary>
private void UpdateButtonStates()
{
    bool hasSelection = lvLoopActions.SelectedItems.Count > 0;
    btnMoveUp.IsEnabled = hasSelection;
    btnMoveDown.IsEnabled = hasSelection;
    btnDelete.IsEnabled = hasSelection;
    btnCopy.IsEnabled = hasSelection;
    // Paste is enabled if there are actions to paste
    btnPaste.IsEnabled = ViewModel?.CopiedActions.Count > 0;
}
/// <summary>
/// Event handler for the Move Up button
/// </summary>
private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
{
    if (_loopAction == null) return;
    // Get all selected items
    var selectedItems = lvLoopActions.SelectedItems.Cast<ActionBase>().ToList();
    if (selectedItems.Count > 0)
    {
        // Sort by index so we move from top to bottom
        var itemsWithIndices = selectedItems
            .Select(item => new { Item = item, Index = _loopAction.LoopActions.IndexOf(item) })
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
                int index = _loopAction.LoopActions.IndexOf(itemWithIndex.Item);
                if (index > 0)
                {
                    _loopAction.LoopActions.Move(index, index - 1);
                }
            }
            // Reselect items
            lvLoopActions.SelectedItems.Clear();
            foreach (var index in selectedIndices)
            {
                if (index > 0 && index - 1 < _loopAction.LoopActions.Count)
                {
                    lvLoopActions.SelectedItems.Add(_loopAction.LoopActions[index - 1]);
                }
            }
            lvLoopActions.Focus();
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = selectedItems.Count == 1
                    ? "Action moved up in loop"
                    : $"{selectedItems.Count} actions moved up in loop";
            }
        }
    }
}
/// <summary>
/// Event handler for the Move Down button
/// </summary>
private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
{
    if (_loopAction == null) return;
    // Get all selected items
    var selectedItems = lvLoopActions.SelectedItems.Cast<ActionBase>().ToList();
    if (selectedItems.Count > 0)
    {
        // Sort by index in descending order so we move from bottom to top
        var itemsWithIndices = selectedItems
            .Select(item => new { Item = item, Index = _loopAction.LoopActions.IndexOf(item) })
            .OrderByDescending(x => x.Index)
            .ToList();
        // Check if we can move down (if last item is not at the end)
        if (itemsWithIndices.First().Index < _loopAction.LoopActions.Count - 1)
        {
            // Remember selected indices
            var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();
            // Move each item down
            foreach (var itemWithIndex in itemsWithIndices)
            {
                int index = _loopAction.LoopActions.IndexOf(itemWithIndex.Item);
                if (index < _loopAction.LoopActions.Count - 1)
                {
                    _loopAction.LoopActions.Move(index, index + 1);
                }
            }
            // Reselect items
            lvLoopActions.SelectedItems.Clear();
            foreach (var index in selectedIndices)
            {
                if (index + 1 < _loopAction.LoopActions.Count)
                {
                    lvLoopActions.SelectedItems.Add(_loopAction.LoopActions[index + 1]);
                }
            }
            lvLoopActions.Focus();
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = selectedItems.Count == 1
                    ? "Action moved down in loop"
                    : $"{selectedItems.Count} actions moved down in loop";
            }
        }
    }
}
/// <summary>
/// Event handler for the Delete button
/// </summary>
private void BtnDelete_Click(object sender, RoutedEventArgs e)
{
    if (_loopAction == null) return;
    // Get all selected items
    var selectedItems = lvLoopActions.SelectedItems.Cast<ActionBase>().ToList();
    if (selectedItems.Count > 0)
    {
        // Confirmation dialog before deleting
        string message = selectedItems.Count == 1
            ? $"Are you sure you want to remove the action '{selectedItems[0].Name}' from the loop?"
            : $"Are you sure you want to remove {selectedItems.Count} actions from the loop?";
        var result = MessageBox.Show(
            message,
            "Delete Actions",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            // Find minimum index for selection after deletion
            int firstIndex = selectedItems.Min(item => _loopAction.LoopActions.IndexOf(item));
            // Remove all selected actions
            foreach (var action in selectedItems)
            {
                _loopAction.LoopActions.Remove(action);
                LogManager.Log($"Loop action '{action.Name}' deleted", LogLevel.Debug);
            }
            // Update action count
            UpdateActionCount();
            // Status message
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = selectedItems.Count == 1
                    ? "Action removed from loop"
                    : $"{selectedItems.Count} actions removed from loop";
            }
            // Select next element if available
            if (_loopAction.LoopActions.Count > 0)
            {
                lvLoopActions.SelectedIndex = System.Math.Min(firstIndex, _loopAction.LoopActions.Count - 1);
                lvLoopActions.Focus();
            }
        }
    }
}
/// <summary>
/// Event handler for the Copy button
/// </summary>
private void BtnCopy_Click(object sender, RoutedEventArgs e)
{
    if (_loopAction == null || ViewModel == null) return;
    // Get all selected items
    var selectedItems = lvLoopActions.SelectedItems.Cast<ActionBase>().ToList();
    if (selectedItems.Count > 0)
    {
        // Clear the copied actions and add the selected ones
        ViewModel.CopiedActions.Clear();
        foreach (var action in selectedItems)
        {
            ViewModel.CopiedActions.Add(action);
            LogManager.Log($"Loop action '{action.Name}' added to copy buffer", LogLevel.Debug);
        }
        ViewModel.StatusMessage = selectedItems.Count == 1
            ? "Action copied from loop"
            : $"{selectedItems.Count} actions copied from loop";
    }
}
/// <summary>
/// Event handler for the Paste button
/// </summary>
private void BtnPaste_Click(object sender, RoutedEventArgs e)
{
    if (_loopAction == null || ViewModel == null) return;
    if (ViewModel.CopiedActions.Count > 0)
    {
        // Find the selected item(s) in the loop
        int insertIndex;
        if (lvLoopActions.SelectedItems.Count > 0)
        {
            // Find the last selected item
            var lastSelectedItem = lvLoopActions.SelectedItems[lvLoopActions.SelectedItems.Count - 1] as ActionBase;
            insertIndex = _loopAction.LoopActions.IndexOf(lastSelectedItem) + 1;
        }
        else
        {
            // If nothing is selected, insert at the end
            insertIndex = _loopAction.LoopActions.Count;
        }
        LogManager.Log($"Inserting {ViewModel.CopiedActions.Count} actions at index {insertIndex}", LogLevel.Debug);
        // Create a list to hold the selected insertIndices for later selection
        var insertedIndices = new System.Collections.Generic.List<int>();
        // Clone each action and add to collection
        foreach (var action in ViewModel.CopiedActions)
        {
            // Clone the action
            var clonedAction = action.Clone();
            // Insert at the right position
            if (insertIndex < _loopAction.LoopActions.Count)
            {
                _loopAction.LoopActions.Insert(insertIndex, clonedAction);
            }
            else
            {
                _loopAction.LoopActions.Add(clonedAction);
            }
            insertedIndices.Add(insertIndex);
            insertIndex++;
            LogManager.Log($"Inserted clone of action '{action.Name}'", LogLevel.Debug);
        }
        // Update action count
        UpdateActionCount();
        // Select the newly inserted actions
        lvLoopActions.SelectedItems.Clear();
        foreach (int index in insertedIndices)
        {
            if (index < _loopAction.LoopActions.Count)
            {
                lvLoopActions.SelectedItems.Add(_loopAction.LoopActions[index]);
            }
        }
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = ViewModel.CopiedActions.Count == 1
                ? "Action inserted into loop"
                : $"{ViewModel.CopiedActions.Count} actions inserted into loop";
        }
    }
    else
    {
        LogManager.Log("No actions to paste", LogLevel.Warning);
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "No actions to paste";
        }
    }
}
/// <summary>
/// Event handler for double-clicking on a loop action
/// </summary>
private void LvLoopActions_DoubleClick(object sender, MouseButtonEventArgs e)
{
    if (lvLoopActions.SelectedItem is ActionBase action)
    {
        LogManager.Log($"Double-clicked on loop action: {action.Name}", LogLevel.Debug);
        // Store the selected action
        _selectedBranchAction = action;
        // Raise an event for the parent control to handle
        BranchActionSelected?.Invoke(this, new BranchActionEventArgs(action, _branchContext));
        // Update status message
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = $"Editing loop action: {action.Name}";
        }
    }
}
}
}
