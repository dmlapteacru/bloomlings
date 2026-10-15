using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Bloomlings.Playtest.Design;
using SkiaSharp;
using Bloomlings.AssetPack;

// Input lines: absolute source path|logical asset path. Outputs preserve logical names;
// BitmapFactory/Skia sniff image headers, and HeroFrameStore reads PNG or BPF1.
if (args.Length is < 2 or > 3) throw new ArgumentException("Usage: assetpack <manifest.txt> <output-directory> [UI-max-edge=1024]");
string output = Path.GetFullPath(args[1]);
int maxEdge = args.Length == 3 ? int.Parse(args[2]) : 1024;
if (maxEdge < 512 || maxEdge > 8192) throw new ArgumentOutOfRangeException(nameof(maxEdge));
Directory.CreateDirectory(output);
var entries = File.ReadAllLines(args[0]).Where(s => s.Length != 0).Select(s => s.Split('|')).ToArray();
var report = new ConcurrentBag<Result>();
Parallel.ForEach(entries, new ParallelOptions { MaxDegreeOfParallelism = 2 }, entry =>
{
    string source = entry[0], logical = entry[1].Replace('\\', '/');
    if (!logical.StartsWith("Assets/", StringComparison.Ordinal) || logical.Split('/').Contains("..")) throw new InvalidDataException(logical);
    string target = Path.Combine(output, logical);
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    byte[] original = File.ReadAllBytes(source);
    string hash = Convert.ToHexString(SHA256.HashData(original));
    string stamp = target + ".sha256";
    // The format version also invalidates outputs after changing encoder settings.
    string recipe = $"v4:{maxEdge}:{hash}";
    int sourceWidth, sourceHeight;
    using (var codec = SKCodec.Create(new SKMemoryStream(original)))
    {
        if (codec == null) throw new InvalidDataException("Invalid image: " + logical);
        sourceWidth = codec.Info.Width; sourceHeight = codec.Info.Height;
    }
    if (!File.Exists(target) || !File.Exists(stamp) || File.ReadAllText(stamp) != recipe)
    {
        byte[] packed = original;
        bool jpeg = Path.GetExtension(source).Equals(".jpg", StringComparison.OrdinalIgnoreCase);
        var size = PhoneTextureProfile.Size(logical, sourceWidth, sourceHeight, maxEdge);
        if (!jpeg || size != (sourceWidth, sourceHeight))
        {
            using var sourceBitmap = Decode(original, SKAlphaType.Unpremul);
            if (logical.StartsWith("Assets/heromotion/", StringComparison.Ordinal))
            {
                byte[]? candidate = PackHero(sourceBitmap);
                if (candidate != null && candidate.Length < packed.Length) packed = candidate;
            }
            else
            {
                // All atlas crops and nine-slice borders use ratios of decoded dimensions.
                using var reduced = ResizeUi(original, sourceBitmap.Width, sourceBitmap.Height, PhoneTextureProfile.MaxEdge(logical, maxEdge));
                SKBitmap bitmap = reduced ?? sourceBitmap;
                using var pixels = bitmap.PeekPixels();
                if (reduced != null)
                {
                    using var png = pixels.Encode(jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, jpeg ? 90 : 100) ?? throw new InvalidDataException("Resized image encode failed");
                    packed = png.ToArray();
                }
                using var encoded = jpeg ? null : pixels.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100));
                if (encoded != null && encoded.Size < packed.Length)
                {
                    byte[] candidate = encoded.ToArray();
                    using var after = Decode(candidate, SKAlphaType.Unpremul);
                    if (!SamePixels(bitmap, after))
                        throw new InvalidDataException("Lossless pixel verification failed: " + source);
                    packed = candidate;
                }
            }
        }
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(temporary, packed);
        File.Move(temporary, target, overwrite: true);
        File.WriteAllText(stamp, recipe);
    }
    byte[] header = new byte[4];
    using (var file = File.OpenRead(target)) file.ReadExactly(header);
    var shippedSize = PhoneTextureProfile.Size(logical, sourceWidth, sourceHeight, maxEdge);
    if (header[0] != 'B')
    {
        using var shipped = SKCodec.Create(target) ?? throw new InvalidDataException("Invalid packed image: " + logical);
        if ((shipped.Info.Width, shipped.Info.Height) != shippedSize)
            throw new InvalidDataException("Packed dimensions differ from texture profile: " + logical);
    }
    report.Add(new Result(logical, original.Length, new FileInfo(target).Length,
        header[0] == 'B' ? "BPF1" : header[0] == 'R' ? "WebP lossless" : "PNG/JPEG",
        sourceWidth, sourceHeight, shippedSize.Width, shippedSize.Height,
        (long)sourceWidth * sourceHeight * 4, (long)shippedSize.Width * shippedSize.Height * 4));
});
var sorted = report.OrderBy(e => e.Asset).ToArray();
File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Game assets: {sorted.Sum(e => e.Before) / 1_000_000.0:F2} → {sorted.Sum(e => e.After) / 1_000_000.0:F2} MB ({sorted.Length} files; UI edge {maxEdge}px; lossless PNG/WebP/palette checked, resized JPEG q90)");
var textures = sorted.Where(e => !e.Asset.StartsWith("Assets/heromotion/", StringComparison.Ordinal)).ToArray();
Console.WriteLine($"Regular texture RGBA budget: {textures.Sum(e => e.SourceRgbaBytes) / 1048576.0:F1} → {textures.Sum(e => e.RgbaBytes) / 1048576.0:F1} MiB (all assets; runtime loads visible screens only)");

