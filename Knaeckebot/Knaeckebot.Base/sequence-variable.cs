using Knaeckebot.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Type of a sequence variable
    /// </summary>
    public enum VariableType
    {
        Text,
        Number,
        Boolean,
        List
    }

    /// <summary>
    /// Represents a variable in a sequence
    /// </summary>
    public class SequenceVariable : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private VariableType _type = VariableType.Text;
        private string _textValue = string.Empty;
        private int _numberValue = 0;
        private bool _boolValue = false;
        private string _listValue = string.Empty;
        private string _description = string.Empty;

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of the variable
        /// </summary>
        public VariableType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();

                    // When type changes, notify UI about changed values
                    OnPropertyChanged(nameof(TextValue));
                    OnPropertyChanged(nameof(NumberValue));
                    OnPropertyChanged(nameof(BoolValue));
                    OnPropertyChanged(nameof(ListValue));
                }
            }
        }

        /// <summary>
        /// Text value (for Type Text)
        /// </summary>
        public string TextValue
        {
            get => _textValue;
            set
            {
                if (_textValue != value)
                {
                    _textValue = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number value (for Type Number)
        /// </summary>
        public int NumberValue
        {
            get => _numberValue;
            set
            {
                if (_numberValue != value)
                {
                    _numberValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Boolean value (for Type Boolean)
        /// </summary>
        public bool BoolValue
        {
            get => _boolValue;
            set
            {
                if (_boolValue != value)
                {
                    _boolValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List value as semicolon-separated string (for Type List)
        /// </summary>
        public string ListValue
        {
            get => _listValue;
            set
            {
                if (_listValue != value)
                {
                    _listValue = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the variable
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Returns the current value as string, regardless of type
        /// </summary>
        public string GetValueAsString()
        {
            return Type switch
            {
                VariableType.Text => TextValue,
                VariableType.Number => NumberValue.ToString(),
                VariableType.Boolean => BoolValue.ToString(),
                VariableType.List => ListValue,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Sets the value based on a string
        /// </summary>
        public void SetValueFromString(string value)
        {
            // Ensure value is not null
            string safeValue = value ?? string.Empty;

            switch (Type)
            {
                case VariableType.Text:
                    TextValue = safeValue;
                    break;
                case VariableType.Number:
                    if (int.TryParse(safeValue, out int numValue))
                    {
                        NumberValue = numValue;
                    }
                    else if (!string.IsNullOrEmpty(safeValue))
                    {
                        // If the text cannot be converted to a number
                        // and is not empty, set a default value
                        NumberValue = 0;
                        LogManager.Log($"Warning: Text '{safeValue}' could not be converted to a number, setting to 0");
                    }
                    break;
                case VariableType.Boolean:
                    if (bool.TryParse(safeValue, out bool boolValue))
                    {
                        BoolValue = boolValue;
                    }
                    else if (!string.IsNullOrEmpty(safeValue))
                    {
                        // For non-empty strings that aren't "true" or "false", interpret various values
                        string lowerValue = safeValue.ToLower().Trim();
                        if (lowerValue == "1" || lowerValue == "yes" || lowerValue == "y" || lowerValue == "on")
                            BoolValue = true;
                        else
                            BoolValue = false;

                        LogManager.Log($"Converting '{safeValue}' to boolean value: {BoolValue}");
                    }
                    break;
                case VariableType.List:
                    // Just directly store the string as a list (semicolon-separated)
                    ListValue = safeValue;
                    break;
            }
        }

        /// <summary>
        /// Gets the list items (for one-dimensional list)
        /// </summary>
        /// <returns>Array of list items</returns>
        public string[] GetListItems()
        {
            if (Type != VariableType.List)
                return Array.Empty<string>();

            return ListValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the table data (for two-dimensional list)
        /// </summary>
        /// <returns>Table of data as string[][]</returns>
        public string[][] GetTableData()
        {
            if (Type != VariableType.List)
                return Array.Empty<string[]>();

            try
            {
                // Split by lines first (rows)
                string[] rows = ListValue.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Then split each row by semicolons (columns)
                return rows.Select(row => row.Split(';')).ToArray();
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error parsing table data: {ex.Message}");
                return Array.Empty<string[]>();
            }
        }

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="item">Item to add</param>
        public void AddListItem(string item)
        {
            if (Type != VariableType.List)
                return;

            string safeItem = item ?? string.Empty;

            // Add semicolon if list is not empty
            if (!string.IsNullOrEmpty(ListValue))
                ListValue += ";";

            ListValue += safeItem;
        }

        /// <summary>
        /// Toggles the boolean value
        /// </summary>
        public void ToggleBoolValue()
        {
            if (Type == VariableType.Boolean)
                BoolValue = !BoolValue;
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void ClearList()
        {
            if (Type == VariableType.List)
                ListValue = string.Empty;
        }

        /// <summary>
        /// Removes an item from the list at the specified index
        /// </summary>
        /// <param name="index">Index to remove</param>
        /// <returns>True if successful</returns>
        public bool RemoveListItemAt(int index)
        {
            if (Type != VariableType.List)
                return false;

            string[] items = GetListItems();
            if (index < 0 || index >= items.Length)
                return false;

            var itemList = items.ToList();
            itemList.RemoveAt(index);
            ListValue = string.Join(";", itemList);
            return true;
        }

        /// <summary>
        /// Adds a row to the table
        /// </summary>
        /// <param name="row">Row to add (semicolon-separated)</param>
        public void AddTableRow(string row)
        {
            if (Type != VariableType.List)
                return;

            string safeRow = row ?? string.Empty;

            // Add newline if list is not empty
            if (!string.IsNullOrEmpty(ListValue))
                ListValue += "\n";

            ListValue += safeRow;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}