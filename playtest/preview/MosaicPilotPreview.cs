using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Renders the isolated art pilot with the game's actual gameplay board painter.</summary>
    public static class MosaicPilotPreview
    {
        public static void Write(string root, string output, string? pilotRoot = null, bool gameStates = false)
        {
            string pilot = pilotRoot ?? Path.Combine(root, "content/pictures/pilot");
            var entries = (JArray)JObject.Parse(File.ReadAllText(Path.Combine(pilot, "compositions.json")))["compositions"]!;
            Directory.CreateDirectory(output);
            const int width = 600, height = 1020;
            int columns = Math.Min(3, entries.Count);
            using var sheet = SKSurface.Create(new SKImageInfo(width * columns, height * ((entries.Count + columns - 1) / columns)));
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
                sheet.Canvas.DrawImage(image, (i % columns) * width, (i / columns) * height);
                i++;
                if (gameStates) WriteGameStates(pilot, output, id, level, picture);
            }
            using SKImage result = sheet.Snapshot();
            using SKData data = result.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(output, "mosaic-pilot.png"), data.ToArray());
            using SKData jpeg = result.Encode(SKEncodedImageFormat.Jpeg, 92);
            File.WriteAllBytes(Path.Combine(output, "mosaic-pilot.jpg"), jpeg.ToArray());
            Console.WriteLine($"Mosaic pilot: {entries.Count} actual game board renders → {output}");
        }

        private static void WriteGameStates(string pilot, string output, string id, LevelDefinition definition, BasePicture picture)
        {
            string state = Path.Combine(Path.GetTempPath(), "bloomlings-art-states-" + Guid.NewGuid().ToString("N"));
            try
            {
                var content = new ContentSet(0, 100000, new[] { definition }, new[] { picture });
                var app = new DesignApp(state, content, new Silence(), showSplash: false);
                app.Meta.SkipTo(definition.LevelNumber - 1);
                app.LoadLevel(definition.LevelNumber);
                LevelScreen level = app.Level!;
                level.SkipGuide(); level.CloseDemo();
                using var painter = new SkiaPainter(540, 1170, new Insets(0,0));
                using var journey = SKSurface.Create(new SKImageInfo(2160,1220));
                journey.Canvas.Clear(new SKColor(232,246,235));
                using var face = SKTypeface.FromFamilyName("DejaVu Sans",SKFontStyle.Bold);
                using var font = new SKFont(face,24);
                using var ink = new SKPaint { Color=new SKColor(54,88,75), IsAntialias=true };
                int captured=0;
                void Frames(int count)
                {
                    for(int f=0; f<count; f++) { painter.Now+=.1f; painter.BeginFrame(); app.Draw(painter,.1f); }
                }
                void Capture(string name)
                {
                    Frames(2);
                    byte[] png=painter.Png();
                    File.WriteAllBytes(Path.Combine(output,id+"-"+name+".png"),png);
                    using SKImage image=SKImage.FromEncodedData(png);
                    journey.Canvas.DrawImage(image,captured*540,50);
                    string label=name switch { "start"=>"Старт", "partial"=>"Частичная очистка", "win"=>"Победа", _=>"Коллекция" };
                    journey.Canvas.DrawText(label,captured*540+20,34,font,ink);
                    captured++;
                }
                Frames(15); Capture("start");
                var record = ValidationRecord.Read(File.ReadAllText(Path.Combine(pilot,"samples/validation",$"level-{definition.LevelNumber:00000}.json")));
                int index=0;
                foreach(Command command in record.SolutionTrace)
                {
                    if(command is not TapPod tap) throw new InvalidDataException("Art pilot requires a booster-free replay.");
                    int wait=0;
                    while(level.Animator.FreeOnScreen(level.Session.View)<1 && wait++<2000) level.Advance(.1f);
                    int before=level.Session.CommandLog.Count;
                    level.Tap(tap.PodId);
                    if(level.Session.CommandLog.Count!=before+1) throw new InvalidDataException(id+": pilot replay tap refused.");
                    level.Advance(.2f);
                    if(++index==record.SolutionTrace.Count/2)
                    {
                        for(wait=0;wait<3000&&!level.Animator.Settled;wait++) level.Advance(.1f);
                        if(level.Won) throw new InvalidDataException(id+": partial state unexpectedly complete.");
                        Capture("partial");
                    }
                }
                for(int wait=0;wait<5000&&!level.Animator.Settled;wait++) level.Advance(.1f);
                if(!level.Won||!level.Animator.Settled) throw new InvalidDataException(id+": pilot did not reach a settled win.");
                Frames(35); Capture("win");
                app.GoHome();
                while(app.Overlays.Count>0) app.CloseOverlay();
                app.OpenCollection(); app.CollectionDetail=0;
                Frames(12); Capture("collection");
                if(app.Meta.Collection.Count!=1) throw new InvalidDataException(id+": completed picture was not collected.");
                using SKImage result=journey.Snapshot();
                using SKData jpeg=result.Encode(SKEncodedImageFormat.Jpeg,92);
                File.WriteAllBytes(Path.Combine(output,id+"-journey.jpg"),jpeg.ToArray());
                Console.WriteLine($"  {id}: start, partial, real win and earned Collection detail checked.");
            }
            finally { if(Directory.Exists(state)) Directory.Delete(state,recursive:true); }
        }
    }
}
