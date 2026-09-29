using System;
using System.Security.Cryptography;
using System.Text;

namespace Bloomlings.Content.Packs
{
    /// <summary>Thrown when a pack does not match its manifest entry (FR-078): it is never activated.</summary>
    public sealed class ContentIntegrityException : Exception
    {
        public ContentIntegrityException(string packId, string message)
            : base($"pack '{packId}': {message}")
        {
            PackId = packId;
        }

        public string PackId { get; }
    }

    /// <summary>SHA-256 and byte-length checks for packs listed in a <see cref="ContentManifest"/> (research R5, R6).</summary>
    public static class PackIntegrity
    {
        public static string ComputeSha256Hex(byte[] data)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>Throws <see cref="ContentIntegrityException"/> unless the length and the hash both match.</summary>
        public static void Verify(PackEntry entry, byte[] data)
        {
            if (data.LongLength != entry.Bytes)
            {
                throw new ContentIntegrityException(entry.Id, $"expected {entry.Bytes} bytes, got {data.LongLength}");
            }

            string actual = ComputeSha256Hex(data);
            if (!string.Equals(actual, entry.Sha256, StringComparison.Ordinal))
            {
                throw new ContentIntegrityException(entry.Id, $"SHA-256 mismatch: expected {entry.Sha256}, got {actual}");
            }
        }

        public static bool IsSha256Hex(string text)
        {
            if (text.Length != 64)
            {
                return false;
            }

            foreach (char c in text)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
