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
        UpdateControlFromAction();
    }
    /// <summary>
    /// Update the UI controls based on the clipboard action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_clipboardAction == null) return;
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
    }
    /// <summary>
    /// Update the clipboard action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_clipboardAction == null) return;
        // Set source based on radio button
        _clipboardAction.UseVariable = rbUseVariable.IsChecked == true;
        // Set text or variable name based on source
        if (_clipboardAction.UseVariable)
        {
            _clipboardAction.VariableName = cmbVariable.Text;
        }
        else
        {
            _clipboardAction.Text = txtText.Text;
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
        UpdateUIForSourceType(useVariable);
        LogManager.Log($"ClipboardAction source type changed to: {(useVariable ? "Variable" : "Direct text")}", LogLevel.Debug);
    }
}
}
