using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Knaeckebot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Register global keyboard shortcuts
            this.KeyDown += MainWindow_KeyDown;

            // Register window events
            this.Loaded += Window_Loaded;
        }

        /// <summary>
        /// Called when the window has loaded
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable multiple selection for sequences if desired
            // DisableMultipleSelection(SequencesListView);

            Services.LogManager.Log("MainWindow has been loaded", Services.LogLevel.Debug);

            // Diagnostics for debug builds
#if DEBUG
            Services.LogManager.Log("Debug mode is enabled", Services.LogLevel.Debug);
#endif
        }

        /// <summary>
        /// Disables multiple selection for a ListView
        /// </summary>
        private void DisableMultipleSelection(System.Windows.Controls.ListView listView)
        {
            if (listView != null)
            {
                Services.LogManager.Log($"Multiple selection disabled for {listView.Name}", Services.LogLevel.Debug);
                listView.SelectionMode = System.Windows.Controls.SelectionMode.Single;
            }
        }

        /// <summary>
        /// Handles keyboard events for shortcuts
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                // F5 for playback
                if (e.Key == Key.F5)
                {
                    if (viewModel.CanPlaySequence)
                    {
                        viewModel.PlaySequenceCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                // F6 or Esc to stop
                else if (e.Key == Key.F6 || e.Key == Key.Escape)
                {
                    if (viewModel.IsPlaying)
                    {
                        viewModel.StopPlayingCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                // Ctrl+C for copying actions or duplicating sequences
                else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // First check if we're in an action context
                    if (viewModel.AreActionsSelected)
                    {
                        viewModel.CopyActionsCommand.Execute(null);
                        e.Handled = true;
                    }
                    // If no actions are selected, but sequences are, then duplicate sequences
                    else if (viewModel.AreSequencesSelected || viewModel.IsSequenceSelected)
                    {
                        viewModel.DuplicateSequenceCommand.Execute(null);
                        e.Handled = true;
                        Services.LogManager.Log("Sequences duplicated with Ctrl+C", Services.LogLevel.Info);
                    }
                }
                // Ctrl+V for pasting actions
                else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (viewModel.IsSequenceSelected)
                    {
                        viewModel.PasteActionsCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                // Delete for deleting sequences or actions
                else if (e.Key == Key.Delete)
                {
                    // Check if actions are selected
                    if (viewModel.AreActionsSelected)
                    {
                        viewModel.DeleteActionCommand.Execute(null);
                        e.Handled = true;
                    }
                    // Check if sequences are selected
                    else if (viewModel.AreSequencesSelected || viewModel.IsSequenceSelected)
                    {
                        viewModel.DeleteSequenceCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}