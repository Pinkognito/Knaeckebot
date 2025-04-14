using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for SequenceLibraryControl.xaml
    /// </summary>
    public partial class SequenceLibraryControl : UserControl
    {
        public SequenceLibraryControl()
        {
            InitializeComponent();

            // Add the PreviewKeyDown event - this is triggered before KeyDown and cannot be easily overridden
            SequencesListView.PreviewKeyDown += SequencesListView_PreviewKeyDown;
        }

        /// <summary>
        /// Event is triggered when a sequence is selected
        /// </summary>
        private void SequenceListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Sequence sequence)
            {
                // Get MainViewModel
                if (DataContext is MainViewModel viewModel)
                {
                    LogManager.Log($"=== SEQUENCE SELECTION ===", LogLevel.Debug);
                    LogManager.Log($"Sequence selected: {sequence.Name}, ID: {sequence.Id}", LogLevel.Debug);

                    // Set SelectedSequence - this also adds the sequence to SelectedSequences
                    viewModel.SelectedSequence = sequence;

                    // Check and remove duplicates in SelectedSequences
                    SequenceUtils.RemoveDuplicates(viewModel.SelectedSequences);

                    // Log selection status after selection
                    SequenceUtils.LogSelectedSequences(viewModel.SelectedSequences, "after selection");
                }
            }
        }

        /// <summary>
        /// Event is triggered when a sequence is deselected
        /// </summary>
        private void SequenceListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Sequence sequence)
            {
                // Get MainViewModel
                if (DataContext is MainViewModel viewModel)
                {
                    LogManager.Log($"=== SEQUENCE DESELECTION ===", LogLevel.Debug);
                    LogManager.Log($"Sequence deselected: {sequence.Name}, ID: {sequence.Id}", LogLevel.Debug);

                    // Remove sequence from SelectedSequences
                    if (viewModel.SelectedSequences.Contains(sequence))
                    {
                        viewModel.SelectedSequences.Remove(sequence);
                        LogManager.Log($"Sequence {sequence.Name} removed from selection. Selected sequences: {viewModel.SelectedSequences.Count}", LogLevel.Debug);

                        // If this sequence was also SelectedSequence, set to null or the first selected one
                        if (viewModel.SelectedSequence == sequence)
                        {
                            if (viewModel.SelectedSequences.Count > 0)
                            {
                                viewModel.SelectedSequence = viewModel.SelectedSequences[0];
                                LogManager.Log($"SelectedSequence updated from {sequence.Name} to {viewModel.SelectedSequence.Name}", LogLevel.Debug);
                            }
                            else
                            {
                                viewModel.SelectedSequence = null;
                                LogManager.Log($"SelectedSequence updated from {sequence.Name} to null", LogLevel.Debug);
                            }
                        }
                    }

                    // Log selection status after deselection
                    SequenceUtils.LogSelectedSequences(viewModel.SelectedSequences, "after deselection");
                }
            }
        }

        /// <summary>
        /// KeyDown handler for the ListView
        /// </summary>
        private void SequencesListView_KeyDown(object sender, KeyEventArgs e)
        {
            // Delete key to delete selected sequences
            if (e.Key == Key.Delete)
            {
                if (DataContext is MainViewModel viewModel &&
                    (viewModel.AreSequencesSelected || viewModel.IsSequenceSelected))
                {
                    LogManager.Log("Delete key pressed in sequence list, deleting selected sequences", LogLevel.Debug);
                    viewModel.DeleteSequenceCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// PreviewKeyDown handler for the ListView - this has higher priority than the regular KeyDown
        /// </summary>
        private void SequencesListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+C to duplicate selected sequences
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel viewModel &&
                    (viewModel.AreSequencesSelected || viewModel.IsSequenceSelected))
                {
                    LogManager.Log("Ctrl+C pressed in sequence list, duplicating selected sequences", LogLevel.Debug);
                    viewModel.DuplicateSequenceCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}