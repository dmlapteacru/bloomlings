using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bloomlings.Content.Packs
{
    /// <summary>
    /// The pack container: gzip-compressed JSON Lines, UTF-8 without BOM, one compact canonical document per line, each
    /// line ending in "\n" (research R5).
    /// </summary>
    public static class JsonLinesPack
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static byte[] Compress(IEnumerable<string> lines)
        {
            var text = new StringBuilder();
            foreach (string line in lines)
            {
                if (line.IndexOf('\n') >= 0)
                {
                    throw new ArgumentException("A JSON Lines entry must not contain a newline.", nameof(lines));
                }

                text.Append(line).Append('\n');
            }

            byte[] raw = Utf8.GetBytes(text.ToString());
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(raw, 0, raw.Length);
            }

            byte[] bytes = output.ToArray();

            // Normalize the gzip header (RFC 1952): no modification time, "unknown" OS, so the bytes do not depend on
            // the machine that published them.
            if (bytes.Length >= 10)
            {
                bytes[4] = 0;
                bytes[5] = 0;
                bytes[6] = 0;
                bytes[7] = 0;
                bytes[9] = 255;
            }

            return bytes;
        }

        /// <summary>Decompresses a pack and returns its non-empty lines in order.</summary>
        public static IReadOnlyList<string> ReadLines(byte[] packBytes, string packId)
        {
            string text;
            try
            {
                using var input = new MemoryStream(packBytes, writable: false);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Utf8, detectEncodingFromByteOrderMarks: false);
                text = reader.ReadToEnd();
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is DecoderFallbackException)
            {
                throw new ContentIntegrityException(packId, "not a valid gzip UTF-8 pack: " + ex.Message);
            }

            var lines = new List<string>();
            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                {
                    lines.Add(trimmed);
                }
            }

            return lines;
        }
    }
}
