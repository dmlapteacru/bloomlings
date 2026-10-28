using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using UnityEngine;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// Loads the published round content (spec 006 FR-060, T060), the game's only levels when a build carries it:
    /// <c>manifest.json</c>, <c>levels/*.jsonl.gz</c> and <c>pictures/*.jsonl.gz</c> as <c>rounds publish</c> writes them
    /// to <c>content/game/rounds/published/</c>.
    /// <list type="bullet">
    /// <item>A player build reads them from <c>StreamingAssets/rounds/</c>, where <c>CiBuild.Build</c> and
    /// Tools/Bloomlings/Import Round Content copy the repository's published set, verified (<c>ImportContentMenu.ImportRoundsFrom</c>).</item>
    /// <item>The Editor reads the repository's published set straight from <c>content/game/rounds/published/</c>, so it never
    /// plays a stale copy; without it, the StreamingAssets copy.</item>
    /// </list>
    /// Every pack the manifest lists is read and checked against the manifest's SHA-256 and length before the game starts
    /// (on Android StreamingAssets sit inside the APK and are read through a web request, so nothing is read lazily from
    /// there); the levels and pictures are parsed on first use (<see cref="RoundContentSet"/>). No manifest means the build
    /// carries no round content (null): the legacy levels stay.
    /// </summary>
    public sealed class RoundContentLoader
    {
        /// <summary>The folder under StreamingAssets that holds the published round content.</summary>
        public const string StreamingFolder = "rounds";

        /// <summary>Where the last successful load came from, for logs.</summary>
        public string Source { get; private set; } = "none";

        /// <summary>The published round content in the repository (<c>rounds publish --out</c>'s committed set).</summary>
        public static string RepositoryPublishedFolder => Path.Combine(DevContent.RepositoryContentFolder, "game", "rounds", "published");

        /// <summary>The bundled copy in a build: <c>StreamingAssets/rounds/</c>.</summary>
        public static string StreamingRoot => Path.Combine(Application.streamingAssetsPath, StreamingFolder);

        /// <summary>
        /// Coroutine: calls <paramref name="onLoaded"/> with the round content, or with null when the build carries none, or
        /// <paramref name="onError"/> when it is damaged or inconsistent.
        /// </summary>
        public IEnumerator Load(Action<RoundContentSet?> onLoaded, Action<Exception> onError)
        {
            // Unity's paths are read on the main thread; only the file reading and checking runs on a worker.
            string repository = RepositoryPublishedFolder;
            if (Application.isEditor && File.Exists(Path.Combine(repository, RoundContentManifest.FileName)))
            {
                Task<RoundContentSet> read = Task.Run(() => ReadFolder(repository));
                yield return new WaitUntil(() => read.IsCompleted);
                if (read.IsFaulted || read.IsCanceled)
                {
                    onError(Unwrap(read.Exception));
                    yield break;
                }

                Source = "repository content/game/rounds/published";
                onLoaded(read.Result);
                yield break;
            }

            string streaming = StreamingRoot;
            byte[]? manifestBytes = null;
            yield return BundledContentLoader.ReadBytes(Path.Combine(streaming, RoundContentManifest.FileName), bytes => manifestBytes = bytes, _ => { });
            if (manifestBytes == null)
            {
                Source = "none";
                onLoaded(null);
                yield break;
            }

            RoundContentManifest manifest;
            try
            {
                manifest = RoundContentManifest.Read(Encoding.UTF8.GetString(manifestBytes));
            }
            catch (Exception ex)
            {
                onError(ex);
                yield break;
            }

            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                string? error = null;
                yield return BundledContentLoader.ReadBytes(Path.Combine(streaming, pack.Path), bytes => files[pack.Path] = bytes, message => error = message);
                if (error != null)
                {
                    onError(new FileNotFoundException($"Round pack '{pack.Id}' could not be read: {error}", pack.Path));
                    yield break;
                }
            }

            Task<RoundContentSet> verify = Task.Run(() => FromFiles(manifest, files));
            yield return new WaitUntil(() => verify.IsCompleted);
            if (verify.IsFaulted || verify.IsCanceled)
            {
                onError(Unwrap(verify.Exception));
                yield break;
            }

            Source = $"StreamingAssets rounds v{manifest.ContentVersion}";
            onLoaded(verify.Result);
        }

        /// <summary>
        /// The round content of a published folder on disk (the repository's, or an imported copy), every pack checked
        /// against the manifest first. Engine-free: the DotnetCheck tests read the published set through it, as the Editor does.
        /// </summary>
        /// <exception cref="ContentIntegrityException">No manifest, a missing or damaged pack, or an inconsistent manifest.</exception>
        public static RoundContentSet ReadFolder(string folder)
        {
            string manifestPath = Path.Combine(folder, RoundContentManifest.FileName);
            if (!File.Exists(manifestPath))
            {
                throw new ContentIntegrityException("rounds", "no published round content at " + folder);
            }

            RoundContentManifest manifest = RoundContentManifest.Read(File.ReadAllText(manifestPath));
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                string path = Path.Combine(folder, pack.Path);
                if (!File.Exists(path))
                {
                    throw new ContentIntegrityException(pack.Id, "the published folder does not carry " + pack.Path);
                }

                files[pack.Path] = File.ReadAllBytes(path);
            }

            return FromFiles(manifest, files);
        }

        /// <summary>
        /// The round content of a manifest and its pack files (by the manifest's relative paths): every pack's SHA-256 and
        /// length are checked now; the levels and pictures are parsed on first use.
        /// </summary>
        public static RoundContentSet FromFiles(RoundContentManifest manifest, IReadOnlyDictionary<string, byte[]> files)
        {
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                if (!files.TryGetValue(pack.Path, out byte[]? bytes))
                {
                    throw new ContentIntegrityException(pack.Id, "the build does not carry " + pack.Path);
                }

                pack.Verify(bytes);
            }

            return new RoundContentSet(manifest, path => files.TryGetValue(path, out byte[]? bytes) ? bytes : null);
        }

        private static Exception Unwrap(AggregateException? exception)
        {
            if (exception == null)
            {
                return new OperationCanceledException("Round content loading was cancelled.");
            }

            return exception.InnerExceptions.Count == 1 ? exception.InnerExceptions[0] : exception;
        }
    }
}
