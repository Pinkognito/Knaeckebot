using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using ListViewItem = System.Windows.Controls.ListViewItem;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for SequenceEditorControl.xaml
    /// </summary>
    public partial class SequenceEditorControl : UserControl
    {
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
    }
}