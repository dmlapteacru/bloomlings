using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Renders the isolated art pilot with the game's actual gameplay board painter.</summary>
    public static class MosaicPilotPreview
    {
        public static void Write(string root, string output)
        {
            string pilot = Path.Combine(root, "content/pictures/pilot");
            var entries = (JArray)JObject.Parse(File.ReadAllText(Path.Combine(pilot, "compositions.json")))["compositions"]!;
            Directory.CreateDirectory(output);
            const int width = 600, height = 1020;
            using var sheet = SKSurface.Create(new SKImageInfo(width * entries.Count, height));
            using var face = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold);
            using var heading = new SKFont(face, 25);
            using var small = new SKFont(face, 19);
            using var ink = new SKPaint { Color = new SKColor(0x36, 0x58, 0x4B), IsAntialias = true };
            int i = 0;
            foreach (JObject entry in entries)
            {
                string id = entry.Value<string>("pictureId")!;
                BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(pilot, "lib", id + ".json")));
                int number = entry.Value<int>("sampleLevel");
                LevelDefinition level = DefinitionJson.Read(File.ReadAllText(Path.Combine(pilot, "samples/levels", $"level-{number:00000}.json")));
                if (level.Picture.Id != id || level.Picture.Mirror != Mirror.None || level.Mapping.Count != picture.Roles.Count)
                    throw new InvalidDataException(id + ": sample identity, mirror or mapping differs from the pilot.");
                foreach (JProperty role in ((JObject)entry["previewMapping"]!).Properties())
                    if (!level.Mapping.TryGetValue(role.Name, out VariantId mapped) || mapped.Key != role.Value.Value<string>())
                        throw new InvalidDataException(id + ": solved sample differs from the selected art palette.");
                using var painter = new SkiaPainter(width, height, new Insets(0, 0));
                painter.BeginFrame();
                painter.FillRect(new Box(0, 0, width, height), new Rgba(232, 246, 235));
                void Text(string text, float y, SKFont font)
                {
                    float x = (width - font.MeasureText(text)) / 2;
                    painter.Canvas.DrawText(text, x, y, font, ink);
                }
                Text(entry.Value<string>("title")!, 56, heading);
                Text($"{picture.Width} × {picture.Height} · {level.Mapping.Values.Distinct().Count()} цветов", 94, small);
                string state = Path.Combine(Path.GetTempPath(), "bloomlings-mosaic-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var content = new ContentSet(0, 100000, new[] { level }, new[] { picture });
                    var app = new DesignApp(state, content, new Silence(), showSplash: false);
                    app.LoadLevel(level.LevelNumber);
                    app.Level!.SkipGuide();
                    app.Level.CloseDemo();
                    BoardPainter.Draw(painter, new Box(25, 140, width - 25, 882), app.Level);
                }
                finally
                {
                    if (Directory.Exists(state)) Directory.Delete(state, recursive: true);
                }
                Text($"{picture.Grid.Sum(row => row.Count(cell => cell >= 0))} клеток · {level.Pods.Count} подов", 931, small);
                Text("Пилот · игровой рендер", 973, small);
                byte[] png = painter.Png();
                File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
                using SKImage image = SKImage.FromEncodedData(png);
                sheet.Canvas.DrawImage(image, i++ * width, 0);
            }
            using SKImage result = sheet.Snapshot();
            using SKData data = result.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(output, "mosaic-pilot.png"), data.ToArray());
            Console.WriteLine($"Mosaic pilot: {entries.Count} actual game board renders → {output}");
        }
    }
}
