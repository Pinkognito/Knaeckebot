using System.Windows;
using System.Windows.Controls;
using Knaeckebot.Models;
using Knaeckebot.ViewModels;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for FileActionDetailsControl.xaml
    /// </summary>
    public partial class FileActionDetailsControl : UserControl
    {
        public FileActionDetailsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for the file browse button
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if we have a FileAction selected
            var mainViewModel = DataContext as MainViewModel;
            if (mainViewModel?.SelectedAction is FileAction fileAction)
            {
                // Create OpenFileDialog
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|XML Files (*.xml)|*.xml|CSV Files (*.csv)|*.csv",
                    FilterIndex = 1,
                    Title = "Select a file to read",
                    CheckFileExists = true,
                    Multiselect = false
                };

                // Show dialog
                bool? result = openFileDialog.ShowDialog();

                // If user selected a file, update the file path
                if (result == true)
                {
                    fileAction.FilePath = openFileDialog.FileName;
                }
            }
        }
    }
}