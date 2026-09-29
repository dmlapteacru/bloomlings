using System.Collections.Generic;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>Reads the <c>pictures</c> pack: the whole picture library, one base picture per line (research R5, R7).</summary>
    public static class PicturePackReader
    {
        public static IReadOnlyList<BasePicture> Read(PackEntry entry, byte[] packBytes)
        {
            if (entry.Kind != PackKind.Pictures)
            {
                throw new ContentIntegrityException(entry.Id, $"expected a pictures pack, got {entry.Kind}");
            }

            PackIntegrity.Verify(entry, packBytes);
            return ReadUnverified(packBytes, entry.Id);
        }

        public static IReadOnlyList<BasePicture> ReadUnverified(byte[] packBytes, string packId)
        {
            IReadOnlyList<string> lines = JsonLinesPack.ReadLines(packBytes, packId);
            var pictures = new BasePicture[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                string where = $"{packId}:{i + 1}";
                pictures[i] = BasePictureJson.Read(JsonDoc.ParseObject(lines[i], where), where);
            }

            return pictures;
        }

        public static byte[] Write(IEnumerable<BasePicture> pictures)
        {
            var lines = new List<string>();
            foreach (BasePicture picture in pictures)
            {
                lines.Add(BasePictureJson.Write(picture, indented: false));
            }

            return JsonLinesPack.Compress(lines);
        }
    }
}
