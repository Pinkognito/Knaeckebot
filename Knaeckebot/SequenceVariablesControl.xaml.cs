using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Knaeckebot.Models;
using Knaeckebot.Services;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for SequenceVariablesControl.xaml
    /// </summary>
    public partial class SequenceVariablesControl : UserControl
    {
        public SequenceVariablesControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Helper method to convert list data to a formatted string for UI display
        /// </summary>
        public static string FormatListForDisplay(string listValue, int maxLength = 30)
        {
            if (string.IsNullOrEmpty(listValue))
                return "(Empty)";

            // Replace semicolons with commas for better readability
            string formatted = listValue.Replace(";", ", ");

            // If it contains newlines, it's a table - show summary
            if (listValue.Contains('\n'))
            {
                string[] rows = listValue.Split('\n');
                return $"Table: {rows.Length} rows";
            }

            // Truncate with ellipsis if too long
            if (formatted.Length > maxLength)
                return formatted.Substring(0, maxLength - 3) + "...";

            return formatted;
        }

        /// <summary>
        /// Helper method for validating and formatting list input
        /// </summary>
        public static string FormatListInput(string input)
        {
            // Normalize line endings
            input = input.Replace("\r\n", "\n");

            // Replace commas with semicolons if the user prefers commas
            if (input.Contains(',') && !input.Contains(';'))
            {
                input = input.Replace(",", ";");
            }

            return input;
        }
    }
}