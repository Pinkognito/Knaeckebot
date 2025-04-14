using Knaeckebot.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Type of a sequence variable
    /// </summary>
    public enum VariableType
    {
        Text,
        Number
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
            return Type == VariableType.Text ? TextValue : NumberValue.ToString();
        }

        /// <summary>
        /// Sets the value based on a string
        /// </summary>
        public void SetValueFromString(string value)
        {
            // Ensure value is not null
            string safeValue = value ?? string.Empty;

            if (Type == VariableType.Text)
            {
                TextValue = safeValue;
            }
            else
            {
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
            }
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