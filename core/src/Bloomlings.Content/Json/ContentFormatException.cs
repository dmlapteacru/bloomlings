using System;

namespace Bloomlings.Content.Json
{
    /// <summary>Thrown when a content document does not match its schema (level, picture or manifest).</summary>
    public sealed class ContentFormatException : Exception
    {
        public ContentFormatException(string path, string message)
            : base($"{path}: {message}")
        {
            Path = path;
        }

        /// <summary>JSON path of the offending value, for example <c>pods[2].count</c>.</summary>
        public string Path { get; }
    }
}
