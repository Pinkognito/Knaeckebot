using System;
using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Base
{
/// <summary>
/// Base class for all specialized action control types
/// </summary>
public abstract class ActionControlBase : UserControl, IActionControl
{
// Reference to the main view model
protected MainViewModel ViewModel => DataContext as MainViewModel;
    // Reference to the current action being edited
    protected ActionBase Action { get; private set; }
    /// <summary>
    /// Event that is raised when a branch action is selected for editing
    /// </summary>
    public event EventHandler<BranchActionEventArgs> BranchActionSelected;
    /// <summary>
    /// Initialize the control with an action
    /// </summary>
    /// <param name="action">The action to edit</param>
    public virtual void Initialize(ActionBase action)
    {
        if (action == null)
        {
            LogManager.Log("Attempted to initialize action control with null action", LogLevel.Error);
            return;
        }
        Action = action;
        LogManager.Log($"Initialized {GetType().Name} with action: {action.Name} ({action.GetType().Name})", LogLevel.Debug);
        // Update UI fields with values from the action
        UpdateControlFromAction();
    }
    /// <summary>
    /// Update the control's UI elements from the action's properties
    /// Must be implemented by derived classes
    /// </summary>
    public abstract void UpdateControlFromAction();
    /// <summary>
    /// Update the action's properties from the control's UI elements
    /// Must be implemented by derived classes
    /// </summary>
    public abstract void UpdateActionFromControl();
    /// <summary>
    /// Helper method to safely parse an integer from a TextBox
    /// </summary>
    protected bool TryParseTextBoxToInt(TextBox textBox, out int result, int defaultValue = 0)
    {
        if (textBox == null)
        {
            result = defaultValue;
            return false;
        }
        if (int.TryParse(textBox.Text, out result))
        {
            return true;
        }
        else
        {
            result = defaultValue;
            LogManager.Log($"Failed to parse integer from: '{textBox.Text}', using default: {defaultValue}", LogLevel.Warning);
            return false;
        }
    }
    /// <summary>
    /// Update a status message in the view model
    /// </summary>
    protected void UpdateStatusMessage(string message)
    {
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = message;
        }
    }
    /// <summary>
    /// Show a confirmation dialog and return the result
    /// </summary>
    protected MessageBoxResult ShowConfirmationDialog(string message, string title = "Confirm")
    {
        return MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
    }
    /// <summary>
    /// Raises the BranchActionSelected event
    /// </summary>
    protected void OnBranchActionSelected(ActionBase action, string branchContext)
    {
        BranchActionSelected?.Invoke(this, new BranchActionEventArgs(action, branchContext));
    }
}
}
