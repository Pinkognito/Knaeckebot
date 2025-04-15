using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Knaeckebot.Models;
using Knaeckebot.Services;

namespace Knaeckebot
{
    /// <summary>
    /// Helper class for checking duplicates in SelectedSequences
    /// </summary>
    public static class SequenceUtils
    {
        /// <summary>
        /// Checks for and removes possible duplicates in SelectedSequences
        /// </summary>
        public static void RemoveDuplicates(ObservableCollection<Sequence> sequences)
        {
            // HashSet for checking duplicates
            var uniqueIds = new HashSet<Guid>();

            // List of sequences to be removed
            var toRemove = new List<Sequence>();

            // Check for duplicates
            foreach (var sequence in sequences)
            {
                if (!uniqueIds.Add(sequence.Id))
                {
                    // The ID is already present - duplicate found
                    toRemove.Add(sequence);
                    LogManager.Log($"Duplicate found in SelectedSequences: {sequence.Name}, ID: {sequence.Id}", LogLevel.Debug);
                }
            }

            // Remove duplicates
            foreach (var duplicate in toRemove)
            {
                sequences.Remove(duplicate);
                LogManager.Log($"Duplicate removed from SelectedSequences: {duplicate.Name}, ID: {duplicate.Id}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Diagnostic method for checking SelectedSequences
        /// </summary>
        public static void LogSelectedSequences(ObservableCollection<Sequence> sequences, string context)
        {
            LogManager.Log($"=== DIAGNOSTIC: SelectedSequences in {context} ===", LogLevel.Debug);
            LogManager.Log($"SelectedSequences.Count: {sequences.Count}", LogLevel.Debug);

            // HashSet for detecting duplicates
            var uniqueIds = new HashSet<Guid>();
            int dupCount = 0;

            foreach (var seq in sequences)
            {
                bool isUnique = uniqueIds.Add(seq.Id);
                LogManager.Log($"  - {(isUnique ? "Unique" : "DUPLICATE")}: {seq.Name}, ID: {seq.Id}", LogLevel.Debug);
                if (!isUnique) dupCount++;
            }

            if (dupCount > 0)
            {
                LogManager.Log($"!!! WARNING: {dupCount} duplicates found in SelectedSequences !!!", LogLevel.Warning);
            }

            LogManager.Log($"=== END DIAGNOSTIC ===", LogLevel.Debug);
        }
    }
}