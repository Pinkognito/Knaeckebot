using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Knaeckebot
{
    /// <summary>
    /// Helper class for key manipulations
    /// </summary>
    public static class KeyHelper
    {
        /// <summary>
        /// Returns an alphabetically sorted list of all keys
        /// </summary>
        public static IEnumerable<KeyItem> GetSortedKeyValues()
        {
            return Enum.GetValues(typeof(Key))
                .Cast<Key>()
                .Where(k => k != Key.None && k != Key.ImeProcessed &&
                           k != Key.DeadCharProcessed && k != Key.NoName)
                .OrderBy(k => IsModifierKey(k) ? 0 : 1) // Show modifier keys first
                .ThenBy(k => k.ToString())
                .Select(k => new KeyItem(k));
        }

        /// <summary>
        /// Returns a list of all available keys for use in dropdown selections
        /// </summary>
        public static IEnumerable<KeyItem> GetAllKeys()
        {
            // This method returns the same result as GetSortedKeyValues but was added
            // to accommodate existing code that calls GetAllKeys()
            return GetSortedKeyValues();
        }

        /// <summary>
        /// Checks if a key is a modifier key
        /// </summary>
        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.System || key == Key.LWin || key == Key.RWin;
        }

        /// <summary>
        /// Creates an array with modifier keys and main key
        /// </summary>
        public static Key[] CreateKeyCombination(bool ctrl, bool alt, bool shift, Key mainKey)
        {
            var keys = new List<Key>();

            if (ctrl) keys.Add(Key.LeftCtrl);
            if (alt) keys.Add(Key.LeftAlt);
            if (shift) keys.Add(Key.LeftShift);

            if (mainKey != Key.None)
                keys.Add(mainKey);

            return keys.ToArray();
        }

        /// <summary>
        /// Checks if the Keys array contains the specified modifier key
        /// </summary>
        public static bool HasModifierKey(Key[] keys, Key modifierKey)
        {
            if (keys == null) return false;

            return keys.Contains(modifierKey) ||
                  (modifierKey == Key.LeftCtrl && keys.Contains(Key.RightCtrl)) ||
                  (modifierKey == Key.LeftAlt && keys.Contains(Key.RightAlt)) ||
                  (modifierKey == Key.LeftShift && keys.Contains(Key.RightShift));
        }
    }

    /// <summary>
    /// Wrapper class for keys with better display
    /// </summary>
    public class KeyItem
    {
        public Key KeyValue { get; }
        public string DisplayValue { get; }

        public KeyItem(Key key)
        {
            KeyValue = key;
            DisplayValue = GetDisplayNameForKey(key);
        }

        private string GetDisplayNameForKey(Key key)
        {
            // Customizations for specific keys
            switch (key)
            {
                case Key.Return: return "Enter";
                case Key.Escape: return "Esc";
                case Key.OemPlus: return "+ (Plus)";
                case Key.OemMinus: return "- (Minus)";
                case Key.OemQuestion: return "? (Question Mark)";
                case Key.OemPeriod: return ". (Period)";
                case Key.OemComma: return ", (Comma)";
                case Key.OemQuotes: return "' (Quote)";
                case Key.LeftCtrl: return "Ctrl (Left)";
                case Key.RightCtrl: return "Ctrl (Right)";
                case Key.LeftAlt: return "Alt (Left)";
                case Key.RightAlt: return "Alt (Right)";
                case Key.LeftShift: return "Shift (Left)";
                case Key.RightShift: return "Shift (Right)";
                case Key.D0: return "0";
                case Key.D1: return "1";
                case Key.D2: return "2";
                case Key.D3: return "3";
                case Key.D4: return "4";
                case Key.D5: return "5";
                case Key.D6: return "6";
                case Key.D7: return "7";
                case Key.D8: return "8";
                case Key.D9: return "9";
                case Key.None: return "[No Key]";
                default: return key.ToString();
            }
        }

        public override string ToString()
        {
            return DisplayValue;
        }

        public static implicit operator Key(KeyItem item)
        {
            return item.KeyValue;
        }

        public static implicit operator KeyItem(Key key)
        {
            return new KeyItem(key);
        }
    }
}