using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using Knaeckebot.Services;
using Clipboard = System.Windows.Clipboard;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Action to read a file's content into a variable or the clipboard
    /// </summary>
    public class FileAction : ActionBase
    {
        private FileSourceType _sourceType = FileSourceType.Text;
        private string _filePath = string.Empty;
        private string _variableName = string.Empty;
        private FileDestinationType _destinationType = FileDestinationType.Variable;
        private string _destinationVariableName = string.Empty;
        private FileEncodingType _fileEncoding = FileEncodingType.UTF8;
        private bool _handleIOException = true;

        /// <summary>
        /// Source type for the file path
        /// </summary>
        public FileSourceType SourceType
        {
            get => _sourceType;
            set
            {
                if (_sourceType != value)
                {
                    _sourceType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Direct file path (used when SourceType is Text)
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Variable name containing the file path (used when SourceType is Variable)
        /// </summary>
        public string VariableName
        {
            get => _variableName;
            set
            {
                if (_variableName != value)
                {
                    _variableName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Destination type for the file content
        /// </summary>
        public FileDestinationType DestinationType
        {
            get => _destinationType;
            set
            {
                if (_destinationType != value)
                {
                    _destinationType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Destination variable name (used when DestinationType is Variable)
        /// </summary>
        public string DestinationVariableName
        {
            get => _destinationVariableName;
            set
            {
                if (_destinationVariableName != value)
                {
                    _destinationVariableName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// File encoding to use when reading the file
        /// </summary>
        public FileEncodingType FileEncoding
        {
            get => _fileEncoding;
            set
            {
                if (_fileEncoding != value)
                {
                    _fileEncoding = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether IO exceptions should be handled
        /// </summary>
        public bool HandleIOException
        {
            get => _handleIOException;
            set
            {
                if (_handleIOException != value)
                {
                    _handleIOException = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the file action
        /// </summary>
        public override void Execute()
        {
            try
            {
                // Get the file path from the source
                string path = GetFilePathFromSource();
                if (string.IsNullOrEmpty(path))
                {
                    LogManager.Log($"File action failed: Empty file path from {SourceType}", LogLevel.Error);
                    return;
                }

                // Read the file content
                string content = ReadFileContent(path);

                // Set the content to the destination
                if (DestinationType == FileDestinationType.Variable)
                {
                    SetVariableContent(content);
                }
                else if (DestinationType == FileDestinationType.Clipboard)
                {
                    SetClipboardContent(content);
                }

                LogManager.Log($"File action completed: Read file '{Path.GetFileName(path)}'");
            }
            catch (Exception ex) when (HandleIOException && 
                (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException))
            {
                LogManager.Log($"File action error (handled): {ex.Message}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                LogManager.Log($"File action error: {ex.Message}", LogLevel.Error);
                throw; // Rethrow any other exceptions
            }
        }

        /// <summary>
        /// Executes the file action with cancellation support
        /// </summary>
        public override void Execute(CancellationToken cancellationToken)
        {
            // Check cancellation before executing
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            Execute();
        }

        /// <summary>
        /// Gets the file path from the selected source
        /// </summary>
        private string GetFilePathFromSource()
        {
            switch (SourceType)
            {
                case FileSourceType.Text:
                    return FilePath;

                case FileSourceType.Clipboard:
                    if (Clipboard.ContainsText())
                    {
                        return Clipboard.GetText().Trim();
                    }
                    LogManager.Log("Clipboard does not contain text for file path", LogLevel.Warning);
                    return string.Empty;

                case FileSourceType.Variable:
                    var currentSequence = SequenceManager.CurrentSequence;
                    if (currentSequence != null)
                    {
                        var variable = currentSequence.FindVariableByName(VariableName);
                        if (variable != null)
                        {
                            return variable.GetValueAsString().Trim();
                        }
                        LogManager.Log($"Variable '{VariableName}' not found for file path", LogLevel.Warning);
                    }
                    else
                    {
                        LogManager.Log("No current sequence found for variable lookup", LogLevel.Warning);
                    }
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Reads the content of the specified file
        /// </summary>
        private string ReadFileContent(string path)
        {
            // Get the encoding
            Encoding encoding = GetEncoding();

            // Read the file
            return File.ReadAllText(path, encoding);
        }

        /// <summary>
        /// Gets the encoding based on the selected FileEncodingType
        /// </summary>
        private Encoding GetEncoding()
        {
            return FileEncoding switch
            {
                FileEncodingType.UTF8 => Encoding.UTF8,
                FileEncodingType.ASCII => Encoding.ASCII,
                FileEncodingType.UTF16 => Encoding.Unicode,
                FileEncodingType.UTF32 => Encoding.UTF32,
                FileEncodingType.Default => Encoding.Default,
                _ => Encoding.UTF8
            };
        }

        /// <summary>
        /// Sets the content to the specified variable
        /// </summary>
        private void SetVariableContent(string content)
        {
            var currentSequence = SequenceManager.CurrentSequence;
            if (currentSequence != null)
            {
                currentSequence.SetVariable(DestinationVariableName, content);
                LogManager.Log($"File content set to variable '{DestinationVariableName}'");
            }
            else
            {
                LogManager.Log("No current sequence found for setting variable", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Sets the content to the clipboard
        /// </summary>
        private void SetClipboardContent(string content)
        {
            Clipboard.SetText(content);
            LogManager.Log("File content copied to clipboard");
        }

        /// <summary>
        /// Creates a copy of this action
        /// </summary>
        public override ActionBase Clone()
        {
            return new FileAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                SourceType = this.SourceType,
                FilePath = this.FilePath,
                VariableName = this.VariableName,
                DestinationType = this.DestinationType,
                DestinationVariableName = this.DestinationVariableName,
                FileEncoding = this.FileEncoding,
                HandleIOException = this.HandleIOException
            };
        }

        /// <summary>
        /// Returns a string representation of this action
        /// </summary>
        public override string ToString()
        {
            string source = SourceType switch
            {
                FileSourceType.Text => $"File: {(string.IsNullOrEmpty(FilePath) ? "not specified" : Path.GetFileName(FilePath))}",
                FileSourceType.Clipboard => "File from clipboard",
                FileSourceType.Variable => $"File from variable: {VariableName}",
                _ => "Unknown source"
            };

            string destination = DestinationType switch
            {
                FileDestinationType.Variable => $"Variable: {DestinationVariableName}",
                FileDestinationType.Clipboard => "Clipboard",
                _ => "Unknown destination"
            };

            return $"Read {source} into {destination}";
        }
    }
}