using System.Windows;

namespace Knaeckebot
{
    /// <summary>
    /// Interaction logic for ActionSelectionWindow.xaml
    /// </summary>
    public partial class ActionSelectionWindow : Window
    {
        /// <summary>
        /// The selected action type
        /// </summary>
        public ActionType SelectedActionType { get; private set; }

        /// <summary>
        /// Possible action types
        /// </summary>
        public enum ActionType
        {
            MouseAction,
            KeyboardAction,
            WaitAction,
            BrowserAction,
            JsonAction,
            ClipboardAction,
            VariableAction,
            LoopAction // New action type for loops
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActionSelectionWindow()
        {
            InitializeComponent();

            // Set the default action type (VariableAction is selected by default)
            SelectedActionType = ActionType.VariableAction;
        }

        /// <summary>
        /// Event handler for the OK button
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Determine the selected action type
            int selectedIndex = ActionTypeListBox.SelectedIndex;
            SelectedActionType = selectedIndex switch
            {
                0 => ActionType.MouseAction,
                1 => ActionType.KeyboardAction,
                2 => ActionType.WaitAction,
                3 => ActionType.BrowserAction,
                4 => ActionType.JsonAction,
                5 => ActionType.ClipboardAction,
                6 => ActionType.VariableAction,
                7 => ActionType.LoopAction, // New case for loop action
                _ => ActionType.VariableAction // Default to Variable action
            };

            // Close dialog with result "true"
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Event handler for the Cancel button
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close dialog with result "false"
            DialogResult = false;
            Close();
        }
    }
}