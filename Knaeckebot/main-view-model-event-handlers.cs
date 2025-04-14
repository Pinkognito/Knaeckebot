using Knaeckebot.Models;
using Knaeckebot.Services;
using System;
using System.Threading;

namespace Knaeckebot.ViewModels
{
    public partial class MainViewModel
    {
        #region Event Handlers

        /// <summary>
        /// Handles recorded mouse actions
        /// </summary>
        private void OnMouseActionRecorded(object? sender, Models.MouseAction action)
        {
            if (SelectedSequence == null || !IsRecording || !RecordMouse) return;

            // Add action to UI thread collection
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedSequence.Actions.Add(action);
                StatusMessage = $"Mouse click recorded: ({action.X}, {action.Y})";
                LogManager.Log($"Mouse click recorded: {action.ActionType} at position ({action.X}, {action.Y})");
            });
        }

        /// <summary>
        /// Handles recorded mouse wheel actions
        /// </summary>
        private void OnMouseWheelRecorded(object? sender, Models.MouseAction action)
        {
            if (SelectedSequence == null || !IsRecording || !RecordMouse) return;

            // Add action to UI thread collection
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedSequence.Actions.Add(action);
                StatusMessage = $"Mouse wheel recorded: Delta {action.WheelDelta} at position ({action.X}, {action.Y})";
                LogManager.Log($"Mouse wheel recorded: Delta {action.WheelDelta} at position ({action.X}, {action.Y})");
            });
        }

        /// <summary>
        /// Handles recorded keyboard inputs
        /// </summary>
        private void OnKeyActionRecorded(object? sender, KeyboardAction action)
        {
            if (SelectedSequence == null || !IsRecording || !RecordKeyboard) return;

            // Add action to UI thread collection
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogManager.LogKeyboardAction(action, "Recorded keyboard input BEFORE ADDING");

                // Copy action to ensure we have a new instance
                var actionCopy = (KeyboardAction)action.Clone();

                // Update tracker
                _actionKeysTracker[actionCopy.Id] = actionCopy.Keys?.ToArray();
                LogManager.Log($"Tracker for recorded keyboard input {actionCopy.Id.ToString().Substring(0, 8)} initialized", LogLevel.KeyDebug);

                SelectedSequence.Actions.Add(actionCopy);

                if (actionCopy.ActionType == KeyboardActionType.TypeText)
                {
                    StatusMessage = $"Text input recorded: \"{actionCopy.Text}\"";
                    LogManager.Log($"Text input recorded: \"{actionCopy.Text}\"");
                }
                else
                {
                    StatusMessage = $"Key press recorded: {string.Join("+", actionCopy.Keys ?? Array.Empty<System.Windows.Input.Key>())}";
                    LogManager.Log($"Key press recorded: {string.Join("+", actionCopy.Keys ?? Array.Empty<System.Windows.Input.Key>())}");
                }

                LogManager.LogKeyboardAction(actionCopy, "Recorded keyboard input AFTER ADDING");
            });
        }

        /// <summary>
        /// Handles recorded key combinations
        /// </summary>
        private void OnKeyCombinationRecorded(object? sender, KeyboardAction action)
        {
            if (SelectedSequence == null || !IsRecording || !RecordKeyboard) return;

            // Add action to UI thread collection
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogManager.LogKeyboardAction(action, "Recorded key combination BEFORE ADDING");

                // Copy action to ensure we have a new instance
                var actionCopy = (KeyboardAction)action.Clone();

                // Update tracker
                _actionKeysTracker[actionCopy.Id] = actionCopy.Keys?.ToArray();
                LogManager.Log($"Tracker for recorded key combination {actionCopy.Id.ToString().Substring(0, 8)} initialized", LogLevel.KeyDebug);

                SelectedSequence.Actions.Add(actionCopy);
                StatusMessage = $"Key combination recorded: {string.Join("+", actionCopy.Keys ?? Array.Empty<System.Windows.Input.Key>())}";
                LogManager.Log($"Key combination recorded: {string.Join("+", actionCopy.Keys ?? Array.Empty<System.Windows.Input.Key>())}");

                LogManager.LogKeyboardAction(actionCopy, "Recorded key combination AFTER ADDING");
            });
        }

        #endregion
    }
}