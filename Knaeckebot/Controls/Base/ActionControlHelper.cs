using Knaeckebot.Models;
using Knaeckebot.Services;
using Knaeckebot.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using ListView = System.Windows.Controls.ListView;
using Button = System.Windows.Controls.Button;
namespace Knaeckebot.Controls.Base
{
/// <summary>
/// Helper methods for action controls
/// </summary>
public static class ActionControlHelper
{
/// <summary>
/// Helper method to safely parse an integer from a TextBox
/// </summary>
public static bool TryParseTextBoxToInt(TextBox textBox, out int result, int defaultValue = 0)
{
if (textBox == null)
{
result = defaultValue;
return false;
}
        if (int.TryParse(textBox.Text, out result))
        {
            return true;
        }
        else
        {
            result = defaultValue;
            LogManager.Log($"Failed to parse integer from: '{textBox.Text}', using default: {defaultValue}", LogLevel.Warning);
            return false;
        }
    }
    /// <summary>
    /// Update a status message in the view model
    /// </summary>
    public static void UpdateStatusMessage(MainViewModel viewModel, string message)
    {
        if (viewModel != null)
        {
            viewModel.StatusMessage = message;
        }
    }
    /// <summary>
    /// Show a confirmation dialog and return the result
    /// </summary>
    public static MessageBoxResult ShowConfirmationDialog(string message, string title = "Confirm")
    {
        return MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
    }
    /// <summary>
    /// Move selected actions up in the list
    /// </summary>
    public static void MoveActionsUp<T>(ObservableCollection<T> collection, ListView listView) where T : ActionBase
    {
        // Get all selected items
        var selectedItems = listView.SelectedItems.Cast<T>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index so we move from top to bottom
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = collection.IndexOf(item) })
                .OrderBy(x => x.Index)
                .ToList();
            // Check if we can move up (if first item is not at index 0)
            if (itemsWithIndices.First().Index > 0)
            {
                // Remember selected indices
                var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();
                // Move each item up
                foreach (var itemWithIndex in itemsWithIndices)
                {
                    int index = collection.IndexOf(itemWithIndex.Item);
                    if (index > 0)
                    {
                        collection.Move(index, index - 1);
                    }
                }
                // Reselect items
                listView.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index > 0 && index - 1 < collection.Count)
                    {
                        listView.SelectedItems.Add(collection[index - 1]);
                    }
                }
                listView.Focus();
                LogManager.Log($"Moved {selectedItems.Count} actions up", LogLevel.Debug);
            }
        }
    }
    /// <summary>
    /// Move selected actions down in the list
    /// </summary>
    public static void MoveActionsDown<T>(ObservableCollection<T> collection, ListView listView) where T : ActionBase
    {
        // Get all selected items
        var selectedItems = listView.SelectedItems.Cast<T>().ToList();
        if (selectedItems.Count > 0)
        {
            // Sort by index in descending order so we move from bottom to top
            var itemsWithIndices = selectedItems
                .Select(item => new { Item = item, Index = collection.IndexOf(item) })
                .OrderByDescending(x => x.Index)
                .ToList();
            // Check if we can move down (if last item is not at the end)
            if (itemsWithIndices.First().Index < collection.Count - 1)
            {
                // Remember selected indices
                var selectedIndices = itemsWithIndices.Select(x => x.Index).ToList();
                // Move each item down
                foreach (var itemWithIndex in itemsWithIndices)
                {
                    int index = collection.IndexOf(itemWithIndex.Item);
                    if (index < collection.Count - 1)
                    {
                        collection.Move(index, index + 1);
                    }
                }
                // Reselect items
                listView.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (index + 1 < collection.Count)
                    {
                        listView.SelectedItems.Add(collection[index + 1]);
                    }
                }
                listView.Focus();
                LogManager.Log($"Moved {selectedItems.Count} actions down", LogLevel.Debug);
            }
        }
    }
    /// <summary>
    /// Delete selected actions from the list
    /// </summary>
    public static void DeleteActions<T>(ObservableCollection<T> collection, ListView listView, string context = "") where T : ActionBase
    {
        // Get all selected items
        var selectedItems = listView.SelectedItems.Cast<T>().ToList();
        if (selectedItems.Count > 0)
        {
            // Confirmation dialog before deleting
            string message = selectedItems.Count == 1
                ? $"Are you sure you want to remove the action '{selectedItems[0].Name}'{(string.IsNullOrEmpty(context) ? "" : $" from the {context}")}"
                : $"Are you sure you want to remove {selectedItems.Count} actions{(string.IsNullOrEmpty(context) ? "" : $" from the {context}")}";
            var result = ShowConfirmationDialog(message, "Delete Actions");
            if (result == MessageBoxResult.Yes)
            {
                // Find minimum index for selection after deletion
                int firstIndex = selectedItems.Min(item => collection.IndexOf(item));
                // Remove all selected actions
                foreach (var action in selectedItems)
                {
                    collection.Remove(action);
                    LogManager.Log($"Action '{action.Name}' deleted{(string.IsNullOrEmpty(context) ? "" : $" from {context}")}", LogLevel.Debug);
                }
                // Select next element if available
                if (collection.Count > 0)
                {
                    listView.SelectedIndex = Math.Min(firstIndex, collection.Count - 1);
                    listView.Focus();
                }
            }
        }
    }
    /// <summary>
    /// Copy selected actions to the view model's copied actions collection
    /// </summary>
    public static void CopyActionsToCopiedActions(ListView listView, ObservableCollection<ActionBase> clipboard, string context = "")
    {
        if (listView.SelectedItems.Count > 0)
        {
            // Clear the clipboard
            clipboard.Clear();
            // Add each selected action to the clipboard
            foreach (ActionBase action in listView.SelectedItems)
            {
                clipboard.Add(action);
                LogManager.Log($"Action '{action.Name}' copied to clipboard{(string.IsNullOrEmpty(context) ? "" : $" from {context}")}", LogLevel.Debug);
            }
        }
    }
    /// <summary>
    /// Paste actions from the clipboard to the collection
    /// </summary>
    public static void PasteActionsFromCopiedActions<T>(ObservableCollection<T> collection, ListView listView, ObservableCollection<ActionBase> clipboard, string context = "") where T : ActionBase
    {
        if (clipboard.Count == 0) return;
        LogManager.Log($"*** PasteActions START for {context} ***", LogLevel.Debug);
        try
        {
            // Determine insert position
            int insertIndex;
            if (listView.SelectedItems.Count > 0)
            {
                // Find the last selected item
                T lastSelectedItem = (T)listView.SelectedItems[listView.SelectedItems.Count - 1];
                insertIndex = collection.IndexOf(lastSelectedItem) + 1;
            }
            else
            {
                // If nothing is selected, insert at the end
                insertIndex = collection.Count;
            }
            LogManager.Log($"Inserting {clipboard.Count} actions at index {insertIndex}", LogLevel.Debug);
            // Create a list to hold new actions
            List<T> insertedActions = new List<T>();
            // Clone each action and add to collection
            foreach (ActionBase action in clipboard)
            {
                // Clone the action
                ActionBase clonedAction = action.Clone();
                // Only add if the cloned action is of compatible type
                if (clonedAction is T typedAction)
                {
                    // Insert at the right position
                    if (insertIndex < collection.Count)
                    {
                        collection.Insert(insertIndex, typedAction);
                    }
                    else
                    {
                        collection.Add(typedAction);
                    }
                    insertedActions.Add(typedAction);
                    insertIndex++;
                    LogManager.Log($"Inserted clone of action '{action.Name}'", LogLevel.Debug);
                }
                else
                {
                    LogManager.Log($"Skipped incompatible action '{action.Name}' of type {action.GetType().Name}", LogLevel.Warning);
                }
            }
            // Select the newly inserted actions
            listView.SelectedItems.Clear();
            foreach (T action in insertedActions)
            {
                listView.SelectedItems.Add(action);
            }
            LogManager.Log($"Successfully inserted {insertedActions.Count} actions", LogLevel.Info);
        }
        catch (Exception ex)
        {
            LogManager.Log($"Error pasting actions: {ex.Message}", LogLevel.Error);
            LogManager.Log(ex.StackTrace, LogLevel.Error);
        }
        LogManager.Log($"*** PasteActions END for {context} ***", LogLevel.Debug);
    }
    /// <summary>
    /// Update button states based on selection
    /// </summary>
    public static void UpdateButtonStates(ListView listView, Button moveUpButton, Button moveDownButton, Button deleteButton, Button copyButton, Button pasteButton = null, ObservableCollection<ActionBase> clipboard = null, bool isEnabled = true)
    {
        bool hasSelection = listView.SelectedItems.Count > 0 && isEnabled;
        moveUpButton.IsEnabled = hasSelection;
        moveDownButton.IsEnabled = hasSelection;
        deleteButton.IsEnabled = hasSelection;
        copyButton.IsEnabled = hasSelection;
        // If a paste button and clipboard are provided, update its state
        if (pasteButton != null && clipboard != null)
        {
            pasteButton.IsEnabled = isEnabled && clipboard.Count > 0;
        }
    }
}
}
