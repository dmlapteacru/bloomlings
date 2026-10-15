using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Production art review only: actual finished-board painter, with no claim that a puzzle has been solved.</summary>
    public static class ArtLibraryPreview
    {
        public static void Write(string root,string registry,string output,int first,int last)
        {
            JObject book=JObject.Parse(File.ReadAllText(registry));
            var entries=((JArray)book["compositions"]!).Cast<JObject>().Where(e=>e.Value<int>("launchLevel")>=first&&e.Value<int>("launchLevel")<=last).OrderBy(e=>e.Value<int>("launchLevel")).ToArray();
            if(entries.Length==0)throw new InvalidDataException("No production art in requested range");
            string library=book.Value<string>("libraryDirectory")!;
            var basis=DefinitionJson.Read(File.ReadAllText(Path.Combine(root,"content/curated/level-0001.json")));
            const int width=330,height=450,columns=4;
            using var sheet=SKSurface.Create(new SKImageInfo(width*columns,height*((entries.Length+columns-1)/columns)));
            sheet.Canvas.Clear(new SKColor(232,246,235));
            using var face=SKTypeface.FromFamilyName("DejaVu Sans",SKFontStyle.Bold);
            using var font=new SKFont(face,16);using var small=new SKFont(face,14);
            using var ink=new SKPaint{Color=new SKColor(54,88,75),IsAntialias=true};
            for(int i=0;i<entries.Length;i++)
            {
                var e=entries[i];string id=e.Value<string>("pictureId")!;
                var picture=BasePictureJson.Read(File.ReadAllText(Path.Combine(library,id+".json")));
                var mapping=new SortedDictionary<string,VariantId>(StringComparer.Ordinal);
                foreach(var role in ((JObject)e["previewMapping"]!).Properties())mapping.Add(role.Name,new VariantId(role.Value.Value<string>()!));
                var definition=basis with{Picture=new PictureRef(id,picture.Version,Mirror.None,"default"),Mapping=mapping};
                using var painter=new SkiaPainter(width,height,new Insets(0,0));painter.BeginFrame();
                painter.FillRect(new Box(0,0,width,height),new Rgba(232,246,235));
                string title=e.Value<string>("title")!;
                while(font.MeasureText(title)>width-20&&title.Length>4)title=title.Substring(0,title.Length-2)+"…";
                painter.Canvas.DrawText(title,10,26,font,ink);
                painter.Canvas.DrawText($"L{e.Value<int>("launchLevel")} · {picture.Width}×{picture.Height} · {mapping.Values.Distinct().Count()} цветов",10,48,small,ink);
                BoardPainter.Picture(painter,new Box(12,65,width-12,height-12),definition,picture,shadow:false);
                using var image=SKImage.FromEncodedData(painter.Png());
                sheet.Canvas.DrawImage(image,(i%columns)*width,(i/columns)*height);
            }
            Directory.CreateDirectory(output);using var result=sheet.Snapshot();using var png=result.Encode(SKEncodedImageFormat.Png,100);
            File.WriteAllBytes(Path.Combine(output,$"art-{first:00000}-{last:00000}.png"),png.ToArray());
            Console.WriteLine($"Production art review: {entries.Length} grids, finished-board painter; gameplay validation is separate.");
        }
    }
}
