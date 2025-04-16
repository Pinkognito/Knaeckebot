using System.Windows;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for MouseActionControl.xaml
/// </summary>
public partial class MouseActionControl : UserControl, IActionControl
{
private ActionBase _action;
private MouseAction _mouseAction => _action as MouseAction;
private MainViewModel ViewModel => DataContext as MainViewModel;
    public MouseActionControl()
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
    /// Update the UI controls based on the mouse action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_mouseAction == null) return;
        // Set X, Y positions
        txtXPosition.Text = _mouseAction.X.ToString();
        txtYPosition.Text = _mouseAction.Y.ToString();
        txtWheelDelta.Text = _mouseAction.WheelDelta.ToString();
        // Set action type radio button
        switch (_mouseAction.ActionType)
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
    /// <summary>
    /// Update the mouse action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_mouseAction == null) return;
        // Parse X, Y positions with error handling
        if (ActionControlHelper.TryParseTextBoxToInt(txtXPosition, out int x))
        {
            _mouseAction.X = x;
        }
        if (ActionControlHelper.TryParseTextBoxToInt(txtYPosition, out int y))
        {
            _mouseAction.Y = y;
        }
        // Parse wheel delta
        if (ActionControlHelper.TryParseTextBoxToInt(txtWheelDelta, out int wheelDelta))
        {
            _mouseAction.WheelDelta = wheelDelta;
        }
        // Set action type based on selected radio button
        if (rbLeftClick.IsChecked == true)
            _mouseAction.ActionType = MouseActionType.LeftClick;
        else if (rbRightClick.IsChecked == true)
            _mouseAction.ActionType = MouseActionType.RightClick;
        else if (rbMiddleClick.IsChecked == true)
            _mouseAction.ActionType = MouseActionType.MiddleClick;
        else if (rbDoubleClick.IsChecked == true)
            _mouseAction.ActionType = MouseActionType.DoubleClick;
        else if (rbMouseWheel.IsChecked == true)
            _mouseAction.ActionType = MouseActionType.MouseWheel;
    }
    /// <summary>
    /// Get the current mouse position and update the X, Y fields
    /// </summary>
    private void GetCurrentMousePosition_Click(object sender, RoutedEventArgs e)
    {
        var position = System.Windows.Forms.Cursor.Position;
        txtXPosition.Text = position.X.ToString();
        txtYPosition.Text = position.Y.ToString();
        LogManager.Log($"Current mouse position: {position.X}, {position.Y}", LogLevel.Debug);
    }
}
}
