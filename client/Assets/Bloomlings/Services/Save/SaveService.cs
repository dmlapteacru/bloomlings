using System;
using System.IO;
using System.Text;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>Where the loaded save came from.</summary>
    public enum SaveSource
    {
        /// <summary>No valid save: a new profile was created (first launch).</summary>
        New,
        Main,

        /// <summary>The main file was missing or corrupt; the previous version was used.</summary>
        Backup,
    }

    /// <summary>
    /// The local save file (R15, T059): <c>Application.persistentDataPath/save/player_save_v1.json</c>. The file is an
    /// envelope <c>{"checksum": sha256, "save": player-save.v1}</c>; the checksum covers the compact canonical save
    /// document. Writes are atomic: the new file is written to <c>.tmp</c> and then swapped in, keeping the previous
    /// version as <c>.bak</c>. A corrupt or partial main file falls back to the backup. The game saves after a win, a
    /// purchase, a claim, a settings change or a booster use, and never mid-level.
    /// </summary>
    public sealed class SaveService
    {
        public const string FileName = "player_save_v1.json";

        private readonly string _folder;
        private readonly IClock _clock;
        private readonly SaveMigrations _migrations;

        public SaveService(string folder, IClock clock, SaveMigrations? migrations = null)
        {
            _folder = folder;
            _clock = clock;
            _migrations = migrations ?? SaveMigrations.Default;
            Current = PlayerSave.CreateNew(NewPlayerId(), clock.UtcNow);
        }

        public PlayerSave Current { get; private set; }

        /// <summary>The save in <c>Application.persistentDataPath/save/</c>.</summary>
        public static SaveService CreateDefault(IClock clock) =>
            new SaveService(Path.Combine(UnityEngine.Application.persistentDataPath, "save"), clock);

        public SaveSource Source { get; private set; } = SaveSource.New;

        /// <summary>True when no save existed: Level 1 starts directly with no menus (US2 scenario 1).</summary>
        public bool IsFirstLaunch => Source == SaveSource.New;

        public string MainPath => Path.Combine(_folder, FileName);

        public string BackupPath => MainPath + ".bak";

        public string TempPath => MainPath + ".tmp";

        /// <summary>Test seam: runs between the temp write and the swap, to simulate a crash.</summary>
        internal Action? AfterTempWrite { get; set; }

        /// <summary>Loads the main file, else the backup, else creates a new profile. Leftover temp files are removed.</summary>
        public PlayerSave Load()
        {
            Directory.CreateDirectory(_folder);
            if (File.Exists(TempPath))
            {
                File.Delete(TempPath);
            }

            if (TryRead(MainPath, out PlayerSave? main))
            {
                Current = main!;
                Source = SaveSource.Main;
            }
            else if (TryRead(BackupPath, out PlayerSave? backup))
            {
                Current = backup!;
                Source = SaveSource.Backup;
            }
            else
            {
                Current = PlayerSave.CreateNew(NewPlayerId(), _clock.UtcNow);
                Source = SaveSource.New;
            }

            return Current;
        }

        /// <summary>Writes <see cref="Current"/> atomically.</summary>
        public void Save()
        {
            Directory.CreateDirectory(_folder);
            Current.Touch(_clock.UtcNow);
            byte[] bytes = Encoding.UTF8.GetBytes(Envelope(Current));
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            AfterTempWrite?.Invoke();
            if (File.Exists(MainPath))
            {
                try
                {
                    File.Replace(TempPath, MainPath, BackupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(MainPath, BackupPath, overwrite: true);
                    File.Delete(MainPath);
                    File.Move(TempPath, MainPath);
                }
            }
            else
            {
                File.Move(TempPath, MainPath);
            }
        }

        /// <summary>The envelope text written to disk.</summary>
        public static string Envelope(PlayerSave save)
        {
            JObject document = SaveSerializer.ToJObject(save);
            return CanonicalJson.Write(new JObject { ["checksum"] = Checksum(document), ["save"] = document }, indented: true);
        }

        private bool TryRead(string path, out PlayerSave? save)
        {
            save = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                JObject envelope = JsonDoc.ParseObject(File.ReadAllText(path, Encoding.UTF8), path);
                JsonDoc.AllowOnly(envelope, string.Empty, "checksum", "save");
                string checksum = JsonDoc.String(JsonDoc.Required(envelope, string.Empty, "checksum"), "checksum");
                JObject document = JsonDoc.Object(JsonDoc.Required(envelope, string.Empty, "save"), "save");
                if (!string.Equals(checksum, Checksum(document), StringComparison.Ordinal))
                {
                    return false;
                }

                save = SaveSerializer.Read(document, _migrations);
                return true;
            }
            catch (ContentFormatException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static string Checksum(JObject document) =>
            PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(CanonicalJson.Write(document, indented: false)));

        private static string NewPlayerId() => Guid.NewGuid().ToString("N");
    }
}
