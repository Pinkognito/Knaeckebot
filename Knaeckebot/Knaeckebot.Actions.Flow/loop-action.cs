using Knaeckebot.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Possible condition sources for comparison
    /// </summary>
    public enum ConditionSourceType
    {
        Variable,
        Clipboard,
        Text
    }

    /// <summary>
    /// Possible comparison operators
    /// </summary>
    public enum ComparisonOperator
    {
        Equals,
        Contains,
        StartsWith,
        EndsWith,
        NotEquals
    }

    /// <summary>
    /// Action that executes a loop of actions
    /// </summary>
    public class LoopAction : ActionBase
    {
        private int _maxIterations = 10;
        private bool _useCondition = false;
        private ConditionSourceType _leftSourceType = ConditionSourceType.Variable;
        private string _leftVariableName = string.Empty;
        private string _leftCustomText = string.Empty;
        private ConditionSourceType _rightSourceType = ConditionSourceType.Text;
        private string _rightVariableName = string.Empty;
        private string _rightCustomText = string.Empty;
        private ComparisonOperator _operator = ComparisonOperator.Equals;
        private ObservableCollection<ActionBase> _loopActions = new ObservableCollection<ActionBase>();
        private int _currentIteration = 0;

        /// <summary>
        /// Maximum number of loop iterations
        /// </summary>
        public int MaxIterations
        {
            get => _maxIterations;
            set
            {
                if (_maxIterations != value)
                {
                    _maxIterations = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether a condition should be used for loop termination
        /// </summary>
        public bool UseCondition
        {
            get => _useCondition;
            set
            {
                if (_useCondition != value)
                {
                    _useCondition = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of source for the left side of the comparison
        /// </summary>
        public ConditionSourceType LeftSourceType
        {
            get => _leftSourceType;
            set
            {
                if (_leftSourceType != value)
                {
                    _leftSourceType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name of the variable for the left side of the comparison (when SourceType = Variable)
        /// </summary>
        public string LeftVariableName
        {
            get => _leftVariableName;
            set
            {
                if (_leftVariableName != value)
                {
                    _leftVariableName = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Custom text for the left side of the comparison (when SourceType = Text)
        /// </summary>
        public string LeftCustomText
        {
            get => _leftCustomText;
            set
            {
                if (_leftCustomText != value)
                {
                    _leftCustomText = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of source for the right side of the comparison
        /// </summary>
        public ConditionSourceType RightSourceType
        {
            get => _rightSourceType;
            set
            {
                if (_rightSourceType != value)
                {
                    _rightSourceType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name of the variable for the right side of the comparison (when SourceType = Variable)
        /// </summary>
        public string RightVariableName
        {
            get => _rightVariableName;
            set
            {
                if (_rightVariableName != value)
                {
                    _rightVariableName = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Custom text for the right side of the comparison (when SourceType = Text)
        /// </summary>
        public string RightCustomText
        {
            get => _rightCustomText;
            set
            {
                if (_rightCustomText != value)
                {
                    _rightCustomText = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Comparison operator
        /// </summary>
        public ComparisonOperator Operator
        {
            get => _operator;
            set
            {
                if (_operator != value)
                {
                    _operator = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List of actions to be executed in the loop
        /// </summary>
        public ObservableCollection<ActionBase> LoopActions
        {
            get => _loopActions;
            set
            {
                if (_loopActions != value)
                {
                    _loopActions = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current iteration of the loop (used during execution)
        /// </summary>
        public int CurrentIteration
        {
            get => _currentIteration;
            private set
            {
                if (_currentIteration != value)
                {
                    _currentIteration = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the loop action
        /// </summary>
        public override void Execute()
        {
            try
            {
                LogManager.Log($"Starting loop action: {Name}, Max iterations: {MaxIterations}");

                // Reset iteration
                CurrentIteration = 0;

                // Execute loop
                while (CurrentIteration < MaxIterations)
                {
                    CurrentIteration++;
                    LogManager.Log($"Loop iteration {CurrentIteration}/{MaxIterations}");

                    // Check if condition for termination is met
                    if (UseCondition && EvaluateCondition())
                    {
                        LogManager.Log("Condition for loop termination met, ending loop");
                        break;
                    }

                    // Execute all actions in the loop
                    foreach (var action in LoopActions.Where(a => a.IsEnabled))
                    {
                        // Delay before the action
                        if (action.DelayBefore > 0)
                        {
                            System.Threading.Thread.Sleep(action.DelayBefore);
                        }

                        // Execute action
                        action.Execute();
                    }
                }

                LogManager.Log($"Loop action completed after {CurrentIteration} iterations");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in loop action: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Evaluates the termination condition
        /// </summary>
        private bool EvaluateCondition()
        {
            try
            {
                // Determine values for left and right side
                string leftValue = GetValueFromSource(LeftSourceType, LeftVariableName, LeftCustomText);
                string rightValue = GetValueFromSource(RightSourceType, RightVariableName, RightCustomText);

                LogManager.Log($"Comparing: '{leftValue}' {Operator} '{rightValue}'");

                // Perform comparison
                bool result = Operator switch
                {
                    ComparisonOperator.Equals => leftValue.Equals(rightValue),
                    ComparisonOperator.Contains => leftValue.Contains(rightValue),
                    ComparisonOperator.StartsWith => leftValue.StartsWith(rightValue),
                    ComparisonOperator.EndsWith => leftValue.EndsWith(rightValue),
                    ComparisonOperator.NotEquals => !leftValue.Equals(rightValue),
                    _ => false
                };

                LogManager.Log($"Comparison result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error evaluating condition: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a value from the specified source
        /// </summary>
        private string GetValueFromSource(ConditionSourceType sourceType, string variableName, string customText)
        {
            try
            {
                switch (sourceType)
                {
                    case ConditionSourceType.Variable:
                        // Get value from a sequence variable
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
                        }

                        if (currentSequence != null)
                        {
                            var variable = currentSequence.FindVariableByName(variableName);
                            if (variable != null)
                            {
                                return variable.GetValueAsString();
                            }

                            LogManager.Log($"Variable '{variableName}' not found");
                        }
                        else
                        {
                            LogManager.Log("No sequence found");
                        }

                        return string.Empty;

                    case ConditionSourceType.Clipboard:
                        // Get value from clipboard
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            return System.Windows.Clipboard.GetText() ?? string.Empty;
                        }
                        return string.Empty;

                    case ConditionSourceType.Text:
                        // Return custom text
                        return customText ?? string.Empty;

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error retrieving value: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates a copy of this loop action
        /// </summary>
        public override ActionBase Clone()
        {
            var clone = new LoopAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                MaxIterations = this.MaxIterations,
                UseCondition = this.UseCondition,
                LeftSourceType = this.LeftSourceType,
                LeftVariableName = this.LeftVariableName,
                LeftCustomText = this.LeftCustomText,
                RightSourceType = this.RightSourceType,
                RightVariableName = this.RightVariableName,
                RightCustomText = this.RightCustomText,
                Operator = this.Operator
            };

            // Clone loop actions
            foreach (var action in this.LoopActions)
            {
                clone.LoopActions.Add(action.Clone());
            }

            return clone;
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            if (UseCondition)
            {
                return $"Loop: {LoopActions.Count} actions, Max: {MaxIterations}, with condition";
            }
            else
            {
                return $"Loop: {LoopActions.Count} actions, Max: {MaxIterations}";
            }
        }
    }
}