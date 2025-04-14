using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for ActionDetailsControl.xaml
    /// </summary>
    public partial class ActionDetailsControl : UserControl
    {
        public ActionDetailsControl()
        {
            InitializeComponent();

            // Add event handler for the ListView
            LoopActionsListView.SelectionChanged += LoopActionsListView_SelectionChanged;
        }

        /// <summary>
        /// Handles selection changes in the LoopActions ListView
        /// </summary>
        private void LoopActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if items are selected and enable/disable buttons accordingly
            UpdateLoopActionButtonStates();
        }

        /// <summary>
        /// Updates the activation state of the loop action buttons
        /// </summary>
        private void UpdateLoopActionButtonStates()
        {
            bool hasSelection = LoopActionsListView.SelectedItem != null;

            // Find the buttons by their names and set their IsEnabled status
            if (this.FindName("MoveUpButton") is Button moveUpButton)
                moveUpButton.IsEnabled = hasSelection;

            if (this.FindName("MoveDownButton") is Button moveDownButton)
                moveDownButton.IsEnabled = hasSelection;

            if (this.FindName("DeleteButton") is Button deleteButton)
                deleteButton.IsEnabled = hasSelection;
        }

        /// <summary>
        /// Event handler for the "Current Position" button
        /// </summary>
        private void GetCurrentMousePosition_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var mouseAction = vm?.SelectedAction as MouseAction;

            if (mouseAction != null)
            {
                // Get current mouse position
                var position = System.Windows.Forms.Cursor.Position;
                mouseAction.X = position.X;
                mouseAction.Y = position.Y;
            }
        }

        /// <summary>
        /// Event handler for the "Move Up" button for loop actions
        /// </summary>
        private void MoveLoopActionUp_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction && LoopActionsListView.SelectedItem is ActionBase selectedAction)
            {
                // Determine index of the selected action
                int index = loopAction.LoopActions.IndexOf(selectedAction);
                if (index > 0)
                {
                    // Swap actions
                    loopAction.LoopActions.Move(index, index - 1);
                    // Keep focus and selection on the moved action
                    LoopActionsListView.SelectedIndex = index - 1;
                    LoopActionsListView.Focus();
                    LogManager.Log($"Loop action '{selectedAction.Name}' moved up");
                }
            }
        }

        /// <summary>
        /// Event handler for the "Move Down" button for loop actions
        /// </summary>
        private void MoveLoopActionDown_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction && LoopActionsListView.SelectedItem is ActionBase selectedAction)
            {
                // Determine index of the selected action
                int index = loopAction.LoopActions.IndexOf(selectedAction);
                if (index < loopAction.LoopActions.Count - 1)
                {
                    // Swap actions
                    loopAction.LoopActions.Move(index, index + 1);
                    // Keep focus and selection on the moved action
                    LoopActionsListView.SelectedIndex = index + 1;
                    LoopActionsListView.Focus();
                    LogManager.Log($"Loop action '{selectedAction.Name}' moved down");
                }
            }
        }

        /// <summary>
        /// Event handler for the "Delete" button for loop actions
        /// </summary>
        private void DeleteLoopAction_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedAction is LoopAction loopAction && LoopActionsListView.SelectedItem is ActionBase selectedAction)
            {
                // Confirmation dialog before deleting
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to remove the action '{selectedAction.Name}' from the loop?",
                    "Delete Action",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Save index for next selection
                    int index = loopAction.LoopActions.IndexOf(selectedAction);

                    // Remove action from the loop
                    loopAction.LoopActions.Remove(selectedAction);
                    LogManager.Log($"Loop action '{selectedAction.Name}' deleted");

                    // Select next element if available
                    if (loopAction.LoopActions.Count > 0)
                    {
                        LoopActionsListView.SelectedIndex = Math.Min(index, loopAction.LoopActions.Count - 1);
                        LoopActionsListView.Focus();
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for the "Generate Sequence JSON Example" button
        /// </summary>
        private void GenerateSequenceJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use sequence name if a sequence is selected
                string sequenceName = "Example Sequence";

                if (vm?.SelectedSequence != null)
                {
                    sequenceName = vm.SelectedSequence.Name;
                }

                // Generate JSON template
                jsonAction.JsonTemplate = JsonAction.CreateSequenceJson(sequenceName);
            }
        }

        /// <summary>
        /// Event handler for the "Sequence with Vars Example" button
        /// </summary>
        private void GenerateSequenceWithVarsJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use sequence name if a sequence is selected
                string sequenceName = "Example Sequence";

                if (vm?.SelectedSequence != null)
                {
                    sequenceName = vm.SelectedSequence.Name;
                }

                // Create example variables
                var variables = new Dictionary<string, string>
                {
                    { "counter", "1" },
                    { "text", "Hello World" },
                    { "date", DateTime.Now.ToString("yyyy-MM-dd") }
                };

                // Generate JSON template
                jsonAction.JsonTemplate = JsonAction.CreateSequenceJson(sequenceName, variables);
            }
        }

        /// <summary>
        /// Event handler for the "Click JSON Example" button
        /// </summary>
        private void GenerateClickJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Use current mouse position
                var position = System.Windows.Forms.Cursor.Position;

                // Generate JSON template for click action
                jsonAction.JsonTemplate = JsonAction.CreateClickJson(position.X, position.Y);
            }
        }

        /// <summary>
        /// Event handler for the "Wait JSON Example" button
        /// </summary>
        private void GenerateWaitJson_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var jsonAction = vm?.SelectedAction as JsonAction;

            if (jsonAction != null)
            {
                // Default wait time: 2 seconds
                int waitTime = 2000;

                // Generate JSON template for wait action
                jsonAction.JsonTemplate = JsonAction.CreateWaitJson(waitTime);
            }
        }

        private void RadioButton_DirectText_Checked(object sender, RoutedEventArgs e)
        {
            // Direct text selected
            if (DataContext is MainViewModel viewModel &&
                viewModel.SelectedAction is ClipboardAction action)
            {
                LogManager.Log($"### XAML: RadioButton DirectText set for ClipboardAction {action.Id.ToString().Substring(0, 8)}", LogLevel.Debug);

                // Explicitly set UseVariable to false
                bool oldValue = action.UseVariable;
                action.UseVariable = false;

                LogManager.Log($"### XAML: UseVariable changed from {oldValue} to {action.UseVariable} by RadioButton", LogLevel.Debug);

                // Check if the change was applied
                if (action.UseVariable != false)
                {
                    LogManager.Log($"!!! CRITICAL: UseVariable was not set to false! Still: {action.UseVariable}", LogLevel.Error);
                }
            }
        }

        private void RadioButton_UseVariable_Checked(object sender, RoutedEventArgs e)
        {
            // Use variable selected
            if (DataContext is MainViewModel viewModel &&
                viewModel.SelectedAction is ClipboardAction action)
            {
                LogManager.Log($"### XAML: RadioButton UseVariable set for ClipboardAction {action.Id.ToString().Substring(0, 8)}", LogLevel.Debug);

                // Explicitly set UseVariable to true
                bool oldValue = action.UseVariable;
                action.UseVariable = true;

                LogManager.Log($"### XAML: UseVariable changed from {oldValue} to {action.UseVariable} by RadioButton", LogLevel.Debug);

                // Check if the change was applied
                if (action.UseVariable != true)
                {
                    LogManager.Log($"!!! CRITICAL: UseVariable was not set to true! Still: {action.UseVariable}", LogLevel.Error);
                }
            }
        }
    }
}