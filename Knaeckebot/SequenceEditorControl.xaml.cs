using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using ListViewItem = System.Windows.Controls.ListViewItem;
using UserControl = System.Windows.Controls.UserControl;
using CheckBox = System.Windows.Controls.CheckBox;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for SequenceEditorControl.xaml
    /// </summary>
    public partial class SequenceEditorControl : UserControl
    {
        // Flag to prevent recursive event handling
        private bool _isUpdatingCheckboxes = false;

        public SequenceEditorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event is triggered when an item is selected
        /// </summary>
        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is ActionBase action)
            {
                // Get MainViewModel
                if (DataContext is MainViewModel viewModel)
                {
                    // Add action to SelectedActions if it's not already included
                    if (!viewModel.SelectedActions.Contains(action))
                    {
                        viewModel.SelectedActions.Add(action);
                        LogManager.Log($"Action {action.Name} added to selection. Selected actions: {viewModel.SelectedActions.Count}", LogLevel.Debug);
                    }
                }
            }
        }

        /// <summary>
        /// Event is triggered when an item is deselected
        /// </summary>
        private void ListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is ActionBase action)
            {
                // Get MainViewModel
                if (DataContext is MainViewModel viewModel)
                {
                    // Remove action from SelectedActions
                    if (viewModel.SelectedActions.Contains(action))
                    {
                        viewModel.SelectedActions.Remove(action);
                        LogManager.Log($"Action {action.Name} removed from selection. Selected actions: {viewModel.SelectedActions.Count}", LogLevel.Debug);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for checkbox Checked event on actions
        /// </summary>
        private void Action_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingCheckboxes) return;

            if (sender is CheckBox checkBox && checkBox.DataContext is ActionBase action)
            {
                // Handle checkbox checked event for multi-selection
                HandleActionCheckboxChange(action, true);
            }
        }

        /// <summary>
        /// Event handler for checkbox Unchecked event on actions
        /// </summary>
        private void Action_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingCheckboxes) return;

            if (sender is CheckBox checkBox && checkBox.DataContext is ActionBase action)
            {
                // Handle checkbox unchecked event for multi-selection
                HandleActionCheckboxChange(action, false);
            }
        }

        /// <summary>
        /// Handles checkbox state changes for actions, applying the change to all selected actions
        /// </summary>
        private void HandleActionCheckboxChange(ActionBase action, bool isChecked)
        {
            // Get MainViewModel
            if (DataContext is MainViewModel viewModel)
            {
                // Only apply to multiple items if the changed action is among the selected ones
                if (viewModel.SelectedActions.Contains(action) && viewModel.SelectedActions.Count > 1)
                {
                    LogManager.Log($"Applying IsEnabled={isChecked} to {viewModel.SelectedActions.Count} selected actions", LogLevel.Debug);

                    // Set flag to prevent recursive event handling
                    _isUpdatingCheckboxes = true;

                    try
                    {
                        foreach (var selectedAction in viewModel.SelectedActions)
                        {
                            // Update all selected actions to match the same enabled state
                            selectedAction.IsEnabled = isChecked;
                        }
                    }
                    finally
                    {
                        // Reset flag
                        _isUpdatingCheckboxes = false;
                    }
                }
            }
        }
    }
}