static SKBitmap Decode(byte[] bytes, SKAlphaType alpha)
{
    using var codec = SKCodec.Create(new SKMemoryStream(bytes)) ?? throw new InvalidDataException("Invalid image");
    var image = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, alpha));
    if (codec.GetPixels(image.Info, image.GetPixels()) != SKCodecResult.Success) { image.Dispose(); throw new InvalidDataException("Image decode failed"); }
    return image;
}

static bool SamePixels(SKBitmap before, SKBitmap after)
{
    if (before.Width != after.Width || before.Height != after.Height) return false;
    ReadOnlySpan<byte> a = before.GetPixelSpan(), b = after.GetPixelSpan();
    for (int i = 0; i < a.Length; i += 4)
    {
        // Compare straight RGBA. PNG/WebP decoders round premultiplication differently;
        // WebP may discard RGB only where alpha is exactly zero (invisible pixels).
        if (a[i + 3] != b[i + 3] || (a[i + 3] != 0 && !a.Slice(i, 3).SequenceEqual(b.Slice(i, 3)))) return false;
    }
    return true;
}

static SKBitmap? ResizeUi(byte[] original, int width, int height, int maxEdge)
{
    if (Math.Max(width, height) <= maxEdge) return null;
    float scale = maxEdge / (float)Math.Max(width, height);
    // Filter premultiplied colors to avoid dark fringes around transparent artwork.
    using var premultiplied = Decode(original, SKAlphaType.Premul);
    using var resized = premultiplied.Resize(new SKImageInfo(Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)), SKColorType.Rgba8888, SKAlphaType.Premul), new SKSamplingOptions(SKFilterMode.Linear))
        ?? throw new InvalidDataException("UI resize failed");
    var straight = new SKBitmap(new SKImageInfo(resized.Width, resized.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
    using var view = resized.PeekPixels();
    if (!view.ReadPixels(straight.Info, straight.GetPixels(), straight.RowBytes, 0, 0))
    { straight.Dispose(); throw new InvalidDataException("UI alpha conversion failed"); }
    return straight;
}

static byte[]? PackHero(SKBitmap image)
{
    var colors = new Dictionary<uint, byte>();
    byte[] palette = new byte[1024], indices = new byte[image.Width * image.Height];
    ReadOnlySpan<byte> pixels = image.GetPixelSpan();
    for (int i = 0; i < indices.Length; i++)
    {
        uint color = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(pixels.Slice(i * 4, 4));
        if (!colors.TryGetValue(color, out byte index))
        {
            if (colors.Count == 256) return null; // Never quantize: keep the original if it is not indexed.
            index = (byte)colors.Count;
            colors.Add(color, index);
            pixels.Slice(i * 4, 4).CopyTo(palette.AsSpan(index * 4, 4));
        }
        indices[i] = index;
    }
    byte[] candidate = PackedPalette.Encode(image.Width, image.Height, palette, indices);
    if (!PackedPalette.TryDecode(candidate, out int w, out int h, out var decodedPalette, out var decodedIndices) || w != image.Width || h != image.Height)
        throw new InvalidDataException("Packed frame round trip failed");
    for (int i = 0; i < indices.Length; i++)
        if (!pixels.Slice(i * 4, 4).SequenceEqual(decodedPalette.AsSpan(decodedIndices[i] * 4, 4)))
            throw new InvalidDataException("Packed frame pixel verification failed");
    return candidate;
}

record Result(string Asset, long Before, long After, string Format, int SourceWidth, int SourceHeight, int Width, int Height, long SourceRgbaBytes, long RgbaBytes);
