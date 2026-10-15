using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    public class ArtPaletteTests
    {
        private static string Repo
        {
            get
            {
                for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                    if (File.Exists(Path.Combine(d.FullName, "content/launch.props"))) return d.FullName;
                throw new DirectoryNotFoundException();
            }
        }
        private static string Pilot => Path.Combine(Repo, "content/pictures/pilot-v2");
        private static JObject Recipe(string id)
        {
            JObject entry = JObject.Parse(File.ReadAllText(Path.Combine(Pilot,"compositions.json")))["compositions"]!
                .Cast<JObject>().Single(e => e.Value<string>("pictureId") == id);
            JObject profile = JObject.Parse(File.ReadAllText(Path.Combine(Repo,"content/profiles",entry.Value<string>("sampleProfile")+".json")));
            profile["mappingConstraints"]!["fixedMapping"] = entry["previewMapping"]!.DeepClone();
            profile["mappingConstraints"]!["allowRoleMerge"] = false;
            int colors = ((JObject)entry["previewMapping"]!).Properties().Select(p=>p.Value.Value<string>()).Distinct().Count();
            profile["variantCount"] = new JArray(colors,colors);
            profile["pictureMirror"] = "none";
            return profile;
        }
        private static BasePicture Picture(string id) => BasePictureJson.Read(File.ReadAllText(Path.Combine(Pilot,"lib",id+".json")));

        [Test]
        public void FixedArtPaletteCannotBeSilentlyRecoloredMergedOrMisspelled()
        {
            const string id="icon_leaf_steam_cup";
            GenerationProfile profile=ProfileLoader.Read(Recipe(id).ToString());
            BasePicture picture=Picture(id);
            var mappings=RoleMapper.Mappings(picture,profile,(_,_)=>true);
            Assert.That(mappings, Has.Count.EqualTo(1));
            Assert.That(mappings[0], Is.EquivalentTo(profile.FixedMapping!));
            Assert.That(RoleMapper.Mappings(picture,profile,(_,_)=>false), Is.Empty, "fixed art cannot bypass readable pairs");
            foreach (string defect in new[]{"wrong-group","missing-role","extra-role","disallowed"})
            {
                var palette=new Dictionary<string,VariantId>(profile.FixedMapping!);
                if(defect=="wrong-group") palette["sky"]=VariantId.Leaf;
                if(defect=="missing-role") palette.Remove("sky");
                if(defect=="extra-role") palette["typo"]=VariantId.Dew;
                GenerationProfile bad=profile with { FixedMapping=palette };
                if(defect=="disallowed") bad=bad with { AllowedVariants=bad.AllowedVariants.Where(v=>v!=VariantId.Dew).ToArray() };
                Assert.That(RoleMapper.Mappings(picture,bad,(_,_)=>true), Is.Empty,defect);
            }
        }

        [TestCase("empty-palette")] [TestCase("unknown-variant")] [TestCase("invalid-mirror")]
        public void InvalidArtRecipeFailsAtLoad(string defect)
        {
            JObject recipe=Recipe("icon_leaf_steam_cup");
            if(defect=="empty-palette") recipe["mappingConstraints"]!["fixedMapping"]=new JObject();
            if(defect=="unknown-variant") recipe["mappingConstraints"]!["fixedMapping"]!["sky"]="unicorn";
            if(defect=="invalid-mirror") recipe["pictureMirror"]="vertical";
            Assert.Throws<ContentFormatException>(()=>ProfileLoader.Read(recipe.ToString()));
        }

        [TestCase(Mirror.None, 1UL)] [TestCase(Mirror.Horizontal, 2UL)]
        public void GeneratedPuzzleKeepsTheAuthoredPaletteAndRequestedMirror(Mirror mirror, ulong seed)
        {
            const string id = "icon_leaf_steam_cup";
            GenerationProfile profile = ProfileLoader.Read(Recipe(id).ToString()) with
            {
                PictureMirror = mirror, AllowedMechanics = Array.Empty<string>(), MaxLayerDepth = 0,
            };
            var thresholds = ProfileLoader.ReadThresholds(File.ReadAllText(Path.Combine(Repo,"content/profiles/difficulty-thresholds.json")), profile.BandId);
            var pairs = Bloomlings.Pipeline.Catalog.ContentStore.LoadPairs(Path.Combine(Repo,"content/readability/approved-pairs.json"))!;
            var generator = new LevelGenerator(profile,new PicturePicker(new[]{Picture(id)}),thresholds,pairs.IsApproved,new DifficultySchedule(seed))
            { ForcedClass = DifficultyClass.Normal, ForcedMechanics = Array.Empty<string>() };
            var result = generator.Generate(41,41,seed,new SortedDictionary<int,LevelDefinition>());
            Assert.That(result.Accepted,Has.Count.EqualTo(1),string.Join("; ",result.Rejections.Select(r=>r.Reason)));
            Assert.That(result.Accepted[0].Definition.Mapping,Is.EquivalentTo(profile.FixedMapping!));
            Assert.That(result.Accepted[0].Definition.Picture.Mirror,Is.EqualTo(mirror));
        }

        [TestCase("icon_leaf_steam_cup",41)] [TestCase("icon_leaf_sail_boat",71)]
        [TestCase("story_boot_greenhouse",601)] [TestCase("story_crescent_cloud_mill",651)]
        [TestCase("story_jellyfish_garden_airship",701)] [TestCase("story_lilypad_bakery",751)]
        public void PilotCertificatesMatchArtAndReplayBothWinAndJam(string id,int number)
        {
            BasePicture picture=Picture(id);
            string filename=$"level-{number:00000}.json";
            LevelDefinition level=DefinitionJson.Read(File.ReadAllText(Path.Combine(Pilot,"samples/levels",filename)));
            ValidationRecord record=ValidationRecord.Read(File.ReadAllText(Path.Combine(Pilot,"samples/validation",filename)));
            Assert.That(record.DefinitionHash, Is.EqualTo(ValidationRecord.HashOf(level)));
            Assert.That(level.Picture.Id, Is.EqualTo(id));
            Assert.That(level.Picture.Mirror, Is.EqualTo(Mirror.None));
            var profile=ProfileLoader.Read(Recipe(id).ToString());
            Assert.That(level.Mapping, Is.EquivalentTo(profile.FixedMapping!));
            foreach(bool jam in new[]{false,true})
            {
                var session=LevelSession.Load(level,picture,new SessionOptions(1,20000));
                var trace=jam?record.JamWitness!:record.SolutionTrace;
                Assert.That(trace,Is.Not.Empty);
                foreach(Command command in trace)
                {
                    Assert.That(command,Is.InstanceOf<TapPod>(),"no booster required");
                    Assert.That(session.Apply(command).Accepted,Is.True);
                }
                Assert.That(session.Status,Is.EqualTo(jam?LevelStatus.Jammed:LevelStatus.Won));
            }
        }
    }
}
