using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>
    /// Durable first-win replay records for a local guest. Store before advancing local progress; failures propagate
    /// to the caller. These records provide neither proof of ownership nor authorization to grant rewards.
    /// </summary>
    public sealed class GuestWinArchive
    {
        public const int MaxLevel = 2500;
        // Evidence is at most 1 MiB. Its canonical JSON is escaped once more inside this checksum envelope.
        private const int MaxArchiveBytes = (2 * WinEvidence.MaxBytes) + 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly string _folder;

        public GuestWinArchive(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("An archive folder is required.", nameof(folder));
            _folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(_folder);
        }

        /// <summary>True only when this call durably stores the first evidence for the level. Existing evidence is never replaced.</summary>
        public bool Store(WinEvidence evidence)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            string path = LevelPath(evidence.Level);
            if (Read(evidence.Level) != null) return false; // also refuses to write over corrupt evidence
            string json = CanonicalJson.Write(new JObject { ["hash"] = evidence.Hash, ["evidence"] = evidence.Write() }, false);
            byte[] bytes = Utf8.GetBytes(json);
            if (bytes.Length > MaxArchiveBytes) throw new InvalidDataException("Guest win archive entry exceeds its size limit.");
            string temp = Path.Combine(_folder, ".win-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                try { File.Move(temp, path); } // no overwrite, including a concurrent first writer
                catch (IOException)
                {
                    if (Read(evidence.Level) != null) return false;
                    throw;
                }
                return true;
            }
            finally
            {
                // A leftover temp is never treated as a committed entry. Do not mask the original write failure.
                try { File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>Null means absent. A corrupt, truncated, oversized or misplaced record throws instead of becoming a new win.</summary>
        public WinEvidence? Read(int level)
        {
            string path = LevelPath(level);
            byte[] bytes;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length > MaxArchiveBytes) throw new InvalidDataException("Guest win archive entry exceeds its size limit.");
                bytes = new byte[(int)stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0) throw new InvalidDataException("Guest win archive entry is truncated.");
                    offset += count;
                }
                if (stream.ReadByte() != -1) throw new InvalidDataException("Guest win archive entry changed during read.");
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }

            try
            {
                JObject wrapper = JsonDoc.ParseObject(Utf8.GetString(bytes), "guestWin");
                JsonDoc.AllowOnly(wrapper, "guestWin", "hash", "evidence");
                string hash = JsonDoc.String(JsonDoc.Required(wrapper, "guestWin", "hash"), "hash");
                string json = JsonDoc.String(JsonDoc.Required(wrapper, "guestWin", "evidence"), "evidence");
                WinEvidence evidence = WinEvidence.Read(json);
                if (evidence.Level != level || evidence.Hash != hash || evidence.Write() != json)
                    throw new InvalidDataException("Guest win archive checksum, level or canonical encoding does not match.");
                return evidence;
            }
            catch (ContentFormatException ex) { throw new InvalidDataException("Guest win archive entry is invalid.", ex); }
            catch (DecoderFallbackException ex) { throw new InvalidDataException("Guest win archive entry is not valid UTF-8.", ex); }
        }

        /// <summary>Committed records in ascending level order. Temporary files are ignored; corruption is not silently skipped.</summary>
        public IReadOnlyList<WinEvidence> All()
        {
            var records = new List<WinEvidence>();
            for (int level = 1; level <= MaxLevel; level++)
            {
                WinEvidence? evidence = Read(level);
                if (evidence != null) records.Add(evidence);
            }
            return records.AsReadOnly();
        }

        private string LevelPath(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level), "Archive levels must be 1–2500.");
            return Path.Combine(_folder, "level-" + level.ToString("D5", CultureInfo.InvariantCulture) + ".json");
        }
    }
}
