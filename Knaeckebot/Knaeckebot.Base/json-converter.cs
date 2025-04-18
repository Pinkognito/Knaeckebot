using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knaeckebot.Models
{
    /// <summary>
    /// JSON converter for the abstract ActionBase class and its derivatives
    /// </summary>
    public class ActionBaseJsonConverter : JsonConverter<ActionBase>
    {
        public override ActionBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // We need to save the original JSON reader position
            var readerAtStart = reader;

            // First read the JSON document as JsonDocument to determine the type
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                // Try to determine the action type
                string? actionType = null;

                if (document.RootElement.TryGetProperty("$type", out JsonElement typeElement))
                {
                    actionType = typeElement.GetString();
                }
                else
                {
                    // Try to guess the type based on the available properties
                    if (document.RootElement.TryGetProperty("ActionType", out JsonElement actionTypeElement))
                    {
                        if (document.RootElement.TryGetProperty("X", out _) &&
                            document.RootElement.TryGetProperty("Y", out _))
                        {
                            actionType = "MouseAction";
                        }
                        else if (document.RootElement.TryGetProperty("Text", out _) ||
                                document.RootElement.TryGetProperty("Keys", out _))
                        {
                            actionType = "KeyboardAction";
                        }
                        else if (document.RootElement.TryGetProperty("JavaScript", out _) ||
                                document.RootElement.TryGetProperty("Selector", out _))
                        {
                            actionType = "BrowserAction";
                        }
                        else if (document.RootElement.TryGetProperty("VariableName", out _) ||
                                document.RootElement.TryGetProperty("IncrementValue", out _) ||
                                document.RootElement.TryGetProperty("ListIndex", out _))
                        {
                            actionType = "VariableAction";
                        }
                    }
                    else if (document.RootElement.TryGetProperty("WaitTime", out _))
                    {
                        actionType = "WaitAction";
                    }
                    else if (document.RootElement.TryGetProperty("JsonTemplate", out _) ||
                             document.RootElement.TryGetProperty("CheckClipboard", out _))
                    {
                        actionType = "JsonAction";
                    }
                    else if (document.RootElement.TryGetProperty("Text", out _) &&
                             document.RootElement.TryGetProperty("AppendToClipboard", out _))
                    {
                        actionType = "ClipboardAction";
                    }
                    else if (document.RootElement.TryGetProperty("MaxIterations", out _) ||
                             document.RootElement.TryGetProperty("LoopActions", out _) ||
                             document.RootElement.TryGetProperty("ListVariableName", out _))
                    {
                        actionType = "LoopAction";
                    }
                    else if (document.RootElement.TryGetProperty("ThenActions", out _) ||
                             document.RootElement.TryGetProperty("ElseActions", out _))
                    {
                        actionType = "IfAction";
                    }
                    else if (document.RootElement.TryGetProperty("FilePath", out _) ||
                             document.RootElement.TryGetProperty("SourceType", out _) ||
                             document.RootElement.TryGetProperty("DestinationType", out _))
                    {
                        actionType = "FileAction";
                    }
                }

                // JSON as string for redeserialization
                string json = document.RootElement.GetRawText();

                // Deserialize corresponding action type
                return actionType switch
                {
                    "MouseAction" => JsonSerializer.Deserialize<MouseAction>(json, options)!,
                    "KeyboardAction" => JsonSerializer.Deserialize<KeyboardAction>(json, options)!,
                    "WaitAction" => JsonSerializer.Deserialize<WaitAction>(json, options)!,
                    "BrowserAction" => JsonSerializer.Deserialize<BrowserAction>(json, options)!,
                    "JsonAction" => JsonSerializer.Deserialize<JsonAction>(json, options)!,
                    "ClipboardAction" => JsonSerializer.Deserialize<ClipboardAction>(json, options)!,
                    "VariableAction" => JsonSerializer.Deserialize<VariableAction>(json, options)!,
                    "LoopAction" => JsonSerializer.Deserialize<LoopAction>(json, options)!,
                    "IfAction" => JsonSerializer.Deserialize<IfAction>(json, options)!,
                    "FileAction" => JsonSerializer.Deserialize<FileAction>(json, options)!,
                    _ => throw new JsonException($"Unknown action type: {actionType ?? "not found"}")
                };
            }
        }

        public override void Write(Utf8JsonWriter writer, ActionBase value, JsonSerializerOptions options)
        {
            // Store the concrete type in JSON
            writer.WriteStartObject();

            // Save type information
            string typeName = value switch
            {
                MouseAction => "MouseAction",
                KeyboardAction => "KeyboardAction",
                WaitAction => "WaitAction",
                BrowserAction => "BrowserAction",
                JsonAction => "JsonAction",
                ClipboardAction => "ClipboardAction",
                VariableAction => "VariableAction",
                LoopAction => "LoopAction",
                IfAction => "IfAction",
                FileAction => "FileAction",
                _ => throw new JsonException($"Unsupported action type: {value.GetType().Name}")
            };

            writer.WriteString("$type", typeName);

            // Write properties of the concrete class
            switch (value)
            {
                case MouseAction mouseAction:
                    WriteProperties(writer, mouseAction, options);
                    break;

                case KeyboardAction keyboardAction:
                    WriteProperties(writer, keyboardAction, options);
                    break;

                case WaitAction waitAction:
                    WriteProperties(writer, waitAction, options);
                    break;

                case BrowserAction browserAction:
                    WriteProperties(writer, browserAction, options);
                    break;

                case JsonAction jsonAction:
                    WriteProperties(writer, jsonAction, options);
                    break;

                case ClipboardAction clipboardAction:
                    WriteProperties(writer, clipboardAction, options);
                    break;

                case VariableAction variableAction:
                    WriteProperties(writer, variableAction, options);
                    break;

                case LoopAction loopAction:
                    WriteProperties(writer, loopAction, options);
                    break;

                case IfAction ifAction:
                    WriteProperties(writer, ifAction, options);
                    break;

                case FileAction fileAction:
                    WriteProperties(writer, fileAction, options);
                    break;

                default:
                    throw new JsonException($"Unsupported action type: {value.GetType().Name}");
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, MouseAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, KeyboardAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, WaitAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, BrowserAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, JsonAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, ClipboardAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, VariableAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, LoopAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, IfAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Writes the properties of an object without the surrounding object frame
        /// </summary>
        private void WriteProperties(Utf8JsonWriter writer, FileAction value, JsonSerializerOptions options)
        {
            WritePropertiesCommon(writer, value, options);
        }

        /// <summary>
        /// Common method for writing the properties of an object
        /// </summary>
        private void WritePropertiesCommon<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // We need to write the properties manually since we've already written the outer object frame
            var json = JsonSerializer.Serialize(value, options);
            using var document = JsonDocument.Parse(json);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                // We've already written $type, so we skip it here
                if (property.Name != "$type")
                {
                    property.WriteTo(writer);
                }
            }
        }
    }

    /// <summary>
    /// JSON converter for SequenceVariable to handle the new variable types
    /// </summary>
    public class SequenceVariableJsonConverter : JsonConverter<SequenceVariable>
    {
        public override SequenceVariable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            var variable = new SequenceVariable();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name");

                string propertyName = reader.GetString() ?? string.Empty;
                reader.Read();

                switch (propertyName)
                {
                    case "Name":
                        variable.Name = reader.GetString() ?? string.Empty;
                        break;

                    case "Type":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            string typeStr = reader.GetString() ?? "Text";
                            if (Enum.TryParse<VariableType>(typeStr, out var type))
                                variable.Type = type;
                        }
                        else if (reader.TokenType == JsonTokenType.Number)
                        {
                            int typeInt = reader.GetInt32();
                            if (Enum.IsDefined(typeof(VariableType), typeInt))
                                variable.Type = (VariableType)typeInt;
                        }
                        break;

                    case "TextValue":
                        variable.TextValue = reader.GetString() ?? string.Empty;
                        break;

                    case "NumberValue":
                        if (reader.TokenType == JsonTokenType.Number)
                            variable.NumberValue = reader.GetInt32();
                        break;

                    case "BoolValue":
                        if (reader.TokenType == JsonTokenType.True)
                            variable.BoolValue = true;
                        else if (reader.TokenType == JsonTokenType.False)
                            variable.BoolValue = false;
                        break;

                    case "ListValue":
                        variable.ListValue = reader.GetString() ?? string.Empty;
                        break;

                    case "Description":
                        variable.Description = reader.GetString() ?? string.Empty;
                        break;

                    default:
                        // Skip unknown properties
                        reader.Skip();
                        break;
                }
            }

            return variable;
        }

        public override void Write(Utf8JsonWriter writer, SequenceVariable value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("Name", value.Name);
            writer.WriteString("Type", value.Type.ToString());
            writer.WriteString("TextValue", value.TextValue);
            writer.WriteNumber("NumberValue", value.NumberValue);
            writer.WriteBoolean("BoolValue", value.BoolValue);
            writer.WriteString("ListValue", value.ListValue);
            writer.WriteString("Description", value.Description);

            writer.WriteEndObject();
        }
    }
}