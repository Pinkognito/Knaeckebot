using Knaeckebot.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MouseAction = Knaeckebot.Models.MouseAction;

namespace Knaeckebot.Converters
{
    /// <summary>
    /// Converts Boolean values to Visibility
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    /// <summary>
    /// Converts Boolean values inverted to Visibility
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && !boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Inverts a Boolean value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && !boolValue;
        }
    }

    /// <summary>
    /// Converts a Boolean value to a recording status text
    /// </summary>
    public class BoolToRecordingStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue ? "Recording active" : "Inactive";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string stringValue && stringValue == "Recording active";
        }
    }

    /// <summary>
    /// Converts an action to a detail string
    /// </summary>
    public class ActionToDetailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MouseAction mouseAction)
            {
                return $"{mouseAction.ActionType} at position ({mouseAction.X}, {mouseAction.Y})";
            }
            else if (value is KeyboardAction keyboardAction)
            {
                return keyboardAction.ActionType switch
                {
                    KeyboardActionType.TypeText => $"Text: \"{(keyboardAction.Text?.Length > 20 ? keyboardAction.Text.Substring(0, 17) + "..." : keyboardAction.Text)}\"",
                    KeyboardActionType.KeyPress => $"Key: {string.Join("+", keyboardAction.Keys ?? Array.Empty<System.Windows.Input.Key>())}",
                    KeyboardActionType.KeyCombination => $"Combination: {string.Join("+", keyboardAction.Keys ?? Array.Empty<System.Windows.Input.Key>())}",
                    KeyboardActionType.Hotkey => $"Hotkey: {string.Join("+", keyboardAction.Keys ?? Array.Empty<System.Windows.Input.Key>())}",
                    _ => "Keyboard Input"
                };
            }
            else if (value is WaitAction waitAction)
            {
                return $"Wait: {waitAction.WaitTime} ms";
            }
            else if (value is BrowserAction browserAction)
            {
                return browserAction.ActionType switch
                {
                    BrowserActionType.FindElementAndClick => $"Element search: {browserAction.Selector}",
                    BrowserActionType.ExecuteJavaScript => "Execute JavaScript",
                    BrowserActionType.GetCoordinates => $"Coordinates: ({browserAction.XResult}, {browserAction.YResult})",
                    _ => "Browser Action"
                };
            }
            else if (value is JsonAction jsonAction)
            {
                return $"JSON Action: {(jsonAction.CheckClipboard ? "From clipboard" : "From template")}";
            }
            else if (value is ClipboardAction clipboardAction)
            {
                string textPreview = clipboardAction.Text != null && clipboardAction.Text.Length > 20
                    ? clipboardAction.Text.Substring(0, 17) + "..."
                    : clipboardAction.Text;
                return $"Clipboard: \"{textPreview}\"" + (clipboardAction.AppendToClipboard ? " (append)" : "");
            }
            else if (value is LoopAction loopAction)
            {
                return $"Loop: {loopAction.LoopActions.Count} actions, Max: {loopAction.MaxIterations}" +
                       (loopAction.UseCondition ? ", with condition" : "");
            }
            else if (value is VariableAction variableAction)
            {
                switch (variableAction.ActionType)
                {
                    case VariableActionType.ToggleBoolean:
                        return $"Toggle boolean: {variableAction.VariableName}";
                    case VariableActionType.AddListItem:
                        return $"Add to list: {variableAction.VariableName}, Item: \"{EllipsisText(variableAction.Value, 15)}\"";
                    case VariableActionType.RemoveListItem:
                        return $"Remove from list: {variableAction.VariableName}, Index: {variableAction.ListIndex}";
                    case VariableActionType.ClearList:
                        return $"Clear list: {variableAction.VariableName}";
                    case VariableActionType.AddTableRow:
                        return $"Add table row: {variableAction.VariableName}, Row: \"{EllipsisText(variableAction.Value, 15)}\"";
                    default:
                        // Use existing behavior for other types
                        break;
                }
            }

            return value?.ToString() ?? string.Empty;
        }

        private string EllipsisText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > maxLength ? text.Substring(0, maxLength - 3) + "..." : text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an action to a type string
    /// </summary>
    public class ActionToTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                MouseAction => "Mouse Action",
                KeyboardAction => "Keyboard Input",
                WaitAction => "Wait",
                BrowserAction => "Browser Action",
                JsonAction => "JSON Action",
                ClipboardAction => "Clipboard Action",
                VariableAction => "Variable Action",
                LoopAction => "Loop Action",
                IfAction => "If Action",
                _ => "Unknown Action"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a KeyboardActionType to Visibility, depending on the parameter
    /// </summary>
    public class KeyboardActionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not KeyboardActionType actionType || parameter is not string param)
            {
                return Visibility.Collapsed;
            }

            bool visible = param switch
            {
                "TypeText" => actionType == KeyboardActionType.TypeText,
                "Keys" => actionType == KeyboardActionType.KeyPress || actionType == KeyboardActionType.KeyCombination || actionType == KeyboardActionType.Hotkey,
                "KeyCombination" => actionType == KeyboardActionType.KeyCombination || actionType == KeyboardActionType.Hotkey,
                _ => false
            };

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a BrowserActionType to Visibility, depending on the parameter
    /// </summary>
    public class BrowserActionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BrowserActionType actionType || parameter is not string param)
            {
                return Visibility.Collapsed;
            }

            bool visible = param switch
            {
                "FindElementAndClick" => actionType == BrowserActionType.FindElementAndClick,
                "ExecuteJavaScript" => actionType == BrowserActionType.ExecuteJavaScript,
                "GetCoordinates" => actionType == BrowserActionType.GetCoordinates,
                _ => false
            };

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a VariableActionType to Visibility, depending on the parameter
    /// </summary>
    public class VariableActionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not VariableActionType actionType || parameter is not string param)
            {
                return Visibility.Collapsed;
            }

            bool visible = param switch
            {
                "SetValue" => actionType == VariableActionType.SetValue,
                "Increment" => actionType == VariableActionType.Increment,
                "Decrement" => actionType == VariableActionType.Decrement,
                "AppendText" => actionType == VariableActionType.AppendText,
                "ClearValue" => actionType == VariableActionType.ClearValue,
                "ToggleBoolean" => actionType == VariableActionType.ToggleBoolean,
                "AddListItem" => actionType == VariableActionType.AddListItem,
                "RemoveListItem" => actionType == VariableActionType.RemoveListItem,
                "ClearList" => actionType == VariableActionType.ClearList,
                "AddTableRow" => actionType == VariableActionType.AddTableRow,
                "List" => actionType == VariableActionType.AddListItem ||
                          actionType == VariableActionType.RemoveListItem ||
                          actionType == VariableActionType.ClearList ||
                          actionType == VariableActionType.AddTableRow,
                "NeedIndex" => actionType == VariableActionType.RemoveListItem,
                "NeedValue" => actionType == VariableActionType.SetValue ||
                               actionType == VariableActionType.AppendText ||
                               actionType == VariableActionType.AddListItem ||
                               actionType == VariableActionType.AddTableRow,
                _ => false
            };

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an object to Visibility based on its type
    /// </summary>
    public class ObjectToTypeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // Parameter is the name of the type (e.g. "MouseAction")
            string typeName = parameter.ToString() ?? string.Empty;
            string actualTypeName = value.GetType().Name;

            return actualTypeName == typeName ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an enum value to Visibility based on equality
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // Compare both as strings
            string enumValue = value.ToString() ?? string.Empty;
            string paramValue = parameter.ToString() ?? string.Empty;

            return enumValue == paramValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an enum value to a Boolean for RadioButtons
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString() ?? string.Empty;
            string paramValue = parameter.ToString() ?? string.Empty;

            return enumValue.Equals(paramValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool) || !(bool)value)
                return System.Windows.Data.Binding.DoNothing;

            if (targetType.IsEnum && parameter != null)
            {
                try
                {
                    string? paramString = parameter.ToString();
                    if (!string.IsNullOrEmpty(paramString))
                    {
                        return Enum.Parse(targetType, paramString);
                    }
                }
                catch
                {
                    // If the parameter cannot be converted to the enum type
                    return System.Windows.Data.Binding.DoNothing;
                }
            }

            return System.Windows.Data.Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts a Key array to a readable string
    /// </summary>
    public class KeyArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Key[] keys && keys.Length > 0)
            {
                return string.Join(" + ", keys.Select(k => new KeyItem(k).DisplayValue));
            }
            return "(No keys assigned)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a list value to a readable string with line wrapping
    /// </summary>
    public class ListValueToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string listValue && !string.IsNullOrEmpty(listValue))
            {
                // Replace semicolons with commas for display
                string displayValue = listValue.Replace(";", ", ");

                // Limit length for display
                if (displayValue.Length > 50)
                {
                    displayValue = displayValue.Substring(0, 47) + "...";
                }

                return displayValue;
            }
            return "(Empty list)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a numeric value to a Boolean (true if > 0)
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            else if (value is double doubleValue)
            {
                return doubleValue > 0;
            }
            else if (value is long longValue)
            {
                return longValue > 0;
            }
            else if (value is short shortValue)
            {
                return shortValue > 0;
            }
            else if (value is byte byteValue)
            {
                return byteValue > 0;
            }
            // Collection Count property
            else if (value is System.Collections.ICollection collection)
            {
                return collection.Count > 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Key to DisplayString for UI display
    /// </summary>
    public class KeyToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Key key)
            {
                var keyItem = new KeyItem(key);
                return keyItem.DisplayValue;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Converts between Key and KeyItem for bindings in UI
    /// </summary>
    public class KeyToKeyItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Key key)
            {
                return new KeyItem(key);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KeyItem keyItem)
            {
                return keyItem.KeyValue;
            }
            return Key.None;
        }
    }
}