using System;
using System.Windows;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for ClipboardActionControl.xaml
/// </summary>
public partial class ClipboardActionControl : UserControl, IActionControl
{
private ActionBase _action;
private ClipboardAction _clipboardAction => _action as ClipboardAction;
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
public ClipboardActionControl()
{
    InitializeComponent();
}
/// <summary>
/// Initialize the control with an action
/// </summary>
public void Initialize(ActionBase action)
{
    _action = action;
    LogManager.Log($"Initializing ClipboardActionControl with action {action.Id}", LogLevel.Debug);
    if (_clipboardAction != null)
    {
        LogManager.Log($"ClipboardAction details: UseVariable={_clipboardAction.UseVariable}, " +
                      $"VariableName='{_clipboardAction.VariableName}', " +
                      $"Text='{_clipboardAction.Text?.Substring(0, Math.Min(20, _clipboardAction.Text?.Length ?? 0))}...'", 
                      LogLevel.Debug);
    }
    UpdateControlFromAction();
}
/// <summary>
/// Update the UI controls based on the clipboard action properties
/// </summary>
public void UpdateControlFromAction()
{
    if (_clipboardAction == null) return;
    LogManager.Log($"Updating UI from ClipboardAction: UseVariable={_clipboardAction.UseVariable}", LogLevel.Debug);
    // Set source radio button
    rbDirectText.IsChecked = !_clipboardAction.UseVariable;
    rbUseVariable.IsChecked = _clipboardAction.UseVariable;
    // Set text fields
    txtText.Text = _clipboardAction.Text;
    // Set variable name and populate dropdown
    if (ViewModel != null)
    {
        cmbVariable.ItemsSource = ViewModel.VariableNames;
        cmbVariable.Text = _clipboardAction.VariableName;
    }
    // Set append flag
    chkAppend.IsChecked = _clipboardAction.AppendToClipboard;
    // Set retry settings
    txtRetryCount.Text = _clipboardAction.RetryCount.ToString();
    txtRetryWaitTime.Text = _clipboardAction.RetryWaitTime.ToString();
    // Update UI based on source type
    UpdateUIForSourceType(_clipboardAction.UseVariable);
    LogManager.Log($"After UI update: rbDirectText.IsChecked={rbDirectText.IsChecked}, " +
                  $"rbUseVariable.IsChecked={rbUseVariable.IsChecked}, " +
                  $"txtText.Text='{txtText.Text?.Substring(0, Math.Min(20, txtText.Text?.Length ?? 0))}...'", 
                  LogLevel.Debug);
}
/// <summary>
/// Update the clipboard action with values from the UI controls
/// </summary>
public void UpdateActionFromControl()
{
    if (_clipboardAction == null) return;
    LogManager.Log($"Updating ClipboardAction from UI: rbDirectText.IsChecked={rbDirectText.IsChecked}, " +
                  $"rbUseVariable.IsChecked={rbUseVariable.IsChecked}", 
                  LogLevel.Debug);
    // Set source based on radio button - this order is important!
    bool useVariable = rbUseVariable.IsChecked == true;
    LogManager.Log($"Setting UseVariable={useVariable}", LogLevel.Debug);
    _clipboardAction.UseVariable = useVariable;
    // Set text or variable name based on source
    if (useVariable)
    {
        _clipboardAction.VariableName = cmbVariable.Text;
        LogManager.Log($"Set VariableName='{_clipboardAction.VariableName}'", LogLevel.Debug);
    }
    else
    {
        _clipboardAction.Text = txtText.Text;
        LogManager.Log($"Set Text='{_clipboardAction.Text?.Substring(0, Math.Min(20, _clipboardAction.Text?.Length ?? 0))}...'", LogLevel.Debug);
    }
    // Set append flag
    _clipboardAction.AppendToClipboard = chkAppend.IsChecked == true;
    // Set retry settings
    if (ActionControlHelper.TryParseTextBoxToInt(txtRetryCount, out int retryCount))
    {
        _clipboardAction.RetryCount = retryCount;
    }
    if (ActionControlHelper.TryParseTextBoxToInt(txtRetryWaitTime, out int retryWaitTime))
    {
        _clipboardAction.RetryWaitTime = retryWaitTime;
    }
    // Verify the result
    LogManager.Log($"After update: UseVariable={_clipboardAction.UseVariable}, " +
                  $"VariableName='{_clipboardAction.VariableName}', " +
                  $"Text='{_clipboardAction.Text?.Substring(0, Math.Min(20, _clipboardAction.Text?.Length ?? 0))}...'", 
                  LogLevel.Debug);
}
/// <summary>
/// Update which fields are visible based on the source type
/// </summary>
private void UpdateUIForSourceType(bool useVariable)
{
    // Update visibility of text and variable fields
    lblText.Visibility = useVariable ? Visibility.Collapsed : Visibility.Visible;
    txtText.Visibility = useVariable ? Visibility.Collapsed : Visibility.Visible;
    lblVariable.Visibility = useVariable ? Visibility.Visible : Visibility.Collapsed;
    cmbVariable.Visibility = useVariable ? Visibility.Visible : Visibility.Collapsed;
}
/// <summary>
/// Event handler for source type radio buttons
/// </summary>
private void RbSourceType_Checked(object sender, RoutedEventArgs e)
{
    bool useVariable = sender == rbUseVariable;
    LogManager.Log($"RadioButton checked: {sender.GetType().Name}, useVariable={useVariable}", LogLevel.Debug);
    // Update UI visibility
    UpdateUIForSourceType(useVariable);
    // IMPORTANT: Also update the model property directly to ensure it's set correctly
    if (_clipboardAction != null)
    {
        LogManager.Log($"Setting ClipboardAction.UseVariable={useVariable} from radio button event", LogLevel.Debug);
        _clipboardAction.UseVariable = useVariable;
    }
}
}
}
