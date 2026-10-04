using System;
using System.IO;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The local save (T054, R15).</summary>
    public class SaveServiceTests
    {
        private string _folder = string.Empty;
        private ManualClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "bloomlings-save-" + Guid.NewGuid().ToString("N"));
            _clock = new ManualClock(new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }

        [Test]
        public void FirstLoad_CreatesANewProfile()
        {
            var service = new SaveService(_folder, _clock);

            PlayerSave save = service.Load();

            Assert.That(service.IsFirstLaunch, Is.True);
            Assert.That(save.Progression.CurrentLevel, Is.EqualTo(1));
            Assert.That(save.LocalPlayerId, Is.Not.Empty);
        }

        [Test]
        public void SavedData_RoundTrips()
        {
            var service = new SaveService(_folder, _clock);
            service.Load();
            service.Current.Progression.HighestCompletedLevel = 7;
            service.Current.Wallet.AddPetals(35);
            service.Current.Boosters.Add(BoosterKind.Shuffle, 2);
            service.Current.Unlocks.Flags["booster.shuffle"] = true;
            service.Current.Unlocks.MarkDemoSeen("booster.shuffle");
            service.Current.Settings.Speed2x = true;
            service.Current.Settings.HomePetals = false;
            service.Current.Stats.Increment("boostersUsed", "shuffle");
            service.Save();

            var reloaded = new SaveService(_folder, _clock);
            PlayerSave save = reloaded.Load();

            Assert.That(reloaded.Source, Is.EqualTo(SaveSource.Main));
            Assert.That(save.Progression.HighestCompletedLevel, Is.EqualTo(7));
            Assert.That(save.Wallet.Petals, Is.EqualTo(35));
            Assert.That(save.Boosters.Get(BoosterKind.Shuffle), Is.EqualTo(2));
            Assert.That(save.Unlocks.IsSet("booster.shuffle"), Is.True);
            Assert.That(save.Unlocks.HasSeenDemo("booster.shuffle"), Is.True);
            Assert.That(save.Settings.Speed2x, Is.True);
            Assert.That(save.Settings.HomePetals, Is.False);
            Assert.That(save.Stats.Groups["boostersUsed"]["shuffle"], Is.EqualTo(1));
            Assert.That(SaveService.Envelope(save), Is.EqualTo(File.ReadAllText(reloaded.MainPath)), "Stable canonical file.");
        }

        /// <summary>Home's falling petals (the owner's Settings switch of 2026-10-04): a save written before it shows them.</summary>
        [Test]
        public void ASaveWithoutTheHomePetalsSwitch_ShowsThePetals()
        {
            PlayerSave save = PlayerSave.CreateNew("local", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            save.Settings.HomePetals = false;
            JObject document = SaveSerializer.ToJObject(save);
            Assert.That(((JObject)document["settings"]!).Remove("homePetals"), Is.True, "the switch is written");
            Assert.That(SaveSerializer.Read(document).Settings.HomePetals, Is.True, "on when the save does not say");
            Assert.That(SaveSerializer.Read(SaveSerializer.Write(save)).Settings.HomePetals, Is.False, "off once switched off");
        }

        [Test]
        public void AtomicWrite_SurvivesACrashBetweenTempWriteAndRename()
        {
            var service = new SaveService(_folder, _clock);
            service.Load();
            service.Current.Wallet.AddPetals(10);
            service.Save();

            service.Current.Wallet.AddPetals(90);
            service.AfterTempWrite = () => throw new IOException("simulated crash");
            Assert.Throws<IOException>(() => service.Save());
            Assert.That(File.Exists(service.TempPath), Is.True, "The crash left a partial write behind.");

            var restarted = new SaveService(_folder, _clock);
            PlayerSave save = restarted.Load();

            Assert.That(save.Wallet.Petals, Is.EqualTo(10), "The last complete save survives.");
            Assert.That(restarted.Source, Is.EqualTo(SaveSource.Main));
            Assert.That(File.Exists(restarted.TempPath), Is.False, "The partial write is discarded.");
        }

        [Test]
        public void CorruptMainFile_FallsBackToTheBackup()
        {
            var service = new SaveService(_folder, _clock);
            service.Load();
            service.Current.Wallet.AddPetals(10);
            service.Save();
            service.Current.Wallet.AddPetals(5);
            service.Save();
            File.WriteAllText(service.MainPath, "{ \"checksum\": \"x\", \"sa");

            var restarted = new SaveService(_folder, _clock);
            PlayerSave save = restarted.Load();

            Assert.That(restarted.Source, Is.EqualTo(SaveSource.Backup));
            Assert.That(save.Wallet.Petals, Is.EqualTo(10));
        }

        [Test]
        public void EditedFileWithAStaleChecksum_FallsBackToTheBackup()
        {
            var service = new SaveService(_folder, _clock);
            service.Load();
            service.Current.Wallet.AddPetals(10);
            service.Save();
            service.Current.Wallet.AddPetals(5);
            service.Save();
            File.WriteAllText(service.MainPath, File.ReadAllText(service.MainPath).Replace("\"petals\": 15", "\"petals\": 99999"));

            var restarted = new SaveService(_folder, _clock);

            Assert.That(restarted.Load().Wallet.Petals, Is.EqualTo(10));
            Assert.That(restarted.Source, Is.EqualTo(SaveSource.Backup));
        }

        [Test]
        public void SchemaMigrationHook_RunsForOlderSaves()
        {
            JObject current = SaveSerializer.ToJObject(PlayerSave.CreateNew("p1", _clock.UtcNow));
            current["schemaVersion"] = 0;
            ((JObject)current["wallet"]!)["petals"] = 3;
            bool ran = false;
            var migrations = new SaveMigrations();
            migrations.Register(0, doc =>
            {
                ran = true;
                doc["schemaVersion"] = 1;
                ((JObject)doc["wallet"]!)["petals"] = 30;
                return doc;
            });

            PlayerSave save = SaveSerializer.Read(current.ToString(), migrations);

            Assert.That(ran, Is.True);
            Assert.That(save.SchemaVersion, Is.EqualTo(1));
            Assert.That(save.Wallet.Petals, Is.EqualTo(30));
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(current.ToString()), "No migration registered from version 0.");
        }

        [Test]
        public void PetalsAndCharges_AreNeverNegative()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", _clock.UtcNow);
            save.Wallet.AddPetals(20);

            Assert.That(save.Wallet.TrySpendPetals(21), Is.False);
            Assert.That(save.Wallet.Petals, Is.EqualTo(20));
            Assert.That(save.Wallet.TrySpendPetals(20), Is.True);
            Assert.That(save.Wallet.Petals, Is.EqualTo(0));
            Assert.That(save.Boosters.TryUse(BoosterKind.BloomBurst), Is.False);
            Assert.That(save.Boosters.Get(BoosterKind.BloomBurst), Is.EqualTo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => save.Wallet.AddPetals(-5));
            Assert.Throws<ArgumentOutOfRangeException>(() => save.Boosters.Add(BoosterKind.Return, -1));

            JObject document = SaveSerializer.ToJObject(save);
            ((JObject)document["wallet"]!)["petals"] = -1;
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(document.ToString()));
            document = SaveSerializer.ToJObject(save);
            ((JObject)document["boosters"]!)["shuffle"] = -2;
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(document.ToString()));
        }

        [Test]
        public void MilestoneClaimsAndLedgerTransactions_AreUnique()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", _clock.UtcNow);

            Assert.That(save.Milestones.TryClaim(25), Is.True);
            Assert.That(save.Milestones.TryClaim(25), Is.False);
            var entry = new LedgerEntry("tx-1", "petals_small", "2026-09-29T12:00:00Z", 100, null);
            Assert.That(save.Purchases.TryAdd(entry), Is.True);
            Assert.That(save.Purchases.TryAdd(entry with { ProductId = "other" }), Is.False);

            JObject document = SaveSerializer.ToJObject(save);
            ((JArray)document["milestones"]!["claimed"]!).Add(25);
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(document.ToString()));

            document = SaveSerializer.ToJObject(save);
            JArray ledger = (JArray)document["purchases"]!["ledger"]!;
            ledger.Add(ledger[0].DeepClone());
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(document.ToString()));
        }
    }
}
