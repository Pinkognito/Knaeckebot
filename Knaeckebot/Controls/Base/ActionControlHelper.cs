using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

// ActionControlHelper.cs
namespace Knaeckebot.Controls.Base
{
    /// <summary>
    /// Helper methods for action controls
    /// </summary>
    public static class ActionControlHelper
    {
        /// <summary>
        /// Helper method to safely parse an integer from a TextBox
        /// </summary>
        public static bool TryParseTextBoxToInt(TextBox textBox, out int result, int defaultValue = 0)
        {
            if (textBox == null)
            {
                result = defaultValue;
                return false;
            }

            if (int.TryParse(textBox.Text, out result))
            {
                return true;
            }
            else
            {
                result = defaultValue;
                LogManager.Log($"Failed to parse integer from: '{textBox.Text}', using default: {defaultValue}", LogLevel.Warning);
                return false;
            }
        }

        /// <summary>
        /// Update a status message in the view model
        /// </summary>
        public static void UpdateStatusMessage(MainViewModel viewModel, string message)
        {
            if (viewModel != null)
            {
                viewModel.StatusMessage = message;
            }
        }

        /// <summary>
        /// Show a confirmation dialog and return the result
        /// </summary>
        public static MessageBoxResult ShowConfirmationDialog(string message, string title = "Confirm")
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }
    }
}
