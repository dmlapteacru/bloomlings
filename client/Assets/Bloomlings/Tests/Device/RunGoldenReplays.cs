using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.UI;
using Bloomlings.Content.Golden;
using TMPro;
using UnityEngine;

namespace Bloomlings.Client.Tests.Device
{
    /// <summary>
    /// The device determinism check (SC-005, SC-011, T151): runs the golden corpus
    /// (<c>StreamingAssets/golden/</c>, copied by Tools/Bloomlings/Device Tests/Prepare Golden Replays) with the
    /// IL2CPP-compiled core, compares every state hash, events digest and status with the values <c>dotnet test</c>
    /// recorded, shows the result on screen, logs <see cref="GoldenReport.ResultMarker"/> and writes
    /// <c>golden-results.json</c> to the persistent data path. The scene is
    /// <c>Assets/Bloomlings/Tests/Device/RunGoldenReplays.unity</c>.
    /// </summary>
    public sealed class RunGoldenReplays : MonoBehaviour
    {
        public const string Folder = "golden";
        public const string IndexFile = "index.txt";

        private readonly List<GoldenResult> _results = new List<GoldenResult>();

        public IReadOnlyList<GoldenResult> Results => _results;

        public bool Finished { get; private set; }

        private IEnumerator Start()
        {
            Canvas canvas = UiFactory.CreateCanvas("GoldenCanvas", 0);
            canvas.transform.SetParent(transform, false);
            TextMeshProUGUI text = UiFactory.CreateText("Report", canvas.transform, "Running golden replays…", 36f, UiTheme.Text, TextAlignmentOptions.Left);
            UiFactory.Place(text.rectTransform, 0.04f, 0.02f, 0.96f, 0.98f);

            string root = Path.Combine(Application.streamingAssetsPath, Folder);
            string? index = null;
            string? error = null;
            yield return BundledContentLoader.ReadBytes(Path.Combine(root, IndexFile), bytes => index = Encoding.UTF8.GetString(bytes), message => error = message);
            if (index == null)
            {
                text.text = "No golden corpus: " + error;
                Debug.LogError(GoldenReport.ResultMarker + " FAIL 0/0 (no corpus: " + error + ")");
                Finished = true;
                yield break;
            }

            foreach (string line in index.Split('\n'))
            {
                string name = line.Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                string? json = null;
                yield return BundledContentLoader.ReadBytes(Path.Combine(root, name), bytes => json = Encoding.UTF8.GetString(bytes), message => error = message);
                _results.Add(json == null ? new GoldenResult(name, false, string.Empty, string.Empty, string.Empty, error) : Run(name, json));
                text.text = Describe();
                yield return null;
            }

            string platform = Application.platform.ToString();
            string backend = Backend();
            string summary = GoldenReport.SummaryLine(_results, platform, backend);
            text.text = summary + "\n\n" + Describe();
            Debug.Log(summary);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "golden-results.json"), GoldenReport.ToJson(_results, platform, backend));
            Finished = true;
        }

        private static GoldenResult Run(string name, string json)
        {
            try
            {
                GoldenCase golden = GoldenCase.Read(json);
                GoldenOutcome outcome = GoldenRunner.Run(golden);
                bool passed = outcome.StateHash == golden.ExpectedStateHash
                    && outcome.EventsDigest == golden.ExpectedEventsDigest
                    && outcome.Status == golden.ExpectedStatus;
                return new GoldenResult(name, passed, outcome.StateHash, outcome.EventsDigest, GoldenCase.StatusToWire(outcome.Status), passed ? null : "differs from dotnet test");
            }
            catch (Exception ex)
            {
                return new GoldenResult(name, false, string.Empty, string.Empty, string.Empty, ex.GetType().Name + ": " + ex.Message);
            }
        }

        private string Describe()
        {
            var text = new StringBuilder();
            foreach (GoldenResult result in _results)
            {
                text.Append(result.Passed ? "✓ " : "✗ ").Append(result.Name);
                if (result.Error != null)
                {
                    text.Append(" — ").Append(result.Error);
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        private static string Backend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }
    }
}
