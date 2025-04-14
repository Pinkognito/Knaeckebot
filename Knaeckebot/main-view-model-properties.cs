using Knaeckebot.Models;
using Knaeckebot.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Knaeckebot.ViewModels
{
    public partial class MainViewModel
    {
        #region Properties

        /// <summary>
        /// List of all available sequences
        /// </summary>
        public ObservableCollection<Sequence> Sequences
        {
            get => _sequences;
            set
            {
                if (_sequences != value)
                {
                    _sequences = value;
                    // Update SequenceManager with the new sequence list
                    SequenceManager.Initialize(_sequences);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List of all selected sequences (for multiple selection)
        /// </summary>
        public ObservableCollection<Sequence> SelectedSequences
        {
            get => _selectedSequences;
            set
            {
                if (_selectedSequences != value)
                {
                    _selectedSequences = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AreSequencesSelected));
                    OnPropertyChanged(nameof(SelectedSequencesCount));
                }
            }
        }

        /// <summary>
        /// Number of selected sequences
        /// </summary>
        public int SelectedSequencesCount => SelectedSequences.Count;

        /// <summary>
        /// Indicates whether at least one sequence is selected (for multiple selection)
        /// </summary>
        public bool AreSequencesSelected => SelectedSequences.Count > 0;

        /// <summary>
        /// Currently selected sequence
        /// </summary>
        public Sequence? SelectedSequence
        {
            get => _selectedSequence;
            set
            {
                if (_selectedSequence != value)
                {
                    LogManager.Log($"=== SELECTED SEQUENCE CHANGE ===", LogLevel.Debug);
                    LogManager.Log($"SelectedSequence change from: {_selectedSequence?.Name ?? "null"}, ID: {_selectedSequence?.Id.ToString() ?? "null"}", LogLevel.Debug);
                    LogManager.Log($"SelectedSequence change to: {value?.Name ?? "null"}, ID: {value?.Id.ToString() ?? "null"}", LogLevel.Debug);

                    // Log SelectedSequences before the change
                    LogManager.Log($"SelectedSequences before change: {SelectedSequences.Count}", LogLevel.Debug);
                    foreach (var seq in SelectedSequences)
                    {
                        LogManager.Log($"  - In selection: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
                    }

                    _selectedSequence = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSequenceSelected));
                    OnPropertyChanged(nameof(HasVariables));
                    OnPropertyChanged(nameof(VariableNames));

                    LogManager.Log($"Sequence selected: {value?.Name ?? "none"}", LogLevel.Debug);

                    // If a new sequence is selected and it's not already in SelectedSequences,
                    // it should be added - but only if it's not null
                    if (value != null)
                    {
                        bool isInSelectedSequences = SelectedSequences.Contains(value);
                        LogManager.Log($"New SelectedSequence is in SelectedSequences: {isInSelectedSequences}", LogLevel.Debug);

                        if (!isInSelectedSequences)
                        {
                            LogManager.Log($"Adding new SelectedSequence {value.Name} to SelectedSequences", LogLevel.Debug);
                            SelectedSequences.Add(value);
                        }
                    }

                    // Changing the sequence also resets the current action
                    SelectedAction = null;
                    SelectedActions.Clear();
                    SelectedVariable = null;

                    // Log SelectedSequences after the change
                    LogManager.Log($"SelectedSequences after change: {SelectedSequences.Count}", LogLevel.Debug);
                    foreach (var seq in SelectedSequences)
                    {
                        LogManager.Log($"  - In selection: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
                    }

                    LogManager.Log($"=== SELECTED SEQUENCE CHANGE END ===", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Currently selected variable
        /// </summary>
        public SequenceVariable? SelectedVariable
        {
            get => _selectedVariable;
            set
            {
                if (_selectedVariable != value)
                {
                    _selectedVariable = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsVariableSelected));
                }
            }
        }

        /// <summary>
        /// List of available variable names in the current sequence
        /// </summary>
        public List<string> VariableNames
        {
            get
            {
                if (SelectedSequence == null || SelectedSequence.Variables == null)
                    return new List<string>();

                return SelectedSequence.Variables.Select(v => v.Name).ToList();
            }
        }


        /// <summary>
        /// Currently selected action (single selection for editing in detail area)
        /// </summary>
        public ActionBase? SelectedAction
        {
            get => _selectedAction;
            set
            {
                // Avoid recursive calls
                if (_isChangingAction)
                {
                    LogManager.Log("!!! WARNING: Recursive call detected in SelectedAction and prevented", LogLevel.Warning);
                    return;
                }

                _isChangingAction = true;

                try
                {
                    var oldAction = _selectedAction;

                    // If the old action is a ClipboardAction, save its state in the cache
                    if (oldAction is ClipboardAction oldClipAction)
                    {
                        // Save the state of the old ClipboardAction in the cache before it's changed
                        _actionStateCache[oldClipAction.Id] = new
                        {
                            UseVariable = oldClipAction.UseVariable,
                            VariableName = oldClipAction.VariableName,
                            Text = oldClipAction.Text
                        };

                        LogManager.Log($"### CACHE: State saved for ClipboardAction {oldClipAction.Id.ToString().Substring(0, 8)}. UseVariable={oldClipAction.UseVariable}, VariableName={oldClipAction.VariableName}", LogLevel.Debug);
                    }

                    if (_selectedAction != value)
                    {
                        _selectedAction = value;

                        // Debug information about the old and new action
                        if (oldAction != null)
                        {
                            LogManager.Log($"Old action: {oldAction.GetType().Name} ID: {oldAction.Id.ToString().Substring(0, 8)}, Name: {oldAction.Name}", LogLevel.Debug);
                            if (oldAction is KeyboardAction oldKeyAction)
                            {
                                LogManager.LogKeyboardAction(oldKeyAction, "Old selected action");
                            }
                            else if (oldAction is ClipboardAction oldClipAction2)
                            {
                                LogManager.Log($"Old ClipboardAction: UseVariable={oldClipAction2.UseVariable}, VariableName={oldClipAction2.VariableName}", LogLevel.Debug);
                            }
                        }

                        if (value != null)
                        {
                            LogManager.Log($"New action selected: {value.GetType().Name} ID: {value.Id.ToString().Substring(0, 8)}, Name: {value.Name}", LogLevel.Debug);
                            if (value is KeyboardAction newKeyAction)
                            {
                                LogManager.LogKeyboardAction(newKeyAction, "New selected action");
                            }
                            else if (value is ClipboardAction newClipAction)
                            {
                                LogManager.Log($"New ClipboardAction: UseVariable={newClipAction.UseVariable}, VariableName={newClipAction.VariableName}", LogLevel.Debug);

                                // Check if we have this state in the cache and if it has changed
                                if (_actionStateCache.TryGetValue(newClipAction.Id, out var cachedState))
                                {
                                    var state = (dynamic)cachedState;
                                    if (state.UseVariable != newClipAction.UseVariable)
                                    {
                                        LogManager.Log($"!!! CRITICAL: Cache and action do not match for {newClipAction.Id.ToString().Substring(0, 8)}!", LogLevel.Error);
                                        LogManager.Log($"!!! Cache: UseVariable={state.UseVariable}, Action: UseVariable={newClipAction.UseVariable}", LogLevel.Error);

                                        // Restore state from cache
                                        LogManager.Log($"### RESTORING UseVariable={state.UseVariable} for ClipboardAction {newClipAction.Id.ToString().Substring(0, 8)}", LogLevel.Warning);

                                        // First set UseVariable, then VariableName to avoid problems
                                        bool oldUseVariable = newClipAction.UseVariable;
                                        newClipAction.UseVariable = state.UseVariable;

                                        // Check if the change was made
                                        if (newClipAction.UseVariable != state.UseVariable)
                                        {
                                            LogManager.Log($"!!! CRITICAL: UseVariable could not be restored! Now: {newClipAction.UseVariable}", LogLevel.Error);
                                        }

                                        newClipAction.VariableName = state.VariableName;
                                        LogManager.Log($"### After restoration: UseVariable={newClipAction.UseVariable}, VariableName={newClipAction.VariableName}", LogLevel.Debug);
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogManager.Log("No action selected (null)", LogLevel.Debug);
                        }

                        OnPropertyChanged();
                        OnPropertyChanged(nameof(IsActionSelected));

                        // Important: Force UI update - if UI controls are already displayed,
                        // a refresh must be performed here
                        System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            ForceUIUpdate();
                        }, System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"!!! ERROR in SelectedAction Property: {ex.Message}", LogLevel.Error);
                    LogManager.Log($"!!! StackTrace: {ex.StackTrace}", LogLevel.Error);
                }
                finally
                {
                    _isChangingAction = false;
                }
            }
        }

        /// <summary>
        /// Collection of all currently selected actions (multiple selection for group operations)
        /// </summary>
        public ObservableCollection<ActionBase> SelectedActions
        {
            get => _selectedActions;
            set
            {
                if (_selectedActions != value)
                {
                    _selectedActions = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AreActionsSelected));
                    OnPropertyChanged(nameof(SelectedActionsCount));

                    // If actions are selected, set the first selected action as SelectedAction
                    // for the detail view
                    if (_selectedActions.Count > 0 && SelectedAction != _selectedActions[0])
                    {
                        SelectedAction = _selectedActions[0];
                    }
                    else if (_selectedActions.Count == 0 && SelectedAction != null)
                    {
                        SelectedAction = null;
                    }

                    LogManager.Log($"Selected actions: {_selectedActions.Count}", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Number of selected actions
        /// </summary>
        public int SelectedActionsCount => SelectedActions.Count;

        /// <summary>
        /// Indicates whether at least one action is selected
        /// </summary>
        public bool AreActionsSelected => SelectedActions.Count > 0;

        /// <summary>
        /// Indicates whether a variable is selected
        /// </summary>
        public bool IsVariableSelected => SelectedVariable != null;

        /// <summary>
        /// Indicates whether the current sequence has variables
        /// </summary>
        public bool HasVariables => SelectedSequence != null && SelectedSequence.Variables.Count > 0;

        /// <summary>
        /// Returns the saved keys of the current action (for text display)
        /// </summary>
        public string SavedKeys
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null)
                {
                    return string.Join(" + ", keyboardAction.Keys.Select(k => new KeyItem(k).DisplayValue));
                }
                return "(No keys assigned)";
            }
        }

        /// <summary>
        /// Indicates whether the Ctrl key is pressed in the current key combination
        /// </summary>
        public bool IsCtrlModifierPressed
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null)
                {
                    bool isPressed = keyboardAction.Keys.Contains(Key.LeftCtrl) || keyboardAction.Keys.Contains(Key.RightCtrl);
                    LogManager.Log($"IsCtrlModifierPressed for action {keyboardAction.Id.ToString().Substring(0, 8)}: {isPressed}", LogLevel.KeyDebug);
                    return isPressed;
                }
                return false;
            }
            set
            {
                if (SelectedAction is KeyboardAction keyboardAction)
                {
                    LogManager.Log($"IsCtrlModifierPressed being set to: {value} for action {keyboardAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    LogManager.LogKeyArray(keyboardAction.Keys, "Before change");

                    var keys = new List<Key>();

                    // Keep all existing keys except Ctrl
                    if (keyboardAction.Keys != null)
                    {
                        foreach (var key in keyboardAction.Keys)
                        {
                            if (key != Key.LeftCtrl && key != Key.RightCtrl)
                            {
                                keys.Add(key);
                            }
                        }
                    }

                    // Add Ctrl if activated
                    if (value)
                    {
                        keys.Insert(0, Key.LeftCtrl);
                    }

                    keyboardAction.Keys = keys.ToArray();
                    // Update tracker
                    _actionKeysTracker[keyboardAction.Id] = keys.ToArray();

                    LogManager.LogKeyArray(keyboardAction.Keys, "After change");

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainKey));
                    OnPropertyChanged(nameof(MainKeyItem));
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));

                    // Update ViewModels
                    KeyboardActionViewModel.KeyboardAction = keyboardAction;
                    KeyCombinationViewModel.KeyboardAction = keyboardAction;
                }
            }
        }

        /// <summary>
        /// Indicates whether the Alt key is pressed in the current key combination
        /// </summary>
        public bool IsAltModifierPressed
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null)
                {
                    bool isPressed = keyboardAction.Keys.Contains(Key.LeftAlt) || keyboardAction.Keys.Contains(Key.RightAlt);
                    LogManager.Log($"IsAltModifierPressed for action {keyboardAction.Id.ToString().Substring(0, 8)}: {isPressed}", LogLevel.KeyDebug);
                    return isPressed;
                }
                return false;
            }
            set
            {
                if (SelectedAction is KeyboardAction keyboardAction)
                {
                    LogManager.Log($"IsAltModifierPressed being set to: {value} for action {keyboardAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    LogManager.LogKeyArray(keyboardAction.Keys, "Before change");

                    var keys = new List<Key>();

                    // Keep all existing keys except Alt
                    if (keyboardAction.Keys != null)
                    {
                        foreach (var key in keyboardAction.Keys)
                        {
                            if (key != Key.LeftAlt && key != Key.RightAlt)
                            {
                                keys.Add(key);
                            }
                        }
                    }

                    // Add Alt if activated (after Ctrl, if present)
                    if (value)
                    {
                        int insertIndex = keys.Contains(Key.LeftCtrl) || keys.Contains(Key.RightCtrl) ? 1 : 0;
                        keys.Insert(insertIndex, Key.LeftAlt);
                    }

                    keyboardAction.Keys = keys.ToArray();
                    // Update tracker
                    _actionKeysTracker[keyboardAction.Id] = keys.ToArray();

                    LogManager.LogKeyArray(keyboardAction.Keys, "After change");

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainKey));
                    OnPropertyChanged(nameof(MainKeyItem));
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));

                    // Update ViewModels
                    KeyboardActionViewModel.KeyboardAction = keyboardAction;
                    KeyCombinationViewModel.KeyboardAction = keyboardAction;
                }
            }
        }

        /// <summary>
        /// Indicates whether the Shift key is pressed in the current key combination
        /// </summary>
        public bool IsShiftModifierPressed
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null)
                {
                    bool isPressed = keyboardAction.Keys.Contains(Key.LeftShift) || keyboardAction.Keys.Contains(Key.RightShift);
                    LogManager.Log($"IsShiftModifierPressed for action {keyboardAction.Id.ToString().Substring(0, 8)}: {isPressed}", LogLevel.KeyDebug);
                    return isPressed;
                }
                return false;
            }
            set
            {
                if (SelectedAction is KeyboardAction keyboardAction)
                {
                    LogManager.Log($"IsShiftModifierPressed being set to: {value} for action {keyboardAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    LogManager.LogKeyArray(keyboardAction.Keys, "Before change");

                    var keys = new List<Key>();

                    // Keep all existing keys except Shift
                    if (keyboardAction.Keys != null)
                    {
                        foreach (var key in keyboardAction.Keys)
                        {
                            if (key != Key.LeftShift && key != Key.RightShift)
                            {
                                keys.Add(key);
                            }
                        }
                    }

                    // Add Shift if activated (after Ctrl and Alt, if present)
                    if (value)
                    {
                        int insertIndex = 0;
                        if (keys.Contains(Key.LeftCtrl) || keys.Contains(Key.RightCtrl)) insertIndex++;
                        if (keys.Contains(Key.LeftAlt) || keys.Contains(Key.RightAlt)) insertIndex++;
                        keys.Insert(insertIndex, Key.LeftShift);
                    }

                    keyboardAction.Keys = keys.ToArray();
                    // Update tracker
                    _actionKeysTracker[keyboardAction.Id] = keys.ToArray();

                    LogManager.LogKeyArray(keyboardAction.Keys, "After change");

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainKey));
                    OnPropertyChanged(nameof(MainKeyItem));
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));

                    // Update ViewModels
                    KeyboardActionViewModel.KeyboardAction = keyboardAction;
                    KeyCombinationViewModel.KeyboardAction = keyboardAction;
                }
            }
        }

        /// <summary>
        /// The main key in the current key combination
        /// </summary>
        public Key MainKey
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null && keyboardAction.Keys.Length > 0)
                {
                    // The last key is the main key (after the modifier keys)
                    var lastKey = keyboardAction.Keys[keyboardAction.Keys.Length - 1];

                    // If the last key is a modifier key, return None
                    if (lastKey == Key.LeftCtrl || lastKey == Key.RightCtrl ||
                        lastKey == Key.LeftAlt || lastKey == Key.RightAlt ||
                        lastKey == Key.LeftShift || lastKey == Key.RightShift)
                    {
                        LogManager.Log($"MainKey for action {keyboardAction.Id.ToString().Substring(0, 8)}: None (last key is modifier)", LogLevel.KeyDebug);
                        return Key.None;
                    }

                    LogManager.Log($"MainKey for action {keyboardAction.Id.ToString().Substring(0, 8)}: {lastKey}", LogLevel.KeyDebug);
                    return lastKey;
                }

                LogManager.Log("MainKey: None (no KeyboardAction selected or no Keys)", LogLevel.KeyDebug);
                return Key.None;
            }
            set
            {
                if (SelectedAction is KeyboardAction keyboardAction)
                {
                    LogManager.Log($"MainKey being set to: {value} for action {keyboardAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    LogManager.LogKeyArray(keyboardAction.Keys, "Before change");

                    // Keep current modifier keys
                    var modifiers = new List<Key>();
                    if (keyboardAction.Keys != null)
                    {
                        foreach (var key in keyboardAction.Keys)
                        {
                            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                                key == Key.LeftAlt || key == Key.RightAlt ||
                                key == Key.LeftShift || key == Key.RightShift)
                            {
                                modifiers.Add(key);
                            }
                        }
                    }

                    // Set new key combination (modifier keys + main key)
                    if (value != Key.None)
                    {
                        modifiers.Add(value);
                    }

                    keyboardAction.Keys = modifiers.ToArray();
                    // Update tracker
                    _actionKeysTracker[keyboardAction.Id] = modifiers.ToArray();

                    LogManager.LogKeyArray(keyboardAction.Keys, "After change");

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainKeyItem));
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));

                    // Update ViewModels
                    KeyboardActionViewModel.KeyboardAction = keyboardAction;
                    KeyCombinationViewModel.KeyboardAction = keyboardAction;
                }
            }
        }

        /// <summary>
        /// The main key as KeyItem for binding to ComboBox
        /// </summary>
        public KeyItem MainKeyItem
        {
            get
            {
                if (SelectedAction is KeyboardAction keyboardAction && keyboardAction.Keys != null && keyboardAction.Keys.Length > 0)
                {
                    // The last key is the main key (after the modifier keys)
                    var lastKey = keyboardAction.Keys[keyboardAction.Keys.Length - 1];

                    // If the last key is a modifier key, return None
                    if (lastKey == Key.LeftCtrl || lastKey == Key.RightCtrl ||
                        lastKey == Key.LeftAlt || lastKey == Key.RightAlt ||
                        lastKey == Key.LeftShift || lastKey == Key.RightShift)
                    {
                        LogManager.Log($"MainKeyItem for action {keyboardAction.Id.ToString().Substring(0, 8)}: None (KeyItem, last key is modifier)", LogLevel.KeyDebug);
                        return new KeyItem(Key.None);
                    }

                    LogManager.Log($"MainKeyItem for action {keyboardAction.Id.ToString().Substring(0, 8)}: {lastKey} (KeyItem)", LogLevel.KeyDebug);
                    return new KeyItem(lastKey);
                }

                LogManager.Log("MainKeyItem: None (KeyItem, no KeyboardAction selected or no Keys)", LogLevel.KeyDebug);
                return new KeyItem(Key.None);
            }
            set
            {
                if (SelectedAction is KeyboardAction keyboardAction && value != null)
                {
                    LogManager.Log($"MainKeyItem being set to: {value.KeyValue} for action {keyboardAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    LogManager.LogKeyArray(keyboardAction.Keys, "Before change");

                    // Keep current modifier keys
                    var modifiers = new List<Key>();
                    if (keyboardAction.Keys != null)
                    {
                        foreach (var key in keyboardAction.Keys)
                        {
                            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                                key == Key.LeftAlt || key == Key.RightAlt ||
                                key == Key.LeftShift || key == Key.RightShift)
                            {
                                modifiers.Add(key);
                            }
                        }
                    }

                    // Set new key combination (modifier keys + main key)
                    if (value.KeyValue != Key.None)
                    {
                        modifiers.Add(value.KeyValue);
                    }

                    keyboardAction.Keys = modifiers.ToArray();
                    // Update tracker
                    _actionKeysTracker[keyboardAction.Id] = modifiers.ToArray();

                    LogManager.LogKeyArray(keyboardAction.Keys, "After change");

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MainKey));
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));

                    // Update ViewModels
                    KeyboardActionViewModel.KeyboardAction = keyboardAction;
                    KeyCombinationViewModel.KeyboardAction = keyboardAction;
                }
            }
        }

        /// <summary>
        /// ViewModel for keyboard inputs
        /// </summary>
        public KeyboardActionViewModel KeyboardActionViewModel
        {
            get => _keyboardActionViewModel;
            set
            {
                if (_keyboardActionViewModel != value)
                {
                    var oldViewModel = _keyboardActionViewModel;
                    _keyboardActionViewModel = value;

                    LogManager.Log($"KeyboardActionViewModel updated", LogLevel.KeyDebug);
                    if (value?.KeyboardAction != null)
                    {
                        LogManager.LogKeyboardAction(value.KeyboardAction, "New KeyboardActionViewModel");
                    }

                    OnPropertyChanged();
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));
                }
            }
        }

        /// <summary>
        /// ViewModel for key combinations with variable number of keys
        /// </summary>
        public KeyCombinationViewModel KeyCombinationViewModel
        {
            get => _keyCombinationViewModel;
            set
            {
                if (_keyCombinationViewModel != value)
                {
                    var oldViewModel = _keyCombinationViewModel;
                    _keyCombinationViewModel = value;

                    LogManager.Log($"KeyCombinationViewModel updated", LogLevel.KeyDebug);
                    if (value?.KeyboardAction != null)
                    {
                        LogManager.LogKeyboardAction(value.KeyboardAction, "New KeyCombinationViewModel");
                    }

                    OnPropertyChanged();
                    // Update the display of saved keys
                    OnPropertyChanged(nameof(SavedKeys));
                }
            }
        }

        /// <summary>
        /// Indicates whether recording is currently active
        /// </summary>
        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged();
                    UpdateStatusMessage();
                }
            }
        }

        /// <summary>
        /// Indicates whether a sequence is currently being played
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanStartRecording));
                    OnPropertyChanged(nameof(CanPlaySequence));
                    UpdateStatusMessage();
                }
            }
        }

        /// <summary>
        /// Status message for the status bar
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether mouse actions should be recorded
        /// </summary>
        public bool RecordMouse
        {
            get => _recordMouse;
            set
            {
                if (_recordMouse != value)
                {
                    _recordMouse = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether keyboard inputs should be recorded
        /// </summary>
        public bool RecordKeyboard
        {
            get => _recordKeyboard;
            set
            {
                if (_recordKeyboard != value)
                {
                    _recordKeyboard = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether a sequence is selected
        /// </summary>
        public bool IsSequenceSelected => SelectedSequence != null;

        /// <summary>
        /// Indicates whether an action is selected
        /// </summary>
        public bool IsActionSelected => SelectedAction != null;

        /// <summary>
        /// Indicates whether recording can be started
        /// </summary>
        public bool CanStartRecording => IsSequenceSelected && !IsRecording && !IsPlaying;

        /// <summary>
        /// Indicates whether a sequence can be played
        /// </summary>
        public bool CanPlaySequence => IsSequenceSelected && !IsRecording && !IsPlaying;

        #endregion

        #region Commands

        /// <summary>
        /// Command to create a new sequence
        /// </summary>
        public ICommand NewSequenceCommand { get; private set; }

        /// <summary>
        /// Command to delete the selected sequence
        /// </summary>
        public ICommand DeleteSequenceCommand { get; private set; }

        /// <summary>
        /// Command to save the current sequence
        /// </summary>
        public ICommand SaveSequenceCommand { get; private set; }

        /// <summary>
        /// Command to load a sequence
        /// </summary>
        public ICommand LoadSequenceCommand { get; private set; }

        /// <summary>
        /// Command to duplicate the selected sequence
        /// </summary>
        public ICommand DuplicateSequenceCommand { get; private set; }

        /// <summary>
        /// Command to start recording
        /// </summary>
        public ICommand StartRecordingCommand { get; private set; }

        /// <summary>
        /// Command to stop recording
        /// </summary>
        public ICommand StopRecordingCommand { get; private set; }

        /// <summary>
        /// Command to play the selected sequence
        /// </summary>
        public ICommand PlaySequenceCommand { get; private set; }

        /// <summary>
        /// Command to stop playback
        /// </summary>
        public ICommand StopPlayingCommand { get; private set; }

        /// <summary>
        /// Command to add a new action
        /// </summary>
        public ICommand AddActionCommand { get; private set; }

        /// <summary>
        /// Command to delete the selected action(s)
        /// </summary>
        public ICommand DeleteActionCommand { get; private set; }

        /// <summary>
        /// Command to move an action up
        /// </summary>
        public ICommand MoveActionUpCommand { get; private set; }

        /// <summary>
        /// Command to move an action down
        /// </summary>
        public ICommand MoveActionDownCommand { get; private set; }

        /// <summary>
        /// Command to add a mouse action
        /// </summary>
        public ICommand AddMouseActionCommand { get; private set; }

        /// <summary>
        /// Command to add a keyboard action
        /// </summary>
        public ICommand AddKeyboardActionCommand { get; private set; }

        /// <summary>
        /// Command to add a wait operation
        /// </summary>
        public ICommand AddWaitActionCommand { get; private set; }

        /// <summary>
        /// Command to add a browser action
        /// </summary>
        public ICommand AddBrowserActionCommand { get; private set; }

        /// <summary>
        /// Command to add a JSON action
        /// </summary>
        public ICommand AddJsonActionCommand { get; private set; }

        /// <summary>
        /// Command to add a clipboard action
        /// </summary>
        public ICommand AddClipboardActionCommand { get; private set; }

        /// <summary>
        /// Command to add a variable action
        /// </summary>
        public ICommand AddVariableActionCommand { get; private set; }

        /// <summary>
        /// Command to add a new variable
        /// </summary>
        public ICommand AddVariableCommand { get; private set; }

        /// <summary>
        /// Command to delete the selected variable
        /// </summary>
        public ICommand DeleteVariableCommand { get; private set; }

        /// <summary>
        /// Command to delete all variables
        /// </summary>
        public ICommand ClearVariablesCommand { get; private set; }

        /// <summary>
        /// Command to copy selected actions
        /// </summary>
        public ICommand CopyActionsCommand { get; private set; }

        /// <summary>
        /// Command to paste copied actions
        /// </summary>
        public ICommand PasteActionsCommand { get; private set; }


        /// <summary>
        /// Command to add a loop action
        /// </summary>
        public ICommand AddLoopActionCommand { get; private set; }

        #endregion
    }
}