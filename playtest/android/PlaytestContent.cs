using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Loads the embedded playtest levels and the picture library into a content set. As with
    /// <c>publish --allow-draft</c>, pictures still waiting for approval (FR-084) are used as in-memory preview copies,
    /// so this content is for playtests only.
    /// </summary>
    public static class PlaytestContent
    {
        public static ContentSet Load()
        {
            var pictures = new List<BasePicture>();
            var levels = new List<LevelDefinition>();
            Assembly assembly = typeof(PlaytestContent).Assembly;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                bool level = name.StartsWith("levels/");
                if (!level && !name.StartsWith("pictures/"))
                {
                    continue;
                }

                using Stream stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                if (level)
                {
                    levels.Add(DefinitionJson.Read(json));
                }
                else
                {
                    BasePicture picture = BasePictureJson.Read(json);
                    pictures.Add(picture.Review.Status == ReviewStatus.Approved ? picture : picture with { Review = picture.Review with { Status = ReviewStatus.Approved } });
                }
            }

            return new ContentSet(LooseContentFolder.DevContentVersion, LooseContentFolder.DevShuffleNodeBudget, levels, pictures);
        }
    }
}
