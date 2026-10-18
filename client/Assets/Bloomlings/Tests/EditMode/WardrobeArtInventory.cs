using System;
using System.IO;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Save;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    // Art coverage deliberately owns all current gear. Release progression is tested
    // separately with fresh inventories in WardrobeReleaseTests.
    internal static class WardrobeArtInventory
    {
        public static PlayerSave Create(string id, DateTime now)
        {
            var save=PlayerSave.CreateNew(id,now);
            save.Progression.HighestCompletedLevel=99;
            save.Unlocks.Flags[WardrobeService.UnlockId]=true;
            var catalog=CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
            foreach(var item in catalog.Items)
                if(item.ForFamily.HasValue&&!item.Retired)save.Cosmetics.Owned.Add(item.Id);
            return save;
        }
    }
}
