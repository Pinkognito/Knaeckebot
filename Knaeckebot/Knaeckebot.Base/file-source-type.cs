namespace Knaeckebot.Models
{
    /// <summary>
    /// Source type for the file path in a FileAction
    /// </summary>
    public enum FileSourceType
    {
        Text,
        Clipboard,
        Variable
    }

    /// <summary>
    /// Destination type for the file content in a FileAction
    /// </summary>
    public enum FileDestinationType
    {
        Variable,
        Clipboard
    }

    /// <summary>
    /// Encoding type for reading files in a FileAction
    /// </summary>
    public enum FileEncodingType
    {
        UTF8,
        ASCII,
        UTF16,
        UTF32,
        Default
    }
}