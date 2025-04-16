using Knaeckebot.Controls.Base;
using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for BrowserActionControl.xaml
/// </summary>
public partial class BrowserActionControl : UserControl, IActionControl
{
private ActionBase _action;

        public event EventHandler<BranchActionEventArgs> BranchActionSelected;
        private BrowserAction _browserAction => _action as BrowserAction;
private MainViewModel ViewModel => DataContext as MainViewModel;
    public BrowserActionControl()
    {
        InitializeComponent();
        // Initialize the action type combo box
        cmbActionType.ItemsSource = System.Enum.GetValues(typeof(BrowserActionType));
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
    /// Update the UI controls based on the browser action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_browserAction == null) return;
        // Set action type
        cmbActionType.SelectedItem = _browserAction.ActionType;
        // Set type-specific properties
        txtSelector.Text = _browserAction.Selector;
        txtJavaScript.Text = _browserAction.JavaScript;
        txtXResult.Text = _browserAction.XResult.ToString();
        txtYResult.Text = _browserAction.YResult.ToString();
        chkUseLastResults.IsChecked = _browserAction.UseLastResults;
        // Update UI based on action type
        UpdateUIForActionType(_browserAction.ActionType);
    }
    /// <summary>
    /// Update the browser action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_browserAction == null) return;
        // Set action type if one is selected
        if (cmbActionType.SelectedItem is BrowserActionType selectedType)
        {
            _browserAction.ActionType = selectedType;
        }
        // Set type-specific properties
        _browserAction.Selector = txtSelector.Text;
        _browserAction.JavaScript = txtJavaScript.Text;
        // Parse numeric values with error handling
        if (ActionControlHelper.TryParseTextBoxToInt(txtXResult, out int xResult))
        {
            _browserAction.XResult = xResult;
        }
        if (ActionControlHelper.TryParseTextBoxToInt(txtYResult, out int yResult))
        {
            _browserAction.YResult = yResult;
        }
        _browserAction.UseLastResults = chkUseLastResults.IsChecked == true;
    }
    /// <summary>
    /// Update which fields are visible based on the action type
    /// </summary>
    private void UpdateUIForActionType(BrowserActionType actionType)
    {
        // First hide all specific elements
        lblSelector.Visibility = Visibility.Collapsed;
        txtSelector.Visibility = Visibility.Collapsed;
        lblJavaScript.Visibility = Visibility.Collapsed;
        txtJavaScript.Visibility = Visibility.Collapsed;
        lblCoordinates.Visibility = Visibility.Collapsed;
        panelCoordinates.Visibility = Visibility.Collapsed;
        lblUseLastResults.Visibility = Visibility.Collapsed;
        chkUseLastResults.Visibility = Visibility.Collapsed;
        // Show relevant elements based on action type
        switch (actionType)
        {
            case BrowserActionType.FindElementAndClick:
                lblSelector.Visibility = Visibility.Visible;
                txtSelector.Visibility = Visibility.Visible;
                break;
            case BrowserActionType.ExecuteJavaScript:
                lblJavaScript.Visibility = Visibility.Visible;
                txtJavaScript.Visibility = Visibility.Visible;
                break;
            case BrowserActionType.GetCoordinates:
                lblCoordinates.Visibility = Visibility.Visible;
                panelCoordinates.Visibility = Visibility.Visible;
                lblUseLastResults.Visibility = Visibility.Visible;
                chkUseLastResults.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Event handler for action type selection changes
    /// </summary>
    private void CmbActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbActionType.SelectedItem is BrowserActionType selectedType)
        {
            UpdateUIForActionType(selectedType);
            LogManager.Log($"Browser action type changed to: {selectedType}", LogLevel.Debug);
        }
    }
}
}
