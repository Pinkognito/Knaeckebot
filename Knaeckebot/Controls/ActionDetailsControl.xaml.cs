using System;
using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Controls.Actions;
using Knaeckebot.Controls.Base;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for ActionDetailsControl.xaml
    /// This is a refactored version that acts as a container for specialized action controls
    /// </summary>
    public partial class ActionDetailsControl : UserControl
    {
        // Reference to the current specialized action control
        private IActionControl _currentActionControl;
        // Reference to keyboard-specific view model (needed for keyboard actions)
        private KeyboardActionViewModel _keyboardViewModel;
        public ActionDetailsControl()
        {
            InitializeComponent();
            LogManager.Log("ActionDetailsControl initialized", LogLevel.Debug);
            // Register for data context changed to monitor when the selected action changes
            DataContextChanged += ActionDetailsControl_DataContextChanged;
        }
        /// <summary>
        /// Handles data context changes to monitor selected action changes
        /// </summary>
        private void ActionDetailsControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel viewModel)
            {
                // Create the keyboard action view model
                _keyboardViewModel = new KeyboardActionViewModel();
                // Subscribe to selected action changes
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.SelectedAction))
                    {
                        LogManager.Log("SelectedAction changed in ActionDetailsControl", LogLevel.Debug);
                        // Update the control for the new action type
                        UpdateActionTypeControl(viewModel.SelectedAction);
                    }
                };
            }
        }
        /// <summary>
        /// Creates and initializes the appropriate control for the selected action type
        /// </summary>
        private void UpdateActionTypeControl(ActionBase action)
        {
            // Clear current content
            ActionTypeContent.Content = null;
            _currentActionControl = null;
            if (action == null)
            {
                LogManager.Log("No action selected, cleared action type control", LogLevel.Debug);
                return;
            }
            LogManager.Log($"Creating control for action type: {action.GetType().Name}", LogLevel.Debug);
            // Create the appropriate control based on action type
            if (action is MouseAction)
            {
                _currentActionControl = new MouseActionControl();
            }
            else if (action is KeyboardAction)
            {
                var keyboardControl = new KeyboardActionControl();
                _currentActionControl = keyboardControl;
                // Special initialization for keyboard actions
                keyboardControl.Initialize(action, _keyboardViewModel);
                // Skip the normal initialize call since we did custom initialization
                ActionTypeContent.Content = _currentActionControl;
                return;
            }
            else if (action is WaitAction)
            {
                _currentActionControl = new WaitActionControl();
            }
            else if (action is VariableAction)
            {
                _currentActionControl = new VariableActionControl();
            }
            else if (action is ClipboardAction)
            {
                _currentActionControl = new ClipboardActionControl();
            }
            else if (action is JsonAction)
            {
                _currentActionControl = new JsonActionControl();
            }
            else if (action is BrowserAction)
            {
                _currentActionControl = new BrowserActionControl();
            }
            else if (action is LoopAction)
            {
                _currentActionControl = new LoopActionControl();
            }
            else if (action is IfAction)
            {
                _currentActionControl = new IfActionControl();
            }
            else
            {
                LogManager.Log($"No specialized control available for action type: {action.GetType().Name}", LogLevel.Warning);
                return;
            }
            // Initialize the control with the action and display it
            ((UserControl)_currentActionControl).DataContext = DataContext;
            _currentActionControl.Initialize(action);
            ActionTypeContent.Content = _currentActionControl;
        }
        /// <summary>
        /// Apply changes from the current action control back to the action
        /// This method should be called when saving or switching actions
        /// </summary>
        public void ApplyChanges()
        {
            if (_currentActionControl != null)
            {
                LogManager.Log("Applying changes from specialized action control", LogLevel.Debug);
                _currentActionControl.UpdateActionFromControl();
            }
        }
    }
}
