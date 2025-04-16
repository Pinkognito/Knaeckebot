using Knaeckebot.Models;
using System;
namespace Knaeckebot.Controls.Base
{
/// <summary>
/// Interface for all specialized action control types
/// </summary>
public interface IActionControl
{
/// <summary>
/// Initialize the control with an action
/// </summary>
void Initialize(ActionBase action);
    /// <summary>
    /// Update the control's UI elements from the action's properties
    /// </summary>
    void UpdateControlFromAction();
    /// <summary>
    /// Update the action's properties from the control's UI elements
    /// </summary>
    void UpdateActionFromControl();
    /// <summary>
    /// Event that is raised when a branch action is selected for editing
    /// </summary>
    event EventHandler<BranchActionEventArgs> BranchActionSelected;
}
/// <summary>
/// Event arguments for branch action selection
/// </summary>
public class BranchActionEventArgs : EventArgs
{
    /// <summary>
    /// The selected branch action
    /// </summary>
    public ActionBase SelectedAction { get; }
    /// <summary>
    /// The context of the branch (THEN, ELSE, LOOP, etc.)
    /// </summary>
    public string BranchContext { get; }
    /// <summary>
    /// Creates a new instance of BranchActionEventArgs
    /// </summary>
    public BranchActionEventArgs(ActionBase selectedAction, string branchContext)
    {
        SelectedAction = selectedAction;
        BranchContext = branchContext;
    }
}
}
