using Knaeckebot.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Action that executes actions based on a condition
    /// </summary>
    public class IfAction : ActionBase
    {
        private ConditionSourceType _leftSourceType = ConditionSourceType.Variable;
        private string _leftVariableName = string.Empty;
        private string _leftCustomText = string.Empty;
        private ConditionSourceType _rightSourceType = ConditionSourceType.Text;
        private string _rightVariableName = string.Empty;
        private string _rightCustomText = string.Empty;
        private ComparisonOperator _operator = ComparisonOperator.Equals;
        private ObservableCollection<ActionBase> _thenActions = new ObservableCollection<ActionBase>();
        private ObservableCollection<ActionBase> _elseActions = new ObservableCollection<ActionBase>();
        private bool _useElseBranch = false;

        /// <summary>
        /// Constructor for the IfAction
        /// </summary>
        public IfAction()
        {
            // Register collection change notifications for debugging
            _thenActions.CollectionChanged += ThenActions_CollectionChanged;
            _elseActions.CollectionChanged += ElseActions_CollectionChanged;

            LogManager.Log($"IfAction created with ID: {Id}", LogLevel.Debug);
        }

        /// <summary>
        /// Handles collection changes in the ThenActions collection
        /// </summary>
        private void ThenActions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogManager.Log($"ThenActions collection changed. Action: {e.Action}, Count: {ThenActions.Count}", LogLevel.Debug);
        }

        /// <summary>
        /// Handles collection changes in the ElseActions collection
        /// </summary>
        private void ElseActions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogManager.Log($"ElseActions collection changed. Action: {e.Action}, Count: {ElseActions.Count}", LogLevel.Debug);
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
        /// List of actions to be executed when the condition is true
        /// </summary>
        public ObservableCollection<ActionBase> ThenActions
        {
            get => _thenActions;
            set
            {
                if (_thenActions != value)
                {
                    // Unregister old event handler
                    if (_thenActions != null)
                    {
                        _thenActions.CollectionChanged -= ThenActions_CollectionChanged;
                    }

                    _thenActions = value;

                    // Register new event handler
                    if (_thenActions != null)
                    {
                        _thenActions.CollectionChanged += ThenActions_CollectionChanged;
                    }

                    OnPropertyChanged();
                    LogManager.Log($"ThenActions collection replaced. New count: {value?.Count ?? 0}", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// List of actions to be executed when the condition is false
        /// </summary>
        public ObservableCollection<ActionBase> ElseActions
        {
            get => _elseActions;
            set
            {
                if (_elseActions != value)
                {
                    // Unregister old event handler
                    if (_elseActions != null)
                    {
                        _elseActions.CollectionChanged -= ElseActions_CollectionChanged;
                    }

                    _elseActions = value;

                    // Register new event handler
                    if (_elseActions != null)
                    {
                        _elseActions.CollectionChanged += ElseActions_CollectionChanged;
                    }

                    OnPropertyChanged();
                    LogManager.Log($"ElseActions collection replaced. New count: {value?.Count ?? 0}", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Indicates whether the else branch should be used
        /// </summary>
        public bool UseElseBranch
        {
            get => _useElseBranch;
            set
            {
                if (_useElseBranch != value)
                {
                    var oldValue = _useElseBranch;
                    _useElseBranch = value;
                    OnPropertyChanged();

                    LogManager.Log($"UseElseBranch changed from {oldValue} to {value} in IfAction {Id.ToString().Substring(0, 8)}", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Executes the if action with cancellation support
        /// </summary>
        public override void Execute(CancellationToken cancellationToken)
        {
            try
            {
                // Check cancellation before starting
                if (cancellationToken.IsCancellationRequested)
                {
                    LogManager.Log("If action cancelled before execution");
                    throw new OperationCanceledException();
                }

                LogManager.Log($"Starting if action: {Name}");

                // Evaluate the condition
                bool conditionResult = EvaluateCondition();
                LogManager.Log($"Condition result: {conditionResult}");

                // Execute then or else branch based on the condition
                if (conditionResult)
                {
                    // Check cancellation again
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();

                    LogManager.Log($"Executing THEN branch with {ThenActions.Count} actions");
                    // Execute all actions in the then branch
                    foreach (var action in ThenActions.Where(a => a.IsEnabled))
                    {
                        // Check cancellation before each action
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogManager.Log("If action cancelled during THEN branch");
                            throw new OperationCanceledException();
                        }

                        // Delay before the action with cancellation support
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
                else if (UseElseBranch)
                {
                    // Check cancellation again
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();

                    LogManager.Log($"Executing ELSE branch with {ElseActions.Count} actions");
                    // Execute all actions in the else branch
                    foreach (var action in ElseActions.Where(a => a.IsEnabled))
                    {
                        // Check cancellation before each action
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogManager.Log("If action cancelled during ELSE branch");
                            throw new OperationCanceledException();
                        }

                        // Delay before the action with cancellation support
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
                else
                {
                    LogManager.Log("Condition is false and no ELSE branch is defined, skipping");
                }

                LogManager.Log($"If action completed: {Name}");
            }
            catch (OperationCanceledException)
            {
                LogManager.Log($"If action cancelled: {Name}");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in if action: {ex.Message}");
                throw;
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
        /// Evaluates the condition
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
        /// Creates a copy of this if action
        /// </summary>
        public override ActionBase Clone()
        {
            LogManager.Log($"Cloning IfAction {Id.ToString().Substring(0, 8)}", LogLevel.Debug);

            var clone = new IfAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                LeftSourceType = this.LeftSourceType,
                LeftVariableName = this.LeftVariableName,
                LeftCustomText = this.LeftCustomText,
                RightSourceType = this.RightSourceType,
                RightVariableName = this.RightVariableName,
                RightCustomText = this.RightCustomText,
                Operator = this.Operator,
                UseElseBranch = this.UseElseBranch
            };

            LogManager.Log($"Created new IfAction with ID {clone.Id.ToString().Substring(0, 8)}", LogLevel.Debug);
            LogManager.Log($"Cloning {ThenActions.Count} THEN actions and {ElseActions.Count} ELSE actions", LogLevel.Debug);

            // Clone then actions
            foreach (var action in this.ThenActions)
            {
                var actionClone = action.Clone();
                clone.ThenActions.Add(actionClone);
                LogManager.Log($"Cloned THEN action {action.Name} to new ID {actionClone.Id.ToString().Substring(0, 8)}", LogLevel.Debug);
            }

            // Clone else actions
            foreach (var action in this.ElseActions)
            {
                var actionClone = action.Clone();
                clone.ElseActions.Add(actionClone);
                LogManager.Log($"Cloned ELSE action {action.Name} to new ID {actionClone.Id.ToString().Substring(0, 8)}", LogLevel.Debug);
            }

            return clone;
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            string representation = $"If: ";

            // Add condition info
            representation += $"{LeftSourceType} {Operator} {RightSourceType}";

            // Add action counts
            representation += $", Then: {ThenActions.Count} actions";

            if (UseElseBranch)
            {
                representation += $", Else: {ElseActions.Count} actions";
            }

            return representation;
        }
    }
}