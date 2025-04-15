using Knaeckebot.Models;
using Knaeckebot.Services;
using System;
using System.Linq;
using System.Windows.Input;

namespace Knaeckebot.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Forces a UI update when the selected action changes
        /// </summary>
        private void ForceUIUpdate()
        {
            LogManager.Log("### ForceUIUpdate is being executed", LogLevel.Debug);

            // If the selected action is a ClipboardAction, handle specially
            if (SelectedAction is ClipboardAction clipAction)
            {
                LogManager.Log($"### ForceUIUpdate for ClipboardAction {clipAction.Id.ToString().Substring(0, 8)}", LogLevel.Debug);
                LogManager.Log($"### ClipboardAction status: UseVariable={clipAction.UseVariable}, VariableName={clipAction.VariableName}", LogLevel.Debug);

                // Check if we have this status in the cache
                if (_actionStateCache.TryGetValue(clipAction.Id, out var cachedState))
                {
                    var state = (dynamic)cachedState;
                    LogManager.Log($"### Cache status: UseVariable={state.UseVariable}, VariableName={state.VariableName}", LogLevel.Debug);

                    // If the cache status differs, we should restore the status from the cache
                    if (state.UseVariable != clipAction.UseVariable)
                    {
                        LogManager.Log($"!!! CRITICAL: UseVariable not consistent in UI-Update! Cache: {state.UseVariable}, Current: {clipAction.UseVariable}", LogLevel.Error);

                        // Restore status from cache
                        LogManager.Log($"### RESTORATION of UseVariable={state.UseVariable} for ClipboardAction {clipAction.Id.ToString().Substring(0, 8)}", LogLevel.Warning);

                        // Important: Direct access to internal variable to avoid feedback loops
                        try
                        {
                            // This statement ensures that the UI is updated correctly
                            clipAction.UseVariable = state.UseVariable;

                            // Update the cache after the change
                            _actionStateCache[clipAction.Id] = new
                            {
                                UseVariable = clipAction.UseVariable,
                                VariableName = clipAction.VariableName,
                                Text = clipAction.Text
                            };

                            // Update UI
                            OnPropertyChanged(nameof(SelectedAction));
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log($"!!! ERROR during restoration: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
            }

            // Update KeyboardAction ViewModels when a KeyboardAction is selected
            if (SelectedAction is KeyboardAction keyboardAction)
            {
                LogManager.Log("KeyboardAction selected - updating ViewModels", LogLevel.KeyDebug);

                // Create new instances of the ViewModel and save
                KeyboardActionViewModel = new KeyboardActionViewModel
                {
                    KeyboardAction = keyboardAction
                };

                LogManager.Log("KeyboardActionViewModel updated", LogLevel.KeyDebug);

                KeyCombinationViewModel = new KeyCombinationViewModel();
                KeyCombinationViewModel.KeyboardAction = keyboardAction;

                LogManager.Log("KeyCombinationViewModel updated", LogLevel.KeyDebug);

                // Track keys in dictionary
                if (!_actionKeysTracker.ContainsKey(keyboardAction.Id))
                {
                    _actionKeysTracker[keyboardAction.Id] = keyboardAction.Keys?.ToArray();
                    LogManager.Log($"Keys for action {keyboardAction.Id.ToString().Substring(0, 8)} added to tracker", LogLevel.KeyDebug);
                }

                // Compare the saved keys with the current ones
                if (_actionKeysTracker.TryGetValue(keyboardAction.Id, out var trackedKeys))
                {
                    bool keysMatch = AreKeyArraysEqual(trackedKeys, keyboardAction.Keys);
                    LogManager.Log($"Keys comparison for action {keyboardAction.Id.ToString().Substring(0, 8)}: {keysMatch}", LogLevel.KeyDebug);

                    if (!keysMatch)
                    {
                        LogManager.Log("Keys in tracker and action do not match!", LogLevel.Warning);
                        LogManager.LogKeyArray(trackedKeys, "Tracker");
                        LogManager.LogKeyArray(keyboardAction.Keys, "Action");

                        // Update tracker
                        _actionKeysTracker[keyboardAction.Id] = keyboardAction.Keys?.ToArray();
                    }
                }
            }

            // Update variable names if a variable action or clipboard action is selected
            if (SelectedAction is VariableAction || (SelectedAction is ClipboardAction clipAction2 && clipAction2.UseVariable))
            {
                OnPropertyChanged(nameof(VariableNames));
            }

            // Update modifier properties
            OnPropertyChanged(nameof(IsCtrlModifierPressed));
            OnPropertyChanged(nameof(IsAltModifierPressed));
            OnPropertyChanged(nameof(IsShiftModifierPressed));
            OnPropertyChanged(nameof(MainKey));
            OnPropertyChanged(nameof(MainKeyItem));

            // Update the display of saved keys
            OnPropertyChanged(nameof(SavedKeys));

            LogManager.Log("### ForceUIUpdate completed", LogLevel.Debug);
        }

        /// <summary>
        /// Checks if two Key arrays are equal
        /// </summary>
        private bool AreKeyArraysEqual(Key[]? array1, Key[]? array2)
        {
            if (array1 == null && array2 == null) return true;
            if (array1 == null || array2 == null) return false;
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the status message
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (IsRecording)
            {
                StatusMessage = "Recording in progress...";
            }
            else if (IsPlaying)
            {
                StatusMessage = "Sequence is playing...";
            }
            else
            {
                StatusMessage = "Ready";
            }
        }
    }
}