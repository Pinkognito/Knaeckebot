using Knaeckebot.Services;
using System;
using System.Diagnostics;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Possible operations on variables
    /// </summary>
    public enum VariableActionType
    {
        SetValue,
        Increment,
        Decrement,
        AppendText,
        ClearValue,
        ToggleBoolean,
        AddListItem,
        RemoveListItem,
        ClearList,
        AddTableRow
    }

    /// <summary>
    /// Action that works with sequence variables
    /// </summary>
    public class VariableAction : ActionBase
    {
        private string _variableName = string.Empty;
        private string _value = string.Empty;
        private int _incrementValue = 1;
        private VariableActionType _actionType = VariableActionType.SetValue;
        private int _listIndex = 0;

        /// <summary>
        /// Name of the variable to edit
        /// </summary>
        public string VariableName
        {
            get => _variableName;
            set
            {
                if (_variableName != value)
                {
                    _variableName = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Value for the operation (with SetValue, AppendText, AddListItem, etc.)
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Value for incrementing/decrementing
        /// </summary>
        public int IncrementValue
        {
            get => _incrementValue;
            set
            {
                if (_incrementValue != value)
                {
                    _incrementValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of variable operation
        /// </summary>
        public VariableActionType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType != value)
                {
                    _actionType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Index for list operations
        /// </summary>
        public int ListIndex
        {
            get => _listIndex;
            set
            {
                if (_listIndex != value)
                {
                    _listIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the variable action
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Ensure VariableName is not empty
                if (string.IsNullOrEmpty(VariableName))
                {
                    LogManager.Log("Variable action: VariableName is empty, will be set automatically");
                    VariableName = "var" + DateTime.Now.Ticks.ToString().Substring(10);
                    LogManager.Log($"Variable action: VariableName was set to {VariableName}");
                }

                // Get current sequence from SequenceManager
                var currentSequence = SequenceManager.CurrentSequence;
                if (currentSequence == null)
                {
                    LogManager.Log("No current sequence found for variable action - using the first available");

                    // If no sequence is set as active, use the one currently selected in the UI
                    if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow?.DataContext is ViewModels.MainViewModel viewModel &&
                            viewModel.SelectedSequence != null)
                        {
                            currentSequence = viewModel.SelectedSequence;
                            LogManager.Log($"Using sequence from UI: {currentSequence.Name}");
                        }
                    }

                    if (currentSequence == null)
                    {
                        LogManager.Log("No sequence found, action will be skipped");
                        return;
                    }
                }

                LogManager.Log($"Executing variable action: {ActionType} for variable {VariableName}");

                // Get or create the variable
                var variable = currentSequence.FindVariableByName(VariableName);

                switch (ActionType)
                {
                    case VariableActionType.SetValue:
                        // Set the variable value based on the current type
                        if (variable == null)
                        {
                            // Create a new variable with the appropriate type based on the value
                            VariableType typeToCreate = DetermineVariableType(Value);
                            currentSequence.SetVariable(VariableName, Value ?? string.Empty, typeToCreate);
                            LogManager.Log($"New variable {VariableName} created as {typeToCreate} with value '{Value}'");
                        }
                        else
                        {
                            // Set value for existing variable
                            variable.SetValueFromString(Value ?? string.Empty);
                            LogManager.Log($"Variable {VariableName} set to value '{Value}'");
                        }
                        break;

                    case VariableActionType.Increment:
                        // Try to increment the variable directly
                        bool incSuccess = currentSequence.IncrementVariable(VariableName, IncrementValue);
                        if (incSuccess)
                        {
                            LogManager.Log($"Variable {VariableName} increased by {IncrementValue}");
                        }
                        else
                        {
                            // If it fails, create a new variable or convert the type
                            if (variable != null)
                            {
                                // Variable exists but is not a number variable - convert
                                variable.Type = VariableType.Number;
                                variable.NumberValue = IncrementValue; // Start with the increment value
                                LogManager.Log($"Variable {VariableName} converted to number variable and set to {IncrementValue}");
                            }
                            else
                            {
                                // Variable doesn't exist - create it directly with SetVariable
                                currentSequence.SetVariable(VariableName, IncrementValue.ToString(), VariableType.Number);
                                LogManager.Log($"New number variable {VariableName} created with value {IncrementValue}");
                            }
                        }
                        break;

                    case VariableActionType.Decrement:
                        // Try to decrement the variable directly (negative increment value)
                        bool decSuccess = currentSequence.IncrementVariable(VariableName, -IncrementValue);
                        if (decSuccess)
                        {
                            LogManager.Log($"Variable {VariableName} decreased by {IncrementValue}");
                        }
                        else
                        {
                            // If it fails, create a new variable or convert the type
                            if (variable != null)
                            {
                                // Variable exists but is not a number variable - convert
                                variable.Type = VariableType.Number;
                                variable.NumberValue = -IncrementValue; // Start with the negative increment value
                                LogManager.Log($"Variable {VariableName} converted to number variable and set to {-IncrementValue}");
                            }
                            else
                            {
                                // Variable doesn't exist - create it directly with SetVariable
                                currentSequence.SetVariable(VariableName, (-IncrementValue).ToString(), VariableType.Number);
                                LogManager.Log($"New number variable {VariableName} created with value {-IncrementValue}");
                            }
                        }
                        break;

                    case VariableActionType.AppendText:
                        if (variable != null && variable.Type == VariableType.Text)
                        {
                            variable.TextValue += Value ?? string.Empty;
                            LogManager.Log($"Text '{Value}' appended to variable {VariableName}, new value: '{variable.TextValue}'");
                        }
                        else if (variable != null)
                        {
                            // If the variable exists but is not text, convert
                            string currentValue = variable.GetValueAsString();
                            variable.Type = VariableType.Text;
                            variable.TextValue = currentValue + (Value ?? string.Empty);
                            LogManager.Log($"Variable {VariableName} converted to text variable and text '{Value}' appended");
                        }
                        else
                        {
                            // If the variable doesn't exist, create it
                            currentSequence.SetVariable(VariableName, Value ?? string.Empty, VariableType.Text);
                            LogManager.Log($"New text variable {VariableName} created with value '{Value}'");
                        }
                        break;

                    case VariableActionType.ClearValue:
                        if (variable != null)
                        {
                            switch (variable.Type)
                            {
                                case VariableType.Text:
                                    variable.TextValue = string.Empty;
                                    break;
                                case VariableType.Number:
                                    variable.NumberValue = 0;
                                    break;
                                case VariableType.Boolean:
                                    variable.BoolValue = false;
                                    break;
                                case VariableType.List:
                                    variable.ListValue = string.Empty;
                                    break;
                            }
                            LogManager.Log($"Variable {VariableName} cleared");
                        }
                        else
                        {
                            // If the variable doesn't exist, create it
                            currentSequence.SetVariable(VariableName, string.Empty, VariableType.Text);
                            LogManager.Log($"New empty variable {VariableName} created");
                        }
                        break;

                    case VariableActionType.ToggleBoolean:
                        if (variable != null && variable.Type == VariableType.Boolean)
                        {
                            variable.ToggleBoolValue();
                            LogManager.Log($"Boolean variable {VariableName} toggled to {variable.BoolValue}");
                        }
                        else if (variable != null)
                        {
                            // Convert to boolean and initialize
                            variable.Type = VariableType.Boolean;
                            variable.BoolValue = true; // Default to true when toggling a non-boolean
                            LogManager.Log($"Variable {VariableName} converted to boolean and set to true");
                        }
                        else
                        {
                            // Create new boolean variable defaulting to true
                            var newVar = new SequenceVariable
                            {
                                Name = VariableName,
                                Type = VariableType.Boolean,
                                BoolValue = true
                            };
                            currentSequence.Variables.Add(newVar);
                            LogManager.Log($"New boolean variable {VariableName} created with value true");
                        }
                        break;

                    case VariableActionType.AddListItem:
                        if (variable != null && variable.Type == VariableType.List)
                        {
                            variable.AddListItem(Value ?? string.Empty);
                            LogManager.Log($"Item '{Value}' added to list variable {VariableName}");
                        }
                        else if (variable != null)
                        {
                            // Convert to list and add item
                            string oldValue = variable.GetValueAsString();
                            variable.Type = VariableType.List;

                            // If old value exists, use it as the first item
                            if (!string.IsNullOrEmpty(oldValue))
                                variable.ListValue = oldValue;

                            variable.AddListItem(Value ?? string.Empty);
                            LogManager.Log($"Variable {VariableName} converted to list and item '{Value}' added");
                        }
                        else
                        {
                            // Create new list with first item
                            var newVar = new SequenceVariable
                            {
                                Name = VariableName,
                                Type = VariableType.List,
                                ListValue = Value ?? string.Empty
                            };
                            currentSequence.Variables.Add(newVar);
                            LogManager.Log($"New list variable {VariableName} created with first item '{Value}'");
                        }
                        break;

                    case VariableActionType.RemoveListItem:
                        if (variable != null && variable.Type == VariableType.List)
                        {
                            if (variable.RemoveListItemAt(ListIndex))
                            {
                                LogManager.Log($"Item at index {ListIndex} removed from list variable {VariableName}");
                            }
                            else
                            {
                                LogManager.Log($"Failed to remove item at index {ListIndex} from list {VariableName}");
                            }
                        }
                        else
                        {
                            LogManager.Log($"Cannot remove list item: {VariableName} is not a list or doesn't exist");
                        }
                        break;

                    case VariableActionType.ClearList:
                        if (variable != null && variable.Type == VariableType.List)
                        {
                            variable.ClearList();
                            LogManager.Log($"List variable {VariableName} cleared");
                        }
                        else if (variable != null)
                        {
                            // Convert to empty list
                            variable.Type = VariableType.List;
                            variable.ListValue = string.Empty;
                            LogManager.Log($"Variable {VariableName} converted to empty list");
                        }
                        else
                        {
                            // Create new empty list
                            var newVar = new SequenceVariable
                            {
                                Name = VariableName,
                                Type = VariableType.List,
                                ListValue = string.Empty
                            };
                            currentSequence.Variables.Add(newVar);
                            LogManager.Log($"New empty list variable {VariableName} created");
                        }
                        break;

                    case VariableActionType.AddTableRow:
                        if (variable != null && variable.Type == VariableType.List)
                        {
                            variable.AddTableRow(Value ?? string.Empty);
                            LogManager.Log($"Row '{Value}' added to table variable {VariableName}");
                        }
                        else if (variable != null)
                        {
                            // Convert to table (list with newlines) and add row
                            string oldValue = variable.GetValueAsString();
                            variable.Type = VariableType.List;

                            // If old value exists, use it as the first row
                            if (!string.IsNullOrEmpty(oldValue))
                                variable.ListValue = oldValue;

                            variable.AddTableRow(Value ?? string.Empty);
                            LogManager.Log($"Variable {VariableName} converted to table and row '{Value}' added");
                        }
                        else
                        {
                            // Create new table with first row
                            var newVar = new SequenceVariable
                            {
                                Name = VariableName,
                                Type = VariableType.List,
                                ListValue = Value ?? string.Empty
                            };
                            currentSequence.Variables.Add(newVar);
                            LogManager.Log($"New table variable {VariableName} created with first row '{Value}'");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in variable action: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines the appropriate variable type based on a string value
        /// </summary>
        private VariableType DetermineVariableType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return VariableType.Text;

            // Check if it's a boolean
            if (bool.TryParse(value, out _) ||
                value.ToLower() == "yes" || value.ToLower() == "no" ||
                value.ToLower() == "y" || value.ToLower() == "n" ||
                value == "1" || value == "0" ||
                value.ToLower() == "on" || value.ToLower() == "off")
            {
                return VariableType.Boolean;
            }

            // Check if it's a number
            if (int.TryParse(value, out _))
                return VariableType.Number;

            // Check if it looks like a list (contains semicolons)
            if (value.Contains(';'))
                return VariableType.List;

            // Default to text
            return VariableType.Text;
        }

        /// <summary>
        /// Creates a copy of this variable action
        /// </summary>
        public override ActionBase Clone()
        {
            return new VariableAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                VariableName = this.VariableName,
                Value = this.Value,
                IncrementValue = this.IncrementValue,
                ActionType = this.ActionType,
                ListIndex = this.ListIndex
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return ActionType switch
            {
                VariableActionType.SetValue => $"Variable: Set '{VariableName}' to '{Value}'",
                VariableActionType.Increment => $"Variable: Increase '{VariableName}' by {IncrementValue}",
                VariableActionType.Decrement => $"Variable: Decrease '{VariableName}' by {IncrementValue}",
                VariableActionType.AppendText => $"Variable: Append '{Value}' to '{VariableName}'",
                VariableActionType.ClearValue => $"Variable: Clear '{VariableName}'",
                VariableActionType.ToggleBoolean => $"Variable: Toggle boolean '{VariableName}'",
                VariableActionType.AddListItem => $"Variable: Add item '{Value}' to list '{VariableName}'",
                VariableActionType.RemoveListItem => $"Variable: Remove item at index {ListIndex} from list '{VariableName}'",
                VariableActionType.ClearList => $"Variable: Clear list '{VariableName}'",
                VariableActionType.AddTableRow => $"Variable: Add row '{Value}' to table '{VariableName}'",
                _ => $"Variable action: {VariableName}"
            };
        }
    }
}