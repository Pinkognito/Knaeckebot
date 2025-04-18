using Knaeckebot.Models;
using Knaeckebot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using ListView = System.Windows.Controls.ListView;

namespace Knaeckebot.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Initializes all commands
        /// </summary>
        private void InitializeCommands()
        {
            NewSequenceCommand = new RelayCommand(NewSequence);
            DeleteSequenceCommand = new RelayCommand(DeleteSequence, () => AreSequencesSelected || IsSequenceSelected);
            SaveSequenceCommand = new RelayCommand(SaveSequence, () => IsSequenceSelected);
            LoadSequenceCommand = new RelayCommand(LoadSequences);
            DuplicateSequenceCommand = new RelayCommand(DuplicateSequence, () => AreSequencesSelected || IsSequenceSelected);
            StartRecordingCommand = new RelayCommand(StartRecording, () => CanStartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording, () => IsRecording);
            PlaySequenceCommand = new RelayCommand(PlaySequence, () => CanPlaySequence);
            StopPlayingCommand = new RelayCommand(StopPlaying, () => IsPlaying);

            AddActionCommand = new RelayCommand(ShowAddActionMenu);
            DeleteActionCommand = new RelayCommand(DeleteAction, () => AreActionsSelected);
            MoveActionUpCommand = new RelayCommand(MoveActionUp, () => AreActionsSelected);
            MoveActionDownCommand = new RelayCommand(MoveActionDown, () => AreActionsSelected);

            // Variable commands
            AddVariableCommand = new RelayCommand(AddVariable, () => IsSequenceSelected);
            DeleteVariableCommand = new RelayCommand(DeleteVariable, () => IsVariableSelected);
            ClearVariablesCommand = new RelayCommand(ClearVariables, () => HasVariables);

            // Copy and paste actions
            CopyActionsCommand = new RelayCommand(CopyActions, () => AreActionsSelected);
            PasteActionsCommand = new RelayCommand(PasteActions, () => IsSequenceSelected && _copiedActions.Count > 0);

            // New action commands
            AddMouseActionCommand = new RelayCommand(() => AddSpecificAction(new Models.MouseAction { Name = "New Mouse Action", X = 0, Y = 0 }),
                () => IsSequenceSelected);
            AddKeyboardActionCommand = new RelayCommand(() => AddSpecificAction(new KeyboardAction { Name = "New Keyboard Input" }),
                () => IsSequenceSelected);
            AddWaitActionCommand = new RelayCommand(() => AddSpecificAction(new WaitAction { Name = "New Wait Operation" }),
                () => IsSequenceSelected);
            AddBrowserActionCommand = new RelayCommand(() => AddSpecificAction(new BrowserAction { Name = "New Browser Action" }),
                () => IsSequenceSelected);
            AddJsonActionCommand = new RelayCommand(() => AddSpecificAction(new JsonAction { Name = "New JSON Action" }),
                () => IsSequenceSelected);
            AddClipboardActionCommand = new RelayCommand(() => AddSpecificAction(new ClipboardAction { Name = "New Clipboard Action" }),
                () => IsSequenceSelected);
            AddVariableActionCommand = new RelayCommand(() => AddSpecificAction(new VariableAction { Name = "New Variable Action" }),
                () => IsSequenceSelected);
            AddLoopActionCommand = new RelayCommand(() => AddSpecificAction(new LoopAction { Name = "New Loop Action" }),
                () => IsSequenceSelected);
            AddIfActionCommand = new RelayCommand(() => AddSpecificAction(new IfAction { Name = "New If Action" }),
                () => IsSequenceSelected);
            AddFileActionCommand = new RelayCommand(() => AddSpecificAction(new FileAction { Name = "New File Action" }),
                () => IsSequenceSelected);
        }

        #region Command Methods

        /// <summary>
        /// Creates a new sequence
        /// </summary>
        private void NewSequence()
        {
            var newSequence = new Sequence("New Sequence");
            Sequences.Add(newSequence);
            SelectedSequence = newSequence;
            SelectedSequences.Clear();
            SelectedSequences.Add(newSequence);
            StatusMessage = "New sequence created";
            LogManager.Log("New sequence created");
        }
        /// <summary>
        /// Deletes the selected sequences
        /// </summary>
        private void DeleteSequence()
        {
            LogManager.Log($"=== DELETION START ===", LogLevel.Debug);

            // Diagnostics of current selection
            LogManager.Log($"Existing sequences: {Sequences.Count}", LogLevel.Debug);
            foreach (var seq in Sequences)
            {
                LogManager.Log($"  - Sequence: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
            }
            LogManager.Log($"SelectedSequence: {SelectedSequence?.Name ?? "null"}, ID: {SelectedSequence?.Id.ToString() ?? "null"}", LogLevel.Debug);

            // Remove possible duplicates in the selection before processing
            SequenceUtils.RemoveDuplicates(SelectedSequences);
            SequenceUtils.LogSelectedSequences(SelectedSequences, "Before deletion");

            // If no sequence in SelectedSequences, but one in SelectedSequence, use that one
            if (SelectedSequences.Count == 0 && SelectedSequence != null)
            {
                LogManager.Log($"No selected sequences, but SelectedSequence is set. Adding: {SelectedSequence.Name}", LogLevel.Debug);
                SelectedSequences.Add(SelectedSequence);
            }

            if (SelectedSequences.Count == 0)
            {
                LogManager.Log("No sequences selected, aborting deletion", LogLevel.Debug);
                return;
            }

            string message = SelectedSequences.Count == 1
                ? $"Are you sure you want to delete the sequence '{SelectedSequences[0].Name}'?"
                : $"Are you sure you want to delete {SelectedSequences.Count} sequences?";

            LogManager.Log($"Showing confirmation dialog: {message}", LogLevel.Debug);

            var result = System.Windows.MessageBox.Show(
                message,
                "Delete Sequence",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // IMPORTANT: Create exact new list of sequences to be deleted
                var sequencesToRemove = new List<Sequence>();
                foreach (var seq in SelectedSequences)
                {
                    sequencesToRemove.Add(seq);
                    LogManager.Log($"Marked for deletion: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
                }

                // IMPORTANT: Reset selection BEFORE sequences are deleted
                LogManager.Log("Setting SelectedSequence to null", LogLevel.Debug);
                SelectedSequence = null;

                LogManager.Log("Emptying SelectedSequences before deletion", LogLevel.Debug);
                SelectedSequences.Clear();

                // Delete sequences in separate list
                foreach (var sequence in sequencesToRemove)
                {
                    LogManager.Log($"Deleting sequence '{sequence.Name}', ID: {sequence.Id}", LogLevel.Debug);
                    Sequences.Remove(sequence);
                }

                // Select another sequence if available
                if (Sequences.Count > 0)
                {
                    LogManager.Log($"Selecting first remaining sequence: {Sequences[0].Name}, ID: {Sequences[0].Id}", LogLevel.Debug);
                    SelectedSequence = Sequences[0];
                    // The Add operation is automatically performed in the SelectedSequence setter
                }
                else
                {
                    LogManager.Log("No sequences remaining", LogLevel.Debug);
                }

                // Diagnostics after deletion
                SequenceUtils.LogSelectedSequences(SelectedSequences, "After deletion");

                StatusMessage = sequencesToRemove.Count == 1
                    ? "Sequence deleted"
                    : $"{sequencesToRemove.Count} sequences deleted";

                foreach (var seq in sequencesToRemove)
                {
                    LogManager.Log($"Sequence '{seq.Name}' deleted", LogLevel.Info);
                }
            }
            else
            {
                LogManager.Log("Deletion canceled", LogLevel.Debug);
            }

            LogManager.Log($"=== DELETION END ===", LogLevel.Debug);
        }

        /// <summary>
        /// Saves the current sequence
        /// </summary>
        private void SaveSequence()
        {
            if (SelectedSequence == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Knaeckebot Sequence (*.cms)|*.cms|All Files (*.*)|*.*",
                DefaultExt = ".cms",
                FileName = SelectedSequence.Name
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SelectedSequence.Save(dialog.FileName);
                    StatusMessage = $"Sequence saved: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Error saving sequence: {ex.Message}", LogLevel.Error);
                    System.Windows.MessageBox.Show(
                        $"Error saving sequence: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Loads multiple sequences
        /// </summary>
        private void LoadSequences()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Knaeckebot Sequence (*.cms)|*.cms|All Files (*.*)|*.*",
                DefaultExt = ".cms",
                Multiselect = true // Allow multiple selection
            };

            if (dialog.ShowDialog() == true)
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (string fileName in dialog.FileNames)
                {
                    try
                    {
                        var sequence = Sequence.Load(fileName);
                        Sequences.Add(sequence);

                        // Set first loaded sequence as selected sequence
                        if (successCount == 0)
                        {
                            SelectedSequence = sequence;
                            SelectedSequences.Clear();
                        }

                        // Add sequence to multiple selection
                        SelectedSequences.Add(sequence);
                        successCount++;

                        // Update tracker for all KeyboardActions in the loaded sequence
                        foreach (var action in sequence.Actions)
                        {
                            if (action is KeyboardAction keyAction)
                            {
                                _actionKeysTracker[keyAction.Id] = keyAction.Keys?.ToArray();
                                LogManager.Log($"Tracker updated for loaded action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                                LogManager.LogKeyArray(keyAction.Keys, "Loaded action");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log($"Error loading sequence {fileName}: {ex.Message}", LogLevel.Error);
                        errorCount++;
                    }
                }

                if (successCount > 0)
                {
                    StatusMessage = successCount == 1
                        ? $"Sequence loaded: {Path.GetFileName(dialog.FileNames[0])}"
                        : $"{successCount} sequences loaded" + (errorCount > 0 ? $", {errorCount} with errors" : "");
                }
                else if (errorCount > 0)
                {
                    StatusMessage = $"Error loading {errorCount} sequences";
                    System.Windows.MessageBox.Show(
                        $"No sequences could be loaded. Check the log file for more information.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Duplicates the selected sequences
        /// </summary>
        private void DuplicateSequence()
        {
            LogManager.Log($"=== DUPLICATION START ===", LogLevel.Debug);

            // Diagnostics of current selection
            SequenceUtils.LogSelectedSequences(SelectedSequences, "Before duplication");
            LogManager.Log($"SelectedSequence: {SelectedSequence?.Name ?? "null"}, ID: {SelectedSequence?.Id.ToString() ?? "null"}", LogLevel.Debug);

            // Remove possible duplicates in the selection before processing
            SequenceUtils.RemoveDuplicates(SelectedSequences);

            // If no sequence in SelectedSequences, but one in SelectedSequence, use that one
            if (SelectedSequences.Count == 0 && SelectedSequence != null)
            {
                LogManager.Log($"No selected sequences, but SelectedSequence is set. Adding: {SelectedSequence.Name}", LogLevel.Debug);
                SelectedSequences.Add(SelectedSequence);
            }

            if (SelectedSequences.Count == 0)
            {
                LogManager.Log("No sequences selected, aborting duplication", LogLevel.Debug);
                return;
            }

            // Create copy of sequences, as the collection changes during iteration
            var sequencesToDuplicate = SelectedSequences.ToList();
            LogManager.Log($"Sequences to duplicate: {sequencesToDuplicate.Count}", LogLevel.Debug);
            foreach (var seq in sequencesToDuplicate)
            {
                LogManager.Log($"  - To duplicate: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
            }

            // IMPORTANT: Completely reset selection before duplicating
            LogManager.Log("Setting SelectedSequence to null", LogLevel.Debug);
            SelectedSequence = null;

            LogManager.Log("Emptying SelectedSequences before duplication", LogLevel.Debug);
            SelectedSequences.Clear();

            // List for new duplicated sequences
            var newDuplicates = new List<Sequence>();

            foreach (var sequence in sequencesToDuplicate)
            {
                LogManager.Log($"Duplicating sequence: {sequence.Name}, ID: {sequence.Id}", LogLevel.Debug);

                // Generate unique name
                string baseName = sequence.Name.EndsWith(" Copy") ? sequence.Name : sequence.Name + " Copy";
                string uniqueName = baseName;
                int counter = 1;

                while (Sequences.Any(s => s.Name == uniqueName))
                {
                    uniqueName = baseName + $" ({counter})";
                    counter++;
                    LogManager.Log($"  - Name already exists, trying: {uniqueName}", LogLevel.Debug);
                }

                LogManager.Log($"  - Unique name found: {uniqueName}", LogLevel.Debug);

                // Create clone and set name
                var duplicate = sequence.Clone();

                // Debug: Show values of cloned sequence before name change
                LogManager.Log($"  - Cloned sequence before name change: {duplicate.Name}, ID: {duplicate.Id}", LogLevel.Debug);

                // Explicitly set name
                duplicate.Name = uniqueName;
                LogManager.Log($"  - Name set to: {duplicate.Name}", LogLevel.Debug);

                // Add to sequence collection
                Sequences.Add(duplicate);
                LogManager.Log($"  - Added to Sequences, new size: {Sequences.Count}", LogLevel.Debug);

                // Add to list of newly duplicated sequences
                newDuplicates.Add(duplicate);

                // Update tracker for all KeyboardActions in the duplicated sequence
                foreach (var action in duplicate.Actions)
                {
                    if (action is KeyboardAction keyAction)
                    {
                        _actionKeysTracker[keyAction.Id] = keyAction.Keys?.ToArray();
                        LogManager.Log($"  - Tracker updated for action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.Debug);
                    }
                }
            }

            // IMPORTANT: Only mark the last duplicated sequence as selected
            if (newDuplicates.Count > 0)
            {
                var lastDuplicate = newDuplicates[newDuplicates.Count - 1];
                LogManager.Log($"Setting SelectedSequence to: {lastDuplicate.Name}, ID: {lastDuplicate.Id}", LogLevel.Debug);
                SelectedSequence = lastDuplicate;
                // The Add operation is automatically performed in the SelectedSequence setter
            }

            // Diagnostics after duplication
            SequenceUtils.LogSelectedSequences(SelectedSequences, "After duplication");

            StatusMessage = sequencesToDuplicate.Count == 1
                ? $"Sequence duplicated: {SelectedSequence?.Name}"
                : $"{sequencesToDuplicate.Count} sequences duplicated";

            LogManager.Log(sequencesToDuplicate.Count == 1
                ? $"Sequence duplicated: {SelectedSequence?.Name}"
                : $"{sequencesToDuplicate.Count} sequences duplicated");

            LogManager.Log($"=== DUPLICATION END ===", LogLevel.Debug);
        }

        /// <summary>
        /// Starts recording mouse and keyboard inputs
        /// </summary>
        private void StartRecording()
        {
            if (SelectedSequence == null) return;

            IsRecording = true;

            // Start mouse recording if enabled
            if (RecordMouse)
            {
                MouseService.Instance.StartRecording();
            }

            // Start keyboard recording if enabled
            if (RecordKeyboard)
            {
                KeyboardService.Instance.StartRecording();
            }

            StatusMessage = "Recording started";
            LogManager.Log("Recording started");
        }

        /// <summary>
        /// Stops recording mouse and keyboard inputs
        /// </summary>
        private void StopRecording()
        {
            MouseService.Instance.StopRecording();
            KeyboardService.Instance.StopRecording();
            IsRecording = false;
            StatusMessage = "Recording stopped";
            LogManager.Log("Recording stopped");
        }

        /// <summary>
        /// Plays the selected sequence
        /// </summary>
        private void PlaySequence()
        {
            if (SelectedSequence == null) return;

            StatusMessage = $"Sequence is being played: {SelectedSequence.Name}";
            LogManager.Log($"Starting playback of sequence: {SelectedSequence.Name}");

            // Update status
            IsPlaying = true;

            // Create new CancellationTokenSource
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            // Register global shortcut for F6
            RegisterGlobalF6Shortcut();

            // Play sequence in a separate thread (configured as STA)
            _playbackThread = new Thread(() =>
            {
                try
                {
                    LogManager.Log("Thread for sequence playback started", LogLevel.Debug);

                    // Check for cancellation before each step
                    if (token.IsCancellationRequested)
                    {
                        LogManager.Log("Playback was cancelled", LogLevel.Info);
                        return;
                    }

                    // Pass token to Sequence.Execute
                    SelectedSequence.Execute(token);

                    // If we get here, execution was not cancelled
                    // Update status via the dispatcher
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsPlaying = false;
                        StatusMessage = $"Sequence played: {SelectedSequence.Name}";
                        LogManager.Log($"Playback of sequence {SelectedSequence.Name} completed");

                        // Unregister global shortcut when done
                        UnregisterGlobalF6Shortcut();
                    });
                }
                catch (OperationCanceledException)
                {
                    // Cancellation was requested
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsPlaying = false;
                        StatusMessage = $"Playback of sequence {SelectedSequence.Name} cancelled";
                        LogManager.Log($"Playback of sequence {SelectedSequence.Name} cancelled", LogLevel.Info);

                        // Unregister global shortcut when cancelled
                        UnregisterGlobalF6Shortcut();
                    });
                }
                catch (Exception ex)
                {
                    // Show error via the dispatcher
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsPlaying = false;
                        StatusMessage = $"Error: {ex.Message}";
                        LogManager.Log($"Error during playback: {ex.Message}", LogLevel.Error);
                        System.Windows.MessageBox.Show(
                            $"Error playing the sequence: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        // Unregister global shortcut when error
                        UnregisterGlobalF6Shortcut();
                    });
                }
                finally
                {
                    // Cleanup
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    _playbackThread = null;

                    // Ensure global shortcut is unregistered
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        UnregisterGlobalF6Shortcut();
                    });
                }
            });

            // Configure thread as STA (required for clipboard operations)
            _playbackThread.SetApartmentState(ApartmentState.STA);
            _playbackThread.Start();
        }

        /// <summary>
        /// Stops the running playback of a sequence
        /// </summary>
        private void StopPlaying()
        {
            if (!IsPlaying || _cancellationTokenSource == null) return;

            try
            {
                // Signal cancellation
                _cancellationTokenSource.Cancel();

                // Short wait time to give the thread time to end
                if (_playbackThread != null && _playbackThread.IsAlive)
                {
                    // Wait max. 2 seconds for normal end
                    if (!_playbackThread.Join(2000))
                    {
                        // Abort thread (last option)
                        LogManager.Log("Thread for sequence playback had to be aborted", LogLevel.Warning);
                    }
                }

                // Update status
                IsPlaying = false;
                StatusMessage = "Playback cancelled";
                LogManager.Log("Playback cancelled", LogLevel.Info);

                // Unregister global shortcut
                UnregisterGlobalF6Shortcut();
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error stopping playback: {ex.Message}", LogLevel.Error);

                // Ensure status is reset
                IsPlaying = false;
                StatusMessage = "Playback cancelled with errors";

                // Unregister global shortcut
                UnregisterGlobalF6Shortcut();
            }
            finally
            {
                // Cleanup
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _playbackThread = null;
            }
        }

        /// <summary>
        /// Registers the global F6 shortcut for stopping playback
        /// </summary>
        private void RegisterGlobalF6Shortcut()
        {
            if (_isStopHookRegistered) return;

            try
            {
                _playbackStopHook = new GlobalKeyboardHook();
                _playbackStopHook.KeyDown += PlaybackStopHook_KeyDown;
                _isStopHookRegistered = true;
                LogManager.Log("Global F6 shortcut registered for stopping playback", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                LogManager.Log($"Failed to register global F6 shortcut: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Unregisters the global F6 shortcut
        /// </summary>
        private void UnregisterGlobalF6Shortcut()
        {
            if (!_isStopHookRegistered || _playbackStopHook == null) return;

            try
            {
                _playbackStopHook.KeyDown -= PlaybackStopHook_KeyDown;
                _playbackStopHook.Dispose();
                _playbackStopHook = null;
                _isStopHookRegistered = false;
                LogManager.Log("Global F6 shortcut unregistered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error unregistering global F6 shortcut: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Event handler for the global keyboard hook
        /// </summary>
        private void PlaybackStopHook_KeyDown(object? sender, KeyboardHookEventArgs e)
        {
            if (e.Key == Key.F6 || e.Key == Key.Escape)
            {
                LogManager.Log($"Global shortcut detected: {e.Key}, stopping playback", LogLevel.Debug);

                // Execute on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsPlaying)
                    {
                        StopPlaying();
                    }
                });
            }
        }

        /// <summary>
        /// Shows a dialog to add different action types
        /// </summary>
        private void ShowAddActionMenu()
        {
            if (SelectedSequence == null) return;

            // Create ActionSelectionWindow as modal dialog
            var actionSelectionWindow = new ActionSelectionWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            // Show dialog and wait for result
            bool? result = actionSelectionWindow.ShowDialog();

            // If dialog was closed with "OK", add the selected action
            if (result == true)
            {
                // Create a specific action based on the selected action type
                switch (actionSelectionWindow.SelectedActionType)
                {
                    case ActionSelectionWindow.ActionType.MouseAction:
                        AddSpecificAction(new Models.MouseAction
                        {
                            Name = "New Mouse Action",
                            X = System.Windows.Forms.Cursor.Position.X,
                            Y = System.Windows.Forms.Cursor.Position.Y
                        });
                        break;

                    case ActionSelectionWindow.ActionType.KeyboardAction:
                        AddSpecificAction(new KeyboardAction
                        {
                            Name = "New Keyboard Input"
                        });
                        break;

                    case ActionSelectionWindow.ActionType.WaitAction:
                        AddSpecificAction(new WaitAction
                        {
                            Name = "New Wait Operation",
                            WaitTime = 1000
                        });
                        break;

                    case ActionSelectionWindow.ActionType.BrowserAction:
                        AddSpecificAction(new BrowserAction
                        {
                            Name = "New Browser Action"
                        });
                        break;

                    case ActionSelectionWindow.ActionType.JsonAction:
                        var jsonAction = new JsonAction
                        {
                            Name = "New JSON Action",
                            CheckClipboard = true
                        };
                        AddSpecificAction(jsonAction);
                        break;

                    case ActionSelectionWindow.ActionType.ClipboardAction:
                        var clipboardAction = new ClipboardAction
                        {
                            Name = "New Clipboard Action",
                            Text = ""
                        };
                        AddSpecificAction(clipboardAction);
                        break;

                    case ActionSelectionWindow.ActionType.VariableAction:
                        var variableAction = new VariableAction
                        {
                            Name = "New Variable Action"
                        };
                        AddSpecificAction(variableAction);
                        break;

                    case ActionSelectionWindow.ActionType.LoopAction:
                        // Add new loop action
                        var loopAction = new LoopAction
                        {
                            Name = "New Loop Action",
                            MaxIterations = 10,
                            UseCondition = false
                        };
                        AddSpecificAction(loopAction);
                        break;

                    case ActionSelectionWindow.ActionType.IfAction:
                        // Add new if action
                        var ifAction = new IfAction
                        {
                            Name = "New If Action"
                        };
                        AddSpecificAction(ifAction);
                        break;

                    case ActionSelectionWindow.ActionType.FileAction:
                        // Add new file action
                        var fileAction = new FileAction
                        {
                            Name = "New File Action",
                            SourceType = FileSourceType.Text,
                            FilePath = string.Empty,
                            DestinationType = FileDestinationType.Variable,
                            DestinationVariableName = string.Empty,
                            FileEncoding = FileEncodingType.UTF8,
                            HandleIOException = true
                        };
                        AddSpecificAction(fileAction);
                        break;

                    default:
                        // Default to a Variable action
                        AddSpecificAction(new VariableAction
                        {
                            Name = "New Variable Action"
                        });
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a specific action to the sequence
        /// </summary>
        private void AddSpecificAction(ActionBase action)
        {
            if (SelectedSequence == null) return;

            SelectedSequence.Actions.Add(action);

            // For KeyboardAction immediately update the tracker
            if (action is KeyboardAction keyAction)
            {
                _actionKeysTracker[keyAction.Id] = keyAction.Keys?.ToArray();
                LogManager.Log($"Tracker initialized for new action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                LogManager.LogKeyboardAction(keyAction, "Newly added action");
            }

            SelectedAction = action;
            SelectedActions.Clear();
            SelectedActions.Add(action);
            StatusMessage = $"Action added: {action.Name}";
            LogManager.Log($"Action added: {action.Name} ({action.GetType().Name})");
        }

        /// <summary>
        /// Deletes the selected actions
        /// </summary>
        private void DeleteAction()
        {
            if (SelectedSequence == null || SelectedActions.Count == 0) return;

            string prompt = SelectedActions.Count == 1
                ? $"Are you sure you want to delete the action '{SelectedActions[0].Name}'?"
                : $"Are you sure you want to delete {SelectedActions.Count} actions?";

            var result = System.Windows.MessageBox.Show(
                prompt,
                "Delete Action",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Create copy of selected actions, as the collection changes during iteration
                var actionsToRemove = SelectedActions.ToList();

                foreach (var action in actionsToRemove)
                {
                    // For KeyboardAction clean up the tracker
                    if (action is KeyboardAction keyAction)
                    {
                        _actionKeysTracker.Remove(keyAction.Id);
                        LogManager.Log($"Tracker removed for deleted action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                    }

                    LogManager.Log($"Action '{action.Name}' deleted");
                    SelectedSequence.Actions.Remove(action);
                }

                SelectedAction = null;
                SelectedActions.Clear();
                StatusMessage = SelectedActions.Count == 1 ? "Action deleted" : $"{actionsToRemove.Count} actions deleted";
            }
        }

        /// <summary>
        /// Copies the selected actions to the clipboard
        /// </summary>
        private void CopyActions()
        {
            if (SelectedActions.Count == 0) return;

            _copiedActions.Clear();
            foreach (var action in SelectedActions)
            {
                // Create a copy of the action to separate the original reference
                var actionCopy = action.Clone();
                _copiedActions.Add(actionCopy);

                // For KeyboardActions update the tracker
                if (actionCopy is KeyboardAction keyAction)
                {
                    _actionKeysTracker[keyAction.Id] = keyAction.Keys?.ToArray();
                    LogManager.Log($"Tracker initialized for copied action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                }
            }

            StatusMessage = SelectedActions.Count == 1
                ? "Action copied"
                : $"{SelectedActions.Count} actions copied";

            LogManager.Log(SelectedActions.Count == 1
                ? $"Action '{SelectedActions[0].Name}' copied"
                : $"{SelectedActions.Count} actions copied");
        }

        /// <summary>
        /// Pastes the copied actions into the current sequence
        /// </summary>
        private void PasteActions()
        {
            if (SelectedSequence == null || _copiedActions.Count == 0) return;

            // Check if a loop action is selected
            if (SelectedAction is LoopAction loopAction)
            {
                // Insert actions into the loop
                foreach (var action in _copiedActions)
                {
                    // Create a new copy for the loop
                    var actionCopy = action.Clone();
                    loopAction.LoopActions.Add(actionCopy);
                }

                StatusMessage = _copiedActions.Count == 1
                    ? "Action inserted into loop"
                    : $"{_copiedActions.Count} actions inserted into loop";

                LogManager.Log(_copiedActions.Count == 1
                    ? $"Action '{_copiedActions[0].Name}' inserted into loop"
                    : $"{_copiedActions.Count} actions inserted into loop");

                return; // Important: Exit method here
            }

            // Check if an if action is selected
            if (SelectedAction is IfAction ifAction)
            {
                // Determine if the THEN or ELSE branch is active in the UI
                bool useElseBranch = false;
                var targetCollection = ifAction.ThenActions;
                string branchName = "THEN";

                // Find active ListViews in the UI to determine which branch is active
                try
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Try to find the ElseActionsListView
                        var elseListView = FindActiveListView(mainWindow, "ElseActionsListView");
                        if (elseListView != null && elseListView.IsKeyboardFocusWithin)
                        {
                            useElseBranch = true;
                            LogManager.Log("ELSE branch ListView has focus", LogLevel.Debug);
                        }
                        else
                        {
                            LogManager.Log("ELSE branch ListView not found or doesn't have focus", LogLevel.Debug);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Error determining active branch: {ex.Message}", LogLevel.Error);
                }

                // If ELSE branch should be used and is enabled
                if (useElseBranch && ifAction.UseElseBranch)
                {
                    targetCollection = ifAction.ElseActions;
                    branchName = "ELSE";
                    LogManager.Log("Using ELSE branch for paste operation", LogLevel.Debug);
                }
                else
                {
                    LogManager.Log("Using THEN branch for paste operation", LogLevel.Debug);
                }

                // Insert actions into the appropriate branch
                foreach (var action in _copiedActions)
                {
                    // Create a new copy for the branch
                    var actionCopy = action.Clone();
                    targetCollection.Add(actionCopy);
                }

                StatusMessage = _copiedActions.Count == 1
                    ? $"Action inserted into {branchName} branch"
                    : $"{_copiedActions.Count} actions inserted into {branchName} branch";

                LogManager.Log(_copiedActions.Count == 1
                    ? $"Action '{_copiedActions[0].Name}' inserted into {branchName} branch"
                    : $"{_copiedActions.Count} actions inserted into {branchName} branch");

                return; // Important: Exit method here
            }

            // Determine position for insertion
            int insertPosition = SelectedAction != null
                ? SelectedSequence.Actions.IndexOf(SelectedAction) + 1
                : SelectedSequence.Actions.Count;

            // Insert actions
            foreach (var action in _copiedActions)
            {
                // Create a new copy to ensure multiple insertions work
                var actionCopy = action.Clone();

                if (insertPosition < SelectedSequence.Actions.Count)
                {
                    SelectedSequence.Actions.Insert(insertPosition, actionCopy);
                }
                else
                {
                    SelectedSequence.Actions.Add(actionCopy);
                }

                insertPosition++;

                // For KeyboardActions update the tracker
                if (actionCopy is KeyboardAction keyAction)
                {
                    _actionKeysTracker[keyAction.Id] = keyAction.Keys?.ToArray();
                    LogManager.Log($"Tracker initialized for inserted action {keyAction.Id.ToString().Substring(0, 8)}", LogLevel.KeyDebug);
                }
            }

            // Update selection
            SelectedActions.Clear();
            SelectedAction = SelectedSequence.Actions[insertPosition - _copiedActions.Count];

            StatusMessage = _copiedActions.Count == 1
                ? "Action pasted"
                : $"{_copiedActions.Count} actions pasted";

            LogManager.Log(_copiedActions.Count == 1
                ? $"Action '{_copiedActions[0].Name}' pasted"
                : $"{_copiedActions.Count} actions pasted");
        }

        /// <summary>
        /// Finds a ListView in the control tree by name that has focus
        /// </summary>
        private ListView FindActiveListView(DependencyObject parent, string name)
        {
            // Check if this is the ListView we're looking for
            if (parent is ListView listView && listView.Name == name)
            {
                return listView;
            }

            // Recursively search for the ListView in children
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindActiveListView(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Moves the selected actions up
        /// </summary>
        private void MoveActionUp()
        {
            if (SelectedSequence == null || SelectedActions.Count == 0) return;

            // We can only move if each selected action has a position above it
            var selectedIndices = SelectedActions
                .Select(action => SelectedSequence.Actions.IndexOf(action))
                .OrderBy(index => index)
                .ToList();

            // If the smallest index is 0, we can't move up
            if (selectedIndices.First() <= 0) return;

            // We need to move from top to bottom to maintain the order
            foreach (var index in selectedIndices)
            {
                SelectedSequence.Actions.Move(index, index - 1);
            }

            StatusMessage = SelectedActions.Count == 1
                ? "Action moved up"
                : $"{SelectedActions.Count} actions moved up";

            LogManager.Log($"{SelectedActions.Count} actions moved up");
        }

        /// <summary>
        /// Moves the selected actions down
        /// </summary>
        private void MoveActionDown()
        {
            if (SelectedSequence == null || SelectedActions.Count == 0) return;

            // We can only move if each selected action has a position below it
            var selectedIndices = SelectedActions
                .Select(action => SelectedSequence.Actions.IndexOf(action))
                .OrderByDescending(index => index) // Important: Sort descending for down movement
                .ToList();

            // If the largest index is already at the end, we can't move down
            if (selectedIndices.First() >= SelectedSequence.Actions.Count - 1) return;

            // We need to move from bottom to top to maintain the order
            foreach (var index in selectedIndices)
            {
                SelectedSequence.Actions.Move(index, index + 1);
            }

            StatusMessage = SelectedActions.Count == 1
                ? "Action moved down"
                : $"{SelectedActions.Count} actions moved down";

            LogManager.Log($"{SelectedActions.Count} actions moved down");
        }

        /// <summary>
        /// Adds a new variable to the sequence
        /// </summary>
        private void AddVariable()
        {
            if (SelectedSequence == null) return;

            // Generate a unique default name
            string baseName = "Variable";
            int counter = 1;
            string varName = baseName + counter;

            // Look for a unique name
            while (SelectedSequence.FindVariableByName(varName) != null)
            {
                counter++;
                varName = baseName + counter;
            }

            // New variable
            // Create new variable and add to sequence
            var variable = new SequenceVariable
            {
                Name = varName,
                Type = VariableType.Text,
                TextValue = "",
                Description = "New Variable"
            };

            SelectedSequence.Variables.Add(variable);
            SelectedVariable = variable;
            StatusMessage = $"Variable '{varName}' added";

            // Update variable names in UI
            OnPropertyChanged(nameof(VariableNames));
            OnPropertyChanged(nameof(HasVariables));
            LogManager.Log($"Variable '{varName}' added to sequence");
        }

        /// <summary>
        /// Deletes the selected variable
        /// </summary>
        private void DeleteVariable()
        {
            if (SelectedSequence == null || SelectedVariable == null) return;

            // Store the variable name before deletion to avoid NullReferenceException
            string variableName = SelectedVariable.Name;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the variable '{variableName}'?",
                "Delete Variable",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SelectedSequence.Variables.Remove(SelectedVariable);

                // Use the stored name instead of accessing SelectedVariable.Name
                StatusMessage = $"Variable '{variableName}' deleted";
                LogManager.Log($"Variable '{variableName}' deleted");

                // Reset reference
                SelectedVariable = null;

                // Update UI
                OnPropertyChanged(nameof(VariableNames));
                OnPropertyChanged(nameof(HasVariables));
            }
        }

        /// <summary>
        /// Deletes all variables of the current sequence
        /// </summary>
        private void ClearVariables()
        {
            if (SelectedSequence == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete all {SelectedSequence.Variables.Count} variables?",
                "Delete All Variables",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                int count = SelectedSequence.Variables.Count;
                SelectedSequence.Variables.Clear();
                SelectedVariable = null;

                StatusMessage = $"{count} variables deleted";
                LogManager.Log($"{count} variables deleted from sequence '{SelectedSequence.Name}'");

                // Update UI
                OnPropertyChanged(nameof(VariableNames));
                OnPropertyChanged(nameof(HasVariables));
            }
        }

        #endregion
    }
}