using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Progression;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Audit the content actually bundled by MSBuild, without generating or solving any levels.</summary>
    public static class LaunchAudit
    {
        public static int Run(ContentSet content, string root, string output)
        {
            int count = int.Parse(XDocument.Load(Path.Combine(root,"content/launch.props")).Descendants("LaunchLevelCount").Single().Value);
            using var plan = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"content/launch-art/plan.json")));
            var slots = plan.RootElement.GetProperty("slots").EnumerateArray().ToDictionary(s=>s.GetProperty("level").GetInt32());
            var errors = new List<string>();
            var firstVariants = new SortedDictionary<string,int>(StringComparer.Ordinal);
            var firstMechanics = new SortedDictionary<string,int>(StringComparer.Ordinal);
            var early = new List<object>();
            var sizes = new SortedDictionary<string,int>(StringComparer.Ordinal);
            int audited = 0;
            for(int n=1;n<=count;n++)
            {
                if(!content.TryGetLevel(n,out var level)) { errors.Add("Missing bundled L"+n); continue; }
                audited++;
                var picture=content.GetPicture(level.Picture);
                var slot=slots[n];
                if(picture.Width!=slot.GetProperty("width").GetInt32()||picture.Height!=slot.GetProperty("height").GetInt32()||level.Picture.Id!=slot.GetProperty("pictureId").GetString())
                    errors.Add("L"+n+" differs from its frozen picture/size slot");
                if(n<=10)
                {
                    var canonical=DefinitionJson.Read(File.ReadAllText(Path.Combine(root,$"content/curated/level-{n:0000}.json")));
                    if(DefinitionJson.Write(level)!=DefinitionJson.Write(canonical))errors.Add("L"+n+" is not the canonical tutorial");
                }
                var variants=level.Pods.Select(p=>p.Variant).Distinct().OrderBy(v=>v).ToArray();
                foreach(var variant in variants)
                {
                    string id=variant.ToString(); if(!firstVariants.ContainsKey(id))firstVariants[id]=n;
                    if(!VariantPool.Default.IsAvailable(variant,n,UnlockRoadmap.Default))errors.Add("L"+n+" uses unavailable variant "+id);
                }
                var mechanics=LevelMechanics.UnlocksUsed(level,picture).Where(id=>id.StartsWith("mechanic.",StringComparison.Ordinal)).ToArray();
                foreach(string id in mechanics)
                {
                    if(!firstMechanics.ContainsKey(id))firstMechanics[id]=n;
                    int? unlock=UnlockRoadmap.Default.LevelOf(id);
                    if(unlock==null||n<unlock.Value)errors.Add("L"+n+" uses "+id+" before its unlock");
                }
                string size=picture.Width+"x"+picture.Height;sizes[size]=sizes.TryGetValue(size,out int existing)?existing+1:1;
                if(n<=25)early.Add(new {level=n,picture=level.Picture.Id,width=picture.Width,height=picture.Height,variants=variants.Select(v=>v.ToString()).ToArray(),mechanics});
            }
            var report=new {bundledLaunchLevels=audited,expectedLaunchLevels=count,canonicalTutorials=10,
                firstVariantLevels=firstVariants,firstMechanicLevels=firstMechanics,boardSizeCounts=sizes,first25Levels=early,errors,
                scope="Bundled identities, tutorial equality, frozen picture sizes, mechanic/variant availability; no solver or regeneration"};
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"launch-audit.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})+"\n");
            Console.WriteLine($"Launch audit: {audited}/{count} bundled levels; {errors.Count} errors. Report: {Path.Combine(output,"launch-audit.json")}");
            foreach(string error in errors.Take(20))Console.WriteLine(error);
            return errors.Count==0?0:1;
        }
    }
}
