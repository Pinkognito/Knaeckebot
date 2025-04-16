using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls.Actions
{
/// <summary>
/// Interaction logic for VariableActionControl.xaml
/// </summary>
public partial class VariableActionControl : UserControl, IActionControl
{
private ActionBase _action;
private VariableAction _variableAction => _action as VariableAction;
private MainViewModel ViewModel => DataContext as MainViewModel;
    public VariableActionControl()
    {
        InitializeComponent();
        // Initialize the action type combo box
        cmbActionType.ItemsSource = System.Enum.GetValues(typeof(VariableActionType));
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
    /// Update the UI controls based on the variable action properties
    /// </summary>
    public void UpdateControlFromAction()
    {
        if (_variableAction == null) return;
        // Set action type
        cmbActionType.SelectedItem = _variableAction.ActionType;
        // Set variable name
        cmbVariableName.Text = _variableAction.VariableName;
        // Populate the variable names
        if (ViewModel != null)
        {
            cmbVariableName.ItemsSource = ViewModel.VariableNames;
        }
        // Update UI based on action type
        UpdateUIForActionType(_variableAction.ActionType);
        // Set value or increment based on action type
        if (_variableAction.ActionType == VariableActionType.SetValue ||
            _variableAction.ActionType == VariableActionType.AppendText)
        {
            txtValue.Text = _variableAction.Value;
        }
        else if (_variableAction.ActionType == VariableActionType.Increment ||
                _variableAction.ActionType == VariableActionType.Decrement)
        {
            txtIncrement.Text = _variableAction.IncrementValue.ToString();
        }
    }
    /// <summary>
    /// Update the variable action with values from the UI controls
    /// </summary>
    public void UpdateActionFromControl()
    {
        if (_variableAction == null) return;
        // Update variable name
        _variableAction.VariableName = cmbVariableName.Text;
        // Update action type if one is selected
        if (cmbActionType.SelectedItem is VariableActionType selectedType)
        {
            _variableAction.ActionType = selectedType;
        }
        // Update value or increment based on action type
        if (_variableAction.ActionType == VariableActionType.SetValue ||
            _variableAction.ActionType == VariableActionType.AppendText)
        {
            _variableAction.Value = txtValue.Text;
        }
        else if (_variableAction.ActionType == VariableActionType.Increment ||
                _variableAction.ActionType == VariableActionType.Decrement)
        {
            if (ActionControlHelper.TryParseTextBoxToInt(txtIncrement, out int incrementValue))
            {
                _variableAction.IncrementValue = incrementValue;
            }
        }
    }
    /// <summary>
    /// Update which fields are visible based on the action type
    /// </summary>
    private void UpdateUIForActionType(VariableActionType actionType)
    {
        // First hide all specific elements
        lblValue.Visibility = Visibility.Collapsed;
        txtValue.Visibility = Visibility.Collapsed;
        lblIncrement.Visibility = Visibility.Collapsed;
        txtIncrement.Visibility = Visibility.Collapsed;
        // Show relevant elements based on action type
        switch (actionType)
        {
            case VariableActionType.SetValue:
            case VariableActionType.AppendText:
                lblValue.Visibility = Visibility.Visible;
                txtValue.Visibility = Visibility.Visible;
                break;
            case VariableActionType.Increment:
            case VariableActionType.Decrement:
                lblIncrement.Visibility = Visibility.Visible;
                txtIncrement.Visibility = Visibility.Visible;
                break;
        }
    }
    /// <summary>
    /// Event handler for action type selection changes
    /// </summary>
    private void CmbActionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbActionType.SelectedItem is VariableActionType selectedType)
        {
            UpdateUIForActionType(selectedType);
        }
    }
}
}
