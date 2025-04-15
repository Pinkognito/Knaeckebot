using Knaeckebot.Services;
using System;
using System.Diagnostics;
using System.Threading;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Action that copies text to the clipboard
    /// </summary>
    public class ClipboardAction : ActionBase
    {
        private string _text = string.Empty;
        private bool _appendToClipboard = false;
        private int _retryCount = 3;
        private int _retryWaitTime = 100;
        private bool _useVariable = false;
        private string _variableName = string.Empty;
        private bool _isPropertyChanging = false; // Flag to avoid recursive property changes

        /// <summary>
        /// Text to be copied to the clipboard
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    string oldValue = _text;
                    _text = value ?? string.Empty;
                    LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: Text changed from '{oldValue}' to '{value}'");
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether the text should be appended to existing clipboard content
        /// </summary>
        public bool AppendToClipboard
        {
            get => _appendToClipboard;
            set
            {
                if (_appendToClipboard != value)
                {
                    _appendToClipboard = value;
                    LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: AppendToClipboard set to '{value}'");
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether a sequence variable should be used as text
        /// </summary>
        public bool UseVariable
        {
            get
            {
                LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: UseVariable retrieved, value is '{_useVariable}'");
                return _useVariable;
            }
            set
            {
                LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: Attempting to set UseVariable to '{value}', current value: '{_useVariable}'");

                // Output stack trace to see where the call is coming from
                var stackTrace = new StackTrace();
                LogManager.Log($"### STACKTRACE for UseVariable setter: \n{stackTrace}");

                if (_useVariable != value)
                {
                    // Avoid recursive calls
                    if (_isPropertyChanging)
                    {
                        LogManager.Log($"### WARNING: Avoided recursive call in UseVariable");
                        return;
                    }

                    _isPropertyChanging = true;
                    try
                    {
                        bool oldValue = _useVariable;
                        _useVariable = value;
                        LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: UseVariable CHANGED from '{oldValue}' to '{value}'");

                        // Check if the value was actually changed
                        if (_useVariable != value)
                        {
                            LogManager.Log($"### CRITICAL: UseVariable could not be set! Should be '{value}', but is '{_useVariable}'");
                        }

                        // Trigger additional PropertyChanged events for dependent properties
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(VariableName));
                        OnPropertyChanged(nameof(Text));
                    }
                    finally
                    {
                        _isPropertyChanging = false;
                    }
                }
                else
                {
                    LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: UseVariable unchanged (already '{value}')");
                }
            }
        }

        /// <summary>
        /// Name of the variable to use
        /// </summary>
        public string VariableName
        {
            get
            {
                LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: VariableName retrieved, value is '{_variableName}'");
                return _variableName;
            }
            set
            {
                if (_variableName != value)
                {
                    string oldValue = _variableName;
                    _variableName = value ?? string.Empty;
                    LogManager.Log($"### ClipboardAction ID {Id.ToString().Substring(0, 8)}: VariableName CHANGED from '{oldValue}' to '{value}'");
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number of retry attempts for errors
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
        /// Executes the clipboard action
        /// </summary>
        public override void Execute()
        {
            int currentRetry = 0;
            bool success = false;

            // Explicit debug output of properties
            LogManager.Log($"### ClipboardAction Execution: ID={Id}");
            LogManager.Log($"###   UseVariable = {UseVariable}");
            LogManager.Log($"###   VariableName = '{VariableName}'");
            LogManager.Log($"###   Text = '{Text}'");
            LogManager.Log($"###   AppendToClipboard = {AppendToClipboard}");

            while (currentRetry <= RetryCount && !success)
            {
                try
                {
                    if (currentRetry > 0)
                    {
                        LogManager.Log($"Clipboard action retry attempt {currentRetry}/{RetryCount} after {RetryWaitTime}ms wait time");
                        // Wait before retry
                        Thread.Sleep(RetryWaitTime);
                    }

                    // When using a variable, get the current value
                    string textToUse = Text;

                    if (UseVariable)
                    {
                        LogManager.Log($"### UseVariable is true - trying to use variable");

                        if (string.IsNullOrEmpty(VariableName))
                        {
                            LogManager.Log("### VariableName is empty, using text value");
                        }
                        else
                        {
                            // Get current sequence from SequenceManager
                            var currentSequence = SequenceManager.CurrentSequence;
                            if (currentSequence == null)
                            {
                                LogManager.Log("### No current sequence found - using the first available");

                                // If no sequence is set as active, use the one currently selected in the UI
                                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                                {
                                    var mainWindow = System.Windows.Application.Current.MainWindow;
                                    if (mainWindow?.DataContext is ViewModels.MainViewModel viewModel &&
                                        viewModel.SelectedSequence != null)
                                    {
                                        currentSequence = viewModel.SelectedSequence;
                                        LogManager.Log($"### Using sequence from UI: {currentSequence.Name}");
                                    }
                                }

                                if (currentSequence == null)
                                {
                                    LogManager.Log("### No sequence found, using direct text");
                                }
                            }

                            if (currentSequence != null)
                            {
                                var variable = currentSequence.FindVariableByName(VariableName);
                                if (variable != null)
                                {
                                    textToUse = variable.GetValueAsString();
                                    LogManager.Log($"### Variable '{VariableName}' found, value: '{textToUse}'");
                                }
                                else
                                {
                                    LogManager.Log($"### Variable '{VariableName}' not found, using text value");
                                }
                            }
                        }
                    }
                    else
                    {
                        LogManager.Log("### UseVariable is false - using text value directly");
                    }

                    // Ensure textToUse is not null
                    if (textToUse == null)
                    {
                        textToUse = string.Empty;
                        LogManager.Log("### TextToUse is null, setting to empty string");
                    }

                    // Add default text if empty (to avoid issues with empty strings)
                    if (string.IsNullOrEmpty(textToUse))
                    {
                        textToUse = " "; // A space as minimum entry
                        LogManager.Log("### TextToUse is empty, setting to a space");
                    }

                    LogManager.Log($"### Clipboard action executing with text: '{textToUse}'");

                    // Set text to clipboard
                    SetClipboardText(textToUse);
                    success = true;
                }
                catch (Exception ex)
                {
                    LogManager.Log($"### Error in clipboard action: {ex.Message}");
                    LogManager.Log($"### Exception type: {ex.GetType().Name}");
                    LogManager.Log($"### Stack trace: {ex.StackTrace}");
                    currentRetry++;
                }
            }

            // If all attempts failed, throw an exception
            if (!success)
            {
                throw new Exception("Clipboard action failed after " + RetryCount + " attempts.");
            }
        }

        /// <summary>
        /// Sets text to the clipboard
        /// </summary>
        private void SetClipboardText(string text)
        {
            try
            {
                // Ensure text is not null
                string safeText = text ?? " "; // At least a space

                LogManager.Log($"### SetClipboardText called with text: '{safeText}'");

                // Execute in UI thread if not already in STA thread
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PerformClipboardOperation(safeText);
                    });
                }
                else
                {
                    PerformClipboardOperation(safeText);
                }

                LogManager.Log($"### Text successfully set to clipboard: {(safeText.Length > 30 ? safeText.Substring(0, 27) + "..." : safeText)}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"### Error setting clipboard: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs the actual clipboard operation
        /// </summary>
        private void PerformClipboardOperation(string text)
        {
            try
            {
                // Ensure text is not null or empty
                string textToSet = !string.IsNullOrEmpty(text) ? text : " "; // At least a space

                LogManager.Log($"### PerformClipboardOperation - TextToSet before append: '{textToSet}'");

                // If append is enabled, get existing content
                if (AppendToClipboard)
                {
                    try
                    {
                        // Use WPF Clipboard instead of Windows.Forms
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            string currentText = System.Windows.Clipboard.GetText();
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                textToSet = currentText + textToSet;
                                LogManager.Log($"### Existing text in clipboard: {(currentText.Length > 30 ? currentText.Substring(0, 27) + "..." : currentText)}");
                                LogManager.Log($"### PerformClipboardOperation - TextToSet after append: '{textToSet}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log($"### Error reading clipboard: {ex.Message}, continuing with original text");
                    }
                }

                // Ensure text is not empty
                if (string.IsNullOrEmpty(textToSet))
                {
                    textToSet = " "; // At least a space
                    LogManager.Log("### TextToSet is empty, setting to a space");
                }

                // Set text to clipboard with WPF Clipboard instead of Windows.Forms
                LogManager.Log($"### Setting text to clipboard with WPF: '{textToSet}'");
                System.Windows.Clipboard.SetText(textToSet);
            }
            catch (Exception ex)
            {
                LogManager.Log($"### Error in PerformClipboardOperation: {ex.Message}");

                // Try alternative method if the first fails
                try
                {
                    LogManager.Log("### Trying alternative method with DataObject");
                    var dataObject = new System.Windows.DataObject();
                    dataObject.SetData(System.Windows.DataFormats.Text, " "); // Simple space
                    System.Windows.Clipboard.SetDataObject(dataObject, true);
                    LogManager.Log("### Alternative method successful");
                }
                catch (Exception innerEx)
                {
                    LogManager.Log($"### Alternative method also failed: {innerEx.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Creates a copy of this clipboard action
        /// </summary>
        public override ActionBase Clone()
        {
            LogManager.Log($"### Cloning clipboard action: {Name}, UseVariable={UseVariable}, VariableName={VariableName}");

            // IMPORTANT: When cloning, UseVariable and VariableName must be copied correctly
            var clone = new ClipboardAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                Text = this.Text,
                AppendToClipboard = this.AppendToClipboard,
                UseVariable = this.UseVariable,
                VariableName = this.VariableName,
                RetryCount = this.RetryCount,
                RetryWaitTime = this.RetryWaitTime
            };

            LogManager.Log($"### Cloning completed: ID={clone.Id.ToString().Substring(0, 8)}, UseVariable={clone.UseVariable}, VariableName='{clone.VariableName}'");
            return clone;
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            if (UseVariable && !string.IsNullOrEmpty(VariableName))
            {
                return $"Clipboard: Variable \"{VariableName}\"" + (AppendToClipboard ? " (append)" : "");
            }
            else
            {
                string textPreview = Text != null && Text.Length > 20 ? Text.Substring(0, 17) + "..." : Text;
                return $"Clipboard: \"{textPreview}\"" + (AppendToClipboard ? " (append)" : "");
            }
        }
    }
}