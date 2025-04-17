using Knaeckebot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Action that reads JSON from the clipboard and executes corresponding actions
    /// </summary>
    public class JsonAction : ActionBase
    {
        private bool _checkClipboard = true;
        private string? _jsonTemplate = null;
        private bool _continueOnError = false;
        private int _offsetX = 0;
        private int _offsetY = 0;
        private int _retryCount = 3;
        private int _retryWaitTime = 1000;

        /// <summary>
        /// Indicates whether the clipboard should be checked for JSON
        /// </summary>
        public bool CheckClipboard
        {
            get => _checkClipboard;
            set
            {
                if (_checkClipboard != value)
                {
                    _checkClipboard = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// JSON template as reference (optional)
        /// </summary>
        public string? JsonTemplate
        {
            get => _jsonTemplate;
            set
            {
                if (_jsonTemplate != value)
                {
                    _jsonTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether to continue on errors
        /// </summary>
        public bool ContinueOnError
        {
            get => _continueOnError;
            set
            {
                if (_continueOnError != value)
                {
                    _continueOnError = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// X-offset for click coordinates
        /// </summary>
        public int OffsetX
        {
            get => _offsetX;
            set
            {
                if (_offsetX != value)
                {
                    _offsetX = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Y-offset for click coordinates
        /// </summary>
        public int OffsetY
        {
            get => _offsetY;
            set
            {
                if (_offsetY != value)
                {
                    _offsetY = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number of retry attempts for erroneous JSON
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set
            {
                if (_retryCount != value)
                {
                    _retryCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Wait time in ms between retry attempts
        /// </summary>
        public int RetryWaitTime
        {
            get => _retryWaitTime;
            set
            {
                if (_retryWaitTime != value)
                {
                    _retryWaitTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the JSON action with cancellation support
        /// </summary>
        public override void Execute(CancellationToken cancellationToken)
        {
            int currentRetry = 0;
            bool success = false;

            while (currentRetry <= RetryCount && !success)
            {
                // Check for cancellation at each retry
                if (cancellationToken.IsCancellationRequested)
                {
                    LogManager.Log("JSON action cancelled by user");
                    throw new OperationCanceledException();
                }

                try
                {
                    if (currentRetry > 0)
                    {
                        LogManager.Log($"JSON action retry attempt {currentRetry}/{RetryCount} after {RetryWaitTime}ms wait time");

                        // Wait before retry with cancellation checks
                        for (int i = 0; i < RetryWaitTime; i += 100)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();

                            int sleepTime = Math.Min(100, RetryWaitTime - i);
                            Thread.Sleep(sleepTime);
                        }
                    }

                    LogManager.Log($"Executing JSON action [ID: {Id}, Clipboard: {CheckClipboard}, Template: {(JsonTemplate != null ? "available" : "not available")}, ContinueOnError: {ContinueOnError}, Attempt: {currentRetry + 1}/{RetryCount + 1}]");

                    // Read JSON from clipboard if enabled
                    string? jsonText = null;

                    if (CheckClipboard)
                    {
                        // Read text from clipboard
                        var clipboardText = GetClipboardText();

                        if (string.IsNullOrEmpty(clipboardText))
                        {
                            LogManager.Log("No text found in clipboard");
                            currentRetry++;
                            continue;
                        }

                        LogManager.Log($"Clipboard content (first 100 characters): {clipboardText.Substring(0, Math.Min(clipboardText.Length, 100))}");
                        LogManager.Log($"Total length of clipboard text: {clipboardText.Length} characters");

                        // Output some basic characteristics of the text
                        LogManager.Log($"Text begins with: '{clipboardText.Substring(0, Math.Min(clipboardText.Length, 10))}'");
                        LogManager.Log($"Text contains '{'{'}': {clipboardText.Contains('{')}");
                        LogManager.Log($"Text contains '{'}'}': {clipboardText.Contains('}')}");
                        LogManager.Log($"Text contains '[': {clipboardText.Contains('[')}");
                        LogManager.Log($"Text contains ']': {clipboardText.Contains(']')}");

                        // Try to extract JSON from the text
                        jsonText = ExtractJsonFromText(clipboardText);

                        if (string.IsNullOrEmpty(jsonText))
                        {
                            LogManager.Log("No valid JSON found in clipboard");
                            currentRetry++;
                            continue;
                        }

                        LogManager.Log($"Extracted JSON (first 100 characters): {jsonText.Substring(0, Math.Min(jsonText.Length, 100))}");
                        LogManager.Log($"Total length of extracted JSON: {jsonText.Length} characters");
                    }
                    else if (!string.IsNullOrEmpty(JsonTemplate))
                    {
                        jsonText = JsonTemplate;
                        LogManager.Log($"Using JSON template (first 100 characters): {jsonText.Substring(0, Math.Min(jsonText.Length, 100))}");
                    }
                    else
                    {
                        LogManager.Log("Neither clipboard nor JSON template available");
                        currentRetry++;
                        continue;
                    }

                    // Prepare JSON - escape line breaks and other problematic characters
                    jsonText = SanitizeJsonString(jsonText);

                    // Try to parse the JSON
                    try
                    {
                        LogManager.Log("Begin JSON parsing...");
                        var jsonDoc = JsonDocument.Parse(jsonText);
                        LogManager.Log("JSON parsing successful! JSON structure:");
                        LogManager.Log($"- Root type: {jsonDoc.RootElement.ValueKind}");
                        LogManager.Log($"- Contains 'sequenceName': {jsonDoc.RootElement.TryGetProperty("sequenceName", out _)}");
                        LogManager.Log($"- Contains 'clickAction': {jsonDoc.RootElement.TryGetProperty("clickAction", out _)}");

                        // Describe JSON structure in more detail
                        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            LogManager.Log("JSON object contains the following properties:");
                            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                            {
                                LogManager.Log($"  - {property.Name} ({property.Value.ValueKind})");
                            }
                        }
                        else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            LogManager.Log($"JSON array contains {jsonDoc.RootElement.GetArrayLength()} elements");
                        }

                        ProcessJsonActions(jsonDoc, cancellationToken);
                        success = true;
                    }
                    catch (JsonException ex)
                    {
                        LogManager.Log($"Invalid JSON format: {ex.Message}");
                        LogManager.Log($"JSON parsing error at position: {ex.BytePositionInLine}, line: {ex.LineNumber}");

                        // Try to find and fix the problematic spot
                        if (TryFixJsonAtPosition(ref jsonText, (long)ex.LineNumber, (long)ex.BytePositionInLine))
                        {
                            LogManager.Log("JSON was repaired, trying to parse again");

                            try
                            {
                                var jsonDoc = JsonDocument.Parse(jsonText);
                                LogManager.Log("JSON parsing successful after repair!");

                                ProcessJsonActions(jsonDoc, cancellationToken);
                                success = true;
                            }
                            catch (Exception repairEx)
                            {
                                LogManager.Log($"Error after repair attempt: {repairEx.Message}");
                                currentRetry++;
                            }
                        }
                        else
                        {
                            currentRetry++;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogManager.Log("JSON action cancelled during processing");
                    throw;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogManager.Log("JSON action cancelled during error handling");
                        throw new OperationCanceledException();
                    }

                    LogManager.Log($"Error in JSON action: {ex.Message}");
                    LogManager.Log($"Exception type: {ex.GetType().Name}");
                    LogManager.Log($"Stack trace: {ex.StackTrace}");
                    currentRetry++;
                }
            }

            // If all attempts failed and ContinueOnError is not enabled, throw an exception
            if (!success && !ContinueOnError)
            {
                throw new JsonException("JSON action failed after " + RetryCount + " attempts.");
            }
        }

        /// <summary>
        /// Override base method for backward compatibility
        /// </summary>
        public override void Execute()
        {
            Execute(CancellationToken.None);
        }

        /// <summary>
        /// Cleans and escapes problematic characters in JSON strings
        /// </summary>
        private string SanitizeJsonString(string jsonStr)
        {
            if (string.IsNullOrEmpty(jsonStr))
                return jsonStr;

            LogManager.Log("Cleaning JSON string...");

            // Remember original size for comparison
            int originalLength = jsonStr.Length;

            // Simple approach: Escape all line breaks in string values
            StringBuilder result = new StringBuilder();
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < jsonStr.Length; i++)
            {
                char c = jsonStr[i];

                if (escaped)
                {
                    // Always include characters after an escape character
                    result.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    // Escape character, next character is escaped
                    result.Append(c);
                    escaped = true;
                    continue;
                }

                if (c == '"' && !escaped)
                {
                    // Start or end of a string
                    inString = !inString;
                    result.Append(c);
                    continue;
                }

                if (inString)
                {
                    // Escape unescaped control characters in strings
                    if (c == '\n')
                    {
                        result.Append("\\n");
                    }
                    else if (c == '\r')
                    {
                        result.Append("\\r");
                    }
                    else if (c == '\t')
                    {
                        result.Append("\\t");
                    }
                    else if (c == '\b')
                    {
                        result.Append("\\b");
                    }
                    else if (c == '\f')
                    {
                        result.Append("\\f");
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
                else
                {
                    // Outside strings, keep characters unchanged
                    result.Append(c);
                }
            }

            string sanitized = result.ToString();
            LogManager.Log($"JSON string cleaned: {originalLength} -> {sanitized.Length} characters");

            return sanitized;
        }

        /// <summary>
        /// Tries to fix JSON at a problematic position
        /// </summary>
        private bool TryFixJsonAtPosition(ref string jsonText, long lineNumber, long bytePositionInLine)
        {
            try
            {
                LogManager.Log($"Trying to fix JSON at position line {lineNumber}, byte {bytePositionInLine}");

                // Split string into lines
                string[] lines = jsonText.Split('\n');

                // Ensure line number is in valid range
                if (lineNumber <= 0 || lineNumber > lines.Length)
                {
                    LogManager.Log($"Invalid line number: {lineNumber} (max: {lines.Length})");
                    return false;
                }

                // Get line (0-based, so -1)
                string problematicLine = lines[lineNumber - 1];

                // Ensure byte position is in valid range
                if (bytePositionInLine <= 0 || bytePositionInLine > problematicLine.Length)
                {
                    LogManager.Log($"Invalid byte position: {bytePositionInLine} (line length: {problematicLine.Length})");
                    return false;
                }

                LogManager.Log($"Problematic line: {problematicLine}");

                // Try to find and fix the problematic character
                int bytePos = (int)bytePositionInLine;

                // Check for common problem sources like unescaped quotes or line breaks
                // Here we focus specifically on newlines in strings that aren't escaped
                if (bytePos < problematicLine.Length)
                {
                    char problematicChar = problematicLine[bytePos];
                    LogManager.Log($"Problematic character: '{problematicChar}' (hex: 0x{((int)problematicChar):X2})");

                    // Problem: '0x0A' is invalid within a JSON string. The string should be correctly escaped.
                    // We assume we're in a string and escape the character

                    StringBuilder correctedLine = new StringBuilder(problematicLine);

                    if (problematicChar == '\n')
                    {
                        // Replace '\n' with '\\n'
                        correctedLine[bytePos] = 'n';
                        correctedLine.Insert(bytePos, '\\');
                        LogManager.Log($"Unescaped '\\n' replaced with '\\\\n'");
                    }
                    else if (problematicChar == '\r')
                    {
                        // Replace '\r' with '\\r'
                        correctedLine[bytePos] = 'r';
                        correctedLine.Insert(bytePos, '\\');
                        LogManager.Log($"Unescaped '\\r' replaced with '\\\\r'");
                    }
                    else if (problematicChar == '\t')
                    {
                        // Replace '\t' with '\\t'
                        correctedLine[bytePos] = 't';
                        correctedLine.Insert(bytePos, '\\');
                        LogManager.Log($"Unescaped '\\t' replaced with '\\\\t'");
                    }
                    else if (problematicChar == '"')
                    {
                        // Replace unescaped '"' with '\\"'
                        correctedLine.Insert(bytePos, '\\');
                        LogManager.Log($"Unescaped '\"' replaced with '\\\"'");
                    }
                    else
                    {
                        // Generic solution: Remove character
                        correctedLine.Remove(bytePos, 1);
                        LogManager.Log($"Problematic character removed");
                    }

                    // Write corrected line back
                    lines[lineNumber - 1] = correctedLine.ToString();

                    // Rebuild string
                    jsonText = string.Join('\n', lines);
                    LogManager.Log("JSON corrected!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error during repair attempt: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Extracts JSON from text with additional cleaning
        /// </summary>
        private string? ExtractJsonFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                LogManager.Log("ExtractJsonFromText: Text is empty or null");
                return null;
            }

            LogManager.Log("Begin JSON extraction...");

            // Look for JSON objects (starting with { and ending with })
            int objectStart = text.IndexOf('{');
            if (objectStart >= 0)
            {
                LogManager.Log($"'{'{'}' found at position {objectStart}");
                int objectEnd = FindMatchingBrace(text, objectStart, '{', '}');

                if (objectEnd > objectStart)
                {
                    LogManager.Log($"Matching closing brace found at position {objectEnd}");
                    string jsonCandidate = text.Substring(objectStart, objectEnd - objectStart + 1);
                    LogManager.Log($"JSON candidate (length: {jsonCandidate.Length}): {jsonCandidate.Substring(0, Math.Min(jsonCandidate.Length, 50))}...");

                    // Clean the JSON for better processing
                    string sanitizedJson = SanitizeJsonString(jsonCandidate);

                    bool isValid = IsValidJson(sanitizedJson);
                    LogManager.Log($"JSON candidate is valid JSON: {isValid}");

                    if (isValid)
                        return sanitizedJson;

                    // If still not valid after cleaning, try to fix common problems
                    string fixedJson = FixCommonJsonIssues(jsonCandidate);
                    isValid = IsValidJson(fixedJson);
                    LogManager.Log($"Repaired JSON is valid JSON: {isValid}");

                    if (isValid)
                        return fixedJson;
                }
                else
                {
                    LogManager.Log($"No matching closing brace found");
                }
            }
            else
            {
                LogManager.Log($"No '{'{'}' found in text");
            }

            // Look for JSON arrays (starting with [ and ending with ])
            int arrayStart = text.IndexOf('[');
            if (arrayStart >= 0)
            {
                LogManager.Log($"'[' found at position {arrayStart}");
                int arrayEnd = FindMatchingBrace(text, arrayStart, '[', ']');

                if (arrayEnd > arrayStart)
                {
                    LogManager.Log($"Matching closing bracket ']' found at position {arrayEnd}");
                    string jsonCandidate = text.Substring(arrayStart, arrayEnd - arrayStart + 1);
                    LogManager.Log($"JSON candidate (length: {jsonCandidate.Length}): {jsonCandidate.Substring(0, Math.Min(jsonCandidate.Length, 50))}...");

                    // Clean the JSON for better processing
                    string sanitizedJson = SanitizeJsonString(jsonCandidate);

                    bool isValid = IsValidJson(sanitizedJson);
                    LogManager.Log($"JSON candidate is valid JSON: {isValid}");

                    if (isValid)
                        return sanitizedJson;

                    // If still not valid after cleaning, try to fix common problems
                    string fixedJson = FixCommonJsonIssues(jsonCandidate);
                    isValid = IsValidJson(fixedJson);
                    LogManager.Log($"Repaired JSON is valid JSON: {isValid}");

                    if (isValid)
                        return fixedJson;
                }
                else
                {
                    LogManager.Log($"No matching closing bracket ']' found");
                }
            }
            else
            {
                LogManager.Log($"No '[' found in text");
            }

            LogManager.Log("No valid JSON structures found in text");
            return null;
        }

        /// <summary>
        /// Tries to fix common JSON issues
        /// </summary>
        private string FixCommonJsonIssues(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
                return jsonText;

            LogManager.Log("Trying to fix common JSON issues...");

            // Problem 1: Unescaped line breaks in strings
            Regex stringRegex = new Regex("\"(?:[^\\\\\"]|\\\\.)*\"", RegexOptions.Compiled);
            StringBuilder result = new StringBuilder(jsonText);

            // Replace each string with a cleaned version
            foreach (Match match in stringRegex.Matches(jsonText))
            {
                string originalString = match.Value;
                string cleanString = originalString
                    .Replace("\r\n", "\\n")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\b", "\\b")
                    .Replace("\f", "\\f");

                if (originalString != cleanString)
                {
                    // The string was changed, replace it
                    int startIndex = match.Index;
                    int length = match.Length;

                    result.Remove(startIndex, length);
                    result.Insert(startIndex, cleanString);

                    LogManager.Log($"String cleaned: {originalString.Substring(0, Math.Min(20, originalString.Length))}... -> {cleanString.Substring(0, Math.Min(20, cleanString.Length))}...");
                }
            }

            string fixedJson = result.ToString();
            LogManager.Log($"JSON after repair: {fixedJson.Substring(0, Math.Min(100, fixedJson.Length))}...");

            return fixedJson;
        }

        /// <summary>
        /// Finds the position of the matching closing bracket
        /// </summary>
        private int FindMatchingBrace(string text, int openBracePosition, char openBrace, char closeBrace)
        {
            LogManager.Log($"Looking for matching bracket to '{openBrace}' at position {openBracePosition}");

            // We start with depth 1 since we're at the position of the opening bracket
            int depth = 1;
            bool inString = false;
            char previousChar = '\0';

            // Start after the opening bracket
            for (int i = openBracePosition + 1; i < text.Length; i++)
            {
                char c = text[i];

                // Detect string boundaries (but consider escape sequences)
                if (c == '"' && previousChar != '\\')
                {
                    inString = !inString;
                    if (inString)
                        LogManager.Log($"String start at position {i}");
                    else
                        LogManager.Log($"String end at position {i}");
                }
                // Only count brackets outside strings
                else if (!inString)
                {
                    if (c == openBrace)
                    {
                        depth++;
                        LogManager.Log($"Opening bracket '{openBrace}' at position {i}, new depth: {depth}");
                    }
                    else if (c == closeBrace)
                    {
                        depth--;
                        LogManager.Log($"Closing bracket '{closeBrace}' at position {i}, new depth: {depth}");

                        // When we're back at depth 0, we've found the matching closing bracket
                        if (depth == 0)
                        {
                            LogManager.Log($"Matching closing bracket found at position {i}");
                            return i; // Matching closing bracket found
                        }
                    }
                }

                previousChar = c;
            }

            LogManager.Log("No matching closing bracket found");
            return -1; // No matching bracket found
        }

        /// <summary>
        /// Checks if a string is valid JSON
        /// </summary>
        private bool IsValidJson(string? jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                LogManager.Log("IsValidJson: JSON string is empty or null");
                return false;
            }

            try
            {
                // Try to parse the JSON with relaxed options
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 64
                };

                using (var doc = JsonDocument.Parse(jsonString, options))
                {
                    LogManager.Log($"JSON successfully parsed, root type: {doc.RootElement.ValueKind}");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                LogManager.Log($"JSON is invalid: {ex.Message}");
                LogManager.Log($"Error at position: {ex.BytePositionInLine}, line: {ex.LineNumber}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Other error during JSON parsing: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads text from the clipboard
        /// </summary>
        private string? GetClipboardText()
        {
            try
            {
                LogManager.Log("Trying to read text from clipboard...");

                // Use System.Windows.Forms.Clipboard explicitly to avoid ambiguous references
                if (System.Windows.Forms.Clipboard.ContainsText())
                {
                    var text = System.Windows.Forms.Clipboard.GetText();
                    LogManager.Log($"Text read from clipboard, length: {text.Length} characters");
                    return text;
                }
                else
                {
                    LogManager.Log("Clipboard doesn't contain text");
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error reading from clipboard: {ex.Message}");
                LogManager.Log($"Exception type: {ex.GetType().Name}");
                LogManager.Log($"Stack trace: {ex.StackTrace}");
            }

            LogManager.Log("No text read from clipboard");
            return null;
        }

        /// <summary>
        /// Processes the actions from the JSON with cancellation support
        /// </summary>
        private void ProcessJsonActions(JsonDocument json, CancellationToken cancellationToken)
        {
            // Check cancellation first
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            LogManager.Log("Processing JSON actions...");

            // Check if a sequence ID is present
            if (json.RootElement.TryGetProperty("sequenceName", out JsonElement sequenceNameElement))
            {
                // Check cancellation after each significant step
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                var sequenceName = sequenceNameElement.GetString();
                if (!string.IsNullOrEmpty(sequenceName))
                {
                    LogManager.Log($"JSON contains sequence name: {sequenceName}");

                    // Extract variables from JSON
                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    if (json.RootElement.TryGetProperty("variables", out JsonElement variablesElement) &&
                        variablesElement.ValueKind == JsonValueKind.Object)
                    {
                        LogManager.Log("JSON contains variables");

                        foreach (var property in variablesElement.EnumerateObject())
                        {
                            string variableName = property.Name;
                            string variableValue = property.Value.ToString();
                            variables[variableName] = variableValue;
                            LogManager.Log($"Variable found: {variableName} = {variableValue}");
                        }
                    }

                    // Check for cancellation before executing the sequence
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();

                    // Execute the sequence with the extracted variables
                    bool success = variables.Count > 0
                        ? SequenceManager.ExecuteSequenceWithVariables(sequenceName, variables)
                        : SequenceManager.ExecuteSequenceByName(sequenceName);

                    LogManager.Log($"Sequence execution successful: {success}");

                    if (!success && !ContinueOnError)
                    {
                        LogManager.Log($"Sequence '{sequenceName}' could not be found or executed, and ContinueOnError is false");
                        throw new InvalidOperationException($"Sequence '{sequenceName}' could not be found or executed.");
                    }

                    // No return here anymore, so it continues after sequence execution!
                }
            }
            else
            {
                LogManager.Log("JSON doesn't contain a sequence name");
            }

            // Check for cancellation before processing click coordinates
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            // Check if click coordinates are present
            if (json.RootElement.TryGetProperty("clickAction", out JsonElement clickActionElement))
            {
                LogManager.Log("JSON contains clickAction");

                if (clickActionElement.TryGetProperty("x", out JsonElement xElement) &&
                    clickActionElement.TryGetProperty("y", out JsonElement yElement))
                {
                    LogManager.Log("clickAction contains x and y coordinates");

                    try
                    {
                        int x = xElement.GetInt32() + OffsetX;
                        int y = yElement.GetInt32() + OffsetY;

                        LogManager.Log($"JSON contains click coordinates: ({x}, {y}) with offset ({OffsetX}, {OffsetY})");

                        // Check for cancellation before executing mouse click
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        // Use MouseService to click at the coordinates
                        LogManager.Log($"Performing mouse click at position ({x}, {y})");
                        Services.MouseService.Instance.ClickOnPosition(x, y);
                        LogManager.Log("Mouse click executed");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // Re-throw cancellation exceptions
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log($"Error processing click coordinates: {ex.Message}");
                        LogManager.Log($"Exception type: {ex.GetType().Name}");

                        if (!ContinueOnError)
                        {
                            LogManager.Log("ContinueOnError is false, throwing exception");
                            throw;
                        }
                    }
                }
                else
                {
                    LogManager.Log("clickAction doesn't contain valid x and y coordinates");
                }
            }
            else
            {
                LogManager.Log("JSON doesn't contain a clickAction");
            }

            // Check for cancellation before processing wait command
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            // Check if a wait command is present
            if (json.RootElement.TryGetProperty("waitTime", out JsonElement waitTimeElement))
            {
                try
                {
                    int waitTime = waitTimeElement.GetInt32();
                    LogManager.Log($"JSON contains wait time: {waitTime} ms");

                    // Implement wait with cancellation support
                    for (int i = 0; i < waitTime; i += 100)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        int sleepTime = Math.Min(100, waitTime - i);
                        Thread.Sleep(sleepTime);
                    }

                    LogManager.Log($"Wait time of {waitTime} ms completed");
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Re-throw cancellation exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Error processing wait time: {ex.Message}");
                    if (!ContinueOnError)
                    {
                        throw;
                    }
                }
            }

            LogManager.Log("No valid actions found in JSON");
        }

        /// <summary>
        /// Creates a sample JSON for sequence control
        /// </summary>
        public static string CreateSequenceJson(string sequenceName, Dictionary<string, string>? variables = null)
        {
            LogManager.Log($"Creating sample JSON for sequence: {sequenceName}");

            var jsonObj = new Dictionary<string, object>
            {
                ["sequenceName"] = sequenceName,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["repeatCount"] = 1,
                    ["delay"] = 0
                }
            };

            // Add variables if present
            if (variables != null && variables.Count > 0)
            {
                jsonObj["variables"] = variables;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonObj, options);
            LogManager.Log($"Generated JSON: {json}");
            return json;
        }

        /// <summary>
        /// Creates a sample JSON for a mouse click
        /// </summary>
        public static string CreateClickJson(int x, int y)
        {
            LogManager.Log($"Creating sample JSON for click at position ({x}, {y})");

            var jsonObj = new Dictionary<string, object>
            {
                ["clickAction"] = new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["type"] = "leftClick"
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonObj, options);
            LogManager.Log($"Generated JSON: {json}");
            return json;
        }

        /// <summary>
        /// Creates a sample JSON for a wait command
        /// </summary>
        public static string CreateWaitJson(int waitTime)
        {
            LogManager.Log($"Creating sample JSON for wait time: {waitTime} ms");

            var jsonObj = new Dictionary<string, object>
            {
                ["waitTime"] = waitTime
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonObj, options);
            LogManager.Log($"Generated JSON: {json}");
            return json;
        }

        /// <summary>
        /// Creates a copy of this JSON action
        /// </summary>
        public override ActionBase Clone()
        {
            LogManager.Log($"Cloning JSON action: {Name}");

            return new JsonAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                CheckClipboard = this.CheckClipboard,
                JsonTemplate = this.JsonTemplate,
                ContinueOnError = this.ContinueOnError,
                OffsetX = this.OffsetX,
                OffsetY = this.OffsetY,
                RetryCount = this.RetryCount,
                RetryWaitTime = this.RetryWaitTime
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return $"JSON action: {(CheckClipboard ? "From clipboard" : "From template")}";
        }
    }
}