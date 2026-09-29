using System;

namespace Bloomlings.Client.Services.Analytics
{
    /// <summary>
    /// Crash and non-fatal error reporting (FR-086, research R14). The initial provider is Firebase Crashlytics
    /// (<c>Integrations/Firebase</c>). Every report carries the custom keys of <see cref="CrashKeys"/>, so a report can be
    /// reproduced with the pipeline's <c>replay</c> command.
    /// </summary>
    public interface ICrashReporter
    {
        /// <summary>Starts collection; called once, only after consent (T127).</summary>
        void Initialize();

        void SetCustomKey(string key, string value);

        void RecordException(Exception exception);
    }

    /// <summary>The crash custom keys (R14).</summary>
    public static class CrashKeys
    {
        public const string AppVersion = "app_version";
        public const string ContentVersion = "content_version";
        public const string LevelNumber = "level_number";
        public const string DefinitionVersion = "definition_version";
        public const string PictureId = "picture_id";

        public static readonly string[] All = { AppVersion, ContentVersion, LevelNumber, DefinitionVersion, PictureId };
    }

    /// <summary>No crash reporting: without the SDK, or when consent was refused.</summary>
    public sealed class NullCrashReporter : ICrashReporter
    {
        public void Initialize()
        {
        }

        public void SetCustomKey(string key, string value)
        {
        }

        public void RecordException(Exception exception)
        {
        }
    }
}
