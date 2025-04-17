using Knaeckebot.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
        private bool _useVariableList = false;
        private string _listVariableName = string.Empty;
        private string _currentItemVariableName = string.Empty;
        private bool _useIndexVariable = false;
        private string _indexVariableName = string.Empty;

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
        /// Indicates whether to iterate through a List variable
        /// </summary>
        public bool UseVariableList
        {
            get => _useVariableList;
            set
            {
                if (_useVariableList != value)
                {
                    _useVariableList = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name of the List variable to iterate through
        /// </summary>
        public string ListVariableName
        {
            get => _listVariableName;
            set
            {
                if (_listVariableName != value)
                {
                    _listVariableName = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name of the variable to store the current item during iteration
        /// </summary>
        public string CurrentItemVariableName
        {
            get => _currentItemVariableName;
            set
            {
                if (_currentItemVariableName != value)
                {
                    _currentItemVariableName = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether to track the current index in a variable
        /// </summary>
        public bool UseIndexVariable
        {
            get => _useIndexVariable;
            set
            {
                if (_useIndexVariable != value)
                {
                    _useIndexVariable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name of the variable to store the current index
        /// </summary>
        public string IndexVariableName
        {
            get => _indexVariableName;
            set
            {
                if (_indexVariableName != value)
                {
                    _indexVariableName = value ?? string.Empty;
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
        /// Executes the loop action with cancellation support
        /// </summary>
        public override void Execute(CancellationToken cancellationToken)
        {
            try
            {
                LogManager.Log($"Starting loop action: {Name}, Max iterations: {MaxIterations}");

                // Get current sequence for variable operations
                var currentSequence = SequenceManager.CurrentSequence;
                if (currentSequence == null)
                {
                    LogManager.Log("No current sequence found for loop action - using the first available");

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

                if (currentSequence == null)
                {
                    LogManager.Log("No sequence found, loop action will be skipped");
                    return;
                }

                // Reset iteration
                CurrentIteration = 0;

                // Check if we should loop through a List variable
                if (UseVariableList)
                {
                    ExecuteListLoop(currentSequence, cancellationToken);
                }
                else
                {
                    ExecuteStandardLoop(cancellationToken);
                }

                LogManager.Log($"Loop action completed after {CurrentIteration} iterations");
            }
            catch (OperationCanceledException)
            {
                LogManager.Log($"Loop action cancelled: {Name}");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in loop action: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Executes a loop that iterates through the items in a List variable
        /// </summary>
        private void ExecuteListLoop(Sequence currentSequence, CancellationToken cancellationToken)
        {
            try
            {
                LogManager.Log($"Starting list loop with variable: {ListVariableName}");

                // Find the List variable
                var listVar = currentSequence.FindVariableByName(ListVariableName);
                if (listVar == null)
                {
                    LogManager.Log($"List variable '{ListVariableName}' not found, aborting loop");
                    return;
                }

                // Ensure it's a List type
                if (listVar.Type != VariableType.List)
                {
                    LogManager.Log($"Variable '{ListVariableName}' is not a List type, aborting loop");
                    return;
                }

                // Get the items from the list
                string[] items = listVar.GetListItems();
                LogManager.Log($"Found {items.Length} items in list variable");

                // Create current item variable if it doesn't exist
                if (!string.IsNullOrEmpty(CurrentItemVariableName))
                {
                    var itemVar = currentSequence.FindVariableByName(CurrentItemVariableName);
                    if (itemVar == null)
                    {
                        currentSequence.SetVariable(CurrentItemVariableName, string.Empty, VariableType.Text);
                        LogManager.Log($"Created current item variable: {CurrentItemVariableName}");
                    }
                }

                // Create index variable if it doesn't exist and is requested
                if (UseIndexVariable && !string.IsNullOrEmpty(IndexVariableName))
                {
                    var indexVar = currentSequence.FindVariableByName(IndexVariableName);
                    if (indexVar == null)
                    {
                        currentSequence.SetVariable(IndexVariableName, "0", VariableType.Number);
                        LogManager.Log($"Created index variable: {IndexVariableName}");
                    }
                }

                // Loop through each item in the list
                for (int i = 0; i < items.Length && i < MaxIterations; i++)
                {
                    // Check cancellation at each iteration
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogManager.Log("List loop action cancelled by user");
                        throw new OperationCanceledException();
                    }

                    CurrentIteration = i + 1;
                    LogManager.Log($"List loop iteration {CurrentIteration}/{items.Length}");

                    // Update current item variable
                    if (!string.IsNullOrEmpty(CurrentItemVariableName))
                    {
                        currentSequence.SetVariable(CurrentItemVariableName, items[i]);
                        LogManager.Log($"Set current item variable {CurrentItemVariableName} to '{items[i]}'");
                    }

                    // Update index variable
                    if (UseIndexVariable && !string.IsNullOrEmpty(IndexVariableName))
                    {
                        currentSequence.SetVariable(IndexVariableName, i.ToString(), VariableType.Number);
                        LogManager.Log($"Set index variable {IndexVariableName} to {i}");
                    }

                    // Check condition if enabled
                    if (UseCondition && !EvaluateCondition())
                    {
                        LogManager.Log("Condition for list loop continuation no longer met, breaking loop");
                        break;
                    }

                    // Execute all actions in the loop
                    ExecuteLoopActions(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in list loop: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Executes a standard loop with a fixed number of iterations
        /// </summary>
        private void ExecuteStandardLoop(CancellationToken cancellationToken)
        {
            // Execute standard loop
            while (CurrentIteration < MaxIterations)
            {
                // Check cancellation at each iteration
                if (cancellationToken.IsCancellationRequested)
                {
                    LogManager.Log("Loop action cancelled by user");
                    throw new OperationCanceledException();
                }

                CurrentIteration++;
                LogManager.Log($"Loop iteration {CurrentIteration}/{MaxIterations}");

                // Check if condition for continuation is NOT met (inverted logic)
                if (UseCondition && !EvaluateCondition())
                {
                    LogManager.Log("Condition for loop continuation no longer met, ending loop");
                    break;
                }

                // Execute all actions in the loop
                ExecuteLoopActions(cancellationToken);
            }
        }

        /// <summary>
        /// Executes all actions in the loop
        /// </summary>
        private void ExecuteLoopActions(CancellationToken cancellationToken)
        {
            foreach (var action in LoopActions.Where(a => a.IsEnabled))
            {
                // Check cancellation before each action in the loop
                if (cancellationToken.IsCancellationRequested)
                {
                    LogManager.Log("Loop action cancelled by user during action execution");
                    throw new OperationCanceledException();
                }

                // Delay before the action
                if (action.DelayBefore > 0)
                {
                    for (int i = 0; i < action.DelayBefore; i += 100)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        int sleepTime = Math.Min(100, action.DelayBefore - i);
                        System.Threading.Thread.Sleep(sleepTime);
                    }
                }

                // Execute action with cancellation token
                action.Execute(cancellationToken);
            }
        }

        /// <summary>
        /// Override base method for backward compatibility
        /// </summary>
        public override void Execute()
        {
            Execute(CancellationToken.None);
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
                Operator = this.Operator,
                UseVariableList = this.UseVariableList,
                ListVariableName = this.ListVariableName,
                CurrentItemVariableName = this.CurrentItemVariableName,
                UseIndexVariable = this.UseIndexVariable,
                IndexVariableName = this.IndexVariableName
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
            if (UseVariableList)
            {
                return $"Loop: Through list '{ListVariableName}', {LoopActions.Count} actions";
            }
            else if (UseCondition)
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