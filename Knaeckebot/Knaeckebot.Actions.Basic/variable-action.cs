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
        ClearValue
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
        /// Value for the operation (with SetValue, AppendText)
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

                switch (ActionType)
                {
                    case VariableActionType.SetValue:
                        // Simply and directly set the variable value (worked originally)
                        currentSequence.SetVariable(VariableName, Value ?? string.Empty);
                        LogManager.Log($"Variable {VariableName} set to value '{Value}'");
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
                            var incrementVar = currentSequence.FindVariableByName(VariableName);
                            if (incrementVar != null)
                            {
                                // Variable exists but is not a number variable - convert
                                incrementVar.Type = VariableType.Number;
                                incrementVar.NumberValue = IncrementValue; // Start with the increment value
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
                            var decrementVar = currentSequence.FindVariableByName(VariableName);
                            if (decrementVar != null)
                            {
                                // Variable exists but is not a number variable - convert
                                decrementVar.Type = VariableType.Number;
                                decrementVar.NumberValue = -IncrementValue; // Start with the negative increment value
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
                        var appendVar = currentSequence.FindVariableByName(VariableName);
                        if (appendVar != null && appendVar.Type == VariableType.Text)
                        {
                            appendVar.TextValue += Value ?? string.Empty;
                            LogManager.Log($"Text '{Value}' appended to variable {VariableName}, new value: '{appendVar.TextValue}'");
                        }
                        else if (appendVar != null)
                        {
                            // If the variable exists but is not text, convert
                            string currentValue = appendVar.GetValueAsString();
                            appendVar.Type = VariableType.Text;
                            appendVar.TextValue = currentValue + (Value ?? string.Empty);
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
                        var clearVar = currentSequence.FindVariableByName(VariableName);
                        if (clearVar != null)
                        {
                            if (clearVar.Type == VariableType.Text)
                                clearVar.TextValue = string.Empty;
                            else
                                clearVar.NumberValue = 0;
                            LogManager.Log($"Variable {VariableName} cleared");
                        }
                        else
                        {
                            // If the variable doesn't exist, create it
                            currentSequence.SetVariable(VariableName, string.Empty, VariableType.Text);
                            LogManager.Log($"New empty variable {VariableName} created");
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
                ActionType = this.ActionType
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
                _ => $"Variable action: {VariableName}"
            };
        }
    }
}