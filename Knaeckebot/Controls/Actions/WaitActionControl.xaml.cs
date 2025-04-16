using System.Windows;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for WaitActionControl.xaml
/// </summary>
public partial class WaitActionControl : UserControl, IActionControl
{
private ActionBase _action;
private WaitAction _waitAction => _action as WaitAction;
private MainViewModel ViewModel => DataContext as MainViewModel;
    public WaitActionControl()
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
    /// Update the UI controls based on the wait action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_waitAction == null) return;
        // Set wait time
        txtWaitTime.Text = _waitAction.WaitTime.ToString();
    }
    /// <summary>
    /// Update the wait action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_waitAction == null) return;
        // Parse wait time
        if (ActionControlHelper.TryParseTextBoxToInt(txtWaitTime, out int waitTime))
        {
            _waitAction.WaitTime = waitTime;
            LogManager.Log($"Updated wait time to {waitTime}ms", LogLevel.Debug);
        }
    }
}
}
