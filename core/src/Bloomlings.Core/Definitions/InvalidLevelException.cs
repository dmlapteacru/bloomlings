using System;

namespace Bloomlings.Core.Definitions
{
    /// <summary>Thrown when a level definition and its picture cannot form a valid board.</summary>
    public sealed class InvalidLevelException : Exception
    {
        public InvalidLevelException(string message)
            : base(message)
        {
        }
    }
}
