using System;
using System.Collections.Generic;
using System.Windows;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for JsonActionControl.xaml
/// </summary>
public partial class JsonActionControl : UserControl, IActionControl
{
private ActionBase _action;
private JsonAction _jsonAction => _action as JsonAction;
private MainViewModel ViewModel => DataContext as MainViewModel;
    public JsonActionControl()
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
    /// Update the UI controls based on the JSON action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_jsonAction == null) return;
        // Set properties
        chkClipboard.IsChecked = _jsonAction.CheckClipboard;
        txtJsonTemplate.Text = _jsonAction.JsonTemplate;
        txtOffsetX.Text = _jsonAction.OffsetX.ToString();
        txtOffsetY.Text = _jsonAction.OffsetY.ToString();
        txtRetryCount.Text = _jsonAction.RetryCount.ToString();
        txtRetryWaitTime.Text = _jsonAction.RetryWaitTime.ToString();
        chkContinueOnError.IsChecked = _jsonAction.ContinueOnError;
    }
    /// <summary>
    /// Update the JSON action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_jsonAction == null) return;
        // Set properties
        _jsonAction.CheckClipboard = chkClipboard.IsChecked == true;
        _jsonAction.JsonTemplate = txtJsonTemplate.Text;
        // Parse numeric values with error handling
        if (ActionControlHelper.TryParseTextBoxToInt(txtOffsetX, out int offsetX))
        {
            _jsonAction.OffsetX = offsetX;
        }
        if (ActionControlHelper.TryParseTextBoxToInt(txtOffsetY, out int offsetY))
        {
            _jsonAction.OffsetY = offsetY;
        }
        if (ActionControlHelper.TryParseTextBoxToInt(txtRetryCount, out int retryCount))
        {
            _jsonAction.RetryCount = retryCount;
        }
        if (ActionControlHelper.TryParseTextBoxToInt(txtRetryWaitTime, out int retryWaitTime))
        {
            _jsonAction.RetryWaitTime = retryWaitTime;
        }
        _jsonAction.ContinueOnError = chkContinueOnError.IsChecked == true;
    }
    /// <summary>
    /// Event handler for the "Generate Sequence JSON Example" button
    /// </summary>
    private void GenerateSequenceJson_Click(object sender, RoutedEventArgs e)
    {
        if (_jsonAction != null)
        {
            // Use sequence name if a sequence is selected
            string sequenceName = "Example Sequence";
            if (ViewModel?.SelectedSequence != null)
            {
                sequenceName = ViewModel.SelectedSequence.Name;
            }
            // Generate JSON template
            txtJsonTemplate.Text = JsonAction.CreateSequenceJson(sequenceName);
            _jsonAction.JsonTemplate = txtJsonTemplate.Text;
            ActionControlHelper.UpdateStatusMessage(ViewModel, "Sequence JSON example generated");
        }
    }
    /// <summary>
    /// Event handler for the "Sequence with Vars Example" button
    /// </summary>
    private void GenerateSequenceWithVarsJson_Click(object sender, RoutedEventArgs e)
    {
        if (_jsonAction != null)
        {
            // Use sequence name if a sequence is selected
            string sequenceName = "Example Sequence";
            if (ViewModel?.SelectedSequence != null)
            {
                sequenceName = ViewModel.SelectedSequence.Name;
            }
            // Create example variables
            var variables = new Dictionary<string, string>
            {
                { "counter", "1" },
                { "text", "Hello World" },
                { "date", DateTime.Now.ToString("yyyy-MM-dd") }
            };
            // Generate JSON template
            txtJsonTemplate.Text = JsonAction.CreateSequenceJson(sequenceName, variables);
            _jsonAction.JsonTemplate = txtJsonTemplate.Text;
            ActionControlHelper.UpdateStatusMessage(ViewModel, "Sequence with variables JSON example generated");
        }
    }
    /// <summary>
    /// Event handler for the "Click JSON Example" button
    /// </summary>
    private void GenerateClickJson_Click(object sender, RoutedEventArgs e)
    {
        if (_jsonAction != null)
        {
            // Use current mouse position
            var position = System.Windows.Forms.Cursor.Position;
            // Generate JSON template for click action
            txtJsonTemplate.Text = JsonAction.CreateClickJson(position.X, position.Y);
            _jsonAction.JsonTemplate = txtJsonTemplate.Text;
            ActionControlHelper.UpdateStatusMessage(ViewModel, "Click JSON example generated");
        }
    }
    /// <summary>
    /// Event handler for the "Wait JSON Example" button
    /// </summary>
    private void GenerateWaitJson_Click(object sender, RoutedEventArgs e)
    {
        if (_jsonAction != null)
        {
            // Default wait time: 2 seconds
            int waitTime = 2000;
            // Generate JSON template for wait action
            txtJsonTemplate.Text = JsonAction.CreateWaitJson(waitTime);
            _jsonAction.JsonTemplate = txtJsonTemplate.Text;
            ActionControlHelper.UpdateStatusMessage(ViewModel, "Wait JSON example generated");
        }
    }
}
}
