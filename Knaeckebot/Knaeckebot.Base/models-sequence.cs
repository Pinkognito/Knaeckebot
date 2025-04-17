using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Collections.Generic;
using Knaeckebot.Services;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Represents a sequence of actions
    /// </summary>
    public class Sequence : INotifyPropertyChanged
    {
        private string? _name;
        private string? _description;
        private ObservableCollection<ActionBase> _actions = new ObservableCollection<ActionBase>();
        private ObservableCollection<SequenceVariable> _variables = new ObservableCollection<SequenceVariable>();
        private bool _isRunning;
        private string? _filePath;

        /// <summary>
        /// Unique identifier for the sequence
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Name of the sequence
        /// </summary>
        public string Name
        {
            get => _name ?? string.Empty;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the sequence
        /// </summary>
        public string Description
        {
            get => _description ?? string.Empty;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List of actions in this sequence
        /// </summary>
        public ObservableCollection<ActionBase> Actions
        {
            get => _actions;
            set
            {
                if (_actions != value)
                {
                    _actions = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List of variables in this sequence
        /// </summary>
        public ObservableCollection<SequenceVariable> Variables
        {
            get => _variables;
            set
            {
                if (_variables != value)
                {
                    _variables = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether the sequence is currently running
        /// </summary>
        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// File path where the sequence is saved
        /// </summary>
        [JsonIgnore]
        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Sequence()
        {
            Name = "New Sequence";
            Description = "A new action sequence";
        }

        /// <summary>
        /// Constructor with name
        /// </summary>
        public Sequence(string name)
        {
            Name = name;
            Description = $"Sequence: {name}";
        }

        /// <summary>
        /// Finds a variable by its name
        /// </summary>
        public SequenceVariable? FindVariableByName(string name)
        {
            return Variables.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates or updates a variable with the specified name and value
        /// </summary>
        public SequenceVariable SetVariable(string name, string value, VariableType type = VariableType.Text)
        {
            var variable = FindVariableByName(name);

            if (variable == null)
            {
                variable = new SequenceVariable { Name = name, Type = type };
                Variables.Add(variable);
            }

            variable.SetValueFromString(value);
            return variable;
        }

        /// <summary>
        /// Increments a numeric variable
        /// </summary>
        public bool IncrementVariable(string name, int increment = 1)
        {
            var variable = FindVariableByName(name);

            if (variable == null || variable.Type != VariableType.Number)
            {
                return false;
            }

            variable.NumberValue += increment;
            return true;
        }

        /// <summary>
        /// Executes the entire sequence
        /// </summary>
        public void Execute(CancellationToken cancellationToken = default)
        {
            IsRunning = true;
            LogManager.Log($"Starting sequence: {Name}");

            try
            {
                // Add sequence to call stack if not already there
                if (SequenceManager.CurrentSequence != this)
                {
                    SequenceManager.AddToCallStack(this);
                    LogManager.Log($"Sequence '{Name}' added to call stack");
                }

                // Ensure thread is configured as STA for clipboard operations
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    LogManager.Log("Thread is not in STA mode, executing actions in UI thread");

                    // Execute actions via the UI thread, which is already configured as STA
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExecuteActionsInSequence(cancellationToken);
                    });
                }
                else
                {
                    // Thread is already STA, execute directly
                    ExecuteActionsInSequence(cancellationToken);
                }

                LogManager.Log($"Sequence successfully completed: {Name}");
            }
            catch (OperationCanceledException)
            {
                LogManager.Log($"Sequence execution cancelled: {Name}");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing the sequence: {ex.Message}");
                throw;
            }
            finally
            {
                IsRunning = false;

                // Remove this sequence from the call stack when completed
                SequenceManager.RemoveFromCallStack(this);
            }
        }

        /// <summary>
        /// Executes all actions in the sequence
        /// </summary>
        private void ExecuteActionsInSequence(CancellationToken cancellationToken = default)
        {
            foreach (var action in Actions.Where(a => a.IsEnabled))
            {
                // Check for cancellation before each action
                if (cancellationToken.IsCancellationRequested)
                {
                    LogManager.Log("Execution cancelled before action: " + action.Name);
                    throw new OperationCanceledException();
                }

                LogManager.Log($"Executing action: {action.Name} ({action.GetType().Name})");

                // Execute the delay before the action - consider the full delay time
                if (action.DelayBefore > 0)
                {
                    // Check cancellation during delay instead of sleeping the full time at once
                    for (int i = 0; i < action.DelayBefore; i += 100)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        int sleepTime = Math.Min(100, action.DelayBefore - i);
                        System.Threading.Thread.Sleep(sleepTime);
                    }
                }

                // Execute the action with cancellation token
                action.Execute(cancellationToken);
            }
        }

        /// <summary>
        /// For backwards compatibility
        /// </summary>
        public void Execute()
        {
            Execute(CancellationToken.None);
        }

        /// <summary>
        /// Saves the sequence to a JSON file
        /// </summary>
        /// <param name="filePath">File path where the sequence should be saved</param>
        public void Save(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new ActionBaseJsonConverter() }
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
            FilePath = filePath;
            LogManager.Log($"Sequence saved: {filePath}");
        }

        /// <summary>
        /// Loads a sequence from a JSON file
        /// </summary>
        /// <param name="filePath">File path of the sequence to load</param>
        /// <returns>The loaded sequence</returns>
        public static Sequence Load(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new ActionBaseJsonConverter() }
            };

            LogManager.Log($"Loading sequence from: {filePath}");
            string json = File.ReadAllText(filePath);

            try
            {
                var sequence = JsonSerializer.Deserialize<Sequence>(json, options);
                if (sequence == null)
                {
                    throw new InvalidOperationException("The sequence could not be loaded.");
                }
                sequence.FilePath = filePath;
                LogManager.Log($"Sequence loaded: {sequence.Name} with {sequence.Actions.Count} actions");
                return sequence;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error loading sequence: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a copy of this sequence
        /// </summary>
        public Sequence Clone()
        {
            LogManager.Log($"=== CLONE START for sequence: {this.Name}, ID: {this.Id} ===");

            // IMPORTANT: Name is not changed, no automatic naming here
            var clone = new Sequence
            {
                Name = this.Name, // Name remains the same as the original
                Description = this.Description
            };

            LogManager.Log($"New sequence created: Name={clone.Name}, ID={clone.Id}");
            LogManager.Log($"Original actions: {this.Actions.Count}, Original variables: {this.Variables.Count}");

            // Clone all actions
            foreach (var action in this.Actions)
            {
                var clonedAction = action.Clone();
                clone.Actions.Add(clonedAction);
                LogManager.Log($"  - Action cloned: {action.GetType().Name}, ID: {action.Id} -> {clonedAction.Id}");
            }

            // Also copy variables
            foreach (var variable in this.Variables)
            {
                var clonedVar = new SequenceVariable
                {
                    Name = variable.Name,
                    Type = variable.Type,
                    TextValue = variable.TextValue,
                    NumberValue = variable.NumberValue,
                    Description = variable.Description
                };
                clone.Variables.Add(clonedVar);
                LogManager.Log($"  - Variable cloned: {variable.Name}, Type: {variable.Type}");
            }

            LogManager.Log($"Sequence duplicated: {this.Name} -> {clone.Name}, ID: {this.Id} -> {clone.Id}");
            LogManager.Log($"=== CLONE END ===");

            return clone;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Static class for sequence management
    /// </summary>
    public static class SequenceManager
    {
        /// <summary>
        /// List of all loaded sequences
        /// </summary>
        private static ObservableCollection<Sequence> _sequences = new ObservableCollection<Sequence>();

        /// <summary>
        /// Call stack for sequences to track the current execution hierarchy
        /// </summary>
        private static Stack<Sequence> _callStack = new Stack<Sequence>();

        /// <summary>
        /// Blocks recursive calls of the same sequence
        /// </summary>
        private static bool _preventRecursion = true;

        /// <summary>
        /// Initialize sequence list
        /// </summary>
        /// <param name="sequences">The list of sequences to manage</param>
        public static void Initialize(ObservableCollection<Sequence> sequences)
        {
            _sequences = sequences;
        }

        /// <summary>
        /// Returns the currently executing sequence
        /// </summary>
        public static Sequence? CurrentSequence => _callStack.Count > 0 ? _callStack.Peek() : null;

        /// <summary>
        /// Adds a sequence to the call stack
        /// </summary>
        public static void AddToCallStack(Sequence sequence)
        {
            // Prevent recursive calls
            if (_preventRecursion && _callStack.Contains(sequence))
            {
                LogManager.Log($"Recursive call of sequence '{sequence.Name}' prevented");
                throw new InvalidOperationException($"Recursive call of sequence '{sequence.Name}' is not allowed.");
            }

            _callStack.Push(sequence);
            LogManager.Log($"Sequence '{sequence.Name}' added to call stack. Stack depth: {_callStack.Count}");
        }

        /// <summary>
        /// Removes a sequence from the call stack
        /// </summary>
        public static void RemoveFromCallStack(Sequence sequence)
        {
            if (_callStack.Count > 0 && _callStack.Peek() == sequence)
            {
                _callStack.Pop();
                LogManager.Log($"Sequence '{sequence.Name}' removed from call stack. Stack depth: {_callStack.Count}");
            }
        }

        /// <summary>
        /// Finds a sequence by its name
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to find</param>
        /// <returns>The found sequence or null</returns>
        public static Sequence? FindSequenceByName(string sequenceName)
        {
            LogManager.Log($"Searching for sequence with name: {sequenceName}");

            if (_sequences == null || _sequences.Count == 0)
            {
                LogManager.Log("No sequences available to search");
                return null;
            }

            var sequence = _sequences.FirstOrDefault(s => s.Name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase));

            if (sequence == null)
            {
                LogManager.Log($"No sequence with name '{sequenceName}' found");
                return null;
            }

            LogManager.Log($"Sequence '{sequenceName}' found");
            return sequence;
        }

        /// <summary>
        /// Executes a sequence by its name
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to execute</param>
        /// <returns>True if the sequence was found and executed</returns>
        public static bool ExecuteSequenceByName(string sequenceName)
        {
            var sequence = FindSequenceByName(sequenceName);
            if (sequence == null) return false;

            LogManager.Log($"Executing sequence: {sequenceName}");

            try
            {
                // Add sequence to call stack
                AddToCallStack(sequence);

                // Execute sequence in STA thread
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    // Execute via UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        sequence.Execute();
                    });
                }
                else
                {
                    // Direct call in current thread
                    sequence.Execute();
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing sequence '{sequenceName}': {ex.Message}");

                // Ensure the sequence is removed from the stack if an error occurs
                RemoveFromCallStack(sequence);

                return false;
            }
        }

        /// <summary>
        /// Executes a sequence and passes variables
        /// </summary>
        public static bool ExecuteSequenceWithVariables(string sequenceName, Dictionary<string, string> variables)
        {
            var sequence = FindSequenceByName(sequenceName);
            if (sequence == null) return false;

            LogManager.Log($"Executing sequence with variables: {sequenceName}");

            try
            {
                // Add sequence to call stack
                AddToCallStack(sequence);

                // Set variables
                foreach (var kvp in variables)
                {
                    sequence.SetVariable(kvp.Key, kvp.Value);
                }

                // Execute sequence in STA thread
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    // Execute via UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        sequence.Execute();
                    });
                }
                else
                {
                    // Direct call in current thread
                    sequence.Execute();
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing sequence with variables '{sequenceName}': {ex.Message}");

                // Ensure the sequence is removed from the stack if an error occurs
                RemoveFromCallStack(sequence);

                return false;
            }
        }
    }
}