using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bloomlings.Content.Packs;
using UnityEngine;
using UnityEngine.Networking;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// Loads the content bundled in <c>StreamingAssets/content/</c> (FR-078, research R6):
    /// <list type="bullet">
    /// <item><c>manifest.json</c> and the packs it lists, verified by SHA-256 before use;</item>
    /// <item>in the Editor and development builds without a manifest, the loose <c>dev/</c> folder
    /// (one JSON file per level and picture, until <c>publish</c> exists); in the Editor without it, the curated
    /// levels in the repository's <c>content/</c> folder.</item>
    /// </list>
    /// On Android StreamingAssets sit inside the APK, so files are read through <see cref="UnityWebRequest"/>; other
    /// platforms read the file system directly. Parsing runs on a worker thread to keep the first frame responsive.
    /// </summary>
    public sealed class BundledContentLoader
    {
        public const string ContentFolder = "content";
        public const string ManifestFile = "manifest.json";
        public const string DevFolder = "dev";

        /// <summary>Where the last successful load came from, for logs and the debug overlay.</summary>
        public string Source { get; private set; } = "none";

        private static string ContentRoot => Path.Combine(Application.streamingAssetsPath, ContentFolder);

        /// <summary>Coroutine: calls exactly one of <paramref name="onLoaded"/> and <paramref name="onError"/>.</summary>
        public IEnumerator Load(Action<ContentSet> onLoaded, Action<Exception> onError)
        {
            string manifestPath = Path.Combine(ContentRoot, ManifestFile);
            byte[]? manifestBytes = null;
            string? manifestError = null;
            yield return ReadBytes(manifestPath, bytes => manifestBytes = bytes, message => manifestError = message);

            if (manifestBytes != null)
            {
                yield return LoadPacked(manifestBytes, onLoaded, onError);
                yield break;
            }

            if (AllowDevFolder)
            {
                yield return LoadDevFolder(onLoaded, onError);
                yield break;
            }

            onError(new FileNotFoundException($"Bundled content manifest not found: {manifestError}", manifestPath));
        }

        /// <summary>The loose dev folder is read only where the file system can list StreamingAssets (not Android).</summary>
        private static bool AllowDevFolder =>
            (Application.isEditor || Debug.isDebugBuild) && Application.platform != RuntimePlatform.Android;

        private IEnumerator LoadPacked(byte[] manifestBytes, Action<ContentSet> onLoaded, Action<Exception> onError)
        {
            ContentManifest manifest;
            try
            {
                manifest = ContentManifest.Read(Encoding.UTF8.GetString(manifestBytes));
            }
            catch (Exception ex)
            {
                onError(ex);
                yield break;
            }

            var packs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (PackEntry entry in manifest.Packs)
            {
                if (entry.Kind == PackKind.Daily)
                {
                    continue; // Read by the daily challenge feature (R19).
                }

                if (entry.Path == null)
                {
                    onError(new InvalidDataException($"Bundled pack '{entry.Id}' has no path; URL packs are downloaded by the content update service."));
                    yield break;
                }

                string? error = null;
                yield return ReadBytes(Path.Combine(ContentRoot, entry.Path), bytes => packs[entry.Id] = bytes, message => error = message);
                if (error != null)
                {
                    onError(new FileNotFoundException($"Pack '{entry.Id}' could not be read: {error}", entry.Path));
                    yield break;
                }
            }

            Task<ContentSet> parse = Task.Run(() => ContentLoader.FromPacks(manifest, entry => packs[entry.Id]));
            yield return new WaitUntil(() => parse.IsCompleted);
            if (parse.IsFaulted || parse.IsCanceled)
            {
                onError(Unwrap(parse.Exception));
                yield break;
            }

            Source = $"manifest v{manifest.ContentVersion}";
            onLoaded(parse.Result);
        }

        private IEnumerator LoadDevFolder(Action<ContentSet> onLoaded, Action<Exception> onError)
        {
            string devRoot = Path.Combine(ContentRoot, DevFolder);
            bool devFolder = Directory.Exists(devRoot);
            if (!devFolder && !Application.isEditor)
            {
                onError(new DirectoryNotFoundException($"No bundled manifest and no dev content folder at {devRoot}."));
                yield break;
            }

            // In the Editor without a dev folder, the curated levels are read straight from the repository.
            Task<ContentSet> parse = Task.Run(() => devFolder ? LooseContentFolder.Load(devRoot) : DevContent.LoadCurated());
            yield return new WaitUntil(() => parse.IsCompleted);
            if (parse.IsFaulted || parse.IsCanceled)
            {
                onError(Unwrap(parse.Exception));
                yield break;
            }

            Source = devFolder ? "dev folder" : "repository content/curated";
            onLoaded(parse.Result);
        }

        /// <summary>Reads a StreamingAssets file: <see cref="UnityWebRequest"/> for URL paths (Android), File IO otherwise.</summary>
        private static IEnumerator ReadBytes(string path, Action<byte[]> onRead, Action<string> onError)
        {
            if (path.Contains("://"))
            {
                using UnityWebRequest request = UnityWebRequest.Get(path);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onRead(request.downloadHandler.data);
                }
                else
                {
                    onError(request.error ?? "request failed");
                }

                yield break;
            }

            if (!File.Exists(path))
            {
                onError("file not found");
                yield break;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException ex)
            {
                onError(ex.Message);
                yield break;
            }

            onRead(bytes);
        }

        private static Exception Unwrap(AggregateException? exception)
        {
            if (exception == null)
            {
                return new OperationCanceledException("Content parsing was cancelled.");
            }

            return exception.InnerExceptions.Count == 1 ? exception.InnerExceptions[0] : exception;
        }
    }
}
